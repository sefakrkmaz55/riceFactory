using System;
using RiceFactory.Core;
using RiceFactory.Core.Events;
using RiceFactory.Data.Save;
using RiceFactory.Economy;
using UnityEngine;

namespace RiceFactory.Production
{
    /// <summary>
    /// Tek bir tesis (fabrika) instance'ini temsil eder.
    /// Uretim dongusunu yonetir: Uret -> Isle -> Sat
    /// PriceCalculator ile uretim hizi ve fiyat hesaplamasi yapar.
    ///
    /// Referans: docs/ECONOMY_BALANCE.md Bolum 2.1, 3.2
    /// Referans: docs/TECH_ARCHITECTURE.md Bolum 4.1 — ProductionSystem
    /// </summary>
    public class Factory
    {
        // --- Bagimlliklar ---
        private readonly FactoryData _data;
        private readonly FacilityState _state;
        private readonly PriceCalculator _priceCalculator;
        private readonly IEventManager _eventManager;

        // --- Uretim Durumu ---
        private float _productionTimer;
        private float _currentCycleDuration;
        private bool _isActive;

        // --- Public Erisimciler ---
        public string FactoryId => _data.FactoryId;
        public string FactoryName => _data.FactoryName;
        public FactoryData Data => _data;
        public FacilityState State => _state;
        public bool IsActive => _isActive;
        public bool IsUnlocked => _state.IsUnlocked;

        /// <summary>Mevcut makine seviyesi (1-5).</summary>
        public int MachineLevel => _state.MachineLevel;

        /// <summary>Mevcut calisan seviyesi (1-50).</summary>
        public int WorkerLevel => _state.WorkerLevel;

        /// <summary>Mevcut yildiz seviyesi (1-5).</summary>
        public int StarLevel => _state.StarLevel;

        /// <summary>Uretim dongusunun yuzdesel ilerlemesi (0-1).</summary>
        public float ProductionProgress => _currentCycleDuration > 0
            ? Mathf.Clamp01(_productionTimer / _currentCycleDuration)
            : 0f;

        /// <summary>Mevcut uretim hizi (birim/saniye).</summary>
        public float CurrentProductionRate { get; private set; }

        /// <summary>Mevcut gelir/dakika.</summary>
        public double CurrentRevenuePerMinute { get; private set; }

        public Factory(
            FactoryData data,
            FacilityState state,
            PriceCalculator priceCalculator,
            IEventManager eventManager)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _priceCalculator = priceCalculator ?? throw new ArgumentNullException(nameof(priceCalculator));
            _eventManager = eventManager ?? throw new ArgumentNullException(nameof(eventManager));

            RecalculateProductionStats();

            // Acik tesis varsayilan olarak aktif baslar
            _isActive = _state.IsUnlocked;
        }

        // =====================================================================
        // URETIM DONGUSU
        // Referans: docs/TECH_ARCHITECTURE.md Bolum 4.1 — ProductionPipeline
        // =====================================================================

        /// <summary>
        /// Her frame cagirilir. Uretim zamanlayicisini ilerletir.
        /// Bir uretim dongusu tamamlandiginda ProductionCompletedEvent firlatir.
        /// </summary>
        /// <param name="deltaTime">Gecen sure (saniye)</param>
        public void Tick(float deltaTime)
        {
            if (!_isActive || !_state.IsUnlocked) return;

            _productionTimer += deltaTime;

            // Birden fazla uretim dongusu tamamlanmis olabilir (ornegin offline catch-up)
            while (_productionTimer >= _currentCycleDuration && _currentCycleDuration > 0)
            {
                _productionTimer -= _currentCycleDuration;
                CompleteProductionCycle();
            }
        }

        /// <summary>
        /// Tek bir uretim dongusunu tamamlar.
        /// Uret -> Isle -> Event firlat
        /// </summary>
        private void CompleteProductionCycle()
        {
            // Kalite hesaplama (basitlestirilmis — makine seviyesine bagli)
            // Referans: balance_config.json quality.levels, machine.qualityFloors/Ceilings
            int quality = CalculateProductQuality();

            // Uretim tamamlandi eventi
            _eventManager.Publish(new ProductionCompletedEvent
            {
                FacilityId = _state.Id,
                ProductId = _state.ActiveProductId,
                Quantity = _state.BaseOutputAmount,
                Quality = quality
            });
        }

        /// <summary>
        /// Urun kalitesini hesaplar (1-5).
        /// Makine seviyesine gore minimum ve maksimum kalite siniri vardir.
        ///
        /// Referans: balance_config.json
        ///   machine.qualityFloors:   [1, 1, 2, 3, 4]
        ///   machine.qualityCeilings: [1, 2, 3, 4, 5]
        ///   quality.dropWeights:     [0.40, 0.30, 0.20, 0.08, 0.02]
        /// </summary>
        private int CalculateProductQuality()
        {
            int[] qualityFloors = { 1, 1, 2, 3, 4 };
            int[] qualityCeilings = { 1, 2, 3, 4, 5 };
            float[] dropWeights = { 0.40f, 0.30f, 0.20f, 0.08f, 0.02f };

            int machineIndex = Mathf.Clamp(_state.MachineLevel - 1, 0, 4);
            int minQuality = qualityFloors[machineIndex];
            int maxQuality = qualityCeilings[machineIndex];

            // Calisan kalite becerisi ile agirliklari kaydir
            float qualityShift = _state.WorkerQualityLevel * 0.01f;
            float roll = UnityEngine.Random.value - qualityShift;

            float cumulative = 0f;
            for (int i = 0; i < dropWeights.Length; i++)
            {
                cumulative += dropWeights[i];
                if (roll <= cumulative)
                {
                    return Mathf.Clamp(i + 1, minQuality, maxQuality);
                }
            }

            return maxQuality;
        }

        // =====================================================================
        // URETIM HIZI HESAPLAMA
        // Referans: docs/ECONOMY_BALANCE.md Bolum 2.1
        // =====================================================================

        /// <summary>
        /// Uretim istatistiklerini yeniden hesaplar.
        /// Makine/calisan/yildiz degistiginde cagirilmalidir.
        ///
        /// Formul:
        /// UretimHizi = TemelHiz x MakineCarpani x CalisanBonus x YildizBonus x FP_Bonus
        /// CycleDuration = 1 / UretimHizi
        /// </summary>
        public void RecalculateProductionStats()
        {
            // Franchise bonusunu al
            float fpProductionBonus = 0f;
            if (ServiceLocator.TryGet<ISaveManager>(out var saveManager))
            {
                fpProductionBonus = saveManager.Data.FranchiseBonuses.ProductionSpeedBonus;
            }

            // Arastirma hiz bonusu
            float researchBonus = 0f;
            if (ServiceLocator.TryGet<ISaveManager>(out var sm))
            {
                int speedLevel = sm.Data.Research.GetBranchLevel("hiz");
                researchBonus = speedLevel * 0.10f;
            }

            // PriceCalculator ile uretim hizi hesapla
            CurrentProductionRate = _priceCalculator.CalculateProductionRate(
                _data.BaseProductionTime,
                _state.MachineLevel,
                _state.WorkerLevel,
                _state.StarLevel,
                fpProductionBonus,
                researchBonus
            );

            // Dongu suresi = 1 / uretim hizi
            _currentCycleDuration = CurrentProductionRate > 0
                ? 1f / CurrentProductionRate
                : float.MaxValue;

            // Gelir/dakika hesapla
            CurrentRevenuePerMinute = _data.BasePrice * CurrentProductionRate * 60.0;
        }

        // =====================================================================
        // UPGRADE METOTLARI
        // =====================================================================

        /// <summary>
        /// Makine seviyesini arttirir.
        /// Referans: balance_config.json machine.maxLevel = 5
        /// </summary>
        /// <returns>Basarili ise true</returns>
        public bool TryUpgradeMachine(CurrencySystem currencySystem)
        {
            int maxLevel = 5; // balance_config.json: machine.maxLevel
            if (_state.MachineLevel >= maxLevel)
            {
                Debug.Log($"[Factory] {FactoryId}: Makine zaten maksimum seviyede ({maxLevel}).");
                return false;
            }

            int targetLevel = _state.MachineLevel + 1;
            double cost = _priceCalculator.CalculateUpgradeCost(_data.BaseCost, targetLevel);

            if (!currencySystem.SpendCoins(cost, $"MachineUpgrade:{_state.Id}"))
            {
                return false;
            }

            _state.MachineLevel = targetLevel;
            RecalculateProductionStats();

            _eventManager.Publish(new UpgradeCompletedEvent
            {
                Type = UpgradeType.Machine,
                TargetId = _state.Id,
                NewLevel = _state.MachineLevel
            });

            return true;
        }

        /// <summary>
        /// Calisan seviyesini arttirir.
        /// Referans: balance_config.json worker.maxLevel = 50
        /// </summary>
        public bool TryUpgradeWorker(CurrencySystem currencySystem)
        {
            int maxLevel = 50; // balance_config.json: worker.maxLevel
            if (_state.WorkerLevel >= maxLevel)
            {
                Debug.Log($"[Factory] {FactoryId}: Calisan zaten maksimum seviyede ({maxLevel}).");
                return false;
            }

            int targetLevel = _state.WorkerLevel + 1;
            double cost = _priceCalculator.CalculateWorkerUpgradeCost(targetLevel);

            if (!currencySystem.SpendCoins(cost, $"WorkerUpgrade:{_state.Id}"))
            {
                return false;
            }

            _state.WorkerLevel = targetLevel;
            RecalculateProductionStats();

            _eventManager.Publish(new UpgradeCompletedEvent
            {
                Type = UpgradeType.Worker,
                TargetId = _state.Id,
                NewLevel = _state.WorkerLevel
            });

            return true;
        }

        /// <summary>
        /// Yildiz seviyesini arttirir.
        /// Referans: balance_config.json facilityStar.maxStars = 5
        /// Referans: docs/ECONOMY_BALANCE.md Bolum 3.3
        /// </summary>
        public bool TryUpgradeStar(CurrencySystem currencySystem)
        {
            int maxStars = 5; // balance_config.json: facilityStar.maxStars
            if (_state.StarLevel >= maxStars)
            {
                Debug.Log($"[Factory] {FactoryId}: Yildiz zaten maksimum seviyede ({maxStars}).");
                return false;
            }

            int targetStar = _state.StarLevel + 1;

            // Yildiz icin gerekli makine seviyesi kontrolu
            if (_state.MachineLevel < targetStar)
            {
                Debug.Log($"[Factory] {FactoryId}: Yildiz {targetStar} icin Makine Lv.{targetStar} gerekli.");
                return false;
            }

            // Urun satisi kosulu
            int requiredSales = GetRequiredSalesForStar(targetStar);
            if (_state.TotalProductsSold < requiredSales)
            {
                Debug.Log($"[Factory] {FactoryId}: Yildiz {targetStar} icin {requiredSales} urun satisi gerekli.");
                return false;
            }

            double cost = _priceCalculator.CalculateStarUpgradeCost(_data.UnlockCost, targetStar);

            if (!currencySystem.SpendCoins(cost, $"StarUpgrade:{_state.Id}"))
            {
                return false;
            }

            _state.StarLevel = targetStar;
            RecalculateProductionStats();

            _eventManager.Publish(new UpgradeCompletedEvent
            {
                Type = UpgradeType.Star,
                TargetId = _state.Id,
                NewLevel = _state.StarLevel
            });

            return true;
        }

        /// <summary>
        /// Yildiz icin gerekli minimum urun satisi sayisi.
        /// Referans: docs/TECH_ARCHITECTURE.md Bolum 4.3 — GetRequiredSalesForStar
        /// </summary>
        private int GetRequiredSalesForStar(int targetStar)
        {
            return targetStar switch
            {
                2 => 500,
                3 => 5_000,
                4 => 50_000,
                5 => 500_000,
                _ => int.MaxValue
            };
        }

        // =====================================================================
        // DURUM YONETIMI
        // =====================================================================

        /// <summary>Tesisi aktif duruma getirir (uretim baslar).</summary>
        public void Activate()
        {
            if (!_state.IsUnlocked)
            {
                Debug.LogWarning($"[Factory] {FactoryId}: Tesis henuz acilmamis, aktif edilemez.");
                return;
            }
            _isActive = true;
        }

        /// <summary>Tesisi duraklatir (uretim durur).</summary>
        public void Deactivate()
        {
            _isActive = false;
        }

        /// <summary>Uretim zamanlayicisini sifirlar.</summary>
        public void ResetProductionTimer()
        {
            _productionTimer = 0f;
        }

        /// <summary>
        /// Sonraki upgrade maliyetlerini dondurur (UI icin).
        /// </summary>
        public FactoryUpgradeCosts GetNextUpgradeCosts()
        {
            return new FactoryUpgradeCosts
            {
                MachineCost = _state.MachineLevel < 5
                    ? _priceCalculator.CalculateUpgradeCost(_data.BaseCost, _state.MachineLevel + 1)
                    : -1,
                WorkerCost = _state.WorkerLevel < 50
                    ? _priceCalculator.CalculateWorkerUpgradeCost(_state.WorkerLevel + 1)
                    : -1,
                StarCost = _state.StarLevel < 5
                    ? _priceCalculator.CalculateStarUpgradeCost(_data.UnlockCost, _state.StarLevel + 1)
                    : -1
            };
        }
    }

    /// <summary>Sonraki upgrade maliyetleri (UI icin). -1 = max seviye.</summary>
    public struct FactoryUpgradeCosts
    {
        public double MachineCost;
        public double WorkerCost;
        public double StarCost;
    }
}
