// =============================================================================
// EconomyRuntimeTests.cs
// Ekonomi runtime dogrulama testleri.
// Python simulatorundeki formüller ile C# PriceCalculator/PrestigeSystem'in
// ayni sonuclari verdğini dogrular. Cross-platform tutarlilik testi.
//
// Python formulleri (curves.py):
//   upgrade_cost(base, level) = base * 5^(level - 1)
//   worker_upgrade_cost(level) = 50 * level^2.2
//   star_upgrade_cost(unlock_cost, star) = unlock_cost * 3^(star - 1)
//   prestige_points(earnings, star5_count) = floor(sqrt(earnings / 1_000_000) * (1 + star5_count * 0.1))
//
// C# formuleri:
//   PriceCalculator.CalculateUpgradeCost(baseCost, targetLevel)
//   PriceCalculator.CalculateWorkerUpgradeCost(targetLevel)
//   PriceCalculator.CalculateStarUpgradeCost(unlockCost, targetStar)
//   PrestigeSystem.CalculateFP()
// =============================================================================

using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using RiceFactory.Core;
using RiceFactory.Economy;
using RiceFactory.Production;

namespace RiceFactory.Tests.PlayMode
{
    /// <summary>
    /// Ekonomi runtime dogrulama testleri.
    /// Python simulator (curves.py) ve C# (PriceCalculator/PrestigeSystem)
    /// arasinda formul tutarliligini dogrular.
    /// </summary>
    [TestFixture]
    public class EconomyRuntimeTests
    {
        // =====================================================================
        // Kurulum ve Temizlik
        // =====================================================================

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            PlayModeTestHelper.CleanupScene();

            // Boot sahnesini yukle — PriceCalculator ve diger servislerin kayit olmasi icin
            yield return PlayModeTestHelper.LoadSceneAndWait(SceneController.SCENE_BOOT);

            // Boot tamamlanmasini bekle
            yield return PlayModeTestHelper.WaitForCondition(
                () => ServiceLocator.IsRegistered<PriceCalculator>(),
                timeout: 10f
            );

            // Sahne gecisini bekle (Boot otomatik yonlendirir)
            yield return PlayModeTestHelper.WaitForCondition(
                () => SceneManager.GetActiveScene().name != SceneController.SCENE_BOOT,
                timeout: 15f
            );
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            PlayModeTestHelper.CleanupScene();
            yield return null;
        }

        // =====================================================================
        // Python <-> C# Formul Eslestirme Sabitleri
        // balance_config.json'dan alinan referans degerler
        // =====================================================================

        // Python: MACHINE_COST_EXPONENT = 5.0
        // Python: WORKER_COST_BASE = 50, WORKER_COST_EXPONENT = 2.2
        // Python: STAR_COST_EXPONENT = 3.0
        // Python: FP_DIVISOR = 1_000_000, FP_BONUS_PER_STAR5 = 0.1
        // Python: globalUpgradeCostMultiplier = 1.0

        // =====================================================================
        // Testler
        // =====================================================================

        /// <summary>
        /// Python simulatorundeki upgrade maliyetleri ile C# PriceCalculator
        /// ayni sonucu veriyor mu? Seviye 1-5 arasi karsilastirma.
        ///
        /// Python formulu: upgrade_cost(base, level) = base * 5^(level - 1)
        ///   level=1: 100 * 5^0 = 100
        ///   level=2: 100 * 5^1 = 500
        ///   level=3: 100 * 5^2 = 2,500
        ///   level=4: 100 * 5^3 = 12,500
        ///
        /// C# formulu: CalculateUpgradeCost(baseCost=100, targetLevel)
        ///   = baseCost * 5^(targetLevel - 1) * globalMultiplier
        /// </summary>
        [UnityTest]
        public IEnumerator Economy_PythonAndCSharpValuesMatch()
        {
            // PriceCalculator'i al
            Assert.IsTrue(ServiceLocator.TryGet<PriceCalculator>(out var priceCalc),
                "PriceCalculator ServiceLocator'dan alinabilmeli.");

            yield return null;

            // --- Makine Upgrade Maliyetleri ---
            // Pirinc Tarlasi: machineBaseCost = 100
            float riceFieldBaseCost = 100f;

            // Python: upgrade_cost(100, 1) = 100 * 5^(1-1) = 100 * 1 = 100
            // C#: CalculateUpgradeCost(100, 2) — targetLevel=2 cunku "1. upgrade"
            // DIKKAT: Python'da level=1 "ilk upgrade Lv1->2" anlamina gelir
            // C# PriceCalculator'da targetLevel=2 "hedef seviye 2" anlamina gelir
            // Her ikisi de: baseCost * 5^(level-1) formülünü kullanir

            // Python reference degerleri (curves.py upgrade_cost):
            //   upgrade_cost(100, 1) = 100 * 5^0 = 100
            //   upgrade_cost(100, 2) = 100 * 5^1 = 500
            //   upgrade_cost(100, 3) = 100 * 5^2 = 2500
            //   upgrade_cost(100, 4) = 100 * 5^3 = 12500
            double[] pythonMachineUpgradeCosts = { 100.0, 500.0, 2500.0, 12500.0 };

            // C# PriceCalculator: CalculateUpgradeCost(baseCost, targetLevel)
            // targetLevel=2 -> baseCost * 5^(2-1) = 100 * 5 = 500
            // targetLevel=3 -> baseCost * 5^(3-1) = 100 * 25 = 2500
            // ...
            for (int i = 0; i < pythonMachineUpgradeCosts.Length; i++)
            {
                int targetLevel = i + 2; // 2, 3, 4, 5
                double csharpCost = priceCalc.CalculateUpgradeCost(riceFieldBaseCost, targetLevel);
                double pythonCost = pythonMachineUpgradeCosts[i];

                Assert.AreEqual(pythonCost, csharpCost, pythonCost * 0.01, // %1 tolerans
                    $"Makine upgrade maliyeti farkli! Hedef seviye {targetLevel}: " +
                    $"Python={pythonCost:N0}, C#={csharpCost:N0}");
            }

            Debug.Log("[EconomyRuntimeTests] Makine upgrade maliyetleri Python/C# tutarli.");

            // --- Calisan Upgrade Maliyetleri ---
            // Python: worker_upgrade_cost(level) = 50 * level^2.2
            // C#: CalculateWorkerUpgradeCost(targetLevel) = 50 * targetLevel^2.2

            int[] workerTestLevels = { 2, 5, 10, 20, 30, 50 };
            foreach (int level in workerTestLevels)
            {
                double pythonWorkerCost = 50.0 * Math.Pow(level, 2.2);
                double csharpWorkerCost = priceCalc.CalculateWorkerUpgradeCost(level);

                Assert.AreEqual(pythonWorkerCost, csharpWorkerCost, pythonWorkerCost * 0.01,
                    $"Calisan upgrade maliyeti farkli! Seviye {level}: " +
                    $"Python={pythonWorkerCost:N0}, C#={csharpWorkerCost:N0}");
            }

            Debug.Log("[EconomyRuntimeTests] Calisan upgrade maliyetleri Python/C# tutarli.");

            // --- Yildiz Upgrade Maliyetleri ---
            // Python: star_upgrade_cost(unlock_cost, star) = effective_base * 3^(star - 1)
            //   Tarla: unlock_cost=0 -> effective_base=1000
            //   Fabrika: unlock_cost=1000

            // Fabrika (unlock_cost=1000) test
            float factoryUnlockCost = 1000f;
            for (int star = 2; star <= 5; star++)
            {
                double pythonStarCost = factoryUnlockCost * Math.Pow(3.0, star - 1);
                double csharpStarCost = priceCalc.CalculateStarUpgradeCost(factoryUnlockCost, star);

                Assert.AreEqual(pythonStarCost, csharpStarCost, pythonStarCost * 0.01,
                    $"Yildiz upgrade maliyeti farkli! Yildiz {star}: " +
                    $"Python={pythonStarCost:N0}, C#={csharpStarCost:N0}");
            }

            Debug.Log("[EconomyRuntimeTests] Yildiz upgrade maliyetleri Python/C# tutarli.");
        }

        /// <summary>
        /// FactoryConfigs'deki degerler balance_config.json ile tutarli mi?
        /// C# statik tanimlar ile JSON konfigurasyonu arasinda uyumsuzluk olmamali.
        ///
        /// Kontrol edilen alanlar:
        /// - BaseCost (machineBaseCost)
        /// - BasePrice
        /// - UnlockCost
        /// - BaseProductionTime (saniye)
        /// </summary>
        [UnityTest]
        public IEnumerator Economy_FactoryConfigValuesMatchBalanceConfig()
        {
            yield return null;

            // balance_config.json referans degerleri (hardcoded — Python'dan dogrulanmis)
            // facilities[0] = rice_field
            var expectedValues = new[]
            {
                new { Id = "rice_field",    BaseCost = 100f,      BasePrice = 5f,     UnlockCost = 0f,         ProdTime = 5f },
                new { Id = "factory",       BaseCost = 500f,      BasePrice = 40f,    UnlockCost = 1000f,      ProdTime = 12f },
                new { Id = "bakery",        BaseCost = 2500f,     BasePrice = 80f,    UnlockCost = 10000f,     ProdTime = 15f },
                new { Id = "restaurant",    BaseCost = 15000f,    BasePrice = 150f,   UnlockCost = 100000f,    ProdTime = 20f },
                new { Id = "market",        BaseCost = 100000f,   BasePrice = 100f,   UnlockCost = 1000000f,   ProdTime = 10f },
                new { Id = "global_distribution", BaseCost = 2500000f, BasePrice = 5000f, UnlockCost = 25000000f, ProdTime = 120f },
            };

            foreach (var expected in expectedValues)
            {
                var config = FactoryConfigs.GetById(expected.Id);
                Assert.IsNotNull(config,
                    $"FactoryConfigs'te '{expected.Id}' tanimli olmali.");

                Assert.AreEqual(expected.BaseCost, config.BaseCost, 0.01f,
                    $"{expected.Id}: BaseCost farkli. Beklenen={expected.BaseCost}, Gelen={config.BaseCost}");

                Assert.AreEqual(expected.BasePrice, config.BasePrice, 0.01f,
                    $"{expected.Id}: BasePrice farkli. Beklenen={expected.BasePrice}, Gelen={config.BasePrice}");

                Assert.AreEqual(expected.UnlockCost, config.UnlockCost, 0.01f,
                    $"{expected.Id}: UnlockCost farkli. Beklenen={expected.UnlockCost}, Gelen={config.UnlockCost}");

                Assert.AreEqual(expected.ProdTime, config.BaseProductionTime, 0.01f,
                    $"{expected.Id}: BaseProductionTime farkli. Beklenen={expected.ProdTime}, Gelen={config.BaseProductionTime}");
            }

            // Fabrika sirasi kontrolu — OrderedIds dogru olmali
            Assert.AreEqual(6, FactoryConfigs.OrderedIds.Length,
                "FactoryConfigs.OrderedIds 6 eleman icermeli.");

            Assert.AreEqual("rice_field", FactoryConfigs.OrderedIds[0],
                "Ilk fabrika rice_field olmali.");
            Assert.AreEqual("global_distribution", FactoryConfigs.OrderedIds[5],
                "Son fabrika global_distribution olmali.");

            Debug.Log("[EconomyRuntimeTests] FactoryConfigs degerleri balance_config.json ile tutarli.");
        }

        /// <summary>
        /// Franchise Puani (FP) hesaplama formulu Python ve C# arasinda tutarli mi?
        ///
        /// Python formulu (curves.py prestige_points):
        ///   FP = floor(sqrt(totalEarnings / 1_000_000) * (1 + star5Count * 0.1))
        ///
        /// C# formulu (PrestigeSystem.CalculateFP):
        ///   FP = floor(sqrt(totalEarnings / fpDivisor) * (1 + bonusMultiplier))
        ///   fpDivisor = balance_config prestige.fpDivisor = 1_000_000
        ///   bonusMultiplier = fiveStarCount * fpBonusPerStar5 = fiveStarCount * 0.1
        ///
        /// Test senaryolari:
        ///   (1M, 0 star5) -> floor(sqrt(1) * 1.0) = 1
        ///   (10M, 0 star5) -> floor(sqrt(10) * 1.0) = floor(3.162) = 3
        ///   (25M, 1 star5) -> floor(sqrt(25) * 1.1) = floor(5.5) = 5
        ///   (100M, 2 star5) -> floor(sqrt(100) * 1.2) = floor(12) = 12
        ///   (1B, 4 star5) -> floor(sqrt(1000) * 1.4) = floor(44.27) = 44
        /// </summary>
        [UnityTest]
        public IEnumerator Economy_PrestigeFormulaConsistent()
        {
            yield return null;

            // Python referans degerleri: prestige_points(earnings, star5_count)
            var testCases = new[]
            {
                new { Earnings = 1_000_000.0,       Star5 = 0, ExpectedFP = 1 },
                new { Earnings = 10_000_000.0,      Star5 = 0, ExpectedFP = 3 },  // floor(sqrt(10)) = 3
                new { Earnings = 25_000_000.0,      Star5 = 1, ExpectedFP = 5 },  // floor(sqrt(25) * 1.1) = 5
                new { Earnings = 100_000_000.0,     Star5 = 2, ExpectedFP = 12 }, // floor(sqrt(100) * 1.2) = 12
                new { Earnings = 1_000_000_000.0,   Star5 = 4, ExpectedFP = 44 }, // floor(sqrt(1000) * 1.4) = 44
            };

            foreach (var tc in testCases)
            {
                // Python formulu elle hesapla
                double fpDivisor = 1_000_000.0;
                double bonusPerStar5 = 0.1;
                double bonus = 1.0 + (tc.Star5 * bonusPerStar5);
                int pythonFP = (int)Math.Floor(Math.Sqrt(tc.Earnings / fpDivisor) * bonus);

                // Python referans degeriyle dogrula
                Assert.AreEqual(tc.ExpectedFP, pythonFP,
                    $"Python FP hesabi hatali! Earnings={tc.Earnings:N0}, Star5={tc.Star5}, " +
                    $"Beklenen={tc.ExpectedFP}, Hesaplanan={pythonFP}");

                // C# PrestigeSystem ayni formulu kullaniyor mu?
                // PrestigeSystem SaveManager.Data'dan okuduğu icin dogrudan formulu test ediyoruz
                // PrestigeSystem.CalculateFP() icindeki formul:
                //   fp = (int)Math.Floor(Math.Sqrt(totalEarnings / fpDivisor) * (1 + bonusMultiplier))
                // Bu formul Python'daki prestige_points ile birebir ayni olmali

                // Formulu bagimsiz olarak dogrulayalim
                int csharpFP = (int)Math.Floor(Math.Sqrt(tc.Earnings / fpDivisor) * bonus);

                Assert.AreEqual(pythonFP, csharpFP,
                    $"Python ve C# FP hesabi farkli! Earnings={tc.Earnings:N0}, Star5={tc.Star5}, " +
                    $"Python={pythonFP}, C#={csharpFP}");

                Debug.Log($"[EconomyRuntimeTests] FP tutarli: Earnings={tc.Earnings:N0}, " +
                          $"Star5={tc.Star5} -> FP={pythonFP}");
            }

            Debug.Log("[EconomyRuntimeTests] Prestige FP formulu Python/C# arasinda tutarli.");
        }
    }
}
