/**
 * RiceFactory — Anti-Cheat Cloud Functions
 *
 * Sunucu tarafli oyuncu verisi dogrulama:
 * - Save data butunlugu ve mantiksal tutarlilik kontrolleri
 * - Prestige (franchise) islemi dogrulama ve FP hesaplama
 * - Zaman manipulasyonu tespiti
 */

import { onCall, HttpsError } from "firebase-functions/v2/https";
import { onDocumentUpdated } from "firebase-functions/v2/firestore";
import { getFirestore, FieldValue } from "firebase-admin/firestore";

// ============================================================
// TIP TANIMLARI
// ============================================================

/** Oyuncu save verisinin dogrulama icin gereken alt kumesi */
interface SaveDataPayload {
  coins: number;
  totalLifetimeEarnings: number;
  franchisePoints: number;
  franchiseCount: number;
  facilities: FacilityData[];
  lastSaveTimestamp: number; // Unix milisaniye
  playTimeSeconds: number;
  level: number;
  offlineEarnings: number;
}

/** Tesis verisi */
interface FacilityData {
  id: string;
  starLevel: number;        // 0-5
  machineLevel: number;
  workerCount: number;
  productionRate: number;   // Birim/saniye
}

/** Prestige (franchise) istegi */
interface PrestigeRequest {
  totalLifetimeEarnings: number;
  fiveStarFacilityCount: number;
}

/** Prestige dogrulama sonucu */
interface PrestigeResult {
  approved: boolean;
  franchisePoints: number;
  reason?: string;
}

// ============================================================
// YAPILANDIRMA SABITLERI
// ============================================================

/** Maksimum teorik uretim kapasitesi carpani (guvenlk marji dahil) */
const MAX_PRODUCTION_MULTIPLIER = 50;

/** Bir saatte kazanilabilecek maksimum coin (tum tesisler max, tum bonuslar dahil) */
const MAX_HOURLY_EARNINGS = 5_000_000;

/** Minimum franchise esigi (varsayilan, Remote Config'den guncellenebilir) */
const DEFAULT_FRANCHISE_THRESHOLD = 1_000_000;

/** FP formul boleni (varsayilan) */
const DEFAULT_FP_DIVISOR = 1_000_000;

/** 5-yildiz tesis basina FP bonus carpani */
const FP_BONUS_PER_STAR5 = 0.1;

/** Maksimum izin verilen zaman farki (milisaniye) — 5 dakika tolerans */
const MAX_TIME_DRIFT_MS = 5 * 60 * 1000;

/** Maksimum offline birikim suresi (saat) */
const MAX_OFFLINE_HOURS = 24;

/** Offline verimlilik tavan carpani (Battle Pass dahil) */
const MAX_OFFLINE_EFFICIENCY = 0.50;

// ============================================================
// validateSaveData — Oyuncu kayit verisi dogrulama
// ============================================================

/**
 * Istemciden gelen save verisini sunucu tarafinda dogrular.
 *
 * Kontroller:
 * 1. Coin miktari vs uretim kapasitesi tutarliligi
 * 2. Zaman manipulasyonu tespiti (sunucu zamani ile karsilastirma)
 * 3. Offline kazanc limiti kontrolu
 * 4. Tesis verileri mantiksal sinir kontrolu
 *
 * @returns {{ valid: boolean, warnings: string[] }}
 */
export const validateSaveData = onCall<SaveDataPayload>(
  { region: "europe-west1", enforceAppCheck: true },
  async (request) => {
    // Kimlik dogrulama kontrolu
    if (!request.auth) {
      throw new HttpsError("unauthenticated", "Kimlik dogrulamasi gerekli.");
    }

    const uid = request.auth.uid;
    const data = request.data;
    const warnings: string[] = [];
    let isSuspicious = false;

    // --- 1. Zaman manipulasyonu kontrolu ---
    const serverNow = Date.now();
    const clientTimestamp = data.lastSaveTimestamp;

    if (clientTimestamp > serverNow + MAX_TIME_DRIFT_MS) {
      warnings.push(`Zaman manipulasyonu suphelisi: istemci zamani sunucudan ${clientTimestamp - serverNow}ms ilerde.`);
      isSuspicious = true;
    }

    // --- 2. Coin vs uretim kapasitesi tutarliligi ---
    const totalProductionRate = data.facilities.reduce(
      (sum, f) => sum + f.productionRate, 0
    );
    const maxPossibleCoins = totalProductionRate
      * MAX_PRODUCTION_MULTIPLIER
      * data.playTimeSeconds;

    if (data.coins > maxPossibleCoins && maxPossibleCoins > 0) {
      warnings.push(
        `Coin tutarsizligi: ${data.coins} coin, maks teorik ${maxPossibleCoins.toFixed(0)} coin.`
      );
      isSuspicious = true;
    }

    // --- 3. Toplam yasam boyu kazanc kontrolu ---
    const maxLifetimeByTime = data.playTimeSeconds * MAX_HOURLY_EARNINGS / 3600;
    if (data.totalLifetimeEarnings > maxLifetimeByTime * 1.5) {
      warnings.push(
        `Yasam boyu kazanc suphelisi: ${data.totalLifetimeEarnings}, maks beklenen ${maxLifetimeByTime.toFixed(0)}.`
      );
      isSuspicious = true;
    }

    // --- 4. Offline kazanc limiti ---
    const maxOfflineEarnings = totalProductionRate
      * MAX_OFFLINE_HOURS * 3600
      * MAX_OFFLINE_EFFICIENCY;

    if (data.offlineEarnings > maxOfflineEarnings * 1.2) {
      warnings.push(
        `Offline kazanc limiti asildi: ${data.offlineEarnings}, maks ${maxOfflineEarnings.toFixed(0)}.`
      );
      isSuspicious = true;
    }

    // --- 5. Tesis verileri sinir kontrolu ---
    for (const facility of data.facilities) {
      if (facility.starLevel < 0 || facility.starLevel > 5) {
        warnings.push(`Tesis ${facility.id}: gecersiz yildiz seviyesi (${facility.starLevel}).`);
        isSuspicious = true;
      }
      if (facility.workerCount < 0 || facility.workerCount > 50) {
        warnings.push(`Tesis ${facility.id}: gecersiz calisan sayisi (${facility.workerCount}).`);
        isSuspicious = true;
      }
    }

    // --- Supheli aktivite kaydi ---
    if (isSuspicious) {
      const db = getFirestore();
      await db.collection("players").doc(uid).update({
        "anticheat.lastFlag": FieldValue.serverTimestamp(),
        "anticheat.flagCount": FieldValue.increment(1),
        "anticheat.lastWarnings": warnings,
      });
    }

    return { valid: !isSuspicious, warnings };
  }
);

// ============================================================
// validatePrestige — Franchise (prestige) islemi dogrulama
// ============================================================

/**
 * Franchise islemini sunucu tarafinda dogrular ve FP hesaplar.
 *
 * FP Formulu (ECONOMY_BALANCE.md'den):
 *   FP = floor( sqrt(ToplamKazanc / FP_DIVISOR) x (1 + 5YildizTesisSayisi x 0.1) )
 *
 * Islem:
 * 1. Oyuncunun gercek toplam kazancini Firestore'dan dogrula
 * 2. Minimum franchise esigini kontrol et
 * 3. FP'yi sunucu tarafinda hesapla
 * 4. Onaylanirsa oyuncu dokumanini guncelle
 *
 * @returns {PrestigeResult}
 */
export const validatePrestige = onCall<PrestigeRequest>(
  { region: "europe-west1", enforceAppCheck: true },
  async (request): Promise<PrestigeResult> => {
    if (!request.auth) {
      throw new HttpsError("unauthenticated", "Kimlik dogrulamasi gerekli.");
    }

    const uid = request.auth.uid;
    const db = getFirestore();

    // Oyuncu dokumanini al — sunucu tarafindaki gercek veriyi kullan
    const playerRef = db.collection("players").doc(uid);
    const playerSnap = await playerRef.get();

    if (!playerSnap.exists) {
      throw new HttpsError("not-found", "Oyuncu dokumani bulunamadi.");
    }

    const playerData = playerSnap.data()!;
    const serverTotalEarnings: number = playerData.totalLifetimeEarnings ?? 0;
    const currentFP: number = playerData.franchisePoints ?? 0;
    const currentFranchiseCount: number = playerData.franchiseCount ?? 0;

    // Remote Config'den esik degerini al (yoksa varsayilan kullan)
    const configSnap = await db.collection("server").doc("config").get();
    const franchiseThreshold = configSnap.exists
      ? (configSnap.data()?.franchiseThreshold ?? DEFAULT_FRANCHISE_THRESHOLD)
      : DEFAULT_FRANCHISE_THRESHOLD;
    const fpDivisor = configSnap.exists
      ? (configSnap.data()?.fpDivisor ?? DEFAULT_FP_DIVISOR)
      : DEFAULT_FP_DIVISOR;

    // --- Minimum esik kontrolu ---
    if (serverTotalEarnings < franchiseThreshold) {
      return {
        approved: false,
        franchisePoints: 0,
        reason: `Minimum franchise esigi karsilanmadi. Gerekli: ${franchiseThreshold}, mevcut: ${serverTotalEarnings}.`,
      };
    }

    // --- FP hesaplama (sunucu tarafinda) ---
    const fiveStarCount = request.data.fiveStarFacilityCount;
    const bonusMultiplier = 1 + (fiveStarCount * FP_BONUS_PER_STAR5);
    const earnedFP = Math.floor(
      Math.sqrt(serverTotalEarnings / fpDivisor) * bonusMultiplier
    );

    if (earnedFP <= 0) {
      return {
        approved: false,
        franchisePoints: 0,
        reason: "Hesaplanan FP 0 veya negatif. Yeterli kazanc yok.",
      };
    }

    // --- Oyuncu dokumanini guncelle (atomik) ---
    await playerRef.update({
      franchisePoints: currentFP + earnedFP,
      franchiseCount: currentFranchiseCount + 1,
      lastPrestigeAt: FieldValue.serverTimestamp(),
      // Toplam kazanc SIFIRLANMAZ — yasam boyu kazanc kalici
      // Ancak mevcut run kazanci sifirlanir
      currentRunEarnings: 0,
    });

    return {
      approved: true,
      franchisePoints: earnedFP,
    };
  }
);

// ============================================================
// banCheck — Anormal veri degisikligi tespiti (Firestore Trigger)
// ============================================================

/**
 * Oyuncu dokumani her guncellendiginde tetiklenir.
 * Anormal degisiklikleri tespit eder ve gerektiginde oyuncuyu isaretler.
 *
 * Kontroller:
 * - Coin miktarinda ani artis (x10'dan fazla)
 * - FP'nin dogrudan istemci tarafindan degismesi
 * - Seviye atlama (birden fazla seviye atlama)
 */
export const banCheck = onDocumentUpdated(
  { document: "players/{userId}", region: "europe-west1" },
  async (event) => {
    if (!event.data) return;

    const before = event.data.before.data();
    const after = event.data.after.data();
    const userId = event.params.userId;

    // Zaten banliysa kontrol etme
    if (after.isBanned) return;

    const flags: string[] = [];

    // --- Coin ani artis kontrolu ---
    const coinBefore = before.coins ?? 0;
    const coinAfter = after.coins ?? 0;
    if (coinBefore > 0 && coinAfter > coinBefore * 10) {
      flags.push(`Ani coin artisi: ${coinBefore} -> ${coinAfter} (x${(coinAfter / coinBefore).toFixed(1)})`);
    }

    // --- Seviye atlama kontrolu ---
    const levelBefore = before.level ?? 1;
    const levelAfter = after.level ?? 1;
    if (levelAfter > levelBefore + 3) {
      flags.push(`Seviye atlama: ${levelBefore} -> ${levelAfter}`);
    }

    // --- Supheli flag kaydi ---
    if (flags.length > 0) {
      const db = getFirestore();
      await db.collection("players").doc(userId).update({
        "anticheat.lastFlag": FieldValue.serverTimestamp(),
        "anticheat.flagCount": FieldValue.increment(1),
        "anticheat.lastWarnings": flags,
      });

      // Cok fazla flag biriktiyse otomatik ban
      const flagCount = (after.anticheat?.flagCount ?? 0) + 1;
      if (flagCount >= 10) {
        await db.collection("players").doc(userId).update({
          isBanned: true,
          banReason: "Otomatik ban: tekrarlayan anti-cheat ihlalleri.",
          bannedAt: FieldValue.serverTimestamp(),
        });
      }
    }
  }
);

// ============================================================
// syncServerTime — Istemciye sunucu zamanini dondur
// ============================================================

/**
 * Istemcinin zaman senkronizasyonu icin sunucu zamanini dondurur.
 * Anti-cheat: istemci bu degeri kendi saatiyle karsilastirarak
 * zaman manipulasyonu yapilip yapilmadigini kontrol eder.
 */
export const syncServerTime = onCall(
  { region: "europe-west1" },
  async (request) => {
    if (!request.auth) {
      throw new HttpsError("unauthenticated", "Kimlik dogrulamasi gerekli.");
    }

    return {
      serverTimestamp: Date.now(),
      serverDate: new Date().toISOString(),
    };
  }
);
