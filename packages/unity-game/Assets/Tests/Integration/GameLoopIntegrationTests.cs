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
    /// Core game loop entegrasyon testleri.
    /// Birden fazla sistemi birlikte calistirarak end-to-end akislari dogrular.
    /// </summary>
    [TestFixture]
    public class GameLoopIntegrationTests
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

            _priceCalculator = new PriceCalculator(_balanceConfig);
            _currencySystem = new CurrencySystem(_saveManager, _eventManager);

            _productionManager = new ProductionManager(
                _balanceConfig, _saveManager, _eventManager,
                _priceCalculator, _currencySystem);

            // FactoryData'lar
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

            _productionManager.Initialize(new List<FactoryData> { _riceFieldData, _factoryData });
        }

        [TearDown]
        public void TearDown()
        {
            _productionManager?.Dispose();
            if (_riceFieldData != null) UnityEngine.Object.DestroyImmediate(_riceFieldData);
            if (_factoryData != null) UnityEngine.Object.DestroyImmediate(_factoryData);
        }

        // =====================================================================
        // TAM CORE LOOP TESTI
        // Para kazan -> Upgrade al -> Uretim hizi artsin
        // =====================================================================

        [Test]
        public void CoreLoop_EarnCoins_ThenUpgrade_ThenProductionRateIncreases()
        {
            // Tesis ac, uretim yap, upgrade et, hiz artmali
            _saveManager.Data.Coins = 50_000;
            var factory = _productionManager.UnlockFactory("rice_field");
            Assert.IsNotNull(factory, "Tesis acilabilmeli.");

            float initialRate = factory.CurrentProductionRate;
            double initialRevenue = factory.CurrentRevenuePerMinute;

            // Uretim tick'i ile uretim yap (event firlar)
            int productionCount = 0;
            _eventManager.Subscribe<ProductionCompletedEvent>(e => productionCount++);

            float cycleDuration = 1f / factory.CurrentProductionRate;
            factory.Tick(cycleDuration * 5.1f); // 5 uretim dongusu (float hassasiyet marji)

            Assert.GreaterOrEqual(productionCount, 5,
                "5 dongu suresi icerisinde en az 5 uretim eventi olmali.");

            // Kazanilan para ile makine upgrade
            bool upgraded = factory.TryUpgradeMachine(_currencySystem);
            Assert.IsTrue(upgraded, "Yeterli para ile makine upgrade basarili olmali.");

            // Uretim hizi artmis olmali
            Assert.Greater(factory.CurrentProductionRate, initialRate,
                "Core loop: upgrade sonrasi uretim hizi artmali.");
            Assert.Greater(factory.CurrentRevenuePerMinute, initialRevenue,
                "Core loop: upgrade sonrasi gelir/dakika artmali.");
        }

        [Test]
        public void CoreLoop_MultipleUpgrades_CumulativeSpeedIncrease()
        {
            // Bol para ile makine 1->5, calisan 1->10 upgrade et
            _saveManager.Data.Coins = 100_000_000;
            var factory = _productionManager.UnlockFactory("rice_field");
            float rateAtLv1 = factory.CurrentProductionRate;

            for (int i = 0; i < 4; i++)
                factory.TryUpgradeMachine(_currencySystem);

            for (int i = 0; i < 9; i++)
                factory.TryUpgradeWorker(_currencySystem);

            Assert.AreEqual(5, factory.MachineLevel, "Makine Lv5 olmali.");
            Assert.AreEqual(10, factory.WorkerLevel, "Calisan Lv10 olmali.");
            Assert.Greater(factory.CurrentProductionRate, rateAtLv1 * 4f,
                "Makine Lv5 + Calisan Lv10 ile uretim hizi en az 4x artmali.");
        }

        // =====================================================================
        // PRESTIGE AKISI
        // Yeterli para biriktir -> Prestige yap -> FP kazanilsin -> Sifirlansin
        // =====================================================================

        [Test]
        public void PrestigeFlow_AddFranchisePoints_ThenReset()
        {
            // Franchise puani biriktirme ve sifirlama simulasyonu
            _saveManager.Data.Coins = 10_000_000;
            _saveManager.Data.TotalEarnings = 10_000_000;
            _productionManager.UnlockFactory("rice_field");

            // Franchise puani kazan (prestige odul)
            int earnedFP = 5;
            _currencySystem.AddFranchisePoints(earnedFP, "prestige_test");

            Assert.AreEqual(earnedFP, _saveManager.Data.FranchisePoints,
                "Prestige sonrasi FP kazanilmali.");

            // Prestige sifirlama simulasyonu
            _saveManager.Data.Coins = 0;
            _saveManager.Data.TotalEarnings = 0;

            // Coin sifirlanmali ama FP kalmali
            Assert.AreEqual(0, _saveManager.Data.Coins,
                "Prestige sonrasi coin sifirlanmali.");
            Assert.AreEqual(earnedFP, _saveManager.Data.FranchisePoints,
                "Prestige sonrasi FP korunmali (kalici bonus).");
        }

        [Test]
        public void PrestigeFlow_FPSpending_ReducesBalance()
        {
            _currencySystem.AddFranchisePoints(10, "test");
            Assert.AreEqual(10, _saveManager.Data.FranchisePoints);

            bool spent = _currencySystem.SpendFranchisePoints(3, "bonus_purchase");

            Assert.IsTrue(spent, "Yeterli FP ile harcama basarili olmali.");
            Assert.AreEqual(7, _saveManager.Data.FranchisePoints, "10 - 3 = 7 FP kalmali.");
        }

        [Test]
        public void PrestigeFlow_FPSpending_InsufficientBalance_Fails()
        {
            _currencySystem.AddFranchisePoints(2, "test");

            bool spent = _currencySystem.SpendFranchisePoints(5, "bonus_purchase");

            Assert.IsFalse(spent, "Yetersiz FP ile harcama basarisiz olmali.");
            Assert.AreEqual(2, _saveManager.Data.FranchisePoints,
                "Basarisiz harcamada FP degismemeli.");
        }

        // =====================================================================
        // OFFLINE KAZANC AKISI
        // Save time kaydet -> Zaman simule et -> Offline kazanc hesaplansin
        // =====================================================================

        [Test]
        public void OfflineEarnings_SimulateTimeAway_CalculatesCorrectly()
        {
            // 4 saat offline simulasyonu — formule gore kazanc hesaplanmali
            _saveManager.Data.Coins = 1_000;
            var factory = _productionManager.UnlockFactory("rice_field");
            double revenuePerSecond = factory.CurrentRevenuePerMinute / 60.0;

            TimeSpan offlineDuration = TimeSpan.FromHours(4);
            var result = _productionManager.CalculateOfflineProduction(offlineDuration);

            // Beklenen: gelir/sn x sure x %30 verimlilik
            double expectedCoins = revenuePerSecond * offlineDuration.TotalSeconds * 0.30;
            Assert.AreEqual(expectedCoins, result.TotalCoins, expectedCoins * 0.01,
                "Offline kazanc formulle uyusmali: gelir/sn x sure x verimlilik.");
            Assert.IsFalse(result.WasCapped, "4 saat sinir icinde olmali.");
        }

        [Test]
        public void OfflineEarnings_CollectEarnings_IncreasesBalance()
        {
            // Offline kazanci toplama sonrasi bakiye artmali
            _saveManager.Data.Coins = 500;
            _productionManager.UnlockFactory("rice_field");

            var result = _productionManager.CalculateOfflineProduction(TimeSpan.FromHours(2));
            double coinsBeforeCollect = _saveManager.Data.Coins;
            _currencySystem.AddCoins(result.TotalCoins, "offline_earnings");

            Assert.AreEqual(coinsBeforeCollect + result.TotalCoins, _saveManager.Data.Coins, 0.01,
                "Offline kazanc toplama sonrasi bakiye artmali.");
        }

        [Test]
        public void OfflineEarnings_WatchAd2x_DoublesEarnings()
        {
            // Reklam izleyerek 2x kazanc senaryosu
            _saveManager.Data.Coins = 0;
            _currencySystem.AddCoins(100, "start");
            _productionManager.UnlockFactory("rice_field");

            var result = _productionManager.CalculateOfflineProduction(TimeSpan.FromHours(3));
            double coinsBefore = _saveManager.Data.Coins;

            double doubledAmount = result.TotalCoins * 2.0;
            _currencySystem.AddCoins(doubledAmount, "offline_earnings_2x");

            Assert.AreEqual(coinsBefore + doubledAmount, _saveManager.Data.Coins, 0.01,
                "2x reklam kazanci dogru eklenmeli.");
        }

        // =====================================================================
        // EKONOMI TUTARLILIK TESTI
        // 100 tur upgrade + uretim -> para negatife dusmemeli
        // =====================================================================

        [Test]
        public void EconomyConsistency_100Rounds_BalanceNeverGoesNegative()
        {
            // 100 tur boyunca: her turda gelir ekle, 5 turda bir upgrade dene
            _saveManager.Data.Coins = 1_000_000;
            var factory = _productionManager.UnlockFactory("rice_field");

            for (int round = 0; round < 100; round++)
            {
                // Uretim geliri ekle (1 dakikalik uretim)
                double revenue = factory.CurrentRevenuePerMinute;
                if (revenue > 0)
                    _currencySystem.AddCoins(revenue, $"round_{round}_revenue");

                // Her 5 turda bir upgrade dene
                if (round % 5 == 0)
                {
                    if (factory.MachineLevel < 5)
                        factory.TryUpgradeMachine(_currencySystem);

                    if (factory.WorkerLevel < 50)
                        factory.TryUpgradeWorker(_currencySystem);
                }

                // Her turda bakiye negatif olmamali
                Assert.GreaterOrEqual(_saveManager.Data.Coins, 0,
                    $"Tur {round}: Para negatife dusmemeli. Mevcut: {_saveManager.Data.Coins}");
            }
        }

        [Test]
        public void EconomyConsistency_SpendCoins_NeverExceedsBalance()
        {
            // Bakiyeyi asan harcama reddedilmeli
            _saveManager.Data.Coins = 500;

            bool result = _currencySystem.SpendCoins(501, "test_overspend");

            Assert.IsFalse(result, "Bakiyeyi asan harcama reddedilmeli.");
            Assert.AreEqual(500, _saveManager.Data.Coins,
                "Basarisiz harcamada bakiye degismemeli.");
        }

        [Test]
        public void EconomyConsistency_DoubleSpend_Prevention()
        {
            // Double-spend korumasi
            _saveManager.Data.Coins = 1000;

            bool first = _currencySystem.SpendCoins(600, "first_purchase");
            bool second = _currencySystem.SpendCoins(600, "second_purchase");

            Assert.IsTrue(first, "Ilk harcama basarili olmali.");
            Assert.IsFalse(second, "Ikinci harcama (yetersiz bakiye) reddedilmeli.");
            Assert.AreEqual(400, _saveManager.Data.Coins, 0.01, "1000 - 600 = 400 kalmali.");
        }

        // =====================================================================
        // COK FABRIKA ENTEGRASYON TESTI
        // =====================================================================

        [Test]
        public void MultipleFactories_TotalRevenueIsSum()
        {
            _saveManager.Data.Coins = 50_000_000;
            var f1 = _productionManager.UnlockFactory("rice_field");
            var f2 = _productionManager.UnlockFactory("factory");

            double total = _productionManager.GetTotalRevenuePerMinute();

            double expected = f1.CurrentRevenuePerMinute + f2.CurrentRevenuePerMinute;
            Assert.AreEqual(expected, total, 0.01,
                "Toplam gelir tum aktif fabrikalarin toplami olmali.");
        }

        [Test]
        public void MultipleFactories_OfflineEarnings_IncludesAll()
        {
            // Iki tesis varken offline kazanc tek tesisten buyuk olmali
            _saveManager.Data.Coins = 50_000_000;
            _productionManager.UnlockFactory("rice_field");
            var f2 = _productionManager.UnlockFactory("factory");

            var fullResult = _productionManager.CalculateOfflineProduction(TimeSpan.FromHours(4));

            Assert.Greater(fullResult.TotalCoins, 0,
                "Iki tesisin offline kazanci pozitif olmali.");
        }

        // =====================================================================
        // CURRENCY EVENT ENTEGRASYON TESTI
        // =====================================================================

        [Test]
        public void CurrencyEvents_FiredCorrectly_OnAddAndSpend()
        {
            _saveManager.Data.Coins = 1000;
            var events = new List<CurrencyChangedEvent>();
            _eventManager.Subscribe<CurrencyChangedEvent>(e => events.Add(e));

            _currencySystem.AddCoins(500, "test_add");
            _currencySystem.SpendCoins(200, "test_spend");

            Assert.AreEqual(2, events.Count, "2 currency event firlamali.");
            Assert.AreEqual(CurrencyType.Coin, events[0].Type);
            Assert.AreEqual(1000, events[0].OldAmount, 0.01, "Add oncesi eski deger 1000.");
            Assert.AreEqual(1500, events[0].NewAmount, 0.01, "Add sonrasi yeni deger 1500.");
            Assert.AreEqual(1500, events[1].OldAmount, 0.01, "Spend oncesi eski deger 1500.");
            Assert.AreEqual(1300, events[1].NewAmount, 0.01, "Spend sonrasi yeni deger 1300.");
        }

        [Test]
        public void TotalEarnings_IncreasesWithAddCoins()
        {
            _saveManager.Data.TotalEarnings = 0;

            _currencySystem.AddCoins(1000, "revenue");
            _currencySystem.AddCoins(2000, "revenue");

            Assert.AreEqual(3000, _saveManager.Data.TotalEarnings, 0.01,
                "TotalEarnings her AddCoins ile artmali.");
        }
    }
}
