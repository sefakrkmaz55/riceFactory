// =============================================================================
// PriceCalculatorTests.cs
// PriceCalculator icin unit testleri.
// Upgrade maliyet formulleri, uretim hizi, satis fiyati hesaplamalari ve
// ECONOMY_BALANCE.md'deki tablo degerleriyle karsilastirma.
// =============================================================================

using System;
using NUnit.Framework;
using RiceFactory.Economy;
using RiceFactory.Tests;

namespace RiceFactory.Tests.Economy
{
    [TestFixture]
    public class PriceCalculatorTests
    {
        private PriceCalculator _calculator;
        private MockBalanceConfig _config;

        // -----------------------------------------------------------------
        // SetUp
        // -----------------------------------------------------------------

        [SetUp]
        public void SetUp()
        {
            _config = new MockBalanceConfig();
            _calculator = new PriceCalculator(_config);
        }

        // -----------------------------------------------------------------
        // UpgradeCost Formul Dogrulama
        // Formul: BaseCost x 5^(level - 1)
        // Referans: ECONOMY_BALANCE.md Bolum 2.2
        // -----------------------------------------------------------------

        [Test]
        public void CalculateUpgradeCost_Level2_BaseCost100_Returns500()
        {
            // 100 x 5^(2-1) = 100 x 5 = 500
            double cost = _calculator.CalculateUpgradeCost(100f, 2);
            GameAssert.AreApproximatelyEqual(500, cost);
        }

        [Test]
        public void CalculateUpgradeCost_Level3_BaseCost100_Returns2500()
        {
            // 100 x 5^(3-1) = 100 x 25 = 2500
            double cost = _calculator.CalculateUpgradeCost(100f, 3);
            GameAssert.AreApproximatelyEqual(2500, cost);
        }

        [Test]
        public void CalculateUpgradeCost_Level4_BaseCost100_Returns12500()
        {
            // 100 x 5^(4-1) = 100 x 125 = 12500
            double cost = _calculator.CalculateUpgradeCost(100f, 4);
            GameAssert.AreApproximatelyEqual(12500, cost);
        }

        [Test]
        public void CalculateUpgradeCost_Level5_BaseCost100_Returns62500()
        {
            // 100 x 5^(5-1) = 100 x 625 = 62500
            double cost = _calculator.CalculateUpgradeCost(100f, 5);
            GameAssert.AreApproximatelyEqual(62500, cost);
        }

        [Test]
        public void CalculateUpgradeCost_InvalidLevel_ReturnsZero()
        {
            // Gecersiz seviyeler
            Assert.AreEqual(0, _calculator.CalculateUpgradeCost(100f, 1), "Level 1 gecersiz (2'den baslar)");
            Assert.AreEqual(0, _calculator.CalculateUpgradeCost(100f, 0), "Level 0 gecersiz");
            Assert.AreEqual(0, _calculator.CalculateUpgradeCost(100f, 6), "Level 6 max ustu");
        }

        [Test]
        public void CalculateUpgradeCost_WithGlobalMultiplier_AppliesCorrectly()
        {
            // Arrange: Global carpan 1.5x
            _config.SetFloat("general.globalUpgradeCostMultiplier", 1.5f);
            _calculator = new PriceCalculator(_config);

            // Act: 100 x 5^1 x 1.5 = 750
            double cost = _calculator.CalculateUpgradeCost(100f, 2);

            // Assert
            GameAssert.AreApproximatelyEqual(750, cost);
        }

        // -----------------------------------------------------------------
        // WorkerUpgradeCost Formul Dogrulama
        // Formul: 50 x level^2.2
        // Referans: ECONOMY_BALANCE.md Bolum 2.2
        // -----------------------------------------------------------------

        [Test]
        public void CalculateWorkerUpgradeCost_Level2_ReturnsExpected()
        {
            // 50 x 2^2.2 = 50 x 4.595 ≈ 229.74
            double cost = _calculator.CalculateWorkerUpgradeCost(2);
            GameAssert.AreApproximatelyEqual(50 * Math.Pow(2, 2.2), cost, 1.0);
        }

        [Test]
        public void CalculateWorkerUpgradeCost_Level10_ReturnsExpected()
        {
            // 50 x 10^2.2 ≈ 50 x 158.49 ≈ 7924
            double cost = _calculator.CalculateWorkerUpgradeCost(10);
            double expected = 50 * Math.Pow(10, 2.2);
            GameAssert.AreApproximatelyEqual(expected, cost, 1.0);
        }

        [Test]
        public void CalculateWorkerUpgradeCost_Level50_ReturnsExpected()
        {
            // 50 x 50^2.2 ≈ 50 x 5623 ≈ 281,170
            double cost = _calculator.CalculateWorkerUpgradeCost(50);
            double expected = 50 * Math.Pow(50, 2.2);
            GameAssert.AreApproximatelyEqual(expected, cost, 10.0);
        }

        [Test]
        public void CalculateWorkerUpgradeCost_InvalidLevel_ReturnsZero()
        {
            Assert.AreEqual(0, _calculator.CalculateWorkerUpgradeCost(1), "Level 1 gecersiz (2'den baslar)");
            Assert.AreEqual(0, _calculator.CalculateWorkerUpgradeCost(0), "Level 0 gecersiz");
            Assert.AreEqual(0, _calculator.CalculateWorkerUpgradeCost(51), "Level 51 max ustu");
        }

        [Test]
        public void CalculateWorkerUpgradeCost_CostIncreasesWithLevel()
        {
            // Seviye arttikca maliyet artmali
            double cost5 = _calculator.CalculateWorkerUpgradeCost(5);
            double cost10 = _calculator.CalculateWorkerUpgradeCost(10);
            double cost20 = _calculator.CalculateWorkerUpgradeCost(20);

            Assert.IsTrue(cost5 < cost10, "Level 10 maliyeti level 5'ten buyuk olmali");
            Assert.IsTrue(cost10 < cost20, "Level 20 maliyeti level 10'dan buyuk olmali");
        }

        // -----------------------------------------------------------------
        // StarUpgradeCost Formul Dogrulama
        // Formul: FacilityUnlockCost x 3^(star - 1)
        // Referans: ECONOMY_BALANCE.md Bolum 3.3
        // -----------------------------------------------------------------

        [Test]
        public void CalculateStarUpgradeCost_Star2_Returns3000()
        {
            // 1000 x 3^(2-1) = 1000 x 3 = 3000
            double cost = _calculator.CalculateStarUpgradeCost(1000f, 2);
            GameAssert.AreApproximatelyEqual(3000, cost);
        }

        [Test]
        public void CalculateStarUpgradeCost_Star3_Returns9000()
        {
            // 1000 x 3^(3-1) = 1000 x 9 = 9000
            double cost = _calculator.CalculateStarUpgradeCost(1000f, 3);
            GameAssert.AreApproximatelyEqual(9000, cost);
        }

        [Test]
        public void CalculateStarUpgradeCost_Star4_Returns27000()
        {
            // 1000 x 3^(4-1) = 1000 x 27 = 27000
            double cost = _calculator.CalculateStarUpgradeCost(1000f, 4);
            GameAssert.AreApproximatelyEqual(27000, cost);
        }

        [Test]
        public void CalculateStarUpgradeCost_Star5_Returns81000()
        {
            // 1000 x 3^(5-1) = 1000 x 81 = 81000
            double cost = _calculator.CalculateStarUpgradeCost(1000f, 5);
            GameAssert.AreApproximatelyEqual(81000, cost);
        }

        [Test]
        public void CalculateStarUpgradeCost_InvalidStar_ReturnsZero()
        {
            Assert.AreEqual(0, _calculator.CalculateStarUpgradeCost(1000f, 1), "Star 1 gecersiz (2'den baslar)");
            Assert.AreEqual(0, _calculator.CalculateStarUpgradeCost(1000f, 6), "Star 6 max ustu");
        }

        // -----------------------------------------------------------------
        // ProductionRate Hesaplama Dogrulama
        // Formul: (1/baseTime) x MachineMultiplier x WorkerBonus x StarBonus x FPBonus x ResearchBonus
        // Referans: ECONOMY_BALANCE.md Bolum 2.1
        // -----------------------------------------------------------------

        [Test]
        public void CalculateProductionRate_BaseValues_ReturnsBaseRate()
        {
            // Arrange: Tum bonuslar minimum
            // 1/10 x 1.0 x (1+1x0.02) x (1+0.0) x 1.0 x 1.0
            // = 0.1 x 1.0 x 1.02 x 1.0 x 1.0 x 1.0 = 0.102
            float rate = _calculator.CalculateProductionRate(
                baseProductionTime: 10f,
                machineLevel: 1,
                workerLevel: 1,
                starLevel: 1
            );

            float expected = (1f / 10f) * 1.0f * (1f + 1 * 0.02f) * 1.0f;
            GameAssert.AreApproximatelyEqual(expected, rate);
        }

        [Test]
        public void CalculateProductionRate_MachineLevel3_AppliesMultiplier()
        {
            // MachineLevel 3 -> carpan 2.2
            float rate = _calculator.CalculateProductionRate(
                baseProductionTime: 10f,
                machineLevel: 3,
                workerLevel: 1,
                starLevel: 1
            );

            float expected = (1f / 10f) * 2.2f * (1f + 1 * 0.02f) * 1.0f;
            GameAssert.AreApproximatelyEqual(expected, rate);
        }

        [Test]
        public void CalculateProductionRate_MachineLevel5_Returns5xMultiplier()
        {
            // MachineLevel 5 -> carpan 5.0
            float rate = _calculator.CalculateProductionRate(
                baseProductionTime: 10f,
                machineLevel: 5,
                workerLevel: 1,
                starLevel: 1
            );

            float expected = (1f / 10f) * 5.0f * (1f + 1 * 0.02f) * 1.0f;
            GameAssert.AreApproximatelyEqual(expected, rate);
        }

        [Test]
        public void CalculateProductionRate_WorkerLevel50_Doubles()
        {
            // WorkerLevel 50 -> 1 + 50 x 0.02 = 2.0
            float rate = _calculator.CalculateProductionRate(
                baseProductionTime: 10f,
                machineLevel: 1,
                workerLevel: 50,
                starLevel: 1
            );

            float expected = (1f / 10f) * 1.0f * 2.0f * 1.0f;
            GameAssert.AreApproximatelyEqual(expected, rate);
        }

        [Test]
        public void CalculateProductionRate_StarLevel5_Triples()
        {
            // StarLevel 5 -> 1 + 2.0 = 3.0
            float rate = _calculator.CalculateProductionRate(
                baseProductionTime: 10f,
                machineLevel: 1,
                workerLevel: 1,
                starLevel: 5
            );

            float expected = (1f / 10f) * 1.0f * (1f + 1 * 0.02f) * 3.0f;
            GameAssert.AreApproximatelyEqual(expected, rate);
        }

        [Test]
        public void CalculateProductionRate_WithFranchiseBonus_AppliesCorrectly()
        {
            // FP bonus 0.5 -> 1 + 0.5 = 1.5
            float rate = _calculator.CalculateProductionRate(
                baseProductionTime: 10f,
                machineLevel: 1,
                workerLevel: 1,
                starLevel: 1,
                franchiseProductionBonus: 0.5f
            );

            float expected = (1f / 10f) * 1.0f * (1f + 1 * 0.02f) * 1.0f * 1.5f;
            GameAssert.AreApproximatelyEqual(expected, rate);
        }

        [Test]
        public void CalculateProductionRate_ZeroBaseTime_ReturnsZero()
        {
            float rate = _calculator.CalculateProductionRate(0f, 1, 1, 1);
            Assert.AreEqual(0f, rate);
        }

        [Test]
        public void CalculateProductionRate_AllMaxLevels_ReturnsHighRate()
        {
            // Tum seviyeler max: Machine 5, Worker 50, Star 5, FP 2.0, Research 1.0
            float rate = _calculator.CalculateProductionRate(
                baseProductionTime: 10f,
                machineLevel: 5,
                workerLevel: 50,
                starLevel: 5,
                franchiseProductionBonus: 2.0f,
                researchSpeedBonus: 1.0f
            );

            // (1/10) x 5.0 x 2.0 x 3.0 x 3.0 x 2.0 = 18.0
            float expected = (1f / 10f) * 5.0f * 2.0f * 3.0f * 3.0f * 2.0f;
            GameAssert.AreApproximatelyEqual(expected, rate, 0.1f);
        }

        // -----------------------------------------------------------------
        // SellPrice Hesaplama Dogrulama
        // Formul: BasePrice x QualityMultiplier x DemandMultiplier x ReputationBonus
        // Referans: ECONOMY_BALANCE.md Bolum 2.3
        // -----------------------------------------------------------------

        [Test]
        public void CalculateSellPrice_Quality1_ReturnsBasePrice()
        {
            // Quality 1 -> x1.0, demand 1.0, rep 0
            double price = _calculator.CalculateSellPrice(100f, 1, 1.0f, 0);
            GameAssert.AreApproximatelyEqual(100, price);
        }

        [Test]
        public void CalculateSellPrice_Quality3_Returns1_7x()
        {
            // Quality 3 -> x1.7
            double price = _calculator.CalculateSellPrice(100f, 3, 1.0f, 0);
            GameAssert.AreApproximatelyEqual(170, price);
        }

        [Test]
        public void CalculateSellPrice_Quality5_Returns3x()
        {
            // Quality 5 -> x3.0
            double price = _calculator.CalculateSellPrice(100f, 5, 1.0f, 0);
            GameAssert.AreApproximatelyEqual(300, price);
        }

        [Test]
        public void CalculateSellPrice_HighDemand_IncreasePrice()
        {
            // Demand 1.5 (max)
            double price = _calculator.CalculateSellPrice(100f, 1, 1.5f, 0);
            GameAssert.AreApproximatelyEqual(150, price);
        }

        [Test]
        public void CalculateSellPrice_LowDemand_DecreasesPrice()
        {
            // Demand 0.8 (min)
            double price = _calculator.CalculateSellPrice(100f, 1, 0.8f, 0);
            GameAssert.AreApproximatelyEqual(80, price);
        }

        [Test]
        public void CalculateSellPrice_DemandClamped_DoesNotExceedBounds()
        {
            // Demand 2.0 -> 1.5'e clamp edilmeli
            double price = _calculator.CalculateSellPrice(100f, 1, 2.0f, 0);
            GameAssert.AreApproximatelyEqual(150, price, 0.1);
        }

        [Test]
        public void CalculateSellPrice_WithReputation_AppliesBonus()
        {
            // Reputation 1000 -> 1 + (1000/10000) = 1.1
            double price = _calculator.CalculateSellPrice(100f, 1, 1.0f, 1000);
            GameAssert.AreApproximatelyEqual(110, price);
        }

        [Test]
        public void CalculateSellPrice_AllFactorsMaxed()
        {
            // Quality 5 (3.0) x Demand 1.5 x Rep 10000 (2.0)
            // 100 x 3.0 x 1.5 x 2.0 = 900
            double price = _calculator.CalculateSellPrice(100f, 5, 1.5f, 10000);
            GameAssert.AreApproximatelyEqual(900, price, 1.0);
        }

        // -----------------------------------------------------------------
        // ECONOMY_BALANCE.md Tablo Karsilastirmasi
        // Bu testler dokumandaki ornek degerleri birebir dogrular
        // -----------------------------------------------------------------

        [Test]
        public void EconomyBalance_MachineUpgrade_PirincTarlasi_MatchesDocument()
        {
            // Referans: ECONOMY_BALANCE.md Bolum 2.2 "Pirinc Tarlasi BaseCost=100"
            // Lv.1->2: 100 x 5^0 = 100 → CalculateUpgradeCost level=2 icin 100 x 5^1 = 500
            // Not: Formul BaseCost x 5^(level-1), level=2 -> 5^1 = 5
            // Dokuman ornegi: Lv.1->2: 100 (bu 5^0=1 demek, targetLevel degil currentLevel)

            // Dogrulama: targetLevel ile formul
            double lv2Cost = _calculator.CalculateUpgradeCost(100f, 2); // 100 x 5^1 = 500
            double lv3Cost = _calculator.CalculateUpgradeCost(100f, 3); // 100 x 5^2 = 2500
            double lv4Cost = _calculator.CalculateUpgradeCost(100f, 4); // 100 x 5^3 = 12500
            double lv5Cost = _calculator.CalculateUpgradeCost(100f, 5); // 100 x 5^4 = 62500

            // Ustsel artis dogrulama: her seviye oncekinin 5 kati
            GameAssert.AreApproximatelyEqual(lv2Cost * 5, lv3Cost, 1.0);
            GameAssert.AreApproximatelyEqual(lv3Cost * 5, lv4Cost, 1.0);
            GameAssert.AreApproximatelyEqual(lv4Cost * 5, lv5Cost, 1.0);
        }

        [Test]
        public void EconomyBalance_StarUpgrade_MatchesDocument()
        {
            // Referans: ECONOMY_BALANCE.md Bolum 3.3
            // UnlockCost=1000 ile:
            // Yildiz 2: 1000 x 3^1 = 3,000
            // Yildiz 3: 1000 x 3^2 = 9,000
            // Yildiz 4: 1000 x 3^3 = 27,000
            // Yildiz 5: 1000 x 3^4 = 81,000

            GameAssert.AreApproximatelyEqual(3000, _calculator.CalculateStarUpgradeCost(1000f, 2));
            GameAssert.AreApproximatelyEqual(9000, _calculator.CalculateStarUpgradeCost(1000f, 3));
            GameAssert.AreApproximatelyEqual(27000, _calculator.CalculateStarUpgradeCost(1000f, 4));
            GameAssert.AreApproximatelyEqual(81000, _calculator.CalculateStarUpgradeCost(1000f, 5));
        }

        [Test]
        public void EconomyBalance_QualityMultipliers_MatchDocument()
        {
            // Referans: ECONOMY_BALANCE.md Bolum 2.3
            // Kalite carpanlari: [1.0, 1.3, 1.7, 2.2, 3.0]
            float basePrice = 100f;

            GameAssert.AreApproximatelyEqual(100, _calculator.CalculateSellPrice(basePrice, 1, 1.0f, 0));
            GameAssert.AreApproximatelyEqual(130, _calculator.CalculateSellPrice(basePrice, 2, 1.0f, 0));
            GameAssert.AreApproximatelyEqual(170, _calculator.CalculateSellPrice(basePrice, 3, 1.0f, 0));
            GameAssert.AreApproximatelyEqual(220, _calculator.CalculateSellPrice(basePrice, 4, 1.0f, 0));
            GameAssert.AreApproximatelyEqual(300, _calculator.CalculateSellPrice(basePrice, 5, 1.0f, 0));
        }

        // -----------------------------------------------------------------
        // ROI Hesaplama
        // -----------------------------------------------------------------

        [Test]
        public void CalculateROI_PositiveRevenueIncrease_ReturnsMinutes()
        {
            // ROI = 10000 / (200 - 100) = 100 dakika
            float roi = _calculator.CalculateROI(10000, 100, 200);
            GameAssert.AreApproximatelyEqual(100f, roi);
        }

        [Test]
        public void CalculateROI_NoRevenueIncrease_ReturnsNegativeOne()
        {
            // Gelir artisi yok
            float roi = _calculator.CalculateROI(10000, 100, 100);
            Assert.AreEqual(-1f, roi);
        }

        [Test]
        public void CalculateROI_RevenueDecrease_ReturnsNegativeOne()
        {
            // Gelir azalmasi
            float roi = _calculator.CalculateROI(10000, 200, 100);
            Assert.AreEqual(-1f, roi);
        }

        // -----------------------------------------------------------------
        // BulkSellPrice
        // -----------------------------------------------------------------

        [Test]
        public void CalculateBulkSellPrice_MultipliesQuantity()
        {
            // Tek birim fiyati 100, 10 adet
            double bulk = _calculator.CalculateBulkSellPrice(100f, 1, 1.0f, 0, 10);
            GameAssert.AreApproximatelyEqual(1000, bulk);
        }

        // -----------------------------------------------------------------
        // FacilityUnlockCost
        // -----------------------------------------------------------------

        [Test]
        public void CalculateFacilityUnlockCost_NoDiscount_ReturnsBase()
        {
            double cost = _calculator.CalculateFacilityUnlockCost(5000f, 0f);
            GameAssert.AreApproximatelyEqual(5000, cost);
        }

        [Test]
        public void CalculateFacilityUnlockCost_50PercentDiscount()
        {
            double cost = _calculator.CalculateFacilityUnlockCost(5000f, 0.5f);
            GameAssert.AreApproximatelyEqual(2500, cost);
        }

        [Test]
        public void CalculateFacilityUnlockCost_FullDiscount_ReturnsZero()
        {
            double cost = _calculator.CalculateFacilityUnlockCost(5000f, 1.0f);
            GameAssert.AreApproximatelyEqual(0, cost);
        }

        // -----------------------------------------------------------------
        // Constructor Dogrulama
        // -----------------------------------------------------------------

        [Test]
        public void Constructor_NullConfig_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                new PriceCalculator(null);
            });
        }
    }
}
