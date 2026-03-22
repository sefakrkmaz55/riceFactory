/**
 * RiceFactory — Push Notification Cloud Functions
 *
 * Firebase Cloud Messaging (FCM) ile bildirim yonetimi:
 * - Donus hatirlatma: 24+ saat offline oyunculara
 * - Etkinlik bildirimi: yeni etkinlik baslangici
 * - Liderboard bildirimi: haftalik odul kazananlara
 *
 * FCM token'lari Firestore'da players/{userId}.fcmToken alaninda saklanir.
 */

import { onSchedule } from "firebase-functions/v2/scheduler";
import { onDocumentCreated } from "firebase-functions/v2/firestore";
import { getFirestore, Timestamp } from "firebase-admin/firestore";
import { getMessaging, MulticastMessage } from "firebase-admin/messaging";

// ============================================================
// TIP TANIMLARI
// ============================================================

/** Bildirim sablon tipleri */
interface NotificationTemplate {
  title: string;
  body: string;
  imageUrl?: string;
  data?: Record<string, string>;
}

// ============================================================
// YAPILANDIRMA
// ============================================================

/** Donus hatirlatma esigi (milisaniye) — 24 saat */
const RETURN_REMINDER_THRESHOLD_MS = 24 * 60 * 60 * 1000;

/** Bir batch'te gonderilebilecek maksimum FCM mesaji */
const FCM_BATCH_SIZE = 500;

/** Oyuncu sorgulama limiti (pagination icin) */
const PLAYER_QUERY_LIMIT = 1000;

// ============================================================
// sendReturnReminder — Donus hatirlatma bildirimi (zamanlanmis)
// ============================================================

/**
 * Her gun 10:00 UTC'de calisir.
 * 24+ saat once son giren oyunculara push bildirim gonderir.
 *
 * Bildirim icerigi oyuncunun offline kazancini vurgular,
 * boylece geri donme motivasyonu olusturur.
 *
 * Not: Gunluk maksimum 1 hatirlatma (spam onleme).
 */
export const sendReturnReminder = onSchedule(
  {
    schedule: "every day 10:00",
    timeZone: "UTC",
    region: "europe-west1",
  },
  async () => {
    const db = getFirestore();
    const messaging = getMessaging();
    const now = Date.now();
    const threshold = Timestamp.fromMillis(now - RETURN_REMINDER_THRESHOLD_MS);

    // Son 24 saat icinde giris yapmamis oyunculari bul
    // Ayni zamanda son 7 gun icinde aktif olmus olmali (terk etmis oyunculara gonderme)
    const sevenDaysAgo = Timestamp.fromMillis(now - 7 * 24 * 60 * 60 * 1000);

    let lastDoc: FirebaseFirestore.QueryDocumentSnapshot | undefined;
    let totalSent = 0;

    // Pagination ile tum uygun oyunculari isle
    while (true) {
      let query = db
        .collection("players")
        .where("lastOnline", "<", threshold)
        .where("lastOnline", ">", sevenDaysAgo)
        .orderBy("lastOnline", "desc")
        .limit(PLAYER_QUERY_LIMIT);

      if (lastDoc) {
        query = query.startAfter(lastDoc);
      }

      const snapshot = await query.get();
      if (snapshot.empty) break;

      // FCM token'lari topla
      const tokens: string[] = [];
      for (const doc of snapshot.docs) {
        const data = doc.data();
        // FCM token mevcut, ban yok, bildirim tercihi acik
        if (
          data.fcmToken &&
          !data.isBanned &&
          data.notificationPrefs?.returnReminder !== false
        ) {
          tokens.push(data.fcmToken);
        }
      }

      // Batch halinde bildirim gonder
      if (tokens.length > 0) {
        const notification = buildReturnReminderNotification();

        for (let i = 0; i < tokens.length; i += FCM_BATCH_SIZE) {
          const batch = tokens.slice(i, i + FCM_BATCH_SIZE);
          const message: MulticastMessage = {
            tokens: batch,
            notification: {
              title: notification.title,
              body: notification.body,
              imageUrl: notification.imageUrl,
            },
            data: notification.data,
            android: {
              priority: "normal",
              notification: {
                channelId: "return_reminder",
                icon: "ic_notification",
              },
            },
            apns: {
              payload: {
                aps: {
                  badge: 1,
                  sound: "default",
                },
              },
            },
          };

          try {
            const response = await messaging.sendEachForMulticast(message);
            totalSent += response.successCount;

            // Basarisiz token'lari temizle (gecersiz/suresi dolmus)
            if (response.failureCount > 0) {
              await cleanupInvalidTokens(db, batch, response.responses);
            }
          } catch (error) {
            console.error("FCM gonderim hatasi:", error);
          }
        }
      }

      lastDoc = snapshot.docs[snapshot.docs.length - 1];
      if (snapshot.docs.length < PLAYER_QUERY_LIMIT) break;
    }

    console.log(`Donus hatirlatma bildirimi gonderildi: ${totalSent} oyuncu.`);
  }
);

// ============================================================
// sendEventNotification — Yeni etkinlik bildirimi (Firestore trigger)
// ============================================================

/**
 * Yeni bir etkinlik dokumani olusturuldugunda tetiklenir.
 * Tum aktif oyunculara yeni etkinlik hakkinda bildirim gonderir.
 */
export const sendEventNotification = onDocumentCreated(
  { document: "events/{eventId}", region: "europe-west1" },
  async (event) => {
    if (!event.data) return;

    const eventData = event.data.data();
    const eventId = event.params.eventId;
    const db = getFirestore();
    const messaging = getMessaging();

    // Etkinlik aktif degilse bildirim gonderme
    if (!eventData.isActive) return;

    const notification = buildEventNotification(
      eventData.name,
      eventData.description,
      eventData.theme
    );

    // Son 7 gunde aktif olan oyuncularin FCM token'larini topla
    const sevenDaysAgo = Timestamp.fromMillis(Date.now() - 7 * 24 * 60 * 60 * 1000);
    let lastDoc: FirebaseFirestore.QueryDocumentSnapshot | undefined;
    let totalSent = 0;

    while (true) {
      let query = db
        .collection("players")
        .where("lastOnline", ">", sevenDaysAgo)
        .orderBy("lastOnline", "desc")
        .limit(PLAYER_QUERY_LIMIT);

      if (lastDoc) {
        query = query.startAfter(lastDoc);
      }

      const snapshot = await query.get();
      if (snapshot.empty) break;

      const tokens: string[] = [];
      for (const doc of snapshot.docs) {
        const data = doc.data();
        if (
          data.fcmToken &&
          !data.isBanned &&
          data.notificationPrefs?.eventNotifications !== false
        ) {
          tokens.push(data.fcmToken);
        }
      }

      if (tokens.length > 0) {
        for (let i = 0; i < tokens.length; i += FCM_BATCH_SIZE) {
          const batch = tokens.slice(i, i + FCM_BATCH_SIZE);
          const message: MulticastMessage = {
            tokens: batch,
            notification: {
              title: notification.title,
              body: notification.body,
              imageUrl: notification.imageUrl,
            },
            data: {
              ...notification.data,
              eventId,
            },
            android: {
              priority: "high",
              notification: {
                channelId: "events",
                icon: "ic_notification",
              },
            },
            apns: {
              payload: {
                aps: {
                  badge: 1,
                  sound: "event_start.caf",
                },
              },
            },
          };

          try {
            const response = await messaging.sendEachForMulticast(message);
            totalSent += response.successCount;

            if (response.failureCount > 0) {
              await cleanupInvalidTokens(db, batch, response.responses);
            }
          } catch (error) {
            console.error("Etkinlik bildirim hatasi:", error);
          }
        }
      }

      lastDoc = snapshot.docs[snapshot.docs.length - 1];
      if (snapshot.docs.length < PLAYER_QUERY_LIMIT) break;
    }

    console.log(`Etkinlik bildirimi gonderildi: "${eventData.name}" — ${totalSent} oyuncu.`);
  }
);

// ============================================================
// YARDIMCI FONKSIYONLAR
// ============================================================

/**
 * Donus hatirlatma bildirim sablonu olusturur.
 * Rastgele mesaj secimi ile tekrar eden bildirimlerin sikiciligini azaltir.
 */
function buildReturnReminderNotification(): NotificationTemplate {
  const messages = [
    {
      title: "Fabrikan seni ozledi!",
      body: "Yoklugunun ardindan coin'lerin birikmis. Gel topla!",
    },
    {
      title: "Pirinclerin hazir!",
      body: "Offline kazancin seni bekliyor. Fabrikanin basina gec!",
    },
    {
      title: "Patron neredesin?",
      body: "Calisanlarin emirlerini bekliyor. Uretim durmasin!",
    },
    {
      title: "Kazancin birikmis!",
      body: "Offline suresince kazandigin coin'leri toplamak icin geri don!",
    },
  ];

  const selected = messages[Math.floor(Math.random() * messages.length)];

  return {
    ...selected,
    data: {
      type: "return_reminder",
      action: "open_factory",
    },
  };
}

/**
 * Yeni etkinlik bildirim sablonu olusturur.
 */
function buildEventNotification(
  eventName: string,
  description: string,
  theme: string
): NotificationTemplate {
  return {
    title: `Yeni Etkinlik: ${eventName}`,
    body: description || `${eventName} basladi! Ozel odulleri kacirma.`,
    data: {
      type: "event_start",
      action: "open_events",
      theme,
    },
  };
}

/**
 * Gecersiz FCM token'lari Firestore'dan temizler.
 * Token suresi dolmus veya uygulamayi kaldirmis oyuncular icin.
 */
async function cleanupInvalidTokens(
  db: FirebaseFirestore.Firestore,
  tokens: string[],
  responses: Array<{ success: boolean; error?: { code: string } }>
): Promise<void> {
  const invalidTokens: string[] = [];

  responses.forEach((resp, idx) => {
    if (
      !resp.success &&
      resp.error &&
      (resp.error.code === "messaging/registration-token-not-registered" ||
        resp.error.code === "messaging/invalid-registration-token")
    ) {
      invalidTokens.push(tokens[idx]);
    }
  });

  if (invalidTokens.length === 0) return;

  // Gecersiz token'a sahip oyunculari bul ve temizle
  // Not: Bu islem maliyetli olabilir, bu yuzden batch halinde yapilir
  for (const token of invalidTokens) {
    try {
      const snapshot = await db
        .collection("players")
        .where("fcmToken", "==", token)
        .limit(1)
        .get();

      if (!snapshot.empty) {
        await snapshot.docs[0].ref.update({ fcmToken: null });
      }
    } catch (error) {
      console.error(`Token temizleme hatasi: ${token}`, error);
    }
  }
}
