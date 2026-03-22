using System;
using System.Collections.Generic;
using System.Linq;
using RiceFactory.Core;
using RiceFactory.Core.Events;
using RiceFactory.Data.Save;
using UnityEngine;

namespace RiceFactory.Economy
{
    /// <summary>
    /// Franchise (Prestige) sistemi.
    /// Oyuncu ilerlemeyi sifirlar, karsiliginda kalici Franchise Puani (FP) kazanir.
    /// FP ile kalici bonuslar satin alinir.
    ///
    /// Referans: docs/ECONOMY_BALANCE.md Bolum 4 — Prestige Dengesi
    /// </summary>
    public class PrestigeSystem : IPrestigeSystem
    {
        private readonly IBalanceConfig _config;
        private readonly ISaveManager _saveManager;
        private readonly IEventManager _eventManager;

        /// <summary>
        /// FP bonus tanimlari.
        /// Referans: balance_config.json prestige.bonuses
        /// </summary>
        private static readonly Dictionary<FranchiseBonusType, FPBonusDefinition> BonusDefinitions = new()
        {
            // Referans: docs/ECONOMY_BALANCE.md Bolum 4.2
            [FranchiseBonusType.ProductionSpeed] = new FPBonusDefinition
            {
                FPPerLevel = 5,
                MaxLevel = 20,
                EffectPerLevel = 0.10f,
                Description = "Uretim Hizi +%10 / seviye"
            },
            [FranchiseBonusType.StartingCoins] = new FPBonusDefinition
            {
                FPPerLevel = 3,
                MaxLevel = 10,
                EffectPerLevel = 0.50f,
                Description = "Baslangic Parasi +%50 / seviye"
            },
            [FranchiseBonusType.OfflineEarnings] = new FPBonusDefinition
            {
                FPPerLevel = 4,
                MaxLevel = 20,
                EffectPerLevel = 0.05f,
                Description = "Offline Kazanc +%5 / seviye"
            },
            [FranchiseBonusType.FacilityCostReduction] = new FPBonusDefinition
            {
                FPPerLevel = 6,
                MaxLevel = 8,
                EffectPerLevel = 0.10f,
                Description = "Tesis Acma Maliyeti -%10 / seviye"
            },
            [FranchiseBonusType.CriticalProduction] = new FPBonusDefinition
            {
                FPPerLevel = 8,
                MaxLevel = 10,
                EffectPerLevel = 0.02f,
                Description = "Kritik Uretim Sansi +%2 / seviye"
            },
            [FranchiseBonusType.SpecialWorker] = new FPBonusDefinition
            {
                FPPerLevel = 15,
                MaxLevel = 1,
                EffectPerLevel = 1.0f,
                Description = "Ozel Calisan Acma (tek seferlik)"
            }
        };

        public PrestigeSystem(IBalanceConfig config, ISaveManager saveManager, IEventManager eventManager)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _saveManager = saveManager ?? throw new ArgumentNullException(nameof(saveManager));
            _eventManager = eventManager ?? throw new ArgumentNullException(nameof(eventManager));
        }

        // =====================================================================
        // FRANCHISE PUANI (FP) HESAPLAMA
        // Referans: docs/ECONOMY_BALANCE.md Bolum 4.1
        // =====================================================================

        /// <summary>
        /// Kazanilacak Franchise Puanini hesaplar (onizleme).
        ///
        /// Formul:
        /// FP = floor( sqrt(ToplamKazanc / 1,000,000) x (1 + BonusCarpan) )
        /// BonusCarpan = (5-yildiz tesis sayisi) x 0.1
        ///
        /// Ornek:
        ///   ToplamKazanc = 25,000,000, 1 adet 5-yildiz tesis
        ///   BonusCarpan = 1 x 0.1 = 0.1
        ///   FP = floor( sqrt(25,000,000 / 1,000,000) x (1 + 0.1) )
        ///   FP = floor( sqrt(25) x 1.1 ) = floor(5 x 1.1) = floor(5.5) = 5
        ///
        /// Referans: docs/ECONOMY_BALANCE.md Bolum 4.1 "Franchise Puani Formulu"
        /// balance_config.json: prestige.fpDivisor = 1,000,000
        /// </summary>
        /// <returns>Kazanilacak FP miktari</returns>
        public int CalculateFP()
        {
            double totalEarnings = _saveManager.Data.TotalEarnings;
            double fpDivisor = _config.GetFloat("prestige.fpDivisor", 1_000_000f);

            // 5-yildiz tesis sayisini bul
            int fiveStarCount = CountFiveStarFacilities();

            // BonusCarpan = (5-yildiz tesis sayisi) x 0.1
            // Referans: balance_config.json prestige.fpBonusPerStar5 = 0.1
            float bonusPerStar5 = _config.GetFloat("prestige.fpBonusPerStar5", 0.1f);
            float bonusMultiplier = fiveStarCount * bonusPerStar5;

            // FP = floor( sqrt(ToplamKazanc / 1,000,000) x (1 + BonusCarpan) )
            int fp = (int)Math.Floor(Math.Sqrt(totalEarnings / fpDivisor) * (1 + bonusMultiplier));

            return Math.Max(0, fp);
        }

        /// <summary>
        /// Mevcut 5-yildiz tesis sayisini dondurur.
        /// </summary>
        private int CountFiveStarFacilities()
        {
            if (_saveManager.Data.Facilities == null) return 0;
            return _saveManager.Data.Facilities.Count(f => f.IsUnlocked && f.StarLevel >= 5);
        }

        // =====================================================================
        // PRESTIGE KOSUL KONTROLU
        // Referans: docs/ECONOMY_BALANCE.md Bolum 4.1
        // =====================================================================

        /// <summary>
        /// Franchise (prestige) yapilabilir mi kontrol eder.
        /// Kosul: ToplamKazanc >= 1,000,000 coin
        ///
        /// Referans: balance_config.json prestige.franchiseMinEarnings = 1,000,000
        /// </summary>
        public bool CanPrestige()
        {
            double threshold = _config.GetFloat("prestige.franchiseMinEarnings", 1_000_000f);
            return _saveManager.Data.TotalEarnings >= threshold;
        }

        /// <summary>
        /// Prestige onizleme bilgilerini dondurur (UI icin).
        /// </summary>
        public PrestigePreview GetPrestigePreview()
        {
            return new PrestigePreview
            {
                CanPrestige = CanPrestige(),
                CurrentTotalEarnings = _saveManager.Data.TotalEarnings,
                MinimumEarningsRequired = _config.GetFloat("prestige.franchiseMinEarnings", 1_000_000f),
                EstimatedFP = CalculateFP(),
                FiveStarFacilityCount = CountFiveStarFacilities(),
                CurrentFranchiseCount = _saveManager.Data.FranchiseCount
            };
        }

        // =====================================================================
        // PRESTIGE UYGULAMA
        // Referans: docs/ECONOMY_BALANCE.md Bolum 4.1 + TECH_ARCHITECTURE.md 4.4
        // =====================================================================

        /// <summary>
        /// Franchise (prestige) islemini gerceklestirir.
        /// 1. FP hesapla ve ekle
        /// 2. Kalici verileri koru (FP, bonuslar, basarimlar, kozmetikler)
        /// 3. Geri kalan her seyi sifirla (coin, tesisler, envanter vb.)
        /// 4. Baslangic parasi bonusunu uygula
        /// 5. Sehir temasini ayarla
        /// 6. Event firlat
        /// </summary>
        /// <param name="selectedCityId">Yeni franchise icin secilen sehir temasi</param>
        /// <returns>Franchise sonuc bilgisi veya null (kosullar saglanmadiysa)</returns>
        public FranchiseResult ExecuteFranchise(string selectedCityId)
        {
            if (!CanPrestige())
            {
                Debug.LogWarning("[PrestigeSystem] Franchise kosullari saglanmiyor.");
                return null;
            }

            int earnedFP = CalculateFP();
            int franchiseNumber = _saveManager.Data.FranchiseCount + 1;

            // Kalici verileri kaydet
            var persistentData = new PersistentFranchiseData
            {
                FranchisePoints = _saveManager.Data.FranchisePoints + earnedFP,
                FranchiseCount = franchiseNumber,
                FranchiseBonuses = _saveManager.Data.FranchiseBonuses,
                TotalLifetimeEarnings = _saveManager.Data.TotalLifetimeEarnings + _saveManager.Data.TotalEarnings,
                UnlockedCities = _saveManager.Data.UnlockedCities,
                Achievements = _saveManager.Data.Achievements,
                CosmeticInventory = _saveManager.Data.CosmeticInventory
            };

            // Sifirlama uygula
            _saveManager.Data.ResetForFranchise(persistentData);

            // Baslangic parasi bonusu uygula
            double startingCoins = CalculateStartingCoins(persistentData.FranchiseBonuses);
            _saveManager.Data.Coins = startingCoins;

            // Sehir temasi
            _saveManager.Data.CurrentCityId = selectedCityId;

            // Event firlat
            _eventManager.Publish(new FranchiseStartedEvent
            {
                FranchiseNumber = franchiseNumber,
                EarnedFP = earnedFP,
                NewCityId = selectedCityId
            });

            return new FranchiseResult
            {
                FranchiseNumber = franchiseNumber,
                EarnedFP = earnedFP,
                TotalFP = persistentData.FranchisePoints,
                StartingCoins = startingCoins,
                CityId = selectedCityId
            };
        }

        // =====================================================================
        // FP HARCAMA VE BONUS SISTEMI
        // Referans: docs/ECONOMY_BALANCE.md Bolum 4.2
        // =====================================================================

        /// <summary>
        /// FP harcayarak kalici bonus satin alir.
        ///
        /// Bonus Turleri ve Maliyetleri:
        /// - Uretim Hizi:         5 FP/seviye, max 20 seviye, +%10/seviye (toplam +%200)
        /// - Baslangic Parasi:    3 FP/seviye, max 10 seviye, +%50/seviye
        /// - Offline Kazanc:      4 FP/seviye, max 20 seviye, +%5/seviye (toplam +%100)
        /// - Tesis Indirimi:      6 FP/seviye, max 8 seviye,  -%10/seviye (toplam -%80)
        /// - Kritik Uretim:       8 FP/seviye, max 10 seviye, +%2/seviye (toplam +%20)
        /// - Ozel Calisan:       15 FP,        1 kez,         Efsanevi calisanlar
        ///
        /// Referans: docs/ECONOMY_BALANCE.md Bolum 4.2 "Franchise Puani Harcama Plani"
        /// </summary>
        /// <param name="bonusType">Alinmak istenen bonus turu</param>
        /// <returns>Basarili ise true</returns>
        public bool PurchaseFranchiseBonus(FranchiseBonusType bonusType)
        {
            if (!BonusDefinitions.TryGetValue(bonusType, out var definition))
            {
                Debug.LogWarning($"[PrestigeSystem] Bilinmeyen bonus turu: {bonusType}");
                return false;
            }

            int currentLevel = _saveManager.Data.FranchiseBonuses.GetLevel(bonusType);

            // Max seviye kontrolu
            if (currentLevel >= definition.MaxLevel)
            {
                Debug.Log($"[PrestigeSystem] {bonusType} zaten maksimum seviyede ({definition.MaxLevel}).");
                return false;
            }

            // FP yeterliligi kontrolu
            int fpCost = definition.FPPerLevel;
            if (_saveManager.Data.FranchisePoints < fpCost)
            {
                Debug.Log($"[PrestigeSystem] Yetersiz FP. Gereken: {fpCost}, Mevcut: {_saveManager.Data.FranchisePoints}");
                return false;
            }

            // FP harca ve bonus seviyesini artir
            _saveManager.Data.FranchisePoints -= fpCost;
            _saveManager.Data.FranchiseBonuses.SetLevel(bonusType, currentLevel + 1);

            _eventManager.Publish(new CurrencyChangedEvent
            {
                Type = CurrencyType.FP,
                OldAmount = _saveManager.Data.FranchisePoints + fpCost,
                NewAmount = _saveManager.Data.FranchisePoints,
                Reason = $"FranchiseBonus:{bonusType}"
            });

            return true;
        }

        /// <summary>
        /// Belirli bir bonus turunun mevcut etkisini hesaplar.
        /// </summary>
        public float GetBonusEffect(FranchiseBonusType bonusType)
        {
            if (!BonusDefinitions.TryGetValue(bonusType, out var definition))
                return 0f;

            int currentLevel = _saveManager.Data.FranchiseBonuses.GetLevel(bonusType);
            return currentLevel * definition.EffectPerLevel;
        }

        /// <summary>
        /// Belirli bir bonus turunun bilgilerini dondurur (UI icin).
        /// </summary>
        public FPBonusInfo GetBonusInfo(FranchiseBonusType bonusType)
        {
            if (!BonusDefinitions.TryGetValue(bonusType, out var definition))
                return null;

            int currentLevel = _saveManager.Data.FranchiseBonuses.GetLevel(bonusType);

            return new FPBonusInfo
            {
                Type = bonusType,
                Description = definition.Description,
                CurrentLevel = currentLevel,
                MaxLevel = definition.MaxLevel,
                FPCostPerLevel = definition.FPPerLevel,
                CurrentEffect = currentLevel * definition.EffectPerLevel,
                NextLevelEffect = (currentLevel + 1) * definition.EffectPerLevel,
                IsMaxed = currentLevel >= definition.MaxLevel,
                CanAfford = _saveManager.Data.FranchisePoints >= definition.FPPerLevel
            };
        }

        /// <summary>
        /// Tum bonus turlerinin bilgilerini dondurur.
        /// </summary>
        public List<FPBonusInfo> GetAllBonusInfos()
        {
            var infos = new List<FPBonusInfo>();
            foreach (var kvp in BonusDefinitions)
            {
                infos.Add(GetBonusInfo(kvp.Key));
            }
            return infos;
        }

        // =====================================================================
        // IPrestigeSystem IMPLEMENTASYONU
        // =====================================================================

        /// <summary>
        /// Kazanilacak FP miktarini hesaplar.
        /// IPrestigeSystem interface implementasyonu — CalculateFP()'yi sarmalar.
        /// </summary>
        public int CalculateEarnedFP()
        {
            return CalculateFP();
        }

        /// <summary>
        /// Sonraki sehir adini dondurur.
        /// IPrestigeSystem interface implementasyonu.
        /// </summary>
        public string GetNextCityName()
        {
            int franchiseCount = _saveManager.Data.FranchiseCount;
            string[] cityNames = { "Tokyo", "Osaka", "Kyoto", "Seoul", "Beijing", "Shanghai", "Bangkok", "Hanoi", "Jakarta", "Mumbai" };
            int index = franchiseCount % cityNames.Length;
            return cityNames[index];
        }

        /// <summary>
        /// Prestige kosulu karsilanmadiysa aciklama dondurur.
        /// IPrestigeSystem interface implementasyonu.
        /// </summary>
        public string GetPrestigeRequirementDescription()
        {
            double threshold = _config.GetFloat("prestige.franchiseMinEarnings", 1_000_000f);
            double current = _saveManager.Data.TotalEarnings;

            if (current < threshold)
            {
                return $"Franchise icin en az {threshold:N0} coin kazanmalisin. Mevcut: {current:N0}";
            }

            return "Gereksinimler karsilandi!";
        }

        /// <summary>
        /// Satin alinabilir FP bonus listesini PrestigeBonusItem formatinda dondurur.
        /// IPrestigeSystem interface implementasyonu.
        /// </summary>
        public List<PrestigeBonusItem> GetAvailableBonuses()
        {
            var bonuses = new List<PrestigeBonusItem>();
            foreach (var kvp in BonusDefinitions)
            {
                var bonusType = kvp.Key;
                var definition = kvp.Value;
                int currentLevel = _saveManager.Data.FranchiseBonuses.GetLevel(bonusType);

                if (currentLevel >= definition.MaxLevel)
                    continue;

                bonuses.Add(new PrestigeBonusItem
                {
                    Id = bonusType.ToString(),
                    DisplayName = $"{definition.Description} (Lv.{currentLevel + 1}/{definition.MaxLevel})",
                    FPCost = definition.FPPerLevel,
                    CanAfford = _saveManager.Data.FranchisePoints >= definition.FPPerLevel
                });
            }
            return bonuses;
        }

        /// <summary>
        /// Prestige islemini gerceklestirir.
        /// IPrestigeSystem interface implementasyonu — sonraki sehir otomatik secilir.
        /// </summary>
        public void ExecutePrestige()
        {
            string nextCity = GetNextCityName();
            ExecuteFranchise(nextCity);
        }

        /// <summary>
        /// FP bonus satin alir (string bonusId ile).
        /// IPrestigeSystem interface implementasyonu.
        /// </summary>
        public bool PurchaseBonus(string bonusId)
        {
            if (Enum.TryParse<FranchiseBonusType>(bonusId, out var bonusType))
            {
                return PurchaseFranchiseBonus(bonusType);
            }

            Debug.LogWarning($"[PrestigeSystem] PurchaseBonus: Gecersiz bonus ID: {bonusId}");
            return false;
        }

        // =====================================================================
        // YARDIMCI METOTLAR
        // =====================================================================

        /// <summary>
        /// Franchise sonrasi baslangic parasini hesaplar.
        ///
        /// Her seviye baslangic parasini %50 arttirir.
        /// Formul: 500 x 1.5^level (level > 0 ise)
        ///
        /// Referans: docs/ECONOMY_BALANCE.md Bolum 4.2 "Baslangic Parasi +%50 / seviye"
        /// </summary>
        private double CalculateStartingCoins(FranchiseBonuses bonuses)
        {
            int level = bonuses.GetLevel(FranchiseBonusType.StartingCoins);
            if (level <= 0) return 0;

            return 500.0 * Math.Pow(1.5, level);
        }
    }

    // =====================================================================
    // VERI YAPILARI
    // =====================================================================

    /// <summary>FP bonus tanimi (statik konfigurasyon).</summary>
    public class FPBonusDefinition
    {
        public int FPPerLevel;
        public int MaxLevel;
        public float EffectPerLevel;
        public string Description;
    }

    /// <summary>Franchise sonuc bilgisi.</summary>
    public class FranchiseResult
    {
        public int FranchiseNumber;
        public int EarnedFP;
        public int TotalFP;
        public double StartingCoins;
        public string CityId;
    }

    /// <summary>Prestige onizleme bilgisi (UI icin).</summary>
    public class PrestigePreview
    {
        public bool CanPrestige;
        public double CurrentTotalEarnings;
        public double MinimumEarningsRequired;
        public int EstimatedFP;
        public int FiveStarFacilityCount;
        public int CurrentFranchiseCount;
    }

    /// <summary>FP bonus bilgisi (UI icin).</summary>
    public class FPBonusInfo
    {
        public FranchiseBonusType Type;
        public string Description;
        public int CurrentLevel;
        public int MaxLevel;
        public int FPCostPerLevel;
        public float CurrentEffect;
        public float NextLevelEffect;
        public bool IsMaxed;
        public bool CanAfford;
    }

    /// <summary>Franchise sirasinda korunan kalici veriler.</summary>
    public class PersistentFranchiseData
    {
        public int FranchisePoints;
        public int FranchiseCount;
        public FranchiseBonuses FranchiseBonuses;
        public double TotalLifetimeEarnings;
        public List<string> UnlockedCities;
        public List<string> Achievements;
        public List<string> CosmeticInventory;
    }
}
