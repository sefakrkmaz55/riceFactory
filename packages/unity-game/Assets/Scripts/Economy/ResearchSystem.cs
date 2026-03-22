// =============================================================================
// ResearchSystem.cs
// Arastirma agaci sistemi — 4 dal, her dal 8 seviye.
// Dallar: Otomasyon, Kalite, Hiz, Kapasite
//
// Referans: docs/GDD.md Bolum 3.4 — Arastirma Agaci
// Referans: docs/ECONOMY_BALANCE.md Bolum 2.2 (maliyet formulu)
// Parametreler: balance_config.json research.*
// =============================================================================

using System;
using System.Collections.Generic;
using RiceFactory.Core;
using RiceFactory.Core.Events;
using RiceFactory.Data.Save;
using UnityEngine;

namespace RiceFactory.Economy
{
    /// <summary>
    /// Arastirma agaci sistemi.
    /// 4 dal (otomasyon, kalite, hiz, kapasite), her dal 8 seviye.
    ///
    /// Maliyet formulu:
    ///   ResearchCost(level) = BaseCost x 3^(level - 1)
    ///   ResearchTime(level) = BaseTime x 2^(level - 1)
    ///
    /// IBalanceConfig'den parametre okur.
    /// ResearchCompletedEvent firlat: UpgradeCompletedEvent (UpgradeType.Research).
    /// </summary>
    public class ResearchSystem
    {
        private readonly IBalanceConfig _config;
        private readonly ISaveManager _saveManager;
        private readonly IEventManager _eventManager;
        private readonly CurrencySystem _currencySystem;

        // Aktif arastirma durumu
        private string _activeResearchBranch;
        private float _activeResearchTimer;
        private float _activeResearchDuration;

        // --- Statik Arastirma Tanimlari ---

        /// <summary>
        /// 4 arastirma dalinin tanimlari.
        /// Referans: docs/GDD.md Bolum 3.4
        /// balance_config.json: research.branches = ["otomasyon", "kalite", "hiz", "kapasite"]
        /// </summary>
        public static readonly IReadOnlyDictionary<string, ResearchBranchDefinition> BranchDefinitions =
            new Dictionary<string, ResearchBranchDefinition>
            {
                // ---- OTOMASYON DALI ----
                // GDD: Otomasyon -> Tesisler arasi transfer, otomatik hasat, robot calisan, tam otomasyon
                ["otomasyon"] = new ResearchBranchDefinition
                {
                    BranchId = "otomasyon",
                    DisplayName = "Otomasyon",
                    Description = "Otomatik satis, otomatik upgrade ve offline verimlilik artisi.",
                    MaxLevel = 8,
                    Levels = new[]
                    {
                        new ResearchLevelInfo { Level = 1, Name = "Basit Konveyor",  Effect = "Tesisler arasi transfer %10 hizlanir" },
                        new ResearchLevelInfo { Level = 2, Name = "Otomatik Hasat",  Effect = "Tarla hasat otomatik (dokunma gerekmiyor)" },
                        new ResearchLevelInfo { Level = 3, Name = "Akilli Siralama", Effect = "Urunler otomatik dogru tesise yonlenir" },
                        new ResearchLevelInfo { Level = 4, Name = "Robot Calisan",   Effect = "Her tesise 1 ucretsiz calisan slotu" },
                        new ResearchLevelInfo { Level = 5, Name = "Yapay Zeka",      Effect = "Tesisler en karli urunu otomatik secer" },
                        new ResearchLevelInfo { Level = 6, Name = "Nano Bakim",      Effect = "Makine bozulma olasiligi -%50" },
                        new ResearchLevelInfo { Level = 7, Name = "Tam Otomasyon",   Effect = "Tum tesisler offline'da %80 verimle calisir" },
                        new ResearchLevelInfo { Level = 8, Name = "Singularite",     Effect = "Otomasyon bonusu 2x (tum etkiler ikiye katlanir)" }
                    }
                },

                // ---- KALITE DALI ----
                // GDD: Kalite -> Kalite kontrol, premium malzeme, gurme tarif, marka gucu
                ["kalite"] = new ResearchBranchDefinition
                {
                    BranchId = "kalite",
                    DisplayName = "Kalite",
                    Description = "Urun kalite carpanini arttirir, premium tarifler acar.",
                    MaxLevel = 8,
                    Levels = new[]
                    {
                        new ResearchLevelInfo { Level = 1, Name = "Kalite Kontrol",    Effect = "1 yildiz urun olasiligi -%20" },
                        new ResearchLevelInfo { Level = 2, Name = "Premium Malzeme",   Effect = "Tum urunlerin taban kalitesi +0.5" },
                        new ResearchLevelInfo { Level = 3, Name = "Usta Sef",          Effect = "Restoran urunleri +1 kalite" },
                        new ResearchLevelInfo { Level = 4, Name = "Organik Sertifika", Effect = "Organik etiketi: satis fiyati +30%" },
                        new ResearchLevelInfo { Level = 5, Name = "Gurme Tarif",       Effect = "Her tesise 1 yeni premium urun tarifi" },
                        new ResearchLevelInfo { Level = 6, Name = "Michelin Yildiz",   Effect = "Restoran satislari 2x" },
                        new ResearchLevelInfo { Level = 7, Name = "Marka Gucu",        Effect = "Tum satis fiyatlari +50%" },
                        new ResearchLevelInfo { Level = 8, Name = "Efsanevi Kalite",   Effect = "5 yildiz urun sansi 2x, efsanevi tarifler acilir" }
                    }
                },

                // ---- HIZ DALI ----
                // GDD: Hiz -> Uretim hizi carpani artisi
                ["hiz"] = new ResearchBranchDefinition
                {
                    BranchId = "hiz",
                    DisplayName = "Hiz",
                    Description = "Uretim hizi carpanini arttirir, tum tesisleri hizlandirir.",
                    MaxLevel = 8,
                    Levels = new[]
                    {
                        new ResearchLevelInfo { Level = 1, Name = "Hizli Hasat",     Effect = "Tarla uretim hizi +15%" },
                        new ResearchLevelInfo { Level = 2, Name = "Turbo Firin",     Effect = "Firin uretim hizi +25%" },
                        new ResearchLevelInfo { Level = 3, Name = "Ekspres Mutfak",  Effect = "Restoran servis hizi +20%" },
                        new ResearchLevelInfo { Level = 4, Name = "Lojistik Agi",    Effect = "Tesisler arasi transfer %30 hizlanir" },
                        new ResearchLevelInfo { Level = 5, Name = "Hiper Isleme",    Effect = "Fabrika uretim hizi +50%" },
                        new ResearchLevelInfo { Level = 6, Name = "Aninda Teslimat", Effect = "Satis suresi (market) %40 azalir" },
                        new ResearchLevelInfo { Level = 7, Name = "Kuantum Uretim",  Effect = "Tum uretim hizlari +75%" },
                        new ResearchLevelInfo { Level = 8, Name = "Zaman Bukucu",    Effect = "Tum hiz bonuslari 2x" }
                    }
                },

                // ---- KAPASITE DALI ----
                // GDD: Kapasite -> Depo kapasitesi, batch uretim
                ["kapasite"] = new ResearchBranchDefinition
                {
                    BranchId = "kapasite",
                    DisplayName = "Kapasite",
                    Description = "Depo kapasitesi, batch uretim ve tesis kopyalama.",
                    MaxLevel = 8,
                    Levels = new[]
                    {
                        new ResearchLevelInfo { Level = 1, Name = "Ek Depo",            Effect = "Stok kapasitesi +25%" },
                        new ResearchLevelInfo { Level = 2, Name = "Genisletilmis Tarla", Effect = "Tarla ciktisi +30%" },
                        new ResearchLevelInfo { Level = 3, Name = "Cift Hat",            Effect = "Her tesise +1 uretim hatti" },
                        new ResearchLevelInfo { Level = 4, Name = "Mega Fabrika",        Effect = "Fabrika kapasitesi 2x" },
                        new ResearchLevelInfo { Level = 5, Name = "Franchise Hazirlik",  Effect = "Ayni tesis turunden 2. tane acilabilir" },
                        new ResearchLevelInfo { Level = 6, Name = "Endustriyel Bolge",   Effect = "Tum tesis kapasiteleri +50%" },
                        new ResearchLevelInfo { Level = 7, Name = "Global Lojistik",     Effect = "Market satis kapasitesi 3x" },
                        new ResearchLevelInfo { Level = 8, Name = "Imparatorluk",        Effect = "Tum kapasite bonuslari 2x, 3. tesis kopyasi acilabilir" }
                    }
                }
            };

        // =====================================================================
        // PUBLIC ERISIMCILER
        // =====================================================================

        /// <summary>Aktif arastirma yapiliyor mu?</summary>
        public bool IsResearching => !string.IsNullOrEmpty(_activeResearchBranch);

        /// <summary>Aktif arastirma dalinin ID'si. Yoksa null.</summary>
        public string ActiveResearchBranch => _activeResearchBranch;

        /// <summary>Aktif arastirmanin ilerleme yuzdesi (0-1).</summary>
        public float ActiveResearchProgress => _activeResearchDuration > 0
            ? Mathf.Clamp01(_activeResearchTimer / _activeResearchDuration)
            : 0f;

        /// <summary>Aktif arastirmanin kalan suresi (saniye).</summary>
        public float ActiveResearchRemainingTime => _activeResearchDuration > 0
            ? Mathf.Max(0f, _activeResearchDuration - _activeResearchTimer)
            : 0f;

        // =====================================================================
        // CONSTRUCTOR
        // =====================================================================

        public ResearchSystem(
            IBalanceConfig config,
            ISaveManager saveManager,
            IEventManager eventManager,
            CurrencySystem currencySystem)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _saveManager = saveManager ?? throw new ArgumentNullException(nameof(saveManager));
            _eventManager = eventManager ?? throw new ArgumentNullException(nameof(eventManager));
            _currencySystem = currencySystem ?? throw new ArgumentNullException(nameof(currencySystem));

            // GameTick eventine abone ol — arastirma zamanlayicisini ilerletmek icin
            _eventManager.Subscribe<GameTickEvent>(OnGameTick);
        }

        // =====================================================================
        // ARASTIRMA BASLAT
        // =====================================================================

        /// <summary>
        /// Belirtilen dalda arastirma baslatir.
        /// Kosullar: Yeterli coin, zaten arastirma yapilmiyor, max seviyede degil.
        ///
        /// Maliyet: BaseCost x 3^(level - 1)
        /// Sure:    BaseTime x 2^(level - 1)
        ///
        /// Referans: docs/GDD.md Bolum 3.4 — Arastirma Maliyet Formulu
        /// </summary>
        /// <param name="branchId">Arastirma dali (otomasyon, kalite, hiz, kapasite)</param>
        /// <returns>Basarili ise true</returns>
        public bool TryStartResearch(string branchId)
        {
            if (!BranchDefinitions.ContainsKey(branchId))
            {
                Debug.LogWarning($"[ResearchSystem] Bilinmeyen arastirma dali: {branchId}");
                return false;
            }

            // Zaten arastirma yapiliyor mu?
            // balance_config.json: research.parallelSlotsFree = 1
            if (IsResearching)
            {
                Debug.Log("[ResearchSystem] Zaten bir arastirma devam ediyor.");
                return false;
            }

            int currentLevel = _saveManager.Data.Research.GetBranchLevel(branchId);
            int maxLevel = _config.GetInt("research.maxLevel", 8);

            if (currentLevel >= maxLevel)
            {
                Debug.Log($"[ResearchSystem] {branchId} zaten maksimum seviyede ({maxLevel}).");
                return false;
            }

            int targetLevel = currentLevel + 1;

            // Maliyet hesapla: BaseCost x 3^(level - 1)
            double cost = CalculateResearchCost(branchId, targetLevel);

            if (!_currencySystem.SpendCoins(cost, $"Research:{branchId}_Lv{targetLevel}"))
            {
                return false;
            }

            // Arastirma suresini hesapla: BaseTime(dk) x 2^(level - 1) -> saniyeye cevir
            float durationSeconds = CalculateResearchTime(branchId, targetLevel);

            _activeResearchBranch = branchId;
            _activeResearchTimer = 0f;
            _activeResearchDuration = durationSeconds;

            Debug.Log($"[ResearchSystem] {branchId} Lv.{targetLevel} arastirmasi basladi. Sure: {durationSeconds:F0}s, Maliyet: {cost:N0}");
            return true;
        }

        // =====================================================================
        // ARASTIRMA TAMAMLA
        // =====================================================================

        /// <summary>
        /// Aktif arastirmayi aninda tamamlar (test/hizlandirma icin).
        /// </summary>
        public void CompleteActiveResearch()
        {
            if (!IsResearching) return;
            FinishResearch();
        }

        /// <summary>Arastirma tamamlandiktan sonra cagirilir.</summary>
        private void FinishResearch()
        {
            string branchId = _activeResearchBranch;
            int currentLevel = _saveManager.Data.Research.GetBranchLevel(branchId);
            int newLevel = currentLevel + 1;

            // Seviyeyi kaydet
            _saveManager.Data.Research.SetBranchLevel(branchId, newLevel);

            // Aktif arastirmayi temizle
            _activeResearchBranch = null;
            _activeResearchTimer = 0f;
            _activeResearchDuration = 0f;

            Debug.Log($"[ResearchSystem] {branchId} Lv.{newLevel} arastirmasi tamamlandi!");

            // Event firlat — UpgradeCompletedEvent (Research tipi)
            _eventManager.Publish(new UpgradeCompletedEvent
            {
                Type = UpgradeType.Research,
                TargetId = branchId,
                NewLevel = newLevel
            });
        }

        // =====================================================================
        // TICK — Arastirma Zamanlayicisi
        // =====================================================================

        /// <summary>
        /// Her frame cagirilir. Aktif arastirmanin zamanlayicisini ilerletir.
        /// Arastirma offline'da da devam eder (AppResumedEvent ile catch-up).
        /// </summary>
        private void OnGameTick(GameTickEvent e)
        {
            if (!IsResearching) return;

            _activeResearchTimer += e.DeltaTime;

            if (_activeResearchTimer >= _activeResearchDuration)
            {
                FinishResearch();
            }
        }

        // =====================================================================
        // MALIYET VE SURE HESAPLAMA
        // =====================================================================

        /// <summary>
        /// Arastirma maliyetini hesaplar.
        /// Formul: BaseCost x 3^(level - 1)
        ///
        /// Referans: balance_config.json
        ///   research.baseCosts.{branchId} = 500
        ///   research.costExponent = 3.0
        /// </summary>
        public double CalculateResearchCost(string branchId, int level)
        {
            float baseCost = _config.GetFloat($"research.baseCosts.{branchId}", 500f);
            float exponent = _config.GetFloat("research.costExponent", 3.0f);
            return baseCost * Math.Pow(exponent, level - 1);
        }

        /// <summary>
        /// Arastirma suresini hesaplar (saniye cinsinden).
        /// Formul: BaseTime(dk) x 2^(level - 1) -> saniyeye cevir
        ///
        /// Referans: balance_config.json
        ///   research.baseTime_minutes.{branchId} = 5
        ///   research.timeExponent = 2.0
        /// </summary>
        public float CalculateResearchTime(string branchId, int level)
        {
            float baseTimeMinutes = _config.GetFloat($"research.baseTime_minutes.{branchId}", 5f);
            float exponent = _config.GetFloat("research.timeExponent", 2.0f);
            float durationMinutes = baseTimeMinutes * (float)Math.Pow(exponent, level - 1);
            return durationMinutes * 60f; // dakikayi saniyeye cevir
        }

        // =====================================================================
        // SORGULAMA METOTLARI (UI ICIN)
        // =====================================================================

        /// <summary>Belirtilen dalın mevcut seviyesini dondurur.</summary>
        public int GetBranchLevel(string branchId)
        {
            return _saveManager.Data.Research.GetBranchLevel(branchId);
        }

        /// <summary>
        /// Belirtilen dal icin UI bilgisini dondurur.
        /// Mevcut seviye, sonraki maliyeti, etki aciklamasi, CanAfford kontrolu.
        /// </summary>
        public ResearchBranchInfo GetBranchInfo(string branchId)
        {
            if (!BranchDefinitions.TryGetValue(branchId, out var definition))
                return null;

            int currentLevel = _saveManager.Data.Research.GetBranchLevel(branchId);
            int maxLevel = _config.GetInt("research.maxLevel", 8);
            bool isMaxed = currentLevel >= maxLevel;

            double nextCost = 0;
            float nextTime = 0;
            string nextEffect = "";

            if (!isMaxed)
            {
                int targetLevel = currentLevel + 1;
                nextCost = CalculateResearchCost(branchId, targetLevel);
                nextTime = CalculateResearchTime(branchId, targetLevel);

                // Seviye bilgisinden etki aciklamasini al
                if (targetLevel - 1 < definition.Levels.Length)
                {
                    nextEffect = definition.Levels[targetLevel - 1].Effect;
                }
            }

            string currentEffect = "";
            if (currentLevel > 0 && currentLevel - 1 < definition.Levels.Length)
            {
                currentEffect = definition.Levels[currentLevel - 1].Effect;
            }

            return new ResearchBranchInfo
            {
                BranchId = branchId,
                DisplayName = definition.DisplayName,
                Description = definition.Description,
                CurrentLevel = currentLevel,
                MaxLevel = maxLevel,
                IsMaxed = isMaxed,
                NextLevelCost = nextCost,
                NextLevelTime = nextTime,
                NextLevelEffect = nextEffect,
                CurrentLevelEffect = currentEffect,
                CanAfford = !isMaxed && _currencySystem.Coins >= nextCost,
                IsBeingResearched = _activeResearchBranch == branchId
            };
        }

        /// <summary>Tum arastirma dallarinin bilgisini dondurur.</summary>
        public List<ResearchBranchInfo> GetAllBranchInfos()
        {
            var infos = new List<ResearchBranchInfo>();
            foreach (var kvp in BranchDefinitions)
            {
                infos.Add(GetBranchInfo(kvp.Key));
            }
            return infos;
        }

        /// <summary>Belirtilen dalın belirtilen seviyesindeki etki aciklamasini dondurur.</summary>
        public string GetLevelEffect(string branchId, int level)
        {
            if (!BranchDefinitions.TryGetValue(branchId, out var definition))
                return "";

            int index = level - 1;
            if (index < 0 || index >= definition.Levels.Length)
                return "";

            return definition.Levels[index].Effect;
        }

        // =====================================================================
        // OFFLINE CATCH-UP
        // =====================================================================

        /// <summary>
        /// Offline surede devam eden arastirmayi hesaplar.
        /// Referans: GDD 5.3 — "Arastirma offline'da devam eder."
        /// </summary>
        public void ProcessOfflineTime(float offlineSeconds)
        {
            if (!IsResearching) return;

            _activeResearchTimer += offlineSeconds;

            if (_activeResearchTimer >= _activeResearchDuration)
            {
                FinishResearch();
            }
        }

        // =====================================================================
        // TEMIZLIK
        // =====================================================================

        /// <summary>EventManager aboneligini temizler.</summary>
        public void Dispose()
        {
            _eventManager.Unsubscribe<GameTickEvent>(OnGameTick);
        }
    }

    // =====================================================================
    // VERI YAPILARI
    // =====================================================================

    /// <summary>Bir arastirma dalinin statik tanimi.</summary>
    public class ResearchBranchDefinition
    {
        public string BranchId;
        public string DisplayName;
        public string Description;
        public int MaxLevel;
        public ResearchLevelInfo[] Levels;
    }

    /// <summary>Tek bir arastirma seviyesinin bilgisi.</summary>
    public class ResearchLevelInfo
    {
        public int Level;
        public string Name;
        public string Effect;
    }

    /// <summary>Arastirma dalinin UI bilgisi.</summary>
    public class ResearchBranchInfo
    {
        public string BranchId;
        public string DisplayName;
        public string Description;
        public int CurrentLevel;
        public int MaxLevel;
        public bool IsMaxed;
        public double NextLevelCost;
        public float NextLevelTime;        // saniye
        public string NextLevelEffect;
        public string CurrentLevelEffect;
        public bool CanAfford;
        public bool IsBeingResearched;
    }
}
