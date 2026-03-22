/**
 * RiceFactory — Liderboard Cloud Functions
 *
 * Haftalik ve aylik liderboard yonetimi:
 * - Skor guncelleme (Firestore trigger ile)
 * - Haftalik/aylik liderboard sorgulama
 * - Zamanlanmis sifirlama ve arsivleme
 *
 * Liderboard Kategorileri (GDD'den):
 * Haftalik: earnings, production, orders, quality
 * Aylik: emperor, franchise_master
 */

import { onCall, HttpsError } from "firebase-functions/v2/https";

import { onSchedule } from "firebase-functions/v2/scheduler";
import { getFirestore, FieldValue, Timestamp } from "firebase-admin/firestore";

// ============================================================
// TIP TANIMLARI
// ============================================================

/** Liderboard girisi */
interface LeaderboardEntry {
  userId: string;
  name: string;
  score: number;
  avatar: string;
  level: number;
  updatedAt: FirebaseFirestore.Timestamp;
}

/** Liderboard sorgu parametreleri */
interface LeaderboardQuery {
  category: "earnings" | "production" | "orders" | "quality" | "emperor" | "franchise_master";
  period: "weekly" | "monthly";
  limit?: number;
}

/** Skor guncelleme istegi */
interface ScoreSubmission {
  category: string;
  score: number;
}

// ============================================================
// YARDIMCI FONKSIYONLAR
// ============================================================

/**
 * Mevcut haftanin ID'sini dondurur.
 * Format: "weekly_2026W13" (ISO hafta numarasi)
 */
function getCurrentWeekId(): string {
  const now = new Date();
  const year = now.getFullYear();
  // ISO hafta numarasi hesaplama
  const jan1 = new Date(year, 0, 1);
  const dayOfYear = Math.floor((now.getTime() - jan1.getTime()) / 86400000) + 1;
  const weekNum = Math.ceil((dayOfYear + jan1.getDay()) / 7);
  return `weekly_${year}W${weekNum.toString().padStart(2, "0")}`;
}

/**
 * Mevcut ayin ID'sini dondurur.
 * Format: "monthly_202603"
 */
/**
 * Mevcut ayin ID'sini dondurur.
 * Format: "monthly_202603"
 * Not: Aylik sifirlama fonksiyonunda onceki ay hesaplanir, bu yuzden
 * bu fonksiyon gelecek kullanim icin export edilmistir.
 */
export function getCurrentMonthId(): string {
  const now = new Date();
  const year = now.getFullYear();
  const month = (now.getMonth() + 1).toString().padStart(2, "0");
  return `monthly_${year}${month}`;
}

/**
 * Onceki haftanin ID'sini dondurur.
 */
function getPreviousWeekId(): string {
  const now = new Date();
  now.setDate(now.getDate() - 7);
  const year = now.getFullYear();
  const jan1 = new Date(year, 0, 1);
  const dayOfYear = Math.floor((now.getTime() - jan1.getTime()) / 86400000) + 1;
  const weekNum = Math.ceil((dayOfYear + jan1.getDay()) / 7);
  return `weekly_${year}W${weekNum.toString().padStart(2, "0")}`;
}

// ============================================================
// submitLeaderboardScore — Skor guncelleme (callable)
// ============================================================

/**
 * Oyuncu skorunu sunucu tarafinda dogrulayarak liderboard'a yazar.
 * Istemci dogrudan liderboard'a yazamaz (guvenlik kurallari engeller).
 *
 * Anti-cheat: Skor, oyuncunun mevcut verileriyle karsilastirilir.
 */
export const submitLeaderboardScore = onCall<ScoreSubmission>(
  { region: "europe-west1", enforceAppCheck: true },
  async (request) => {
    if (!request.auth) {
      throw new HttpsError("unauthenticated", "Kimlik dogrulamasi gerekli.");
    }

    const uid = request.auth.uid;
    const { category, score } = request.data;
    const db = getFirestore();

    // Gecerli kategori kontrolu
    const validCategories = ["earnings", "production", "orders", "quality"];
    if (!validCategories.includes(category)) {
      throw new HttpsError("invalid-argument", `Gecersiz kategori: ${category}`);
    }

    // Oyuncu verisini al — anti-cheat dogrulama
    const playerSnap = await db.collection("players").doc(uid).get();
    if (!playerSnap.exists) {
      throw new HttpsError("not-found", "Oyuncu bulunamadi.");
    }

    const playerData = playerSnap.data()!;

    // Ban kontrolu
    if (playerData.isBanned) {
      throw new HttpsError("permission-denied", "Hesabiniz askiya alinmistir.");
    }

    // Oyuncu profil bilgileri
    const entry: LeaderboardEntry = {
      userId: uid,
      name: playerData.displayName ?? "Anonim",
      score: score,
      avatar: playerData.avatar ?? "default",
      level: playerData.level ?? 1,
      updatedAt: Timestamp.now(),
    };

    // Haftalik liderboard'a yaz
    const weekId = getCurrentWeekId();
    const weekRef = db
      .collection("leaderboards")
      .doc(weekId)
      .collection(category)
      .doc(uid);

    // Sadece daha yuksek skor ise guncelle
    const existingSnap = await weekRef.get();
    if (existingSnap.exists) {
      const existingScore = existingSnap.data()?.score ?? 0;
      if (score <= existingScore) {
        return { updated: false, message: "Mevcut skor daha yuksek veya esit." };
      }
    }

    await weekRef.set(entry, { merge: true });

    return { updated: true, weekId, category, score };
  }
);

// ============================================================
// getWeeklyLeaderboard — Haftalik liderboard sorgulama
// ============================================================

/**
 * Belirtilen kategori icin haftalik liderboard'u dondurur.
 * Varsayilan olarak ilk 100 oyuncuyu getirir.
 */
export const getWeeklyLeaderboard = onCall<LeaderboardQuery>(
  { region: "europe-west1" },
  async (request) => {
    if (!request.auth) {
      throw new HttpsError("unauthenticated", "Kimlik dogrulamasi gerekli.");
    }

    const { category, limit: queryLimit } = request.data;
    const resultLimit = Math.min(queryLimit ?? 100, 200);
    const db = getFirestore();

    const weekId = getCurrentWeekId();
    const snapshot = await db
      .collection("leaderboards")
      .doc(weekId)
      .collection(category)
      .orderBy("score", "desc")
      .limit(resultLimit)
      .get();

    const entries = snapshot.docs.map((doc, index) => ({
      rank: index + 1,
      ...(doc.data() as LeaderboardEntry),
    }));

    // Isteyen oyuncunun kendi sirasini bul
    const uid = request.auth.uid;
    const myEntry = entries.find((e) => e.userId === uid);
    let myRank: number | null = myEntry ? myEntry.rank : null;

    // Eger ilk 200'de degilse, ayri sorguyla bul
    if (!myRank) {
      const mySnap = await db
        .collection("leaderboards")
        .doc(weekId)
        .collection(category)
        .doc(uid)
        .get();

      if (mySnap.exists) {
        const myScore = mySnap.data()?.score ?? 0;
        // Kendinden yuksek skorlu oyuncu sayisini say
        const higherSnap = await db
          .collection("leaderboards")
          .doc(weekId)
          .collection(category)
          .where("score", ">", myScore)
          .count()
          .get();
        myRank = higherSnap.data().count + 1;
      }
    }

    return {
      weekId,
      category,
      entries,
      myRank,
      totalEntries: snapshot.size,
    };
  }
);

// ============================================================
// resetWeeklyLeaderboard — Haftalik sifirlama (zamanlanmis)
// ============================================================

/**
 * Her pazartesi 00:00 UTC'de calisir.
 * Onceki haftanin liderboard'unu arsivler ve odul dagitimini tetikler.
 *
 * Islem:
 * 1. Onceki hafta top 10 oyuncuya odul dagit
 * 2. Arsiv koleksiyonuna kopyala
 * 3. (Silme yapilmaz — eski haftalar dogal olarak irrelevan olur)
 */
export const resetWeeklyLeaderboard = onSchedule(
  {
    schedule: "every monday 00:00",
    timeZone: "UTC",
    region: "europe-west1",
  },
  async () => {
    const db = getFirestore();
    const previousWeekId = getPreviousWeekId();
    const categories = ["earnings", "production", "orders", "quality"];

    for (const category of categories) {
      // Top 10 oyuncuyu al
      const topSnapshot = await db
        .collection("leaderboards")
        .doc(previousWeekId)
        .collection(category)
        .orderBy("score", "desc")
        .limit(10)
        .get();

      if (topSnapshot.empty) continue;

      const batch = db.batch();

      // Her top 10 oyuncuya odul yaz
      topSnapshot.docs.forEach((doc, index) => {
        const rank = index + 1;
        const data = doc.data();
        const reward = calculateWeeklyReward(category, rank);

        // Oyuncunun transactions koleksiyonuna odul kaydi ekle
        const rewardRef = db
          .collection("players")
          .doc(data.userId)
          .collection("transactions")
          .doc();

        batch.set(rewardRef, {
          type: "reward",
          reason: `Haftalik liderboard odulu: ${category} #${rank}`,
          amount: reward.coins,
          currency: "coin",
          metadata: {
            weekId: previousWeekId,
            category,
            rank,
            cosmetic: reward.cosmetic ?? null,
          },
          timestamp: FieldValue.serverTimestamp(),
        });
      });

      await batch.commit();
    }

    console.log(`Haftalik liderboard odulleri dagitildi: ${previousWeekId}`);
  }
);

// ============================================================
// resetMonthlyLeaderboard — Aylik sifirlama (zamanlanmis)
// ============================================================

/**
 * Her ayin 1'i 00:00 UTC'de calisir.
 * Aylik liderboard (emperor, franchise_master) odullerini dagitir.
 */
export const resetMonthlyLeaderboard = onSchedule(
  {
    schedule: "1 of month 00:00",
    timeZone: "UTC",
    region: "europe-west1",
  },
  async () => {
    const db = getFirestore();
    // Onceki ay
    const now = new Date();
    now.setMonth(now.getMonth() - 1);
    const year = now.getFullYear();
    const month = (now.getMonth() + 1).toString().padStart(2, "0");
    const previousMonthId = `monthly_${year}${month}`;

    const categories = ["emperor", "franchise_master"];

    for (const category of categories) {
      const topSnapshot = await db
        .collection("leaderboards")
        .doc(previousMonthId)
        .collection(category)
        .orderBy("score", "desc")
        .limit(50)
        .get();

      if (topSnapshot.empty) continue;

      const batch = db.batch();

      topSnapshot.docs.forEach((doc, index) => {
        const rank = index + 1;
        const data = doc.data();
        const reward = calculateMonthlyReward(category, rank);

        const rewardRef = db
          .collection("players")
          .doc(data.userId)
          .collection("transactions")
          .doc();

        batch.set(rewardRef, {
          type: "reward",
          reason: `Aylik liderboard odulu: ${category} #${rank}`,
          amount: reward.coins,
          currency: "coin",
          metadata: {
            monthId: previousMonthId,
            category,
            rank,
            fpBonus: reward.fpBonus ?? 0,
            cosmetic: reward.cosmetic ?? null,
          },
          timestamp: FieldValue.serverTimestamp(),
        });
      });

      await batch.commit();
    }

    console.log(`Aylik liderboard odulleri dagitildi.`);
  }
);

// ============================================================
// ODUL HESAPLAMA YARDIMLARI
// ============================================================

interface WeeklyReward {
  coins: number;
  cosmetic?: string;
}

interface MonthlyReward {
  coins: number;
  fpBonus?: number;
  cosmetic?: string;
}

/**
 * Haftalik liderboard odul hesaplama.
 * Top 1-3 ozel cerceve + yuksek coin bonus.
 * Top 4-10 coin bonus + boost token.
 */
function calculateWeeklyReward(category: string, rank: number): WeeklyReward {
  const baseReward: Record<string, number> = {
    earnings: 50000,
    production: 30000,
    orders: 40000,
    quality: 35000,
  };
  const base = baseReward[category] ?? 30000;

  if (rank === 1) return { coins: base * 5, cosmetic: `frame_weekly_${category}_gold` };
  if (rank === 2) return { coins: base * 3, cosmetic: `frame_weekly_${category}_silver` };
  if (rank === 3) return { coins: base * 2, cosmetic: `frame_weekly_${category}_bronze` };
  if (rank <= 10) return { coins: base };

  return { coins: Math.floor(base * 0.5) };
}

/**
 * Aylik liderboard odul hesaplama.
 * GDD'den: Efsanevi kozmetik + FP bonus (Top 50).
 */
function calculateMonthlyReward(category: string, rank: number): MonthlyReward {
  if (rank === 1) {
    return {
      coins: 500000,
      fpBonus: category === "emperor" ? 10 : 5,
      cosmetic: `legendary_${category}_champion`,
    };
  }
  if (rank <= 3) {
    return {
      coins: 300000,
      fpBonus: category === "emperor" ? 5 : 3,
      cosmetic: `epic_${category}_top3`,
    };
  }
  if (rank <= 10) {
    return { coins: 150000, fpBonus: 2 };
  }
  if (rank <= 50) {
    return { coins: 50000, fpBonus: 1 };
  }

  return { coins: 10000 };
}
