// =============================================================================
// TestHelper.cs
// Test altyapisi icin yardimci siniflar, mock'lar ve factory metotlari.
// Tum test dosyalari bu helper'i kullanarak bagimsiz ve tekrarlanabilir
// test ortami olusturur.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using RiceFactory.Core;
using RiceFactory.Core.Events;
using RiceFactory.Data.Save;

namespace RiceFactory.Tests
{
    // =========================================================================
    // Test Ortami Temizleme
    // =========================================================================

    /// <summary>
    /// Tum testlerde kullanilacak ortak setup/teardown islemleri.
    /// ServiceLocator'i temizler ve mock servisleri kaydeder.
    /// </summary>
    public static class TestSetup
    {
        /// <summary>
        /// ServiceLocator'i sifirlar ve temiz bir test ortami olusturur.
        /// Her [SetUp] metodunda cagirilmalidir.
        /// </summary>
        public static void ResetAll()
        {
            ServiceLocator.Reset();
        }

        /// <summary>
        /// Mock EventManager olusturur ve ServiceLocator'a kaydeder.
        /// </summary>
        public static EventManager CreateAndRegisterEventManager()
        {
            var eventManager = new EventManager();
            ServiceLocator.Register<IEventManager>(eventManager);
            return eventManager;
        }

        /// <summary>
        /// Mock SaveManager olusturur ve ServiceLocator'a kaydeder.
        /// </summary>
        public static MockSaveManager CreateAndRegisterSaveManager()
        {
            var saveManager = new MockSaveManager();
            ServiceLocator.Register<ISaveManager>(saveManager);
            return saveManager;
        }

        /// <summary>
        /// Mock TimeManager olusturur ve ServiceLocator'a kaydeder.
        /// </summary>
        public static MockTimeManager CreateAndRegisterTimeManager()
        {
            var timeManager = new MockTimeManager();
            ServiceLocator.Register<ITimeManager>(timeManager);
            return timeManager;
        }
    }

    // =========================================================================
    // Mock SaveManager
    // =========================================================================

    /// <summary>
    /// Test icin SaveManager mock'u.
    /// Dosya sistemine dokunmadan PlayerSaveData yonetir.
    /// </summary>
    public class MockSaveManager : ISaveManager
    {
        public PlayerSaveData Data { get; set; }

        public int SaveLocalCallCount { get; private set; }
        public int SaveAsyncCallCount { get; private set; }
        public int LoadAsyncCallCount { get; private set; }
        public int DeleteSaveCallCount { get; private set; }
        public float TotalTickTime { get; private set; }

        public MockSaveManager()
        {
            Data = SaveDataFactory.CreateDefault();
        }

        public void SaveLocal()
        {
            SaveLocalCallCount++;
        }

        public Task SaveAsync()
        {
            SaveAsyncCallCount++;
            return Task.CompletedTask;
        }

        public Task LoadAsync()
        {
            LoadAsyncCallCount++;
            return Task.CompletedTask;
        }

        public void DeleteSave()
        {
            DeleteSaveCallCount++;
            Data = SaveDataFactory.CreateDefault();
        }

        public void Tick(float deltaTime)
        {
            TotalTickTime += deltaTime;
        }
    }

    // =========================================================================
    // Mock TimeManager
    // =========================================================================

    /// <summary>
    /// Test icin TimeManager mock'u.
    /// Zaman kontrolunu test koduna birakir.
    /// </summary>
    public class MockTimeManager : ITimeManager
    {
        public bool IsTimeReliable { get; set; } = true;
        public float ActivePlayTime { get; set; }
        public float ComboMultiplier { get; set; } = 1.0f;

        public TimeSpan MockPauseDuration { get; set; } = TimeSpan.Zero;
        public int RecordPauseCallCount { get; private set; }
        public int TickCallCount { get; private set; }
        public float TotalTickTime { get; private set; }

        public Task SyncServerTimeAsync()
        {
            return Task.CompletedTask;
        }

        public void RecordPauseTime()
        {
            RecordPauseCallCount++;
        }

        public TimeSpan GetTimeSincePause()
        {
            return MockPauseDuration;
        }

        public void Tick(float deltaTime)
        {
            TickCallCount++;
            TotalTickTime += deltaTime;
            ActivePlayTime += deltaTime;
        }
    }

    // =========================================================================
    // Mock BalanceConfig
    // =========================================================================

    /// <summary>
    /// Test icin IBalanceConfig mock'u.
    /// Varsayilan balance degerlerini dondurur, ozel degerler ayarlanabilir.
    /// </summary>
    public class MockBalanceConfig : IBalanceConfig
    {
        private readonly Dictionary<string, float> _floatValues = new();
        private readonly Dictionary<string, int> _intValues = new();

        public MockBalanceConfig()
        {
            // Varsayilan balance degerleri (balance_config.json'dan)
            SetFloat("machine.costExponent", 5f);
            SetInt("machine.maxLevel", 5);
            SetFloat("worker.costBase", 50f);
            SetFloat("worker.costExponent", 2.2f);
            SetFloat("worker.efficiencyPerLevel", 0.02f);
            SetInt("worker.maxLevel", 50);
            SetFloat("facilityStar.costExponent", 3f);
            SetInt("facilityStar.maxStars", 5);
            SetFloat("research.costExponent", 3f);
            SetInt("research.maxLevel", 8);
            SetFloat("general.globalUpgradeCostMultiplier", 1f);
            SetFloat("general.globalProductionMultiplier", 1f);
            SetFloat("general.globalSellPriceMultiplier", 1f);
            SetFloat("prestige.fpDivisor", 1_000_000f);
            SetFloat("prestige.fpBonusPerStar5", 0.1f);
            SetFloat("prestige.franchiseMinEarnings", 1_000_000f);
        }

        public float GetFloat(string key, float defaultValue = 0f)
        {
            return _floatValues.TryGetValue(key, out var val) ? val : defaultValue;
        }

        public int GetInt(string key, int defaultValue = 0)
        {
            return _intValues.TryGetValue(key, out var val) ? val : defaultValue;
        }

        public string GetString(string key, string defaultValue = "")
        {
            return defaultValue;
        }

        public bool GetBool(string key, bool defaultValue = false)
        {
            return defaultValue;
        }

        public void SetFloat(string key, float value) => _floatValues[key] = value;
        public void SetInt(string key, int value) => _intValues[key] = value;
    }

    // =========================================================================
    // SaveData Factory
    // =========================================================================

    /// <summary>
    /// Test icin PlayerSaveData olusturan factory metotlari.
    /// Farkli senaryolar icin hazir veri setleri sunar.
    /// </summary>
    public static class SaveDataFactory
    {
        /// <summary>Varsayilan (yeni oyuncu) save data olusturur.</summary>
        public static PlayerSaveData CreateDefault()
        {
            return new PlayerSaveData
            {
                PlayerId = "test-player-001",
                PlayerName = "TestPlayer",
                SaveVersion = 1,
                LastSaveTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                FirstPlayTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                GameVersion = "1.0.0",
                Coins = 0,
                Gems = 0,
                FranchisePoints = 0,
                Reputation = 0,
                TotalEarnings = 0,
                TotalLifetimeEarnings = 0,
                PlayerLevel = 1,
                FranchiseCount = 0,
                CurrentCityId = "istanbul",
                HasBattlePass = false,
                BattlePassTier = 0,
                MasterVolume = 1f,
                MusicVolume = 0.7f,
                SFXVolume = 1f,
                Language = "tr",
                NotificationsEnabled = true,
                DataHash = "",
                // Partial class alanlari (TestMocks.cs'den)
                FranchiseBonuses = new FranchiseBonuses(),
                Facilities = new List<FacilityState>(),
                Research = new ResearchData(),
                UnlockedCities = new List<string> { "istanbul" },
                Achievements = new List<string>(),
                CosmeticInventory = new List<string>()
            };
        }

        /// <summary>Zengin oyuncu save data olusturur (ekonomi testleri icin).</summary>
        public static PlayerSaveData CreateRichPlayer(double coins = 1_000_000, int gems = 500)
        {
            var data = CreateDefault();
            data.Coins = coins;
            data.Gems = gems;
            data.FranchisePoints = 50;
            data.Reputation = 1000;
            data.TotalEarnings = coins;
            data.TotalLifetimeEarnings = coins * 2;
            data.PlayerLevel = 10;
            return data;
        }

        /// <summary>Prestige yapabilir durumda save data olusturur.</summary>
        public static PlayerSaveData CreatePrestigeReady(double totalEarnings = 25_000_000)
        {
            var data = CreateDefault();
            data.Coins = 500_000;
            data.TotalEarnings = totalEarnings;
            data.TotalLifetimeEarnings = totalEarnings;
            data.FranchiseCount = 0;
            data.FranchisePoints = 10;
            return data;
        }
    }

    // =========================================================================
    // Assert Helper'lari
    // =========================================================================

    /// <summary>
    /// Oyun mantigi icin ozel assert helper metotlari.
    /// </summary>
    public static class GameAssert
    {
        /// <summary>
        /// Iki double degerin belirli bir tolerans icinde esit oldugunu dogrular.
        /// Ekonomi hesaplamalarinda floating-point hatalari icin.
        /// </summary>
        public static void AreApproximatelyEqual(double expected, double actual, double tolerance = 0.01)
        {
            Assert.That(actual, Is.EqualTo(expected).Within(tolerance),
                $"Beklenen: {expected}, Gercek: {actual}, Tolerans: {tolerance}");
        }

        /// <summary>
        /// Iki float degerin belirli bir tolerans icinde esit oldugunu dogrular.
        /// </summary>
        public static void AreApproximatelyEqual(float expected, float actual, float tolerance = 0.001f)
        {
            Assert.That(actual, Is.EqualTo(expected).Within(tolerance),
                $"Beklenen: {expected}, Gercek: {actual}, Tolerans: {tolerance}");
        }

        /// <summary>
        /// Bir degerin belirli bir aralikta oldugunu dogrular.
        /// </summary>
        public static void IsInRange(double value, double min, double max)
        {
            Assert.That(value, Is.GreaterThanOrEqualTo(min).And.LessThanOrEqualTo(max),
                $"Deger {value}, beklenen aralik [{min}, {max}]");
        }

        /// <summary>
        /// Bir degerin kesinlikle pozitif oldugunu dogrular.
        /// </summary>
        public static void IsPositive(double value, string message = "")
        {
            Assert.That(value, Is.GreaterThan(0),
                string.IsNullOrEmpty(message) ? $"Deger pozitif olmali, gercek: {value}" : message);
        }
    }

    // =========================================================================
    // Event Kaydedici (Test icin)
    // =========================================================================

    /// <summary>
    /// Yayinlanan eventleri kaydederek test dogrulamasi yapmaya olanak tanir.
    /// </summary>
    public class EventRecorder<T> where T : IGameEvent
    {
        public List<T> ReceivedEvents { get; } = new();
        public int Count => ReceivedEvents.Count;
        public T Last => ReceivedEvents.LastOrDefault();
        public bool HasReceived => ReceivedEvents.Count > 0;

        private readonly Action<T> _handler;

        public EventRecorder()
        {
            _handler = e => ReceivedEvents.Add(e);
        }

        /// <summary>EventManager'a subscribe olur.</summary>
        public void SubscribeTo(IEventManager eventManager)
        {
            eventManager.Subscribe(_handler);
        }

        /// <summary>EventManager'dan unsubscribe olur.</summary>
        public void UnsubscribeFrom(IEventManager eventManager)
        {
            eventManager.Unsubscribe(_handler);
        }

        /// <summary>Kayitlari temizler.</summary>
        public void Clear()
        {
            ReceivedEvents.Clear();
        }
    }
}
