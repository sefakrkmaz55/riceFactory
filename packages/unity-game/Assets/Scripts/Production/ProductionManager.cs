using System;
using System.Collections.Generic;
using System.Linq;
using RiceFactory.Core;
using RiceFactory.Core.Events;
using RiceFactory.Data.Save;
using RiceFactory.Economy;
using UnityEngine;

namespace RiceFactory.Production
{
    /// <summary>
    /// Tum tesisleri (Factory) yoneten ust sinif.
    /// Fabrika olusturma, acma, toplam uretim/gelir hesaplama ve offline uretim islemlerini yonetir.
    ///
    /// Referans: docs/TECH_ARCHITECTURE.md Bolum 4.1 — ProductionSystem
    /// Referans: docs/ECONOMY_BALANCE.md Bolum 3 — Tesis Ekonomisi
    /// </summary>
    public class ProductionManager
    {
        private readonly IBalanceConfig _config;
        private readonly ISaveManager _saveManager;
        private readonly IEventManager _eventManager;
        private readonly PriceCalculator _priceCalculator;
        private readonly CurrencySystem _currencySystem;

        /// <summary>Aktif fabrika listesi (factoryId -> Factory).</summary>
        private readonly Dictionary<string, Factory> _factories = new();

        /// <summary>Fabrika verileri (factoryId -> FactoryData SO).</summary>
        private readonly Dictionary<string, FactoryData> _factoryDataMap = new();

        // --- Public Erisimciler ---
        public IReadOnlyDictionary<string, Factory> Factories => _factories;
        public int ActiveFactoryCount => _factories.Count(f => f.Value.IsActive);
        public int UnlockedFactoryCount => _factories.Count(f => f.Value.IsUnlocked);

        public ProductionManager(
            IBalanceConfig config,
            ISaveManager saveManager,
            IEventManager eventManager,
            PriceCalculator priceCalculator,
            CurrencySystem currencySystem)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _saveManager = saveManager ?? throw new ArgumentNullException(nameof(saveManager));
            _eventManager = eventManager ?? throw new ArgumentNullException(nameof(eventManager));
            _priceCalculator = priceCalculator ?? throw new ArgumentNullException(nameof(priceCalculator));
            _currencySystem = currencySystem ?? throw new ArgumentNullException(nameof(currencySystem));

            // GameTick eventine abone ol
            _eventManager.Subscribe<GameTickEvent>(OnGameTick);
        }

        // =====================================================================
        // BASLANGIC VE YUKLEME
        // =====================================================================

        /// <summary>
        /// Kayitli tesis verilerini yukler ve Factory nesnelerini olusturur.
        /// Boot sirasinda cagirilir.
        /// </summary>
        /// <param name="factoryDataList">ScriptableObject listesi (tum tesis tanimlari)</param>
        public void Initialize(IEnumerable<FactoryData> factoryDataList)
        {
            // Veri haritasini olustur
            foreach (var data in factoryDataList)
            {
                _factoryDataMap[data.FactoryId] = data;
            }

            // Kayitli durumdaki tesisleri olustur
            if (_saveManager.Data.Facilities != null)
            {
                foreach (var facilityState in _saveManager.Data.Facilities)
                {
                    if (!facilityState.IsUnlocked) continue;

                    if (_factoryDataMap.TryGetValue(facilityState.FacilityType, out var factoryData))
                    {
                        var factory = new Factory(factoryData, facilityState, _priceCalculator, _eventManager);
                        _factories[facilityState.Id] = factory;
                    }
                    else
                    {
                        Debug.LogWarning($"[ProductionManager] '{facilityState.FacilityType}' icin FactoryData bulunamadi.");
                    }
                }
            }
        }

        // =====================================================================
        // FABRIKA OLUSTURMA VE ACMA
        // Referans: docs/ECONOMY_BALANCE.md Bolum 3.1
        // =====================================================================

        /// <summary>
        /// Yeni bir tesis acar.
        /// Kosullar: Yeterli coin + onceki tesis yildiz kosulu
        ///
        /// Tesis Acma Maliyetleri (balance_config.json):
        ///   Pirinc Tarlasi: 0 (ucretsiz)
        ///   Pirinc Fabrikasi: 1,000
        ///   Firin: 10,000
        ///   Restoran: 100,000
        ///   Market: 1,000,000
        ///   Kuresel Dagitim: 25,000,000
        ///
        /// Referans: docs/ECONOMY_BALANCE.md Bolum 3.1 "Tesis Acilma Maliyetleri"
        /// </summary>
        /// <param name="factoryId">Acilacak tesisin tipi (ornek: "factory", "bakery")</param>
        /// <returns>Olusturulan Factory veya null</returns>
        public Factory UnlockFactory(string factoryId)
        {
            if (!_factoryDataMap.TryGetValue(factoryId, out var factoryData))
            {
                Debug.LogWarning($"[ProductionManager] '{factoryId}' tesis verisi bulunamadi.");
                return null;
            }

            // Zaten acik mi kontrol et
            if (_factories.Values.Any(f => f.Data.FactoryId == factoryId))
            {
                Debug.LogWarning($"[ProductionManager] '{factoryId}' zaten acik.");
                return null;
            }

            // Maliyet hesapla (franchise indirimi dahil)
            float facilityDiscount = _saveManager.Data.FranchiseBonuses.FacilityCostReduction;
            double unlockCost = _priceCalculator.CalculateFacilityUnlockCost(factoryData.UnlockCost, facilityDiscount);

            // Ucretsiz tesis kontrolu (Pirinc Tarlasi)
            if (unlockCost > 0 && !_currencySystem.SpendCoins(unlockCost, $"FacilityUnlock:{factoryId}"))
            {
                return null;
            }

            // Yeni FacilityState olustur
            string instanceId = $"{factoryId}_{Guid.NewGuid().ToString("N").Substring(0, 8)}";
            var newState = new FacilityState
            {
                Id = instanceId,
                FacilityType = factoryId,
                IsUnlocked = true,
                StarLevel = 1,
                MachineLevel = 1,
                WorkerLevel = 1,
                WorkerSpeedLevel = 0,
                WorkerQualityLevel = 0,
                WorkerCapacityLevel = 0,
                WorkerAutomationLevel = 0,
                ActiveProductIndex = 0,
                ActiveProductId = factoryData.ProductChain.Count > 0
                    ? factoryData.ProductChain[0].outputProductId
                    : factoryId,
                AutoSellEnabled = false,
                TotalProductsSold = 0,
                AverageQuality = 1,
                UnlockedRecipes = new List<string>(),
                BaseOutputAmount = 1
            };

            // Save data'ya ekle
            _saveManager.Data.Facilities ??= new List<FacilityState>();
            _saveManager.Data.Facilities.Add(newState);

            // Factory nesnesi olustur
            var factory = new Factory(factoryData, newState, _priceCalculator, _eventManager);
            _factories[instanceId] = factory;

            Debug.Log($"[ProductionManager] '{factoryData.FactoryName}' acildi! (Instance: {instanceId})");

            return factory;
        }

        // =====================================================================
        // URETIM TICK
        // =====================================================================

        /// <summary>
        /// Her frame cagirilir — tum aktif fabrikalarin uretim tick'ini ileri tasir.
        /// </summary>
        private void OnGameTick(GameTickEvent e)
        {
            foreach (var kvp in _factories)
            {
                kvp.Value.Tick(e.DeltaTime);
            }
        }

        // =====================================================================
        // TOPLAM URETIM / GELIR HESAPLAMA
        // =====================================================================

        /// <summary>
        /// Tum aktif fabrikalarin toplam uretim hizini dondurur (birim/saniye).
        /// </summary>
        public float GetTotalProductionRate()
        {
            float total = 0f;
            foreach (var kvp in _factories)
            {
                if (kvp.Value.IsActive)
                {
                    total += kvp.Value.CurrentProductionRate;
                }
            }
            return total;
        }

        /// <summary>
        /// Tum aktif fabrikalarin toplam gelir/dakika dondurur.
        /// </summary>
        public double GetTotalRevenuePerMinute()
        {
            double total = 0;
            foreach (var kvp in _factories)
            {
                if (kvp.Value.IsActive)
                {
                    total += kvp.Value.CurrentRevenuePerMinute;
                }
            }
            return total;
        }

        /// <summary>
        /// Tum aktif fabrikalarin toplam gelir/saat dondurur.
        /// </summary>
        public double GetTotalRevenuePerHour()
        {
            return GetTotalRevenuePerMinute() * 60.0;
        }

        // =====================================================================
        // OFFLINE URETIM HESAPLAMA
        // Referans: docs/ECONOMY_BALANCE.md Bolum 5
        // Referans: docs/TECH_ARCHITECTURE.md Bolum 3.3 — TimeManager
        // =====================================================================

        /// <summary>
        /// Offline uretim kazancini hesaplar.
        ///
        /// Formul:
        /// OfflineKazanc = ToplamGelir/sn x OfflineSure(sn) x OfflineVerim
        ///
        /// OfflineVerim:
        ///   Temel: %30  (balance_config.json: offline.baseEfficiency)
        ///   Otomasyon arastirmasi ile arttirilir
        ///   Franchise bonusu ile arttirilir
        ///   Maks: %180  (balance_config.json: offline.maxEfficiencyCap)
        ///
        /// MaxSure:
        ///   Ucretsiz: 8 saat  (balance_config.json: offline.maxHoursFree)
        ///   Premium:  12 saat  (balance_config.json: offline.maxHoursPremium)
        ///
        /// Referans: docs/ECONOMY_BALANCE.md Bolum 5 "Offline Kazanc"
        /// </summary>
        /// <param name="offlineDuration">Offline gecen sure</param>
        /// <returns>Offline kazanc sonucu</returns>
        public OfflineProductionResult CalculateOfflineProduction(TimeSpan offlineDuration)
        {
            // Max offline suresini belirle
            bool hasBattlePass = _saveManager.Data.HasBattlePass;
            float maxHours = hasBattlePass
                ? _config.GetFloat("offline.maxHoursPremium", 12f)
                : _config.GetFloat("offline.maxHoursFree", 8f);

            double cappedSeconds = Math.Min(offlineDuration.TotalSeconds, maxHours * 3600);

            // Offline verim hesapla
            float baseEfficiency = _config.GetFloat("offline.baseEfficiency", 0.30f);

            // Otomasyon arastirma bonusu
            int automationLevel = _saveManager.Data.Research.GetBranchLevel("otomasyon");
            float automationBonus = CalculateAutomationBonus(automationLevel);

            // Franchise offline kazanc bonusu
            float franchiseBonus = _saveManager.Data.FranchiseBonuses.OfflineEarningBonus;

            // Toplam verim (max %180)
            float maxEfficiency = _config.GetFloat("offline.maxEfficiencyCap", 1.80f);
            float totalEfficiency = Math.Min(baseEfficiency + automationBonus + franchiseBonus, maxEfficiency);

            // Her aktif tesis icin kazanc hesapla
            double totalCoins = 0;
            int totalProducts = 0;

            foreach (var kvp in _factories)
            {
                var factory = kvp.Value;
                if (!factory.IsActive) continue;

                double revenuePerSecond = factory.CurrentRevenuePerMinute / 60.0;
                totalCoins += revenuePerSecond * cappedSeconds * totalEfficiency;
                totalProducts += (int)(factory.CurrentProductionRate * cappedSeconds * totalEfficiency);
            }

            return new OfflineProductionResult
            {
                Duration = TimeSpan.FromSeconds(cappedSeconds),
                TotalCoins = totalCoins,
                TotalProducts = totalProducts,
                Efficiency = totalEfficiency,
                WasCapped = offlineDuration.TotalSeconds > maxHours * 3600
            };
        }

        /// <summary>
        /// Otomasyon arastirma seviyesine gore offline verim bonusu.
        /// Referans: docs/TECH_ARCHITECTURE.md Bolum 3.3 — CalculateAutomationBonus
        /// </summary>
        private float CalculateAutomationBonus(int automationLevel)
        {
            return automationLevel switch
            {
                0 => 0f,
                <= 3 => 0.10f + (automationLevel * 0.033f),  // Lv1-3: %13-%20
                <= 6 => 0.25f + ((automationLevel - 3) * 0.05f), // Lv4-6: %30-%40
                7 => 0.50f,   // Tam Otomasyon: %50
                8 => 0.50f,   // Singularite: ayri 2x carpan
                _ => 0.50f
            };
        }

        // =====================================================================
        // FABRIKA ERISIM YARDIMCILARI
        // =====================================================================

        /// <summary>Instance ID ile fabrika dondurur.</summary>
        public Factory GetFactory(string instanceId)
        {
            return _factories.TryGetValue(instanceId, out var factory) ? factory : null;
        }

        /// <summary>Fabrika tipi ile ilk eslesen fabrikayi dondurur.</summary>
        public Factory GetFactoryByType(string factoryType)
        {
            return _factories.Values.FirstOrDefault(f => f.Data.FactoryId == factoryType);
        }

        /// <summary>Tum fabrikalarin listesini sirayla dondurur.</summary>
        public List<Factory> GetAllFactoriesSorted()
        {
            return _factories.Values
                .OrderBy(f => f.Data.UnlockOrder)
                .ToList();
        }

        /// <summary>Belirli bir tesis tipinin acik olup olmadigini kontrol eder.</summary>
        public bool IsFactoryTypeUnlocked(string factoryType)
        {
            return _factories.Values.Any(f => f.Data.FactoryId == factoryType && f.IsUnlocked);
        }

        /// <summary>
        /// Tum fabrikalarin uretim istatistiklerini yeniden hesaplar.
        /// Prestige, arastirma veya global degisikliklerden sonra cagirilir.
        /// </summary>
        public void RecalculateAllProductionStats()
        {
            foreach (var kvp in _factories)
            {
                kvp.Value.RecalculateProductionStats();
            }
        }

        /// <summary>
        /// EventManager aboneligini temizler. Dispose pattern.
        /// </summary>
        public void Dispose()
        {
            _eventManager.Unsubscribe<GameTickEvent>(OnGameTick);
        }
    }

    /// <summary>Offline uretim hesap sonucu.</summary>
    public class OfflineProductionResult
    {
        /// <summary>Hesaplanan offline sure (sinirli).</summary>
        public TimeSpan Duration;

        /// <summary>Toplam kazanilacak coin.</summary>
        public double TotalCoins;

        /// <summary>Toplam uretilen urun sayisi.</summary>
        public int TotalProducts;

        /// <summary>Uygulanan verimlilik orani (0.0 - 1.8).</summary>
        public float Efficiency;

        /// <summary>Sure sinira takildi mi.</summary>
        public bool WasCapped;
    }
}
