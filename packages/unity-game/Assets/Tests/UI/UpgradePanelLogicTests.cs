using System;
using System.Collections.Generic;
using NUnit.Framework;
using RiceFactory.Core;
using RiceFactory.Core.Events;
using RiceFactory.Data.Save;
using RiceFactory.Economy;
using RiceFactory.Production;
using UnityEngine;

namespace RiceFactory.Tests
{
    /// <summary>
    /// UpgradePanel'in logic kisimlari icin unit testler.
    /// MonoBehaviour bagimli olmayan: CanAfford kontrolu, maliyet artisi, stat degisimi.
    /// Buton aktif/pasif durumunu dogrudan CurrencySystem ve PriceCalculator uzerinden test eder.
    /// </summary>
    [TestFixture]
    public class UpgradePanelLogicTests
    {
        private MockBalanceConfig _balanceConfig;
        private MockSaveManager _saveManager;
        private EventManager _eventManager;
        private PriceCalculator _priceCalculator;
        private CurrencySystem _currencySystem;
        private FactoryData _factoryData;
        private FacilityState _facilityState;

        [SetUp]
        public void SetUp()
        {
            _balanceConfig = new MockBalanceConfig();
            _eventManager = new EventManager();
            _saveManager = new MockSaveManager();

            // Production testleri icin gerekli alanlari ayarla
            _saveManager.Data.Facilities = new List<FacilityState>();
            _saveManager.Data.FranchiseBonuses = new FranchiseBonuses();
            _saveManager.Data.Research = new ResearchData();

            _priceCalculator = new PriceCalculator(_balanceConfig);
            _currencySystem = new CurrencySystem(_saveManager, _eventManager);

            _factoryData = ScriptableObject.CreateInstance<FactoryData>();
            _factoryData.SetFromConfig(
                id: "rice_field", name: "Pirinc Tarlasi", desc: "Test",
                baseCost: 100f, baseProduction: 10f, basePrice: 5f,
                unlockCost: 1000f, order: 1,
                productionTime: 2f, secondaryTime: 0f, secondaryPrice: 0f);

            _facilityState = new FacilityState
            {
                Id = "test_facility",
                FacilityType = "rice_field",
                IsUnlocked = true,
                StarLevel = 1, MachineLevel = 1, WorkerLevel = 1,
                ActiveProductId = "rice", BaseOutputAmount = 1,
                TotalProductsSold = 0,
                UnlockedRecipes = new List<string>()
            };
        }

        [TearDown]
        public void TearDown()
        {
            if (_factoryData != null)
                UnityEngine.Object.DestroyImmediate(_factoryData);
        }

        private Factory CreateFactory()
        {
            return new Factory(_factoryData, _facilityState, _priceCalculator, _eventManager);
        }

        // =====================================================================
        // CAN AFFORD — BUTON AKTIF/PASIF DURUMU
        // UI'da buton rengi: yesil (#4CAF50) aktif, gri (#E0E0E0) pasif
        // =====================================================================

        [Test]
        public void CanAfford_WhenSufficientCoins_ButtonShouldBeActive()
        {
            // Upgrade maliyetini karsilayacak kadar para varken buton aktif olmali
            var factory = CreateFactory();
            var costs = factory.GetNextUpgradeCosts();
            _saveManager.Data.Coins = costs.MachineCost + 1;

            bool canAffordMachine = _currencySystem.CanAffordCoins(costs.MachineCost);

            Assert.IsTrue(canAffordMachine,
                "Yeterli para varken buton aktif (interactable=true) olmali.");
        }

        [Test]
        public void CanNotAfford_WhenInsufficientCoins_ButtonShouldBeDisabled()
        {
            // Upgrade maliyetinden az para varken buton pasif olmali
            var factory = CreateFactory();
            var costs = factory.GetNextUpgradeCosts();
            _saveManager.Data.Coins = costs.MachineCost * 0.5;

            bool canAffordMachine = _currencySystem.CanAffordCoins(costs.MachineCost);

            Assert.IsFalse(canAffordMachine,
                "Yetersiz para varken buton pasif (interactable=false) olmali.");
        }

        [Test]
        public void CanAfford_WorkerUpgrade_CorrectState()
        {
            // Tam maliyet kadar para — karsilayabilmeli
            var factory = CreateFactory();
            var costs = factory.GetNextUpgradeCosts();
            _saveManager.Data.Coins = costs.WorkerCost;

            bool canAfford = _currencySystem.CanAffordCoins(costs.WorkerCost);

            Assert.IsTrue(canAfford,
                "Tam maliyet kadar para varken buton aktif olmali.");
        }

        [Test]
        public void CanAfford_StarUpgrade_WhenZeroCoins_ReturnsFalse()
        {
            var factory = CreateFactory();
            var costs = factory.GetNextUpgradeCosts();
            _saveManager.Data.Coins = 0;

            bool canAfford = _currencySystem.CanAffordCoins(costs.StarCost);

            Assert.IsFalse(canAfford,
                "Sifir bakiye ile yildiz upgrade butonu pasif olmali.");
        }

        // =====================================================================
        // UPGRADE SONRASI MALIYET ARTISI DOGRULAMA
        // =====================================================================

        [Test]
        public void AfterMachineUpgrade_NextCostIncreases()
        {
            // Bir sonraki seviyenin maliyeti daha yuksek olmali
            _saveManager.Data.Coins = 10_000_000;
            var factory = CreateFactory();
            var costsBefore = factory.GetNextUpgradeCosts();

            factory.TryUpgradeMachine(_currencySystem);
            var costsAfter = factory.GetNextUpgradeCosts();

            Assert.Greater(costsAfter.MachineCost, costsBefore.MachineCost,
                "Makine upgrade sonrasi bir sonraki seviyenin maliyeti artmali.");
        }

        [Test]
        public void AfterWorkerUpgrade_NextCostIncreases()
        {
            _saveManager.Data.Coins = 10_000_000;
            var factory = CreateFactory();
            var costsBefore = factory.GetNextUpgradeCosts();

            factory.TryUpgradeWorker(_currencySystem);
            var costsAfter = factory.GetNextUpgradeCosts();

            Assert.Greater(costsAfter.WorkerCost, costsBefore.WorkerCost,
                "Calisan upgrade sonrasi bir sonraki seviyenin maliyeti artmali.");
        }

        [Test]
        public void MachineCostProgression_FollowsExponentialFormula()
        {
            // Formul: BaseCost x 5^(level-1)
            double costLv2 = _priceCalculator.CalculateUpgradeCost(100f, 2);
            double costLv3 = _priceCalculator.CalculateUpgradeCost(100f, 3);
            double costLv4 = _priceCalculator.CalculateUpgradeCost(100f, 4);

            Assert.AreEqual(500, costLv2, 0.01, "Lv2 maliyeti: 100 x 5^1 = 500");
            Assert.AreEqual(2500, costLv3, 0.01, "Lv3 maliyeti: 100 x 5^2 = 2500");
            Assert.AreEqual(12500, costLv4, 0.01, "Lv4 maliyeti: 100 x 5^3 = 12500");
        }

        [Test]
        public void WorkerCostProgression_FollowsPolynomialFormula()
        {
            // Formul: 50 x level^2.2
            double costLv2 = _priceCalculator.CalculateWorkerUpgradeCost(2);
            double costLv10 = _priceCalculator.CalculateWorkerUpgradeCost(10);

            double expectedLv2 = 50 * Math.Pow(2, 2.2);
            double expectedLv10 = 50 * Math.Pow(10, 2.2);
            Assert.AreEqual(expectedLv2, costLv2, 1.0, "Calisan Lv2 maliyeti: 50 x 2^2.2");
            Assert.AreEqual(expectedLv10, costLv10, 1.0, "Calisan Lv10 maliyeti: 50 x 10^2.2");
        }

        // =====================================================================
        // UPGRADE SONRASI STAT DEGISIMI DOGRULAMA
        // =====================================================================

        [Test]
        public void AfterMachineUpgrade_ProductionRateIncreases()
        {
            // Makine carpanlari: [1.0, 1.5, 2.2, 3.5, 5.0]
            // Lv1->Lv2: oran 1.5/1.0 = 1.5x artis
            _saveManager.Data.Coins = 10_000_000;
            var factory = CreateFactory();
            float rateBefore = factory.CurrentProductionRate;

            factory.TryUpgradeMachine(_currencySystem);

            float rateAfter = factory.CurrentProductionRate;
            float ratio = rateAfter / rateBefore;
            Assert.AreEqual(1.5f, ratio, 0.01f,
                "Makine Lv1->Lv2 uretim hizi 1.5x artmali.");
        }

        [Test]
        public void AfterWorkerUpgrade_ProductionRateIncreases()
        {
            // Calisan bonusu: 1 + (level x 0.02)
            _saveManager.Data.Coins = 10_000_000;
            var factory = CreateFactory();
            float rateBefore = factory.CurrentProductionRate;

            factory.TryUpgradeWorker(_currencySystem);

            float rateAfter = factory.CurrentProductionRate;
            Assert.Greater(rateAfter, rateBefore,
                "Calisan upgrade sonrasi uretim hizi artmali.");
        }

        [Test]
        public void AfterMachineUpgrade_RevenuePerMinuteIncreases()
        {
            _saveManager.Data.Coins = 10_000_000;
            var factory = CreateFactory();
            double revenueBefore = factory.CurrentRevenuePerMinute;

            factory.TryUpgradeMachine(_currencySystem);

            Assert.Greater(factory.CurrentRevenuePerMinute, revenueBefore,
                "Makine upgrade sonrasi gelir/dakika artmali.");
        }

        // =====================================================================
        // MAKS SEVIYE DURUMU
        // =====================================================================

        [Test]
        public void AtMaxMachineLevel_UpgradeButtonShouldBeDisabled()
        {
            // Maks makine seviyesinde maliyet -1 donmeli (UI buton deaktif)
            _facilityState.MachineLevel = 5;
            var factory = CreateFactory();
            var costs = factory.GetNextUpgradeCosts();

            Assert.AreEqual(-1, costs.MachineCost,
                "Maks makine seviyesinde maliyet -1 (buton deaktif) olmali.");
        }

        [Test]
        public void AtMaxWorkerLevel_UpgradeButtonShouldBeDisabled()
        {
            _facilityState.WorkerLevel = 50;
            var factory = CreateFactory();
            var costs = factory.GetNextUpgradeCosts();

            Assert.AreEqual(-1, costs.WorkerCost,
                "Maks calisan seviyesinde maliyet -1 (buton deaktif) olmali.");
        }
    }
}
