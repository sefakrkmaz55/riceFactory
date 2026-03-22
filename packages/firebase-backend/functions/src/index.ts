/**
 * RiceFactory — Cloud Functions Ana Giris Noktasi
 *
 * Tum Cloud Functions bu dosyadan export edilir.
 * Firebase CLI bu dosyayi okuyarak deploy edilecek function'lari belirler.
 *
 * Modul yapisi:
 * - anticheat.ts  : Anti-cheat dogrulama (save data, prestige, ban kontrolu)
 * - leaderboard.ts: Liderboard yonetimi (skor, sorgulama, sifirlama)
 * - events.ts     : Sezonluk etkinlik yonetimi (aktif etkinlikler, oduller)
 * - notifications.ts: Push bildirim (donus hatirlatma, etkinlik bildirimi)
 */

import { initializeApp } from "firebase-admin/app";

// Firebase Admin SDK'yi baslat (tum modullerin Firestore/Auth/FCM erisimi icin)
initializeApp();

// ============================================================
// ANTI-CHEAT FUNCTIONS
// ============================================================

export {
  validateSaveData,
  validatePrestige,
  banCheck,
  syncServerTime,
} from "./anticheat";

// ============================================================
// LEADERBOARD FUNCTIONS
// ============================================================

export {
  submitLeaderboardScore,
  getWeeklyLeaderboard,
  resetWeeklyLeaderboard,
  resetMonthlyLeaderboard,
} from "./leaderboard";

// ============================================================
// EVENT FUNCTIONS
// ============================================================

export {
  getActiveEvents,
  checkEventRewards,
  distributeEventRewards,
} from "./events";

// ============================================================
// NOTIFICATION FUNCTIONS
// ============================================================

export {
  sendReturnReminder,
  sendEventNotification,
} from "./notifications";
