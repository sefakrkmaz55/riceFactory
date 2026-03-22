// =============================================================================
// RemoteConfigManager.cs
// Firebase Remote Config uzerinden ekonomi parametreleri ve feature flag'ler.
// IBalanceConfig interface'ini implement eder (Remote Config versiyonu).
// Fetch & Activate dongusu: her 12 saatte bir.
// FIREBASE_ENABLED tanimli degilse sadece varsayilan degerler kullanilir.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

#if FIREBASE_ENABLED
using Firebase.RemoteConfig;
using Firebase.Extensions;
#endif

namespace RiceFactory.Core
{
    // -------------------------------------------------------------------------
    // Remote Config Manager Arayuzu
    // -------------------------------------------------------------------------

    /// <summary>
    /// Remote Config islemlerini tanimlayan arayuz.
    /// </summary>
    public interface IRemoteConfigManager
    {
        /// <summary>Config degerleri hazir mi?</summary>
        bool IsReady { get; }

        /// <summary>Son fetch zamani (Unix timestamp).</summary>
        long LastFetchTimestamp { get; }

        /// <summary>Remote Config'i cek ve aktif et.</summary>
        Task FetchAndActivateAsync();

        /// <summary>Periyodik fetch tick'i.</summary>
        void Tick(float deltaTime);
    }

    // -------------------------------------------------------------------------
    // Remote Config Manager Implementasyonu
    // -------------------------------------------------------------------------

    /// <summary>
    /// Firebase Remote Config wrapper + IBalanceConfig implementasyonu.
    /// - Remote Config degerlerini ceker ve aktif eder.
    /// - 12 saatte bir otomatik yeniden fetch yapar.
    /// - Firebase yokken balance_config.json varsayilanlari kullanilir.
    /// - IBalanceConfig arayuzunu implement ederek diger sistemlerin
    ///   Remote Config veya lokal config arasinda fark gormemesini saglar.
    /// </summary>
    public class RemoteConfigManager : IRemoteConfigManager, RiceFactory.Data.Save.IBalanceConfig
    {
        // =====================================================================
        // Sabitler
        // =====================================================================

        /// <summary>Otomatik fetch araligi (saniye). 12 saat.</summary>
        private const float FETCH_INTERVAL = 43200f;

        /// <summary>Minimum fetch araligi (saniye). Development icin 5 dk.</summary>
        private const float MIN_FETCH_INTERVAL_DEBUG = 300f;

        // =====================================================================
        // Properties
        // =====================================================================

        public bool IsReady { get; private set; }
        public long LastFetchTimestamp { get; private set; }

        // =====================================================================
        // Bagimliliklar
        // =====================================================================

        private readonly IFirebaseManager _firebaseManager;

        // =====================================================================
        // Dahili Durum
        // =====================================================================

        private float _fetchTimer;
        private bool _isFetching;

        /// <summary>
        /// Varsayilan degerler. Firebase yokken veya fetch basarisiz oldugunda
        /// bu degerler kullanilir. Remote Config key -> varsayilan deger.
        /// </summary>
        private readonly Dictionary<string, object> _defaults;

#if FIREBASE_ENABLED
        private FirebaseRemoteConfig _remoteConfig;
#endif

        // =====================================================================
        // Constructor
        // =====================================================================

        public RemoteConfigManager(IFirebaseManager firebaseManager)
        {
            _firebaseManager = firebaseManager ?? throw new ArgumentNullException(nameof(firebaseManager));

            // Varsayilan ekonomi parametreleri (TECH_ARCHITECTURE.md 6.4'ten)
            _defaults = new Dictionary<string, object>
            {
                // Ekonomi carpanlari
                { "economy_upgrade_cost_base_multiplier", 1.0f },
                { "economy_sell_price_multiplier", 1.0f },
                { "economy_offline_base_efficiency", 0.30f },
                { "economy_offline_max_hours", 8 },
                { "economy_franchise_threshold", 1000000 },
                { "economy_fp_formula_divisor", 1000000 },
                { "economy_daily_free_gems", 10 },
                { "economy_ad_reward_multiplier", 2.0f },
                { "economy_order_refresh_minutes", 15 },
                { "economy_minigame_cooldown_hours", 2 },
                { "economy_combo_max_multiplier", 2.0f },
                { "economy_reputation_bonus_per_100", 0.01f },
                { "economy_star_cost_exponent", 3.0f },
                { "economy_worker_cost_exponent", 2.2f },
                { "economy_research_cost_exponent", 3.0f },
                { "economy_machine_cost_exponent", 5.0f },

                // Etkinlik carpanlari
                { "event_production_multiplier", 1.0f },
                { "event_special_order_multiplier", 1.0f },

                // Battle Pass
                { "battlepass_offline_bonus_hours", 4 },

                // Feature flag'ler
                { "feature_flag_trade_enabled", false },
                { "feature_flag_global_facility", false },

                // Reklam
                { "ad_max_daily_count", 12 },
                { "ad_min_interval_seconds", 180 },

                // --- IBalanceConfig uyumlu anahtarlar (dot-notation) ---
                // Diger sistemler "machine.costExponent" gibi anahtarlar kullanabilir
                { "machine.costExponent", 5.0f },
                { "machine.maxLevel", 50 },
                { "worker.costExponent", 2.2f },
                { "worker.maxLevel", 30 },
                { "star.costExponent", 3.0f },
                { "research.costExponent", 3.0f },
                { "offline.baseEfficiency", 0.30f },
                { "offline.maxHours", 8 },
                { "franchise.threshold", 1000000 },
                { "franchise.fpDivisor", 1000000 },
            };
        }

        // =====================================================================
        // Fetch & Activate
        // =====================================================================

        /// <summary>
        /// Remote Config degerlerini Firebase'den ceker ve aktif eder.
        /// Firebase yokken sadece varsayilan degerleri kullanir.
        /// </summary>
        public async Task FetchAndActivateAsync()
        {
            if (_isFetching)
            {
                Debug.LogWarning("[RemoteConfigManager] Fetch islemi zaten devam ediyor.");
                return;
            }

            _isFetching = true;

#if FIREBASE_ENABLED
            if (_firebaseManager.IsInitialized)
            {
                try
                {
                    _remoteConfig = FirebaseRemoteConfig.DefaultInstance;

                    // Varsayilanlari ayarla
                    await _remoteConfig.SetDefaultsAsync(_defaults);

                    // Fetch
                    await _remoteConfig.FetchAsync(TimeSpan.FromSeconds(
                        Debug.isDebugBuild ? MIN_FETCH_INTERVAL_DEBUG : FETCH_INTERVAL));

                    // Activate
                    var activated = await _remoteConfig.ActivateAsync();

                    LastFetchTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    IsReady = true;

                    Debug.Log($"[RemoteConfigManager] Fetch & Activate basarili. " +
                              $"Yeni degerler aktif: {activated}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[RemoteConfigManager] Fetch hatasi: {ex.Message}");

                    // Hata olsa bile varsayilanlar kullanilabilir
                    IsReady = true;
                }

                _isFetching = false;
                return;
            }
#endif

            // Firebase yok — varsayilanlarla calis
            IsReady = true;
            LastFetchTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            Debug.Log("[RemoteConfigManager] Firebase yok, varsayilan degerler kullaniliyor.");

            _isFetching = false;
            await Task.CompletedTask;
        }

        // =====================================================================
        // Periyodik Fetch Tick
        // =====================================================================

        /// <summary>
        /// Her frame cagirilir. FETCH_INTERVAL'da bir otomatik fetch tetikler.
        /// </summary>
        public void Tick(float deltaTime)
        {
#if FIREBASE_ENABLED
            if (!_firebaseManager.IsInitialized) return;

            _fetchTimer += deltaTime;

            float interval = Debug.isDebugBuild ? MIN_FETCH_INTERVAL_DEBUG : FETCH_INTERVAL;
            if (_fetchTimer >= interval)
            {
                _fetchTimer = 0f;
                _ = FetchAndActivateAsync(); // Fire-and-forget
            }
#endif
        }

        // =====================================================================
        // IBalanceConfig Implementasyonu
        // =====================================================================

        /// <summary>Float deger okur. Once Remote Config, sonra varsayilan.</summary>
        public float GetFloat(string key, float defaultValue = 0f)
        {
#if FIREBASE_ENABLED
            if (_firebaseManager.IsInitialized && _remoteConfig != null)
            {
                var configValue = _remoteConfig.GetValue(key);
                if (configValue.Source != ValueSource.StaticValue)
                {
                    return (float)configValue.DoubleValue;
                }
            }
#endif
            // Varsayilanlardan bak
            if (_defaults.TryGetValue(key, out var val))
            {
                if (val is float f) return f;
                if (val is double d) return (float)d;
                if (val is int i) return i;
            }
            return defaultValue;
        }

        /// <summary>Int deger okur. Once Remote Config, sonra varsayilan.</summary>
        public int GetInt(string key, int defaultValue = 0)
        {
#if FIREBASE_ENABLED
            if (_firebaseManager.IsInitialized && _remoteConfig != null)
            {
                var configValue = _remoteConfig.GetValue(key);
                if (configValue.Source != ValueSource.StaticValue)
                {
                    return (int)configValue.LongValue;
                }
            }
#endif
            if (_defaults.TryGetValue(key, out var val))
            {
                if (val is int i) return i;
                if (val is long l) return (int)l;
                if (val is float f) return (int)f;
                if (val is double d) return (int)d;
            }
            return defaultValue;
        }

        /// <summary>String deger okur. Once Remote Config, sonra varsayilan.</summary>
        public string GetString(string key, string defaultValue = "")
        {
#if FIREBASE_ENABLED
            if (_firebaseManager.IsInitialized && _remoteConfig != null)
            {
                var configValue = _remoteConfig.GetValue(key);
                if (configValue.Source != ValueSource.StaticValue)
                {
                    return configValue.StringValue;
                }
            }
#endif
            if (_defaults.TryGetValue(key, out var val) && val is string s)
            {
                return s;
            }
            return defaultValue;
        }

        /// <summary>Bool deger okur. Once Remote Config, sonra varsayilan.</summary>
        public bool GetBool(string key, bool defaultValue = false)
        {
#if FIREBASE_ENABLED
            if (_firebaseManager.IsInitialized && _remoteConfig != null)
            {
                var configValue = _remoteConfig.GetValue(key);
                if (configValue.Source != ValueSource.StaticValue)
                {
                    return configValue.BooleanValue;
                }
            }
#endif
            if (_defaults.TryGetValue(key, out var val) && val is bool b)
            {
                return b;
            }
            return defaultValue;
        }
    }
}
