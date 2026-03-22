/**
 * RiceFactory — Firestore Security Rules Tests
 *
 * @firebase/rules-unit-testing ile Firestore guvenlik kurallarini test eder.
 * Her test bagimsiz calisir: before/after ile emulator verisi temizlenir.
 */

import {
  initializeTestEnvironment,
  assertSucceeds,
  assertFails,
  RulesTestEnvironment,
} from "@firebase/rules-unit-testing";
import { readFileSync } from "fs";
import { resolve } from "path";
import { setDoc, getDoc, doc, updateDoc } from "firebase/firestore";

const PROJECT_ID = "ricefactory-game";
const RULES_PATH = resolve(__dirname, "../firestore.rules");

let testEnv: RulesTestEnvironment;

before(async () => {
  testEnv = await initializeTestEnvironment({
    projectId: PROJECT_ID,
    firestore: {
      rules: readFileSync(RULES_PATH, "utf8"),
      host: "127.0.0.1",
      port: 8080,
    },
  });
});

afterEach(async () => {
  await testEnv.clearFirestore();
});

after(async () => {
  await testEnv.cleanup();
});

// ============================================================
// OYUNCU KOLEKSIYONU TESTLERI
// ============================================================

describe("Firestore Security Rules — Players", () => {
  it("authenticated_user_can_read_own_data", async () => {
    const userId = "user_alice";

    // Admin ile veri olustur
    await testEnv.withSecurityRulesDisabled(async (context) => {
      const adminDb = context.firestore();
      await setDoc(doc(adminDb, "players", userId), {
        displayName: "Alice",
        coins: 1000,
        level: 5,
      });
    });

    // Kullanici kendi verisini okuyabilmeli
    const aliceDb = testEnv.authenticatedContext(userId).firestore();
    await assertSucceeds(getDoc(doc(aliceDb, "players", userId)));
  });

  it("authenticated_user_can_write_own_data", async () => {
    const userId = "user_bob";

    // Dokuman olustur (create izni)
    const bobDb = testEnv.authenticatedContext(userId).firestore();
    await assertSucceeds(
      setDoc(doc(bobDb, "players", userId), {
        displayName: "Bob",
        coins: 500,
        level: 1,
      })
    );
  });

  it("user_cannot_read_other_users_data", async () => {
    const aliceId = "user_alice";
    const bobId = "user_bob";

    // Alice'in verisini olustur
    await testEnv.withSecurityRulesDisabled(async (context) => {
      const adminDb = context.firestore();
      await setDoc(doc(adminDb, "players", aliceId), {
        displayName: "Alice",
        coins: 9999,
      });
    });

    // Bob, Alice'in verisini okuyamamali
    const bobDb = testEnv.authenticatedContext(bobId).firestore();
    await assertFails(getDoc(doc(bobDb, "players", aliceId)));
  });

  it("user_cannot_write_other_users_data", async () => {
    const aliceId = "user_alice";
    const bobId = "user_bob";

    // Alice'in dokumani olustur
    await testEnv.withSecurityRulesDisabled(async (context) => {
      const adminDb = context.firestore();
      await setDoc(doc(adminDb, "players", aliceId), {
        displayName: "Alice",
        coins: 100,
      });
    });

    // Bob, Alice'in dokumanina yazamamali
    const bobDb = testEnv.authenticatedContext(bobId).firestore();
    await assertFails(
      updateDoc(doc(bobDb, "players", aliceId), { coins: 999999 })
    );
  });

  it("user_cannot_modify_protected_fields", async () => {
    const userId = "user_charlie";

    // Mevcut doküman olustur (korunan alanlarla birlikte)
    await testEnv.withSecurityRulesDisabled(async (context) => {
      const adminDb = context.firestore();
      await setDoc(doc(adminDb, "players", userId), {
        displayName: "Charlie",
        coins: 100,
        franchisePoints: 10,
        totalLifetimeEarnings: 5000,
        isBanned: false,
        banReason: "",
      });
    });

    const charlieDb = testEnv.authenticatedContext(userId).firestore();

    // franchisePoints degistirmeye calisma — reddedilmeli
    await assertFails(
      updateDoc(doc(charlieDb, "players", userId), {
        franchisePoints: 99999,
      })
    );

    // totalLifetimeEarnings degistirmeye calisma — reddedilmeli
    await assertFails(
      updateDoc(doc(charlieDb, "players", userId), {
        totalLifetimeEarnings: 99999999,
      })
    );

    // isBanned degistirmeye calisma — reddedilmeli
    await assertFails(
      updateDoc(doc(charlieDb, "players", userId), {
        isBanned: true,
      })
    );

    // Korunmayan alan degistirme — basarili olmali
    await assertSucceeds(
      updateDoc(doc(charlieDb, "players", userId), {
        displayName: "Charlie Updated",
      })
    );
  });

  it("unauthenticated_user_denied", async () => {
    const userId = "user_dave";

    // Veri olustur
    await testEnv.withSecurityRulesDisabled(async (context) => {
      const adminDb = context.firestore();
      await setDoc(doc(adminDb, "players", userId), {
        displayName: "Dave",
        coins: 100,
      });
    });

    // Giris yapmamis kullanici erisememeli
    const unauthDb = testEnv.unauthenticatedContext().firestore();
    await assertFails(getDoc(doc(unauthDb, "players", userId)));
  });
});

// ============================================================
// LIDERBOARD KOLEKSIYONU TESTLERI
// ============================================================

describe("Firestore Security Rules — Leaderboard", () => {
  it("anyone_can_read_leaderboard", async () => {
    const weekId = "weekly_2026W13";
    const userId = "user_reader";

    // Liderboard verisi olustur
    await testEnv.withSecurityRulesDisabled(async (context) => {
      const adminDb = context.firestore();
      await setDoc(doc(adminDb, "leaderboards", weekId), {
        createdAt: new Date().toISOString(),
      });
      await setDoc(
        doc(adminDb, "leaderboards", weekId, "earnings", "player1"),
        {
          userId: "player1",
          name: "TopPlayer",
          score: 50000,
          avatar: "default",
          level: 30,
        }
      );
    });

    // Herhangi bir giris yapmis kullanici okuyabilmeli
    const readerDb = testEnv.authenticatedContext(userId).firestore();
    await assertSucceeds(getDoc(doc(readerDb, "leaderboards", weekId)));
    await assertSucceeds(
      getDoc(doc(readerDb, "leaderboards", weekId, "earnings", "player1"))
    );
  });

  it("user_cannot_write_leaderboard", async () => {
    const weekId = "weekly_2026W13";
    const userId = "user_hacker";

    // Liderboard var
    await testEnv.withSecurityRulesDisabled(async (context) => {
      const adminDb = context.firestore();
      await setDoc(doc(adminDb, "leaderboards", weekId), {
        createdAt: new Date().toISOString(),
      });
    });

    // Istemci liderboard'a yazamamali
    const hackerDb = testEnv.authenticatedContext(userId).firestore();
    await assertFails(
      setDoc(doc(hackerDb, "leaderboards", weekId, "earnings", userId), {
        userId: userId,
        name: "Hacker",
        score: 9999999,
        avatar: "default",
        level: 99,
      })
    );
  });

  it("unauthenticated_user_cannot_read_leaderboard", async () => {
    const weekId = "weekly_2026W13";

    await testEnv.withSecurityRulesDisabled(async (context) => {
      const adminDb = context.firestore();
      await setDoc(doc(adminDb, "leaderboards", weekId), {
        createdAt: new Date().toISOString(),
      });
    });

    // Giris yapmamis kullanici liderboard okuyamamali
    const unauthDb = testEnv.unauthenticatedContext().firestore();
    await assertFails(getDoc(doc(unauthDb, "leaderboards", weekId)));
  });
});
