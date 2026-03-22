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
    /// ProductionManager sinifi icin unit testler.
    /// Fabrika olusturma/acma, toplam uretim/gelir hesaplama ve offline uretim testleri.
    /// </summary>
    [TestFixture]
    public class ProductionManagerTests
    {
        private MockBalanceConfig _balanceConfig;
        private MockSaveManager _saveManager;
        private EventManager _eventManager;
        private PriceCalculator _priceCalculator;
        private CurrencySystem _currencySystem;
        private ProductionManager _productionManager;
        private FactoryData _riceFieldData;
        private FactoryData _factoryData;

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
            _saveManager.Data.Coins = 10_000_000;

            _priceCalculator = new PriceCalculator(_balanceConfig);
            _currencySystem = new CurrencySystem(_saveManager, _eventManager);

            _productionManager = new ProductionManager(
                _balanceConfig, _saveManager, _eventManager,
                _priceCalculator, _currencySystem);

            // FactoryData'lar olustur
            _riceFieldData = ScriptableObject.CreateInstance<FactoryData>();
            _riceFieldData.SetFromConfig(
                id: "rice_field", name: "Pirinc Tarlasi", desc: "Test",
                baseCost: 100f, baseProduction: 10f, basePrice: 5f,
                unlockCost: 0f, order: 1,
                productionTime: 2f, secondaryTime: 0f, secondaryPrice: 0f);

            _factoryData = ScriptableObject.CreateInstance<FactoryData>();
            _factoryData.SetFromConfig(
                id: "factory", name: "Pirinc Fabrikasi", desc: "Test",
                baseCost: 500f, baseProduction: 8f, basePrice: 15f,
                unlockCost: 1000f, order: 2,
                productionTime: 3f, secondaryTime: 0f, secondaryPrice: 0f);
        }

        [TearDown]
        public void TearDown()
        {
            _productionManager?.Dispose();
            if (_riceFieldData != null) UnityEngine.Object.DestroyImmediate(_riceFieldData);
            if (_factoryData != null) UnityEngine.Object.DestroyImmediate(_factoryData);
        }

        /// <summary>Yardimci — Initialize ile FactoryData'lari yukler.</summary>
        private void InitializeManager()
        {
            _productionManager.Initialize(new List<FactoryData> { _riceFieldData, _factoryData });
        }

        // =====================================================================
        // FABRIKA OLUSTURMA TESTLERI
        // =====================================================================

        [Test]
        public void Initialize_WithSavedFacilities_CreatesFactoryInstances()
        {
            // Kayitli bir tesis varsa Initialize sonrasi Factory olusturulmali
            var savedState = new FacilityState
            {
                Id = "rice_field_saved01",
                FacilityType = "rice_field",
                IsUnlocked = true,
                StarLevel = 1, MachineLevel = 1, WorkerLevel = 1,
                ActiveProductId = "rice", BaseOutputAmount = 1,
                UnlockedRecipes = new List<string>()
            };
            _saveManager.Data.Facilities.Add(savedState);

            InitializeManager();

            Assert.AreEqual(1, _productionManager.Factories.Count,
                "Kayitli acik tesis sayisi kadar Factory olusturulmali.");
        }

        // =====================================================================
        // FABRIKA ACMA (UNLOCK) TESTLERI
        // =====================================================================

        [Test]
        public void UnlockFactory_WhenAffordable_CreatesNewFactory()
        {
            InitializeManager();

            var factory = _productionManager.UnlockFactory("factory");

            Assert.IsNotNull(factory, "Yeterli para ile tesis acilmali.");
            Assert.AreEqual("factory", factory.Data.FactoryId);
            Assert.IsTrue(factory.IsUnlocked);
            Assert.IsTrue(factory.IsActive);
        }

        [Test]
        public void UnlockFactory_FreeFactory_DoesNotDeductCoins()
        {
            // Ucretsiz tesis (pirinc tarlasi, unlockCost=0) para kesmemeli
            InitializeManager();
            double coinsBefore = _saveManager.Data.Coins;

            var factory = _productionManager.UnlockFactory("rice_field");

            Assert.IsNotNull(factory, "Ucretsiz tesis acilmali.");
            Assert.AreEqual(coinsBefore, _saveManager.Data.Coins,
                "Ucretsiz tesis acarken para kesilmemeli.");
        }

        [Test]
        public void UnlockFactory_WhenNotAffordable_ReturnsNull()
        {
            // Yetersiz bakiye ile tesis acilamamali
            _saveManager.Data.Coins = 10;
            InitializeManager();

            var factory = _productionManager.UnlockFactory("factory");

            Assert.IsNull(factory, "Yetersiz para ile tesis acilamamali.");
        }

        [Test]
        public void UnlockFactory_AlreadyUnlocked_ReturnsNull()
        {
            // Ayni tesis iki kere acilamamali
            InitializeManager();
            _productionManager.UnlockFactory("factory");

            var secondAttempt = _productionManager.UnlockFactory("factory");

            Assert.IsNull(secondAttempt, "Zaten acik tesis tekrar acilamamali.");
        }

        [Test]
        public void UnlockFactory_InvalidId_ReturnsNull()
        {
            InitializeManager();

            var factory = _productionManager.UnlockFactory("nonexistent_factory");

            Assert.IsNull(factory, "Gecersiz factory ID ile tesis acilamamali.");
        }

        // =====================================================================
        // TOPLAM URETIM HIZI HESAPLAMA
        // =====================================================================

        [Test]
        public void GetTotalProductionRate_WithMultipleFactories_SumsAll()
        {
            // Iki tesis toplam uretim hizi = her ikisinin toplami
            InitializeManager();
            var f1 = _productionManager.UnlockFactory("rice_field");
            var f2 = _productionManager.UnlockFactory("factory");

            float totalRate = _productionManager.GetTotalProductionRate();

            float expectedTotal = f1.CurrentProductionRate + f2.CurrentProductionRate;
            Assert.AreEqual(expectedTotal, totalRate, 0.001f,
                "Toplam uretim hizi tum aktif fabrikalarin toplami olmali.");
        }

        [Test]
        public void GetTotalProductionRate_WithNoFactories_ReturnsZero()
        {
            InitializeManager();

            float totalRate = _productionManager.GetTotalProductionRate();

            Assert.AreEqual(0f, totalRate, "Fabrika yokken toplam hiz sifir olmali.");
        }

        // =====================================================================
        // TOPLAM GELIR HESAPLAMA
        // =====================================================================

        [Test]
        public void GetTotalRevenuePerMinute_WithMultipleFactories_SumsAll()
        {
            InitializeManager();
            var f1 = _productionManager.UnlockFactory("rice_field");
            var f2 = _productionManager.UnlockFactory("factory");

            double totalRevenue = _productionManager.GetTotalRevenuePerMinute();

            double expectedRevenue = f1.CurrentRevenuePerMinute + f2.CurrentRevenuePerMinute;
            Assert.AreEqual(expectedRevenue, totalRevenue, 0.01,
                "Toplam gelir/dakika tum aktif fabrikalarin toplami olmali.");
        }

        [Test]
        public void GetTotalRevenuePerHour_IsRevenuePerMinuteTimes60()
        {
            InitializeManager();
            _productionManager.UnlockFactory("rice_field");

            double perMinute = _productionManager.GetTotalRevenuePerMinute();
            double perHour = _productionManager.GetTotalRevenuePerHour();

            Assert.AreEqual(perMinute * 60.0, perHour, 0.01,
                "Gelir/saat = Gelir/dakika x 60 olmali.");
        }

        // =====================================================================
        // OFFLINE URETIM HESAPLAMA
        // =====================================================================

        [Test]
        public void CalculateOfflineProduction_2Hours_ReturnsPositiveEarnings()
        {
            // 2 saat offline sonrasi kazanc pozitif olmali
            InitializeManager();
            _productionManager.UnlockFactory("rice_field");

            var result = _productionManager.CalculateOfflineProduction(TimeSpan.FromHours(2));

            Assert.Greater(result.TotalCoins, 0, "2 saat offline sonrasi kazanc pozitif olmali.");
            Assert.Greater(result.TotalProducts, 0, "2 saat offline sonrasi urun sayisi pozitif olmali.");
            Assert.IsFalse(result.WasCapped, "2 saat sinir icinde — cap uygulanmamali.");
        }

        [Test]
        public void CalculateOfflineProduction_8Hours_EarningsHigherThan2Hours()
        {
            // 8 saat > 2 saat kazanc
            InitializeManager();
            _productionManager.UnlockFactory("rice_field");

            var result2h = _productionManager.CalculateOfflineProduction(TimeSpan.FromHours(2));
            var result8h = _productionManager.CalculateOfflineProduction(TimeSpan.FromHours(8));

            Assert.Greater(result8h.TotalCoins, result2h.TotalCoins,
                "8 saat offline kazanc 2 saatten buyuk olmali.");
        }

        [Test]
        public void CalculateOfflineProduction_12Hours_Free_CappedAt8Hours()
        {
            // Ucretsiz kullanici icin 12 saat offline, 8 saatte kesilmeli
            _saveManager.Data.HasBattlePass = false;
            InitializeManager();
            _productionManager.UnlockFactory("rice_field");

            var result8h = _productionManager.CalculateOfflineProduction(TimeSpan.FromHours(8));
            var result12h = _productionManager.CalculateOfflineProduction(TimeSpan.FromHours(12));

            Assert.AreEqual(result8h.TotalCoins, result12h.TotalCoins, 0.01,
                "Ucretsiz kullanici icin 12 saat offline, 8 saatte kesilmeli.");
            Assert.IsTrue(result12h.WasCapped,
                "12 saat suresi siniri asmali (WasCapped=true).");
            Assert.AreEqual(TimeSpan.FromHours(8), result12h.Duration,
                "Hesaplanan sure 8 saat olmali.");
        }

        [Test]
        public void CalculateOfflineProduction_12Hours_Premium_NotCapped()
        {
            // Premium kullanici icin 12 saat sinir icinde
            _saveManager.Data.HasBattlePass = true;
            InitializeManager();
            _productionManager.UnlockFactory("rice_field");

            var result = _productionManager.CalculateOfflineProduction(TimeSpan.FromHours(12));

            Assert.IsFalse(result.WasCapped,
                "Premium kullanici icin 12 saat sinir icinde olmali.");
            Assert.AreEqual(TimeSpan.FromHours(12), result.Duration,
                "Premium kullanici icin tam 12 saat hesaplanmali.");
        }

        [Test]
        public void CalculateOfflineProduction_16Hours_Premium_CappedAt12Hours()
        {
            // Premium kullanici bile 12 saatte kesilir
            _saveManager.Data.HasBattlePass = true;
            InitializeManager();
            _productionManager.UnlockFactory("rice_field");

            var result12h = _productionManager.CalculateOfflineProduction(TimeSpan.FromHours(12));
            var result16h = _productionManager.CalculateOfflineProduction(TimeSpan.FromHours(16));

            Assert.AreEqual(result12h.TotalCoins, result16h.TotalCoins, 0.01,
                "Premium kullanici icin 16 saat, 12 saatte kesilmeli.");
            Assert.IsTrue(result16h.WasCapped);
        }

        [Test]
        public void CalculateOfflineProduction_BaseEfficiency_Is30Percent()
        {
            // Varsayilan offline verimlilik %30
            InitializeManager();
            _productionManager.UnlockFactory("rice_field");

            var result = _productionManager.CalculateOfflineProduction(TimeSpan.FromHours(1));

            Assert.AreEqual(0.30f, result.Efficiency, 0.001f,
                "Baz offline verimlilik %30 olmali (bonus yok).");
        }

        // =====================================================================
        // FABRIKA ERISIM YARDIMCILARI
        // =====================================================================

        [Test]
        public void GetFactory_WithValidId_ReturnsFactory()
        {
            InitializeManager();
            var created = _productionManager.UnlockFactory("rice_field");

            var found = _productionManager.GetFactory(created.State.Id);

            Assert.IsNotNull(found);
            Assert.AreEqual(created.FactoryId, found.FactoryId);
        }

        [Test]
        public void GetFactoryByType_ReturnsCorrectFactory()
        {
            InitializeManager();
            _productionManager.UnlockFactory("rice_field");

            var found = _productionManager.GetFactoryByType("rice_field");

            Assert.IsNotNull(found);
            Assert.AreEqual("rice_field", found.Data.FactoryId);
        }

        [Test]
        public void ActiveFactoryCount_ReflectsActiveFactories()
        {
            InitializeManager();
            _productionManager.UnlockFactory("rice_field");
            _productionManager.UnlockFactory("factory");

            Assert.AreEqual(2, _productionManager.ActiveFactoryCount,
                "2 acik tesis = 2 aktif fabrika.");
        }
    }
}
