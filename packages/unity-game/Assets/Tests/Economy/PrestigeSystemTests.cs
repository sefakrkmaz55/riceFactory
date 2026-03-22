// =============================================================================
// PrestigeSystemTests.cs
// PrestigeSystem icin unit testleri.
// FP hesaplama, prestige kosul kontrolu, prestige uygulama ve
// FP bonus hesaplama dogrulanir.
//
// Not: PrestigeSystem bazi SaveData alanlarina (Facilities, FranchiseBonuses,
// UnlockedCities, Achievements, CosmeticInventory, ResetForFranchise) bagimlidir.
// Bu alanlar henuz tam implemente edilmemis olabilir. Bu testler
// mevcut public API uzerinden calisir ve eksik alanlari mock'lar.
// =============================================================================

using System;
using System.Collections.Generic;
using NUnit.Framework;
using RiceFactory.Core;
using RiceFactory.Core.Events;
using RiceFactory.Data.Save;
using RiceFactory.Economy;
using RiceFactory.Tests;

namespace RiceFactory.Tests.Economy
{
    [TestFixture]
    public class PrestigeSystemTests
    {
        private PrestigeSystem _prestigeSystem;
        private MockBalanceConfig _config;
        private MockSaveManager _saveManager;
        private EventManager _eventManager;

        // -----------------------------------------------------------------
        // SetUp / TearDown
        // -----------------------------------------------------------------

        [SetUp]
        public void SetUp()
        {
            _config = new MockBalanceConfig();
            _saveManager = new MockSaveManager();
            _eventManager = new EventManager();
            _prestigeSystem = new PrestigeSystem(_config, _saveManager, _eventManager);
        }

        [TearDown]
        public void TearDown()
        {
            _eventManager.Clear();
        }

        // -----------------------------------------------------------------
        // FP Hesaplama Dogrulama
        // Formul: floor( sqrt(ToplamKazanc / 1,000,000) x (1 + BonusCarpan) )
        // BonusCarpan = (5-yildiz tesis sayisi) x 0.1
        // Referans: ECONOMY_BALANCE.md Bolum 4.1
        // -----------------------------------------------------------------

        [Test]
        public void CalculateFP_25M_Earnings_Returns5()
        {
            // Arrange: 25,000,000 toplam kazanc, 0 adet 5-yildiz tesis
            _saveManager.Data.TotalEarnings = 25_000_000;

            // Act
            int fp = _prestigeSystem.CalculateFP();

            // Assert: floor( sqrt(25,000,000 / 1,000,000) x 1.0 )
            //       = floor( sqrt(25) x 1.0 ) = floor(5.0) = 5
            Assert.AreEqual(5, fp);
        }

        [Test]
        public void CalculateFP_1M_Earnings_Returns1()
        {
            // 1,000,000 kazanc
            _saveManager.Data.TotalEarnings = 1_000_000;

            int fp = _prestigeSystem.CalculateFP();

            // floor( sqrt(1,000,000 / 1,000,000) ) = floor(1.0) = 1
            Assert.AreEqual(1, fp);
        }

        [Test]
        public void CalculateFP_4M_Earnings_Returns2()
        {
            // 4,000,000 kazanc
            _saveManager.Data.TotalEarnings = 4_000_000;

            int fp = _prestigeSystem.CalculateFP();

            // floor( sqrt(4,000,000 / 1,000,000) ) = floor(sqrt(4)) = floor(2.0) = 2
            Assert.AreEqual(2, fp);
        }

        [Test]
        public void CalculateFP_100M_Earnings_Returns10()
        {
            // 100,000,000 kazanc
            _saveManager.Data.TotalEarnings = 100_000_000;

            int fp = _prestigeSystem.CalculateFP();

            // floor( sqrt(100) ) = floor(10.0) = 10
            Assert.AreEqual(10, fp);
        }

        [Test]
        public void CalculateFP_ZeroEarnings_ReturnsZero()
        {
            _saveManager.Data.TotalEarnings = 0;

            int fp = _prestigeSystem.CalculateFP();

            Assert.AreEqual(0, fp);
        }

        [Test]
        public void CalculateFP_BelowDivisor_ReturnsZero()
        {
            // 500,000 < 1,000,000 -> sqrt(0.5) = 0.707 -> floor(0.707) = 0
            _saveManager.Data.TotalEarnings = 500_000;

            int fp = _prestigeSystem.CalculateFP();

            Assert.AreEqual(0, fp);
        }

        // -----------------------------------------------------------------
        // Prestige Kosul Kontrolu
        // Kosul: ToplamKazanc >= 1,000,000
        // Referans: ECONOMY_BALANCE.md Bolum 4.1
        // -----------------------------------------------------------------

        [Test]
        public void CanPrestige_Below1M_ReturnsFalse()
        {
            // 999,999 < 1,000,000
            _saveManager.Data.TotalEarnings = 999_999;

            Assert.IsFalse(_prestigeSystem.CanPrestige(),
                "1M altinda prestige yapilamaz");
        }

        [Test]
        public void CanPrestige_Exactly1M_ReturnsTrue()
        {
            _saveManager.Data.TotalEarnings = 1_000_000;

            Assert.IsTrue(_prestigeSystem.CanPrestige(),
                "Tam 1M ile prestige yapilabilir");
        }

        [Test]
        public void CanPrestige_Above1M_ReturnsTrue()
        {
            _saveManager.Data.TotalEarnings = 5_000_000;

            Assert.IsTrue(_prestigeSystem.CanPrestige());
        }

        [Test]
        public void CanPrestige_ZeroEarnings_ReturnsFalse()
        {
            _saveManager.Data.TotalEarnings = 0;

            Assert.IsFalse(_prestigeSystem.CanPrestige());
        }

        // -----------------------------------------------------------------
        // Prestige Onizleme
        // -----------------------------------------------------------------

        [Test]
        public void GetPrestigePreview_ReturnsCorrectInfo()
        {
            // Arrange
            _saveManager.Data.TotalEarnings = 25_000_000;
            _saveManager.Data.FranchiseCount = 2;

            // Act
            var preview = _prestigeSystem.GetPrestigePreview();

            // Assert
            Assert.IsTrue(preview.CanPrestige);
            Assert.AreEqual(25_000_000, preview.CurrentTotalEarnings);
            Assert.AreEqual(1_000_000, preview.MinimumEarningsRequired);
            Assert.AreEqual(5, preview.EstimatedFP); // sqrt(25) = 5
            Assert.AreEqual(2, preview.CurrentFranchiseCount);
        }

        [Test]
        public void GetPrestigePreview_NotEligible_ShowsCorrectStatus()
        {
            // Arrange: Yetersiz kazanc
            _saveManager.Data.TotalEarnings = 100_000;

            // Act
            var preview = _prestigeSystem.GetPrestigePreview();

            // Assert
            Assert.IsFalse(preview.CanPrestige);
            Assert.AreEqual(0, preview.EstimatedFP);
        }

        // -----------------------------------------------------------------
        // FP Bonus Hesaplama Testi
        // -----------------------------------------------------------------

        [Test]
        public void GetBonusEffect_ProductionSpeed_CalculatesCorrectly()
        {
            // Arrange: Bonus bilgisini al (level 0 varsayilan)
            float effect = _prestigeSystem.GetBonusEffect(FranchiseBonusType.ProductionSpeed);

            // Assert: Level 0 -> effect = 0 x 0.10 = 0
            GameAssert.AreApproximatelyEqual(0f, effect);
        }

        [Test]
        public void GetBonusInfo_ProductionSpeed_ReturnsCorrectDefinition()
        {
            // Act
            var info = _prestigeSystem.GetBonusInfo(FranchiseBonusType.ProductionSpeed);

            // Assert: Tanim degerlerini dogrula
            Assert.IsNotNull(info);
            Assert.AreEqual(FranchiseBonusType.ProductionSpeed, info.Type);
            Assert.AreEqual(5, info.FPCostPerLevel, "Uretim Hizi 5 FP/seviye");
            Assert.AreEqual(20, info.MaxLevel, "Max 20 seviye");
            Assert.AreEqual(0, info.CurrentLevel, "Baslangic 0");
            Assert.IsFalse(info.IsMaxed);
        }

        [Test]
        public void GetBonusInfo_AllBonusTypes_ReturnValidInfo()
        {
            // Tum bonus turleri icin gecerli info donmeli
            var allInfos = _prestigeSystem.GetAllBonusInfos();

            Assert.IsNotNull(allInfos);
            Assert.AreEqual(6, allInfos.Count, "6 farkli bonus turu olmali");

            foreach (var info in allInfos)
            {
                Assert.IsNotNull(info);
                Assert.IsNotEmpty(info.Description);
                Assert.IsTrue(info.MaxLevel > 0, $"{info.Type} MaxLevel pozitif olmali");
                Assert.IsTrue(info.FPCostPerLevel > 0, $"{info.Type} FPCost pozitif olmali");
            }
        }

        // -----------------------------------------------------------------
        // FP Bonus Maliyet Dogrulama (Dokuman Degerleri)
        // Referans: ECONOMY_BALANCE.md Bolum 4.2
        // -----------------------------------------------------------------

        [Test]
        public void BonusDefinitions_MatchDocumentValues()
        {
            // Tum bonus turlerinin maliyet ve limit degerlerini dogrula
            var productionSpeed = _prestigeSystem.GetBonusInfo(FranchiseBonusType.ProductionSpeed);
            Assert.AreEqual(5, productionSpeed.FPCostPerLevel, "Uretim Hizi: 5 FP/seviye");
            Assert.AreEqual(20, productionSpeed.MaxLevel, "Uretim Hizi: max 20");

            var startingCoins = _prestigeSystem.GetBonusInfo(FranchiseBonusType.StartingCoins);
            Assert.AreEqual(3, startingCoins.FPCostPerLevel, "Baslangic Parasi: 3 FP/seviye");
            Assert.AreEqual(10, startingCoins.MaxLevel, "Baslangic Parasi: max 10");

            var offlineEarnings = _prestigeSystem.GetBonusInfo(FranchiseBonusType.OfflineEarnings);
            Assert.AreEqual(4, offlineEarnings.FPCostPerLevel, "Offline Kazanc: 4 FP/seviye");
            Assert.AreEqual(20, offlineEarnings.MaxLevel, "Offline Kazanc: max 20");

            var facilityReduction = _prestigeSystem.GetBonusInfo(FranchiseBonusType.FacilityCostReduction);
            Assert.AreEqual(6, facilityReduction.FPCostPerLevel, "Tesis Indirimi: 6 FP/seviye");
            Assert.AreEqual(8, facilityReduction.MaxLevel, "Tesis Indirimi: max 8");

            var criticalProd = _prestigeSystem.GetBonusInfo(FranchiseBonusType.CriticalProduction);
            Assert.AreEqual(8, criticalProd.FPCostPerLevel, "Kritik Uretim: 8 FP/seviye");
            Assert.AreEqual(10, criticalProd.MaxLevel, "Kritik Uretim: max 10");

            var specialWorker = _prestigeSystem.GetBonusInfo(FranchiseBonusType.SpecialWorker);
            Assert.AreEqual(15, specialWorker.FPCostPerLevel, "Ozel Calisan: 15 FP");
            Assert.AreEqual(1, specialWorker.MaxLevel, "Ozel Calisan: tek seferlik");
        }

        // -----------------------------------------------------------------
        // Constructor Dogrulama
        // -----------------------------------------------------------------

        [Test]
        public void Constructor_NullConfig_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                new PrestigeSystem(null, _saveManager, _eventManager);
            });
        }

        [Test]
        public void Constructor_NullSaveManager_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                new PrestigeSystem(_config, null, _eventManager);
            });
        }

        [Test]
        public void Constructor_NullEventManager_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                new PrestigeSystem(_config, _saveManager, null);
            });
        }

        // -----------------------------------------------------------------
        // FP Hesaplama — Ek Senaryolar
        // -----------------------------------------------------------------

        [Test]
        public void CalculateFP_VeryLargeEarnings_ReturnsReasonableValue()
        {
            // 1 milyar kazanc
            _saveManager.Data.TotalEarnings = 1_000_000_000;

            int fp = _prestigeSystem.CalculateFP();

            // floor( sqrt(1,000,000,000 / 1,000,000) ) = floor(sqrt(1000)) ≈ floor(31.62) = 31
            Assert.AreEqual(31, fp);
        }

        [Test]
        public void CalculateFP_ExactlyAtDivisor_Returns1()
        {
            // Tam divisor degerinde
            _saveManager.Data.TotalEarnings = 1_000_000;

            int fp = _prestigeSystem.CalculateFP();

            // floor( sqrt(1) ) = 1
            Assert.AreEqual(1, fp);
        }

        [Test]
        public void CalculateFP_NegativeEarnings_ReturnsZero()
        {
            // Negatif kazanc (olmasamali ama guvenlik icin)
            _saveManager.Data.TotalEarnings = -1_000_000;

            int fp = _prestigeSystem.CalculateFP();

            // sqrt(negatif) -> NaN -> Math.Max(0, ...) = 0
            Assert.AreEqual(0, fp);
        }
    }
}
