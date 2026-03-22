// =============================================================================
// CloudSaveManager.cs
// Firestore uzerinden bulut kayit ve lokal save senkronizasyonu.
// Conflict resolution: en yeni timestamp kazanir.
// Otomatik bulut kayit: her 5 dakikada veya onemli event'lerde.
// FIREBASE_ENABLED tanimli degilse sadece lokal save kullanir.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

#if FIREBASE_ENABLED
using Firebase.Firestore;
using Firebase.Extensions;
#endif

namespace RiceFactory.Core
{
    // -------------------------------------------------------------------------
    // Cloud Save Manager Arayuzu
    // -------------------------------------------------------------------------

    /// <summary>
    /// Bulut kayit islemlerini tanimlayan arayuz.
    /// </summary>
    public interface ICloudSaveManager
    {
        /// <summary>Son bulut kayit zamani (Unix timestamp).</summary>
        long LastCloudSaveTimestamp { get; }

        /// <summary>Bulut kayit aktif mi?</summary>
        bool IsCloudSaveEnabled { get; }

        /// <summary>Veriyi buluta kaydeder.</summary>
        Task<bool> SaveToCloudAsync();

        /// <summary>Buluttan veri yukler.</summary>
        Task<bool> LoadFromCloudAsync();

        /// <summary>Lokal ve bulut veriyi senkronize eder (en yeni kazanir).</summary>
        Task SyncAsync();

        /// <summary>Her frame cagirilir — otomatik bulut kayit zamanlayicisi.</summary>
        void Tick(float deltaTime);

        /// <summary>Onemli event sonrasi tetiklenen anlik bulut kayit.</summary>
        Task TriggerCloudSaveAsync();
    }

    // -------------------------------------------------------------------------
    // Cloud Save Manager Implementasyonu
    // -------------------------------------------------------------------------

    /// <summary>
    /// Firestore uzerinden bulut kayit yonetimi.
    /// - PlayerSaveData'yi Firestore'a yazar ve okur.
    /// - Lokal ile bulut arasinda conflict resolution: en yeni timestamp kazanir.
    /// - Otomatik bulut kayit her 5 dakikada bir.
    /// - Firebase yokken tum bulut islemleri no-op, sadece lokal save calisir.
    ///
    /// Firestore yolu: players/{userId}/save_data/current
    /// </summary>
    public class CloudSaveManager : ICloudSaveManager
    {
        // =====================================================================
        // Sabitler
        // =====================================================================

        /// <summary>Otomatik bulut kayit araligi (saniye). 5 dakika.</summary>
        private const float CLOUD_SAVE_INTERVAL = 300f;

        /// <summary>Art arda basarisiz deneme siniri.</summary>
        private const int MAX_RETRY_FAILURES = 3;

        // =====================================================================
        // Properties
        // =====================================================================

        public long LastCloudSaveTimestamp { get; private set; }
        public bool IsCloudSaveEnabled => _firebaseManager.IsInitialized && _authManager.IsSignedIn;

        // =====================================================================
        // Bagimliliklar
        // =====================================================================

        private readonly IFirebaseManager _firebaseManager;
        private readonly IAuthManager _authManager;
        private readonly ISaveManager _saveManager;

        // =====================================================================
        // Dahili Durum
        // =====================================================================

        private float _cloudSaveTimer;
        private int _consecutiveFailures;
        private bool _isSaving;
        private bool _isLoading;

#if FIREBASE_ENABLED
        private FirebaseFirestore _firestore;
#endif

        // =====================================================================
        // Constructor
        // =====================================================================

        public CloudSaveManager(
            IFirebaseManager firebaseManager,
            IAuthManager authManager,
            ISaveManager saveManager)
        {
            _firebaseManager = firebaseManager ?? throw new ArgumentNullException(nameof(firebaseManager));
            _authManager = authManager ?? throw new ArgumentNullException(nameof(authManager));
            _saveManager = saveManager ?? throw new ArgumentNullException(nameof(saveManager));

#if FIREBASE_ENABLED
            if (_firebaseManager.IsInitialized)
            {
                _firestore = FirebaseFirestore.DefaultInstance;
            }
#endif
        }

        // =====================================================================
        // Buluta Kaydetme
        // =====================================================================

        /// <summary>
        /// Mevcut PlayerSaveData'yi Firestore'a kaydeder.
        /// Firebase yokken false dondurur.
        /// </summary>
        public async Task<bool> SaveToCloudAsync()
        {
            if (!IsCloudSaveEnabled)
            {
                Debug.Log("[CloudSaveManager] Bulut kayit devre disi — Firebase yok veya oturum yok.");
                return false;
            }

            if (_isSaving)
            {
                Debug.LogWarning("[CloudSaveManager] Kayit islemi zaten devam ediyor, atlanıyor.");
                return false;
            }

            _isSaving = true;

#if FIREBASE_ENABLED
            try
            {
                // Lokal kaydi guncelle
                _saveManager.SaveLocal();
                var data = _saveManager.Data;

                if (data == null)
                {
                    Debug.LogError("[CloudSaveManager] Kaydedilecek veri yok!");
                    _isSaving = false;
                    return false;
                }

                // Firestore document referansi
                var docRef = _firestore
                    .Collection("players")
                    .Document(_authManager.UserId)
                    .Collection("save_data")
                    .Document("current");

                // PlayerSaveData'yi dictionary'ye cevir
                var saveDict = SerializeSaveData(data);

                await docRef.SetAsync(saveDict);

                LastCloudSaveTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                _consecutiveFailures = 0;

                Debug.Log($"[CloudSaveManager] Bulut kayit basarili. Versiyon: {data.SaveVersion}");
                _isSaving = false;
                return true;
            }
            catch (Exception ex)
            {
                _consecutiveFailures++;
                Debug.LogError($"[CloudSaveManager] Bulut kayit hatasi ({_consecutiveFailures}/{MAX_RETRY_FAILURES}): {ex.Message}");
                _isSaving = false;
                return false;
            }
#else
            await Task.CompletedTask;
            _isSaving = false;
            return false;
#endif
        }

        // =====================================================================
        // Buluttan Yukleme
        // =====================================================================

        /// <summary>
        /// Firestore'dan PlayerSaveData yukler.
        /// Firebase yokken false dondurur.
        /// </summary>
        public async Task<bool> LoadFromCloudAsync()
        {
            if (!IsCloudSaveEnabled)
            {
                Debug.Log("[CloudSaveManager] Bulut yukleme devre disi — Firebase yok veya oturum yok.");
                return false;
            }

            if (_isLoading)
            {
                Debug.LogWarning("[CloudSaveManager] Yukleme islemi zaten devam ediyor, atlanıyor.");
                return false;
            }

            _isLoading = true;

#if FIREBASE_ENABLED
            try
            {
                var docRef = _firestore
                    .Collection("players")
                    .Document(_authManager.UserId)
                    .Collection("save_data")
                    .Document("current");

                var snapshot = await docRef.GetSnapshotAsync();

                if (!snapshot.Exists)
                {
                    Debug.Log("[CloudSaveManager] Bulutta kayit bulunamadi.");
                    _isLoading = false;
                    return false;
                }

                var cloudData = DeserializeSaveData(snapshot);

                if (cloudData != null)
                {
                    // Conflict resolution: dogrudan lokal veriyi guncelle
                    // (Bu metot sadece "buluttan yukle" amacli, sync icin SyncAsync kullanilmali)
                    ApplyCloudDataToLocal(cloudData);

                    Debug.Log($"[CloudSaveManager] Bulut verisi yuklendi. Versiyon: {cloudData.SaveVersion}");
                    _isLoading = false;
                    return true;
                }

                _isLoading = false;
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CloudSaveManager] Bulut yukleme hatasi: {ex.Message}");
                _isLoading = false;
                return false;
            }
#else
            await Task.CompletedTask;
            _isLoading = false;
            return false;
#endif
        }

        // =====================================================================
        // Senkronizasyon (Conflict Resolution)
        // =====================================================================

        /// <summary>
        /// Lokal ve bulut verisini karsilastirir, en yeni olan kazanir.
        /// Boot sirasinda cagirilmali.
        /// </summary>
        public async Task SyncAsync()
        {
            if (!IsCloudSaveEnabled)
            {
                Debug.Log("[CloudSaveManager] Sync atlanıyor — Firebase yok veya oturum yok.");
                return;
            }

#if FIREBASE_ENABLED
            try
            {
                var docRef = _firestore
                    .Collection("players")
                    .Document(_authManager.UserId)
                    .Collection("save_data")
                    .Document("current");

                var snapshot = await docRef.GetSnapshotAsync();

                var localData = _saveManager.Data;

                if (!snapshot.Exists)
                {
                    // Bulutta veri yok — lokali buluta yaz
                    Debug.Log("[CloudSaveManager] Bulutta veri yok, lokal veri yukleniyor...");
                    await SaveToCloudAsync();
                    return;
                }

                var cloudData = DeserializeSaveData(snapshot);

                if (cloudData == null)
                {
                    Debug.LogWarning("[CloudSaveManager] Bulut verisi okunamadi, lokal veri korunuyor.");
                    return;
                }

                // Conflict resolution: en yeni timestamp kazanir
                if (localData == null || cloudData.LastSaveTimestamp > localData.LastSaveTimestamp)
                {
                    // Bulut daha yeni — lokale uygula
                    Debug.Log($"[CloudSaveManager] Bulut verisi daha yeni " +
                              $"(bulut: {cloudData.LastSaveTimestamp}, lokal: {localData?.LastSaveTimestamp ?? 0}). " +
                              "Bulut verisi uygulanıyor.");
                    ApplyCloudDataToLocal(cloudData);
                }
                else if (localData.LastSaveTimestamp > cloudData.LastSaveTimestamp)
                {
                    // Lokal daha yeni — buluta yaz
                    Debug.Log($"[CloudSaveManager] Lokal veri daha yeni " +
                              $"(lokal: {localData.LastSaveTimestamp}, bulut: {cloudData.LastSaveTimestamp}). " +
                              "Buluta kaydediliyor.");
                    await SaveToCloudAsync();
                }
                else
                {
                    Debug.Log("[CloudSaveManager] Lokal ve bulut verisi senkron.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CloudSaveManager] Sync hatasi: {ex.Message}");
                // Hata durumunda lokal veri korunur, veri kaybi riski yok
            }
#else
            await Task.CompletedTask;
#endif
        }

        // =====================================================================
        // Otomatik Bulut Kayit (Tick)
        // =====================================================================

        /// <summary>
        /// Her frame cagirilir. CLOUD_SAVE_INTERVAL'da bir otomatik
        /// bulut kayit tetikler. Art arda cok fazla hata olursa durur.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (!IsCloudSaveEnabled) return;
            if (_consecutiveFailures >= MAX_RETRY_FAILURES) return;

            _cloudSaveTimer += deltaTime;
            if (_cloudSaveTimer >= CLOUD_SAVE_INTERVAL)
            {
                _cloudSaveTimer = 0f;
                // Fire-and-forget — hata SaveToCloudAsync icinde loglanir
                _ = SaveToCloudAsync();
            }
        }

        // =====================================================================
        // Onemli Event Sonrasi Anlik Kayit
        // =====================================================================

        /// <summary>
        /// Franchise, IAP, onemli milestone gibi kritik event'lerden sonra
        /// hemen bulut kayit tetikler. Zamanlayiciyi da sifirlar.
        /// </summary>
        public async Task TriggerCloudSaveAsync()
        {
            _cloudSaveTimer = 0f; // Zamanlayiciyi sifirla (cift kaydi onle)
            await SaveToCloudAsync();
        }

        // =====================================================================
        // Serialization Yardimcilari
        // =====================================================================

#if FIREBASE_ENABLED
        /// <summary>
        /// PlayerSaveData'yi Firestore'a yazilabilir dictionary'ye cevirir.
        /// </summary>
        private Dictionary<string, object> SerializeSaveData(PlayerSaveData data)
        {
            return new Dictionary<string, object>
            {
                // Ust duzey meta
                ["version"] = data.SaveVersion,
                ["timestamp"] = FieldValue.ServerTimestamp,
                ["checksum"] = data.DataHash ?? "",

                // Tam save JSON — Firestore'da map olarak
                ["data"] = new Dictionary<string, object>
                {
                    ["playerId"] = data.PlayerId ?? "",
                    ["playerName"] = data.PlayerName ?? "",
                    ["saveVersion"] = data.SaveVersion,
                    ["lastSaveTimestamp"] = data.LastSaveTimestamp,
                    ["firstPlayTimestamp"] = data.FirstPlayTimestamp,
                    ["gameVersion"] = data.GameVersion ?? "",

                    ["coins"] = data.Coins,
                    ["gems"] = data.Gems,
                    ["franchisePoints"] = data.FranchisePoints,
                    ["reputation"] = data.Reputation,
                    ["totalEarnings"] = data.TotalEarnings,
                    ["totalLifetimeEarnings"] = data.TotalLifetimeEarnings,

                    ["playerLevel"] = data.PlayerLevel,
                    ["franchiseCount"] = data.FranchiseCount,
                    ["currentCityId"] = data.CurrentCityId ?? "istanbul",
                    ["hasBattlePass"] = data.HasBattlePass,
                    ["battlePassTier"] = data.BattlePassTier,

                    ["masterVolume"] = (double)data.MasterVolume,
                    ["musicVolume"] = (double)data.MusicVolume,
                    ["sfxVolume"] = (double)data.SFXVolume,
                    ["language"] = data.Language ?? "tr",
                    ["notificationsEnabled"] = data.NotificationsEnabled,

                    ["dataHash"] = data.DataHash ?? ""
                }
            };
        }

        /// <summary>
        /// Firestore snapshot'indan PlayerSaveData olusturur.
        /// </summary>
        private PlayerSaveData DeserializeSaveData(DocumentSnapshot snapshot)
        {
            try
            {
                if (!snapshot.ContainsField("data")) return null;

                var dataMap = snapshot.GetValue<Dictionary<string, object>>("data");
                if (dataMap == null) return null;

                return new PlayerSaveData
                {
                    PlayerId = GetStringValue(dataMap, "playerId", ""),
                    PlayerName = GetStringValue(dataMap, "playerName", ""),
                    SaveVersion = GetIntValue(dataMap, "saveVersion", 1),
                    LastSaveTimestamp = GetLongValue(dataMap, "lastSaveTimestamp", 0),
                    FirstPlayTimestamp = GetLongValue(dataMap, "firstPlayTimestamp", 0),
                    GameVersion = GetStringValue(dataMap, "gameVersion", ""),

                    Coins = GetDoubleValue(dataMap, "coins", 0),
                    Gems = GetIntValue(dataMap, "gems", 0),
                    FranchisePoints = GetIntValue(dataMap, "franchisePoints", 0),
                    Reputation = GetIntValue(dataMap, "reputation", 0),
                    TotalEarnings = GetDoubleValue(dataMap, "totalEarnings", 0),
                    TotalLifetimeEarnings = GetDoubleValue(dataMap, "totalLifetimeEarnings", 0),

                    PlayerLevel = GetIntValue(dataMap, "playerLevel", 1),
                    FranchiseCount = GetIntValue(dataMap, "franchiseCount", 0),
                    CurrentCityId = GetStringValue(dataMap, "currentCityId", "istanbul"),
                    HasBattlePass = GetBoolValue(dataMap, "hasBattlePass", false),
                    BattlePassTier = GetIntValue(dataMap, "battlePassTier", 0),

                    MasterVolume = (float)GetDoubleValue(dataMap, "masterVolume", 1.0),
                    MusicVolume = (float)GetDoubleValue(dataMap, "musicVolume", 0.7),
                    SFXVolume = (float)GetDoubleValue(dataMap, "sfxVolume", 1.0),
                    Language = GetStringValue(dataMap, "language", "tr"),
                    NotificationsEnabled = GetBoolValue(dataMap, "notificationsEnabled", true),

                    DataHash = GetStringValue(dataMap, "dataHash", "")
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CloudSaveManager] Deserialize hatasi: {ex.Message}");
                return null;
            }
        }

        // --- Dictionary okuma yardimcilari (Firestore tip guvenli) ---

        private static string GetStringValue(Dictionary<string, object> map, string key, string defaultValue)
        {
            return map.TryGetValue(key, out var val) && val is string s ? s : defaultValue;
        }

        private static int GetIntValue(Dictionary<string, object> map, string key, int defaultValue)
        {
            if (!map.TryGetValue(key, out var val)) return defaultValue;
            if (val is long l) return (int)l;
            if (val is int i) return i;
            if (val is double d) return (int)d;
            return defaultValue;
        }

        private static long GetLongValue(Dictionary<string, object> map, string key, long defaultValue)
        {
            if (!map.TryGetValue(key, out var val)) return defaultValue;
            if (val is long l) return l;
            if (val is int i) return i;
            if (val is double d) return (long)d;
            return defaultValue;
        }

        private static double GetDoubleValue(Dictionary<string, object> map, string key, double defaultValue)
        {
            if (!map.TryGetValue(key, out var val)) return defaultValue;
            if (val is double d) return d;
            if (val is long l) return l;
            if (val is int i) return i;
            if (val is float f) return f;
            return defaultValue;
        }

        private static bool GetBoolValue(Dictionary<string, object> map, string key, bool defaultValue)
        {
            return map.TryGetValue(key, out var val) && val is bool b ? b : defaultValue;
        }
#endif

        // =====================================================================
        // Lokale Uygulama
        // =====================================================================

        /// <summary>
        /// Bulut verisini lokal save'e uygular.
        /// SaveManager.Data alanlarini tek tek kopyalar ve lokal kaydi gunceller.
        /// </summary>
        private void ApplyCloudDataToLocal(PlayerSaveData cloudData)
        {
            var local = _saveManager.Data;
            if (local == null || cloudData == null) return;

            // Meta
            local.PlayerId = cloudData.PlayerId;
            local.PlayerName = cloudData.PlayerName;
            local.SaveVersion = cloudData.SaveVersion;
            local.LastSaveTimestamp = cloudData.LastSaveTimestamp;
            local.FirstPlayTimestamp = cloudData.FirstPlayTimestamp;
            local.GameVersion = cloudData.GameVersion;

            // Ekonomi
            local.Coins = cloudData.Coins;
            local.Gems = cloudData.Gems;
            local.FranchisePoints = cloudData.FranchisePoints;
            local.Reputation = cloudData.Reputation;
            local.TotalEarnings = cloudData.TotalEarnings;
            local.TotalLifetimeEarnings = cloudData.TotalLifetimeEarnings;

            // Ilerleme
            local.PlayerLevel = cloudData.PlayerLevel;
            local.FranchiseCount = cloudData.FranchiseCount;
            local.CurrentCityId = cloudData.CurrentCityId;
            local.HasBattlePass = cloudData.HasBattlePass;
            local.BattlePassTier = cloudData.BattlePassTier;

            // Ayarlar — buluttan gelen ayarları da senkronize et
            local.MasterVolume = cloudData.MasterVolume;
            local.MusicVolume = cloudData.MusicVolume;
            local.SFXVolume = cloudData.SFXVolume;
            local.Language = cloudData.Language;
            local.NotificationsEnabled = cloudData.NotificationsEnabled;

            // Hash
            local.DataHash = cloudData.DataHash;

            // Lokal dosyaya yaz
            _saveManager.SaveLocal();
        }
    }
}
