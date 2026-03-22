// =============================================================================
// BattlePassSystem.cs
// 28 gunluk sezon sistemi, 30 seviye, ucretsiz + premium track.
// XP kazanma, seviye atlama, odul toplama mekanizmasi.
// Referans: docs/MONETIZATION.md Bolum 4 — Battle Pass / Sezon Karti
// =============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using RiceFactory.Core;
using RiceFactory.Core.Events;

namespace RiceFactory.Economy
{
    // -------------------------------------------------------------------------
    // Interface
    // -------------------------------------------------------------------------

    /// <summary>
    /// Battle Pass sistemi arayuzu.
    /// Sezon yonetimi, XP kazanma, seviye atlama, odul toplama.
    /// </summary>
    public interface IBattlePassSystem
    {
        /// <summary>Mevcut sezon verisi.</summary>
        SeasonData CurrentSeason { get; }

        /// <summary>Mevcut XP.</summary>
        int CurrentXP { get; }

        /// <summary>Mevcut seviye (1-30).</summary>
        int CurrentLevel { get; }

        /// <summary>Mevcut seviyedeki XP ilerlemesi (0.0 - 1.0).</summary>
        float LevelProgress { get; }

        /// <summary>Premium Pass satin alinmis mi?</summary>
        bool IsPremium { get; }

        /// <summary>Sezon bitis tarihine kalan sure.</summary>
        TimeSpan TimeRemaining { get; }

        /// <summary>XP ekler ve seviye atlamayi kontrol eder.</summary>
        void AddXP(int amount, string source);

        /// <summary>Belirtilen seviyedeki ucretsiz odulu toplar.</summary>
        bool ClaimFreeReward(int level);

        /// <summary>Belirtilen seviyedeki premium odulu toplar.</summary>
        bool ClaimPremiumReward(int level);

        /// <summary>Belirtilen seviyenin ucretsiz odulunun toplanip toplanmadigini dondurur.</summary>
        bool IsFreeRewardClaimed(int level);

        /// <summary>Belirtilen seviyenin premium odulunun toplanip toplanmadigini dondurur.</summary>
        bool IsPremiumRewardClaimed(int level);

        /// <summary>Premium Pass'i aktive eder.</summary>
        void ActivatePremium();

        /// <summary>Yeni sezonu baslatir (eski sezon bitmisse).</summary>
        void CheckSeasonTransition();

        /// <summary>Ucretsiz track odullerini dondurur.</summary>
        List<BPReward> GetFreeRewards();

        /// <summary>Premium track odullerini dondurur.</summary>
        List<BPReward> GetPremiumRewards();
    }

    // -------------------------------------------------------------------------
    // Veri Modelleri
    // -------------------------------------------------------------------------

    /// <summary>Sezon verisini temsil eder.</summary>
    [Serializable]
    public class SeasonData
    {
        public int SeasonNumber;
        public string SeasonName;
        public string SeasonTheme;
        public DateTime StartDate;
        public DateTime EndDate;
        public int MaxLevel;
        public int XPPerLevel;
    }

    /// <summary>Battle Pass odul tipi.</summary>
    public enum BPRewardType
    {
        Coin,
        Gem,
        BoostToken,
        OrderRefreshToken,
        MiniGameRefreshToken,
        WorkerBox,
        Cosmetic,
        Frame,
        Badge,
        Emoji,
        NameEffect,
        FactoryTheme
    }

    /// <summary>Tek bir Battle Pass odulunu temsil eder.</summary>
    [Serializable]
    public class BPReward
    {
        public int Level;
        public BPRewardType Type;
        public string DisplayName;
        public int Amount;
        public bool IsPremium;
    }

    // -------------------------------------------------------------------------
    // Event Tanimlamalari
    // -------------------------------------------------------------------------

    /// <summary>Battle Pass seviye atlandiginda tetiklenir.</summary>
    public struct BattlePassLevelUpEvent : IGameEvent
    {
        public int NewLevel;
        public int TotalXP;
    }

    /// <summary>Battle Pass odulu toplandiginda tetiklenir.</summary>
    public struct BattlePassRewardClaimedEvent : IGameEvent
    {
        public int Level;
        public bool IsPremium;
        public BPRewardType RewardType;
        public int Amount;
    }

    /// <summary>Yeni sezon basladiginda tetiklenir.</summary>
    public struct NewSeasonStartedEvent : IGameEvent
    {
        public int SeasonNumber;
        public string SeasonName;
    }

    // -------------------------------------------------------------------------
    // Implementasyon
    // -------------------------------------------------------------------------

    /// <summary>
    /// Battle Pass sistemi.
    /// 28 gunluk sezon, 30 seviye, seviye basi 300 XP.
    /// Ucretsiz ve premium track odulleri MONETIZATION.md'den alinmistir.
    /// </summary>
    public class BattlePassSystem : IBattlePassSystem
    {
        // --- Sabitler (MONETIZATION.md 4.1) ---
        private const int SEASON_DURATION_DAYS = 28;
        private const int MAX_LEVEL = 30;
        private const int XP_PER_LEVEL = 300;
        private const int TOTAL_XP_REQUIRED = MAX_LEVEL * XP_PER_LEVEL; // 9000

        private readonly IEventManager _eventManager;
        private readonly ISaveManager _saveManager;

        // Sezon verisi
        private SeasonData _currentSeason;
        private int _currentXP;
        private bool _isPremium;

        // Toplanan oduller: key = "free_5" veya "premium_10"
        private readonly HashSet<string> _claimedRewards = new();

        // Odul tanimlari
        private readonly List<BPReward> _freeRewards;
        private readonly List<BPReward> _premiumRewards;

        // --- Public Erisimciler ---
        public SeasonData CurrentSeason => _currentSeason;
        public int CurrentXP => _currentXP;
        public int CurrentLevel => Mathf.Clamp(_currentXP / XP_PER_LEVEL + 1, 1, MAX_LEVEL);
        public bool IsPremium => _isPremium;

        public float LevelProgress
        {
            get
            {
                if (CurrentLevel >= MAX_LEVEL) return 1f;
                int levelStartXP = (CurrentLevel - 1) * XP_PER_LEVEL;
                return (float)(_currentXP - levelStartXP) / XP_PER_LEVEL;
            }
        }

        public TimeSpan TimeRemaining
        {
            get
            {
                if (_currentSeason == null) return TimeSpan.Zero;
                var remaining = _currentSeason.EndDate - DateTime.UtcNow;
                return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
            }
        }

        // =====================================================================
        // CONSTRUCTOR
        // =====================================================================

        public BattlePassSystem(IEventManager eventManager, ISaveManager saveManager)
        {
            _eventManager = eventManager ?? throw new ArgumentNullException(nameof(eventManager));
            _saveManager = saveManager ?? throw new ArgumentNullException(nameof(saveManager));

            _freeRewards = BuildFreeRewards();
            _premiumRewards = BuildPremiumRewards();

            // Save'den yukle
            LoadFromSave();

            // Sezon gecisi kontrolu
            CheckSeasonTransition();
        }

        // =====================================================================
        // XP KAZANMA
        // =====================================================================

        /// <summary>
        /// XP ekler. Kaynak ornekleri: daily_quest, weekly_quest, order, mini_game, star, franchise
        /// MONETIZATION.md 4.2'deki XP kaynaklarina uygun.
        /// </summary>
        public void AddXP(int amount, string source)
        {
            if (amount <= 0) return;
            if (_currentSeason == null || TimeRemaining <= TimeSpan.Zero) return;

            int oldLevel = CurrentLevel;
            _currentXP = Mathf.Min(_currentXP + amount, TOTAL_XP_REQUIRED);

            int newLevel = CurrentLevel;

            if (newLevel > oldLevel)
            {
                _eventManager.Publish(new BattlePassLevelUpEvent
                {
                    NewLevel = newLevel,
                    TotalXP = _currentXP
                });

                Debug.Log($"[BattlePass] Seviye atlandi: {oldLevel} -> {newLevel} (XP: {_currentXP}, Kaynak: {source})");
            }

            SaveToData();
        }

        // =====================================================================
        // ODUL TOPLAMA
        // =====================================================================

        public bool ClaimFreeReward(int level)
        {
            if (level < 1 || level > MAX_LEVEL) return false;
            if (level > CurrentLevel) return false;

            string key = $"free_{level}";
            if (_claimedRewards.Contains(key)) return false;

            var reward = _freeRewards.Find(r => r.Level == level);
            if (reward == null) return false;

            _claimedRewards.Add(key);
            GrantReward(reward);
            SaveToData();

            _eventManager.Publish(new BattlePassRewardClaimedEvent
            {
                Level = level,
                IsPremium = false,
                RewardType = reward.Type,
                Amount = reward.Amount
            });

            return true;
        }

        public bool ClaimPremiumReward(int level)
        {
            if (!_isPremium) return false;
            if (level < 1 || level > MAX_LEVEL) return false;
            if (level > CurrentLevel) return false;

            string key = $"premium_{level}";
            if (_claimedRewards.Contains(key)) return false;

            var reward = _premiumRewards.Find(r => r.Level == level);
            if (reward == null) return false;

            _claimedRewards.Add(key);
            GrantReward(reward);
            SaveToData();

            _eventManager.Publish(new BattlePassRewardClaimedEvent
            {
                Level = level,
                IsPremium = true,
                RewardType = reward.Type,
                Amount = reward.Amount
            });

            return true;
        }

        public bool IsFreeRewardClaimed(int level) => _claimedRewards.Contains($"free_{level}");
        public bool IsPremiumRewardClaimed(int level) => _claimedRewards.Contains($"premium_{level}");

        // =====================================================================
        // PREMIUM AKTIVASYON
        // =====================================================================

        public void ActivatePremium()
        {
            if (_isPremium)
            {
                Debug.LogWarning("[BattlePass] Premium zaten aktif.");
                return;
            }

            _isPremium = true;
            SaveToData();
            Debug.Log("[BattlePass] Premium Pass aktive edildi.");
        }

        // =====================================================================
        // SEZON GECISI
        // =====================================================================

        public void CheckSeasonTransition()
        {
            if (_currentSeason == null || TimeRemaining <= TimeSpan.Zero)
            {
                StartNewSeason();
            }
        }

        private void StartNewSeason()
        {
            int nextSeasonNumber = (_currentSeason?.SeasonNumber ?? 0) + 1;

            // Sezon temalari (MONETIZATION.md 4.6)
            var themes = new[]
            {
                ("Bahar Festivali", "sakura"),
                ("Yaz Barbekusu", "beach"),
                ("Okyanus Kesfi", "ocean"),
                ("Uzay Macerasi", "space"),
                ("Orman Kacamagi", "forest"),
                ("Okula Donus", "school"),
                ("Hasat Bayrami", "harvest"),
                ("Cadilar Bayrami", "halloween"),
                ("Kis Soleni", "winter"),
                ("Yeni Yil", "newyear"),
                ("Sevgililer Gunu", "valentine"),
                ("Karnaval", "carnival")
            };

            int themeIndex = (nextSeasonNumber - 1) % themes.Length;

            _currentSeason = new SeasonData
            {
                SeasonNumber = nextSeasonNumber,
                SeasonName = themes[themeIndex].Item1,
                SeasonTheme = themes[themeIndex].Item2,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(SEASON_DURATION_DAYS),
                MaxLevel = MAX_LEVEL,
                XPPerLevel = XP_PER_LEVEL
            };

            _currentXP = 0;
            _isPremium = false;
            _claimedRewards.Clear();

            SaveToData();

            _eventManager.Publish(new NewSeasonStartedEvent
            {
                SeasonNumber = nextSeasonNumber,
                SeasonName = _currentSeason.SeasonName
            });

            Debug.Log($"[BattlePass] Yeni sezon basladi: #{nextSeasonNumber} - {_currentSeason.SeasonName}");
        }

        // =====================================================================
        // ODUL VERME
        // =====================================================================

        private void GrantReward(BPReward reward)
        {
            if (!ServiceLocator.TryGet<IEconomySystem>(out var economy)) return;

            switch (reward.Type)
            {
                case BPRewardType.Coin:
                    economy.AddCurrency(CurrencyType.Coin, reward.Amount, "battle_pass");
                    break;
                case BPRewardType.Gem:
                    economy.AddCurrency(CurrencyType.Gem, reward.Amount, "battle_pass");
                    break;
                // Diger odul tipleri ilgili sistemler tarafindan islenir
                // (boost token, kozmetik, cerceve vb.)
                default:
                    Debug.Log($"[BattlePass] Odul verildi: {reward.DisplayName} x{reward.Amount} (tip: {reward.Type})");
                    break;
            }
        }

        // =====================================================================
        // SAVE / LOAD
        // =====================================================================

        private void SaveToData()
        {
            // Save manager'a battle pass verisini yaz
            // NOT: PlayerSaveData'ya BattlePass alanlari eklenmis varsayilir
            _saveManager.SaveLocal();
        }

        private void LoadFromSave()
        {
            // TODO: PlayerSaveData'dan battle pass verilerini yukle
            // _currentXP = _saveManager.Data.BattlePassXP;
            // _isPremium = _saveManager.Data.BattlePassPremium;
            // _claimedRewards from _saveManager.Data.BattlePassClaimedRewards;
            // _currentSeason from _saveManager.Data.BattlePassSeason;

            // Varsayilan: yeni sezon baslatilacak
        }

        // =====================================================================
        // ODUL TANIMLARI (MONETIZATION.md 4.3 ve 4.4)
        // =====================================================================

        private static List<BPReward> BuildFreeRewards()
        {
            return new List<BPReward>
            {
                new BPReward { Level = 1,  Type = BPRewardType.Coin, DisplayName = "500 Coin", Amount = 500 },
                new BPReward { Level = 2,  Type = BPRewardType.BoostToken, DisplayName = "Uretim Boost (30dk)", Amount = 1 },
                new BPReward { Level = 3,  Type = BPRewardType.Coin, DisplayName = "1,000 Coin", Amount = 1000 },
                new BPReward { Level = 4,  Type = BPRewardType.OrderRefreshToken, DisplayName = "Siparis Yenileme", Amount = 1 },
                new BPReward { Level = 5,  Type = BPRewardType.Coin, DisplayName = "2,500 Coin", Amount = 2500 },
                new BPReward { Level = 6,  Type = BPRewardType.Gem, DisplayName = "10 Elmas", Amount = 10 },
                new BPReward { Level = 7,  Type = BPRewardType.MiniGameRefreshToken, DisplayName = "Mini-game Yenileme", Amount = 1 },
                new BPReward { Level = 8,  Type = BPRewardType.Coin, DisplayName = "5,000 Coin", Amount = 5000 },
                new BPReward { Level = 9,  Type = BPRewardType.BoostToken, DisplayName = "Uretim Boost (1s)", Amount = 1 },
                new BPReward { Level = 10, Type = BPRewardType.Frame, DisplayName = "Sezon Cercevesi (Ucretsiz)", Amount = 1 },
                new BPReward { Level = 11, Type = BPRewardType.Coin, DisplayName = "7,500 Coin", Amount = 7500 },
                new BPReward { Level = 12, Type = BPRewardType.Gem, DisplayName = "15 Elmas", Amount = 15 },
                new BPReward { Level = 13, Type = BPRewardType.OrderRefreshToken, DisplayName = "2x Siparis Yenileme", Amount = 2 },
                new BPReward { Level = 14, Type = BPRewardType.Coin, DisplayName = "10,000 Coin", Amount = 10000 },
                new BPReward { Level = 15, Type = BPRewardType.WorkerBox, DisplayName = "Nadir Calisan Kutusu", Amount = 1 },
                new BPReward { Level = 16, Type = BPRewardType.Gem, DisplayName = "20 Elmas", Amount = 20 },
                new BPReward { Level = 17, Type = BPRewardType.Coin, DisplayName = "15,000 Coin", Amount = 15000 },
                new BPReward { Level = 18, Type = BPRewardType.BoostToken, DisplayName = "2x Uretim Boost (1s)", Amount = 2 },
                new BPReward { Level = 19, Type = BPRewardType.Gem, DisplayName = "25 Elmas", Amount = 25 },
                new BPReward { Level = 20, Type = BPRewardType.Cosmetic, DisplayName = "Sezon Dekorasyon (Ucretsiz)", Amount = 1 },
                new BPReward { Level = 21, Type = BPRewardType.Coin, DisplayName = "20,000 Coin", Amount = 20000 },
                new BPReward { Level = 22, Type = BPRewardType.Gem, DisplayName = "30 Elmas", Amount = 30 },
                new BPReward { Level = 23, Type = BPRewardType.OrderRefreshToken, DisplayName = "3x Siparis Yenileme", Amount = 3 },
                new BPReward { Level = 24, Type = BPRewardType.Coin, DisplayName = "30,000 Coin", Amount = 30000 },
                new BPReward { Level = 25, Type = BPRewardType.WorkerBox, DisplayName = "Epik Calisan Kutusu", Amount = 1 },
                new BPReward { Level = 26, Type = BPRewardType.Gem, DisplayName = "40 Elmas", Amount = 40 },
                new BPReward { Level = 27, Type = BPRewardType.Coin, DisplayName = "50,000 Coin", Amount = 50000 },
                new BPReward { Level = 28, Type = BPRewardType.Gem, DisplayName = "50 Elmas", Amount = 50 },
                new BPReward { Level = 29, Type = BPRewardType.Coin, DisplayName = "75,000 Coin", Amount = 75000 },
                new BPReward { Level = 30, Type = BPRewardType.Badge, DisplayName = "100 Elmas + Sezon Gazisi Rozeti", Amount = 100 }
            };
        }

        private static List<BPReward> BuildPremiumRewards()
        {
            return new List<BPReward>
            {
                new BPReward { Level = 1,  Type = BPRewardType.Coin, DisplayName = "2,000 Coin + 20 Elmas", Amount = 2000, IsPremium = true },
                new BPReward { Level = 2,  Type = BPRewardType.BoostToken, DisplayName = "2x Uretim Boost (1s)", Amount = 2, IsPremium = true },
                new BPReward { Level = 3,  Type = BPRewardType.Coin, DisplayName = "5,000 Coin", Amount = 5000, IsPremium = true },
                new BPReward { Level = 4,  Type = BPRewardType.Cosmetic, DisplayName = "Sezon Calisan Kiyafeti", Amount = 1, IsPremium = true },
                new BPReward { Level = 5,  Type = BPRewardType.FactoryTheme, DisplayName = "Sezon Fabrika Temasi", Amount = 1, IsPremium = true },
                new BPReward { Level = 6,  Type = BPRewardType.Gem, DisplayName = "50 Elmas", Amount = 50, IsPremium = true },
                new BPReward { Level = 7,  Type = BPRewardType.MiniGameRefreshToken, DisplayName = "3x Mini-game Yenileme", Amount = 3, IsPremium = true },
                new BPReward { Level = 8,  Type = BPRewardType.Coin, DisplayName = "10,000 Coin", Amount = 10000, IsPremium = true },
                new BPReward { Level = 9,  Type = BPRewardType.Emoji, DisplayName = "Sezon Emoji Paketi (5)", Amount = 5, IsPremium = true },
                new BPReward { Level = 10, Type = BPRewardType.Frame, DisplayName = "Premium Cerceve (Animasyonlu)", Amount = 1, IsPremium = true },
                new BPReward { Level = 11, Type = BPRewardType.Coin, DisplayName = "15,000 Coin + 30 Elmas", Amount = 15000, IsPremium = true },
                new BPReward { Level = 12, Type = BPRewardType.BoostToken, DisplayName = "3x Uretim Boost (2s)", Amount = 3, IsPremium = true },
                new BPReward { Level = 13, Type = BPRewardType.Coin, DisplayName = "20,000 Coin", Amount = 20000, IsPremium = true },
                new BPReward { Level = 14, Type = BPRewardType.NameEffect, DisplayName = "Sezon Isim Efekti", Amount = 1, IsPremium = true },
                new BPReward { Level = 15, Type = BPRewardType.Cosmetic, DisplayName = "Nadir Tonton Kiyafeti", Amount = 1, IsPremium = true },
                new BPReward { Level = 16, Type = BPRewardType.Gem, DisplayName = "75 Elmas", Amount = 75, IsPremium = true },
                new BPReward { Level = 17, Type = BPRewardType.Coin, DisplayName = "30,000 Coin", Amount = 30000, IsPremium = true },
                new BPReward { Level = 18, Type = BPRewardType.OrderRefreshToken, DisplayName = "5x Siparis Yenileme", Amount = 5, IsPremium = true },
                new BPReward { Level = 19, Type = BPRewardType.Coin, DisplayName = "40,000 Coin", Amount = 40000, IsPremium = true },
                new BPReward { Level = 20, Type = BPRewardType.Cosmetic, DisplayName = "Efsanevi Dekorasyon Seti (5)", Amount = 5, IsPremium = true },
                new BPReward { Level = 21, Type = BPRewardType.Coin, DisplayName = "50,000 Coin + 50 Elmas", Amount = 50000, IsPremium = true },
                new BPReward { Level = 22, Type = BPRewardType.WorkerBox, DisplayName = "Efsanevi Calisan Kutusu", Amount = 1, IsPremium = true },
                new BPReward { Level = 23, Type = BPRewardType.Coin, DisplayName = "75,000 Coin", Amount = 75000, IsPremium = true },
                new BPReward { Level = 24, Type = BPRewardType.Cosmetic, DisplayName = "Animasyonlu Profil Arkaplan", Amount = 1, IsPremium = true },
                new BPReward { Level = 25, Type = BPRewardType.FactoryTheme, DisplayName = "Efsanevi Fabrika Temasi", Amount = 1, IsPremium = true },
                new BPReward { Level = 26, Type = BPRewardType.Gem, DisplayName = "100 Elmas", Amount = 100, IsPremium = true },
                new BPReward { Level = 27, Type = BPRewardType.Coin, DisplayName = "100,000 Coin", Amount = 100000, IsPremium = true },
                new BPReward { Level = 28, Type = BPRewardType.Cosmetic, DisplayName = "Sezon Mini-game Efekti", Amount = 1, IsPremium = true },
                new BPReward { Level = 29, Type = BPRewardType.Coin, DisplayName = "150,000 Coin + 75 Elmas", Amount = 150000, IsPremium = true },
                new BPReward { Level = 30, Type = BPRewardType.Cosmetic, DisplayName = "Efsanevi Tonton + Sampiyon Unvani + 200 Elmas", Amount = 200, IsPremium = true }
            };
        }

        // =====================================================================
        // PUBLIC ODUL ERISIMI (UI icin)
        // =====================================================================

        /// <summary>Ucretsiz track odul listesi.</summary>
        public List<BPReward> GetFreeRewards() => _freeRewards;

        /// <summary>Premium track odul listesi.</summary>
        public List<BPReward> GetPremiumRewards() => _premiumRewards;
    }
}
