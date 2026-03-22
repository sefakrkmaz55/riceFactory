/**
 * RiceFactory — Etkinlik Yonetimi Cloud Functions
 *
 * Sezonluk etkinlik sistemi:
 * - Aktif etkinlikleri sorgulama
 * - Etkinlik odulu hak edis kontrolu
 * - Etkinlik odul dagitimi (zamanlanmis)
 *
 * Etkinlik turleri (GDD'den):
 * - Bahar Festivali (Nisan): Kiraz cicegi toplama
 * - Yaz Barbeku (Temmuz): Barbeku mini-game
 * - Hasat Bayrami (Ekim): Mega hasat
 * - Kis Soleni (Aralik): Hediylesme + advent calendar
 */

import { onCall, HttpsError } from "firebase-functions/v2/https";
import { onSchedule } from "firebase-functions/v2/scheduler";
import { getFirestore, FieldValue, Timestamp } from "firebase-admin/firestore";

// ============================================================
// TIP TANIMLARI
// ============================================================

/** Etkinlik dokumani */
interface EventDocument {
  id: string;
  name: string;
  description: string;
  theme: string;
  startDate: FirebaseFirestore.Timestamp;
  endDate: FirebaseFirestore.Timestamp;
  config: EventConfig;
  rewards: EventRewardTier[];
  isActive: boolean;
}

/** Etkinlik yapilandirmasi */
interface EventConfig {
  /** Etkinlik suresince uretim carpani */
  productionMultiplier: number;
  /** Ozel siparis carpani */
  specialOrderMultiplier: number;
  /** Etkinlige ozel urun ID'leri */
  specialProductIds: string[];
  /** Etkinlik puan mekanigi */
  pointMechanic: string;
  /** Liderboard kategorisi */
  leaderboardCategory: string;
}

/** Etkinlik odul kademesi */
interface EventRewardTier {
  /** Gerekli minimum puan */
  requiredPoints: number;
  /** Odul aciklamasi */
  description: string;
  /** Coin odulu */
  coins: number;
  /** Kozmetik odul (varsa) */
  cosmetic?: string;
  /** FP bonus (varsa) */
  fpBonus?: number;
}

/** Oyuncu etkinlik ilerleme verisi */
interface PlayerEventProgress {
  eventId: string;
  points: number;
  claimedTiers: number[]; // Daha once talep edilen odul kademeleri (index)
}

/** Etkinlik odul kontrolu istegi */
interface CheckRewardRequest {
  eventId: string;
}

// ============================================================
// getActiveEvents — Aktif etkinlikleri dondur
// ============================================================

/**
 * Su an aktif olan tum sezonluk etkinlikleri dondurur.
 *
 * Bir etkinlik aktiftir eger:
 * - startDate <= simdi <= endDate
 * - isActive alani true
 *
 * Ayrica oyuncunun o etkinlikteki ilerlemesini de dondurur.
 */
export const getActiveEvents = onCall(
  { region: "europe-west1" },
  async (request) => {
    if (!request.auth) {
      throw new HttpsError("unauthenticated", "Kimlik dogrulamasi gerekli.");
    }

    const uid = request.auth.uid;
    const db = getFirestore();
    const now = Timestamp.now();

    // Aktif etkinlikleri sorgula
    const eventsSnapshot = await db
      .collection("events")
      .where("startDate", "<=", now)
      .where("isActive", "==", true)
      .get();

    // Bitis tarihi gecmemis olanlari filtrele
    const activeEvents: Array<EventDocument & { playerProgress?: PlayerEventProgress }> = [];

    for (const doc of eventsSnapshot.docs) {
      const eventData = doc.data() as Omit<EventDocument, "id">;
      const endDate = eventData.endDate as Timestamp;

      if (endDate.toMillis() < now.toMillis()) {
        continue; // Suresi dolmus, atla
      }

      // Oyuncunun bu etkinlikteki ilerlemesini al
      const progressSnap = await db
        .collection("players")
        .doc(uid)
        .collection("event_progress")
        .doc(doc.id)
        .get();

      const playerProgress = progressSnap.exists
        ? (progressSnap.data() as PlayerEventProgress)
        : undefined;

      activeEvents.push({
        id: doc.id,
        ...eventData,
        playerProgress,
      });
    }

    return {
      events: activeEvents,
      serverTime: now.toMillis(),
    };
  }
);

// ============================================================
// checkEventRewards — Etkinlik odulu hak edis kontrolu
// ============================================================

/**
 * Oyuncunun belirtilen etkinlikte hak ettigi odulleri kontrol eder
 * ve henuz talep edilmemis odulleri verir.
 *
 * Islem:
 * 1. Etkinligin gecerli oldugunu dogrula
 * 2. Oyuncunun puan ilerlemesini kontrol et
 * 3. Hak edilen ama henuz alinmamis odulleri belirle
 * 4. Odulleri ver ve ilerlemeyi guncelle
 */
export const checkEventRewards = onCall<CheckRewardRequest>(
  { region: "europe-west1", enforceAppCheck: true },
  async (request) => {
    if (!request.auth) {
      throw new HttpsError("unauthenticated", "Kimlik dogrulamasi gerekli.");
    }

    const uid = request.auth.uid;
    const { eventId } = request.data;
    const db = getFirestore();

    // --- Etkinlik dogrulama ---
    const eventSnap = await db.collection("events").doc(eventId).get();
    if (!eventSnap.exists) {
      throw new HttpsError("not-found", "Etkinlik bulunamadi.");
    }

    const eventData = eventSnap.data() as EventDocument;
    const now = Timestamp.now();

    // Etkinlik hala aktif mi?
    if (!eventData.isActive || eventData.endDate.toMillis() < now.toMillis()) {
      throw new HttpsError("failed-precondition", "Bu etkinlik artik aktif degil.");
    }

    // --- Oyuncu ilerlemesi ---
    const progressRef = db
      .collection("players")
      .doc(uid)
      .collection("event_progress")
      .doc(eventId);

    const progressSnap = await progressRef.get();

    if (!progressSnap.exists) {
      return { rewardsClaimed: [], message: "Henuz bu etkinlikte ilerleme yok." };
    }

    const progress = progressSnap.data() as PlayerEventProgress;
    const claimedTiers = new Set(progress.claimedTiers ?? []);

    // --- Hak edilen odulleri belirle ---
    const newRewards: Array<EventRewardTier & { tierIndex: number }> = [];

    eventData.rewards.forEach((tier, index) => {
      if (progress.points >= tier.requiredPoints && !claimedTiers.has(index)) {
        newRewards.push({ ...tier, tierIndex: index });
      }
    });

    if (newRewards.length === 0) {
      return {
        rewardsClaimed: [],
        message: "Yeni hak edilen odul yok.",
        currentPoints: progress.points,
      };
    }

    // --- Odulleri dagit ---
    const batch = db.batch();
    const claimedIndices: number[] = [];
    let totalCoins = 0;

    for (const reward of newRewards) {
      claimedIndices.push(reward.tierIndex);
      totalCoins += reward.coins;

      // Transaction kaydi
      const txRef = db
        .collection("players")
        .doc(uid)
        .collection("transactions")
        .doc();

      batch.set(txRef, {
        type: "reward",
        reason: `Etkinlik odulu: ${eventData.name} — ${reward.description}`,
        amount: reward.coins,
        currency: "coin",
        metadata: {
          eventId,
          tierIndex: reward.tierIndex,
          cosmetic: reward.cosmetic ?? null,
          fpBonus: reward.fpBonus ?? 0,
        },
        timestamp: FieldValue.serverTimestamp(),
      });

      // FP bonus varsa oyuncu dokumanini guncelle
      if (reward.fpBonus && reward.fpBonus > 0) {
        const playerRef = db.collection("players").doc(uid);
        batch.update(playerRef, {
          franchisePoints: FieldValue.increment(reward.fpBonus),
        });
      }
    }

    // Ilerleme dokumanini guncelle — talep edilen kademeleri isaretle
    batch.update(progressRef, {
      claimedTiers: FieldValue.arrayUnion(...claimedIndices),
    });

    await batch.commit();

    return {
      rewardsClaimed: newRewards.map((r) => ({
        description: r.description,
        coins: r.coins,
        cosmetic: r.cosmetic ?? null,
        fpBonus: r.fpBonus ?? 0,
      })),
      totalCoinsAwarded: totalCoins,
      currentPoints: progress.points,
    };
  }
);

// ============================================================
// distributeEventRewards — Etkinlik bitis odul dagitimi (zamanlanmis)
// ============================================================

/**
 * Her gun 01:00 UTC'de calisir.
 * Bitmis etkinliklerin final liderboard odullerini dagitir
 * ve etkinligi pasif olarak isaretler.
 */
export const distributeEventRewards = onSchedule(
  {
    schedule: "every day 01:00",
    timeZone: "UTC",
    region: "europe-west1",
  },
  async () => {
    const db = getFirestore();
    const now = Timestamp.now();

    // Suresi dolmus ama hala aktif isaretli etkinlikleri bul
    const expiredEvents = await db
      .collection("events")
      .where("isActive", "==", true)
      .where("endDate", "<", now)
      .get();

    for (const eventDoc of expiredEvents.docs) {
      const eventData = eventDoc.data() as EventDocument;
      const leaderboardCategory = eventData.config?.leaderboardCategory;

      if (leaderboardCategory) {
        // Etkinlik liderboard top 10 odulleri
        const topSnapshot = await db
          .collection("leaderboards")
          .doc(`event_${eventDoc.id}`)
          .collection(leaderboardCategory)
          .orderBy("score", "desc")
          .limit(10)
          .get();

        const batch = db.batch();

        topSnapshot.docs.forEach((doc, index) => {
          const rank = index + 1;
          const data = doc.data();
          const reward = calculateEventRankReward(rank);

          const rewardRef = db
            .collection("players")
            .doc(data.userId)
            .collection("transactions")
            .doc();

          batch.set(rewardRef, {
            type: "reward",
            reason: `Etkinlik liderboard odulu: ${eventData.name} #${rank}`,
            amount: reward.coins,
            currency: "coin",
            metadata: {
              eventId: eventDoc.id,
              rank,
              cosmetic: reward.cosmetic ?? null,
            },
            timestamp: FieldValue.serverTimestamp(),
          });
        });

        await batch.commit();
      }

      // Etkinligi pasif olarak isaretle
      await eventDoc.ref.update({ isActive: false });
      console.log(`Etkinlik tamamlandi ve odulleri dagitildi: ${eventData.name} (${eventDoc.id})`);
    }
  }
);

// ============================================================
// ODUL HESAPLAMA
// ============================================================

interface EventRankReward {
  coins: number;
  cosmetic?: string;
}

/**
 * Etkinlik liderboard siralama odulu.
 */
function calculateEventRankReward(rank: number): EventRankReward {
  if (rank === 1) return { coins: 100000, cosmetic: "event_champion_frame" };
  if (rank === 2) return { coins: 75000, cosmetic: "event_runner_up_frame" };
  if (rank === 3) return { coins: 50000, cosmetic: "event_third_frame" };
  if (rank <= 5) return { coins: 30000 };
  if (rank <= 10) return { coins: 15000 };
  return { coins: 5000 };
}
