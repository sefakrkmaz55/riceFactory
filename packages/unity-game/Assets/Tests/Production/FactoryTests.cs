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
    /// Factory sinifi icin unit testler.
    /// Uretim dongusu, upgrade mekanikleri ve uretim hizi hesaplamalarini dogrular.
    /// </summary>
    [TestFixture]
    public class FactoryTests
    {
        // --- Test nesneleri ---
        private FactoryData _factoryData;
        private FacilityState _facilityState;
        private PriceCalculator _priceCalculator;
        private EventManager _eventManager;
        private CurrencySystem _currencySystem;
        private MockSaveManager _saveManager;
        private MockBalanceConfig _balanceConfig;

        [SetUp]
        public void SetUp()
        {
            // Temiz test ortami olustur
            _balanceConfig = new MockBalanceConfig();
            _eventManager = new EventManager();
            _saveManager = new MockSaveManager();

            // Production testleri icin gerekli alanlari ayarla
            _saveManager.Data.Facilities = new List<FacilityState>();
            _saveManager.Data.FranchiseBonuses = new FranchiseBonuses();
            _saveManager.Data.Research = new ResearchData();

            _priceCalculator = new PriceCalculator(_balanceConfig);
            _currencySystem = new CurrencySystem(_saveManager, _eventManager);

            // Test icin FactoryData olustur (ScriptableObject)
            _factoryData = ScriptableObject.CreateInstance<FactoryData>();
            _factoryData.SetFromConfig(
                id: "rice_field",
                name: "Pirinc Tarlasi",
                desc: "Test tarlasi",
                baseCost: 100f,
                baseProduction: 10f,
                basePrice: 5f,
                unlockCost: 1000f,
                order: 1,
                productionTime: 2f,     // 2 saniye uretim suresi
                secondaryTime: 0f,
                secondaryPrice: 0f
            );

            // Varsayilan FacilityState — acik, seviye 1
            _facilityState = new FacilityState
            {
                Id = "rice_field_test01",
                FacilityType = "rice_field",
                IsUnlocked = true,
                StarLevel = 1,
                MachineLevel = 1,
                WorkerLevel = 1,
                WorkerSpeedLevel = 0,
                WorkerQualityLevel = 0,
                WorkerCapacityLevel = 0,
                WorkerAutomationLevel = 0,
                ActiveProductIndex = 0,
                ActiveProductId = "rice",
                AutoSellEnabled = false,
                TotalProductsSold = 0,
                AverageQuality = 1,
                UnlockedRecipes = new List<string>(),
                BaseOutputAmount = 1
            };

            // Test oyuncusuna para ver
            _saveManager.Data.Coins = 100_000;
        }

        [TearDown]
        public void TearDown()
        {
            if (_factoryData != null)
                UnityEngine.Object.DestroyImmediate(_factoryData);
        }

        // =====================================================================
        // Yardimci — Factory olusturma
        // =====================================================================

        private Factory CreateFactory()
        {
            return new Factory(_factoryData, _facilityState, _priceCalculator, _eventManager);
        }

        // =====================================================================
        // URETIM DONGUSU TESTLERI
        // =====================================================================

        [Test]
        public void Tick_WhenCalledWithDeltaTime_ProductionProgressAdvances()
        {
            // Uretim dongusu baslatilir ve yari dongu kadar ilerletilir
            var factory = CreateFactory();

            float halfCycle = (1f / factory.CurrentProductionRate) * 0.5f;
            factory.Tick(halfCycle);

            // Ilerleme %50 civarinda olmali
            Assert.Greater(factory.ProductionProgress, 0.4f,
                "Tick sonrasi uretim ilerlemesi artmali.");
            Assert.Less(factory.ProductionProgress, 0.6f,
                "Yari dongu sonrasi ilerleme %50 civarinda olmali.");
        }

        [Test]
        public void Tick_WhenCycleCompletes_PublishesProductionCompletedEvent()
        {
            // Tam bir dongu tamamlaninca event firlamali
            var factory = CreateFactory();
            bool eventFired = false;
            string capturedFacilityId = null;

            _eventManager.Subscribe<ProductionCompletedEvent>(e =>
            {
                eventFired = true;
                capturedFacilityId = e.FacilityId;
            });

            float fullCycle = 1f / factory.CurrentProductionRate;
            factory.Tick(fullCycle + 0.01f);

            Assert.IsTrue(eventFired, "Uretim dongusu tamamlaninca event firlamali.");
            Assert.AreEqual(_facilityState.Id, capturedFacilityId,
                "Event'teki FacilityId, fabrikanin ID'si ile ayni olmali.");
        }

        [Test]
        public void Tick_WhenFactoryIsNotUnlocked_DoesNotProgress()
        {
            // Kilitli fabrikada uretim ilerlemesi olmamali
            _facilityState.IsUnlocked = false;
            var factory = CreateFactory();

            factory.Tick(10f);

            Assert.AreEqual(0f, factory.ProductionProgress,
                "Kilitli fabrikada uretim ilerlemesi olmamali.");
        }

        [Test]
        public void Tick_WhenDeactivated_DoesNotProgress()
        {
            // Deaktif fabrikada uretim durmali
            var factory = CreateFactory();
            factory.Deactivate();

            factory.Tick(10f);

            Assert.AreEqual(0f, factory.ProductionProgress,
                "Deaktif fabrikada uretim ilerlemesi olmamali.");
        }

        [Test]
        public void Tick_WhenMultipleCyclesFit_FiresMultipleEvents()
        {
            // Offline catch-up: birden fazla dongu suresi icerisinde birden fazla event
            var factory = CreateFactory();
            int eventCount = 0;

            _eventManager.Subscribe<ProductionCompletedEvent>(e => eventCount++);

            float cycleDuration = 1f / factory.CurrentProductionRate;
            factory.Tick(cycleDuration * 3f + 0.01f);

            Assert.GreaterOrEqual(eventCount, 3,
                "Birden fazla dongu suresi gecirildiginde birden fazla event firlamali (offline catch-up).");
        }

        // =====================================================================
        // MAKINE UPGRADE TESTLERI
        // =====================================================================

        [Test]
        public void TryUpgradeMachine_WhenAffordable_IncreasesLevel()
        {
            // Yeterli para varken makine seviyesi artmali
            var factory = CreateFactory();
            int initialLevel = factory.MachineLevel;

            bool result = factory.TryUpgradeMachine(_currencySystem);

            Assert.IsTrue(result, "Yeterli para varken upgrade basarili olmali.");
            Assert.AreEqual(initialLevel + 1, factory.MachineLevel,
                "Makine seviyesi 1 artmali.");
        }

        [Test]
        public void TryUpgradeMachine_CostCalculation_MatchesFormula()
        {
            // Makine Lv.1 -> Lv.2 maliyeti: BaseCost x 5^(2-1) = 100 x 5 = 500
            var factory = CreateFactory();
            double initialCoins = _saveManager.Data.Coins;

            factory.TryUpgradeMachine(_currencySystem);

            double expectedCost = 100.0 * Math.Pow(5, 1); // 500
            double spent = initialCoins - _saveManager.Data.Coins;
            Assert.AreEqual(expectedCost, spent, 0.01,
                "Makine upgrade maliyeti BaseCost x 5^(level-1) formulune uymali.");
        }

        [Test]
        public void TryUpgradeMachine_WhenNotAffordable_ReturnsFalse()
        {
            // Yetersiz bakiye ile upgrade reddedilmeli
            _saveManager.Data.Coins = 1;
            var factory = CreateFactory();

            bool result = factory.TryUpgradeMachine(_currencySystem);

            Assert.IsFalse(result, "Yetersiz para ile upgrade reddedilmeli.");
            Assert.AreEqual(1, factory.MachineLevel,
                "Basarisiz upgrade'de seviye degismemeli.");
        }

        [Test]
        public void TryUpgradeMachine_WhenAtMaxLevel_ReturnsFalse()
        {
            // Maks seviye (5) iken upgrade yapilamamali
            _facilityState.MachineLevel = 5;
            var factory = CreateFactory();

            bool result = factory.TryUpgradeMachine(_currencySystem);

            Assert.IsFalse(result, "Maksimum seviyede upgrade reddedilmeli.");
        }

        [Test]
        public void TryUpgradeMachine_PublishesUpgradeCompletedEvent()
        {
            // Upgrade sonrasi event firlamali
            var factory = CreateFactory();
            UpgradeCompletedEvent? capturedEvent = null;

            _eventManager.Subscribe<UpgradeCompletedEvent>(e => capturedEvent = e);
            factory.TryUpgradeMachine(_currencySystem);

            Assert.IsNotNull(capturedEvent, "Upgrade sonrasi UpgradeCompletedEvent firlamali.");
            Assert.AreEqual(UpgradeType.Machine, capturedEvent.Value.Type);
            Assert.AreEqual(2, capturedEvent.Value.NewLevel);
        }

        // =====================================================================
        // CALISAN UPGRADE TESTLERI
        // =====================================================================

        [Test]
        public void TryUpgradeWorker_WhenAffordable_IncreasesLevel()
        {
            var factory = CreateFactory();

            bool result = factory.TryUpgradeWorker(_currencySystem);

            Assert.IsTrue(result, "Yeterli para varken calisan upgrade basarili olmali.");
            Assert.AreEqual(2, factory.WorkerLevel, "Calisan seviyesi 2 olmali.");
        }

        [Test]
        public void TryUpgradeWorker_WhenNotAffordable_ReturnsFalse()
        {
            _saveManager.Data.Coins = 0;
            var factory = CreateFactory();

            bool result = factory.TryUpgradeWorker(_currencySystem);

            Assert.IsFalse(result, "Yetersiz para ile calisan upgrade reddedilmeli.");
            Assert.AreEqual(1, factory.WorkerLevel, "Seviye degismemeli.");
        }

        [Test]
        public void TryUpgradeWorker_WhenAtMaxLevel_ReturnsFalse()
        {
            // Maks calisan seviyesi (50) iken upgrade yapilamamali
            _facilityState.WorkerLevel = 50;
            var factory = CreateFactory();

            bool result = factory.TryUpgradeWorker(_currencySystem);

            Assert.IsFalse(result, "Maks seviyede calisan upgrade reddedilmeli.");
        }

        // =====================================================================
        // YILDIZ UPGRADE TESTLERI
        // =====================================================================

        [Test]
        public void TryUpgradeStar_WhenRequirementsMet_IncreasesStarLevel()
        {
            // Yildiz 2 icin: makine Lv.2+ ve 500 satis gerekli
            _facilityState.MachineLevel = 2;
            _facilityState.TotalProductsSold = 600;
            _saveManager.Data.Coins = 10_000_000;
            var factory = CreateFactory();

            bool result = factory.TryUpgradeStar(_currencySystem);

            Assert.IsTrue(result, "Gereksinimler karsilandiginda yildiz upgrade basarili olmali.");
            Assert.AreEqual(2, factory.StarLevel, "Yildiz seviyesi 2 olmali.");
        }

        [Test]
        public void TryUpgradeStar_WhenMachineLevelInsufficient_ReturnsFalse()
        {
            // Makine seviyesi yetersiz (yildiz 2 icin Lv.2 gerekli)
            _facilityState.MachineLevel = 1;
            _facilityState.TotalProductsSold = 1000;
            var factory = CreateFactory();

            bool result = factory.TryUpgradeStar(_currencySystem);

            Assert.IsFalse(result,
                "Makine seviyesi yetersizken yildiz upgrade reddedilmeli.");
        }

        [Test]
        public void TryUpgradeStar_WhenSalesInsufficient_ReturnsFalse()
        {
            // Satis sayisi yetersiz (yildiz 2 icin 500 satis gerekli)
            _facilityState.MachineLevel = 2;
            _facilityState.TotalProductsSold = 100;
            var factory = CreateFactory();

            bool result = factory.TryUpgradeStar(_currencySystem);

            Assert.IsFalse(result,
                "Satis sayisi yetersizken yildiz upgrade reddedilmeli.");
        }

        [Test]
        public void TryUpgradeStar_WhenAtMaxStar_ReturnsFalse()
        {
            // Maks yildiz (5) iken upgrade yapilamamali
            _facilityState.StarLevel = 5;
            _facilityState.MachineLevel = 5;
            _facilityState.TotalProductsSold = 1_000_000;
            var factory = CreateFactory();

            bool result = factory.TryUpgradeStar(_currencySystem);

            Assert.IsFalse(result, "Maks yildizda upgrade reddedilmeli.");
        }

        // =====================================================================
        // URETIM HIZI DEGISIMI TESTLERI
        // =====================================================================

        [Test]
        public void RecalculateProductionStats_AfterMachineUpgrade_RateIncreases()
        {
            var factory = CreateFactory();
            float rateBefore = factory.CurrentProductionRate;

            factory.TryUpgradeMachine(_currencySystem);

            Assert.Greater(factory.CurrentProductionRate, rateBefore,
                "Makine upgrade sonrasi uretim hizi artmali.");
        }

        [Test]
        public void RecalculateProductionStats_AfterWorkerUpgrade_RateIncreases()
        {
            var factory = CreateFactory();
            float rateBefore = factory.CurrentProductionRate;

            factory.TryUpgradeWorker(_currencySystem);

            Assert.Greater(factory.CurrentProductionRate, rateBefore,
                "Calisan upgrade sonrasi uretim hizi artmali.");
        }

        [Test]
        public void ProductionRate_MatchesExpectedFormula_AtLevel1()
        {
            // Formul: (1 / baseProductionTime) x machineMultiplier x workerBonus x starBonus
            // Lv.1: (1/2) x 1.0 x (1 + 1*0.02) x (1 + 0.0) = 0.51
            var factory = CreateFactory();

            float expected = (1f / 2f) * 1.0f * (1f + 1 * 0.02f) * 1.0f;
            Assert.AreEqual(expected, factory.CurrentProductionRate, 0.001f,
                "Seviye 1'de uretim hizi beklenen formulle uyusmali.");
        }

        [Test]
        public void CurrentRevenuePerMinute_CalculatedCorrectly()
        {
            // Gelir/dakika = basePrice x productionRate x 60
            var factory = CreateFactory();

            double expected = _factoryData.BasePrice * factory.CurrentProductionRate * 60.0;
            Assert.AreEqual(expected, factory.CurrentRevenuePerMinute, 0.01,
                "Gelir/dakika = basePrice x uretimHizi x 60 olmali.");
        }

        // =====================================================================
        // UPGRADE MALIYETLERI UI YARDIMCISI
        // =====================================================================

        [Test]
        public void GetNextUpgradeCosts_ReturnsValidCosts()
        {
            var factory = CreateFactory();
            var costs = factory.GetNextUpgradeCosts();

            Assert.Greater(costs.MachineCost, 0, "Makine maliyeti pozitif olmali.");
            Assert.Greater(costs.WorkerCost, 0, "Calisan maliyeti pozitif olmali.");
            Assert.Greater(costs.StarCost, 0, "Yildiz maliyeti pozitif olmali.");
        }

        [Test]
        public void GetNextUpgradeCosts_AtMaxLevel_ReturnsNegativeOne()
        {
            // Tum seviyeler maks — -1 = max seviye
            _facilityState.MachineLevel = 5;
            _facilityState.WorkerLevel = 50;
            _facilityState.StarLevel = 5;
            var factory = CreateFactory();

            var costs = factory.GetNextUpgradeCosts();

            Assert.AreEqual(-1, costs.MachineCost, "Maks makine seviyesinde -1 donmeli.");
            Assert.AreEqual(-1, costs.WorkerCost, "Maks calisan seviyesinde -1 donmeli.");
            Assert.AreEqual(-1, costs.StarCost, "Maks yildiz seviyesinde -1 donmeli.");
        }

        // =====================================================================
        // DURUM YONETIMI
        // =====================================================================

        [Test]
        public void Activate_WhenUnlocked_SetsIsActiveTrue()
        {
            var factory = CreateFactory();
            factory.Deactivate();
            Assert.IsFalse(factory.IsActive);

            factory.Activate();

            Assert.IsTrue(factory.IsActive, "Acik tesis aktif edilebilmeli.");
        }

        [Test]
        public void Activate_WhenLocked_DoesNotActivate()
        {
            _facilityState.IsUnlocked = false;
            var factory = CreateFactory();

            factory.Activate();

            Assert.IsFalse(factory.IsActive,
                "Kilitli tesis aktif edilememeli.");
        }
    }
}
