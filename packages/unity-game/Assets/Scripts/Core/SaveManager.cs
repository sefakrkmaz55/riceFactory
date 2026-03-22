// =============================================================================
// SaveManager.cs
// Oyuncu verilerinin lokal kaydi ve yuklenmesi.
// JSON serialize/deserialize ile Application.persistentDataPath'e kayit yapar.
// Auto-save (30 sn), backup sistemi ve veri butunlugu kontrolu icerir.
// =============================================================================

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RiceFactory.Core
{
    // -------------------------------------------------------------------------
    // Save Manager Arayuzu
    // -------------------------------------------------------------------------

    /// <summary>
    /// SaveManager icin interface. Cloud sync ve lokal kayit operasyonlari.
    /// </summary>
    public interface ISaveManager
    {
        PlayerSaveData Data { get; }
        void SaveLocal();
        Task SaveAsync();
        Task LoadAsync();
        void DeleteSave();
        void Tick(float deltaTime);
    }

    // -------------------------------------------------------------------------
    // Save Manager Implementasyonu
    // -------------------------------------------------------------------------

    /// <summary>
    /// Lokal save/load islemleri. JSON formatinda Application.persistentDataPath
    /// altinda kayit yapar. Auto-save her 30 saniyede bir tetiklenir.
    /// Backup sistemi ile veri kaybi riski minimize edilir.
    /// </summary>
    public class SaveManager : ISaveManager
    {
        public PlayerSaveData Data { get; private set; }

        // Dosya yollari
        private readonly string _savePath;
        private readonly string _backupPath;

        // Auto-save zamanlayici
        private float _autoSaveTimer;
        private const float AUTO_SAVE_INTERVAL = 30f;

        // Veri butunlugu icin sifreleme anahtari
        private const string HASH_SALT = "riceFactory_2026_salt";

        // =====================================================================
        // Constructor
        // =====================================================================

        public SaveManager()
        {
            _savePath = Path.Combine(Application.persistentDataPath, "save_v1.json");
            _backupPath = Path.Combine(Application.persistentDataPath, "save_v1.backup.json");
        }

        // =====================================================================
        // Load
        // =====================================================================

        /// <summary>
        /// Oyun basinda kayitli veriyi yukler.
        /// Oncelik: ana kayit > backup > yeni veri.
        /// </summary>
        public async Task LoadAsync()
        {
            // Lokal kaydi yukle
            Data = LoadFromFile(_savePath);

            // Ana kayit bozuksa backup'tan yukle
            if (Data == null)
            {
                Debug.LogWarning("[SaveManager] Ana kayit bulunamadi veya bozuk, backup deneniyor...");
                Data = LoadFromFile(_backupPath);
            }

            // Hicbir kayit yoksa yeni olustur
            if (Data == null)
            {
                Debug.Log("[SaveManager] Kayit bulunamadi, yeni oyun verisi olusturuluyor.");
                Data = CreateNewSaveData();
            }
            else
            {
                // Veri butunlugu kontrolu
                if (!ValidateDataHash(Data))
                {
                    Debug.LogWarning("[SaveManager] Veri butunlugu hatasi! Veriler manipule edilmis olabilir.");
                    // Veriyi sifirlamak yerine uyari ver ve devam et
                    // Ciddi durumlarda sunucu tarafinda dogrulama yapilir
                }
            }

            await Task.CompletedTask; // Async uyumluluk (ileride cloud sync eklenecek)
        }

        /// <summary>Dosyadan PlayerSaveData okur. Hata durumunda null dondurur.</summary>
        private PlayerSaveData LoadFromFile(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;

                string json = File.ReadAllText(path);
                if (string.IsNullOrEmpty(json)) return null;

                var data = JsonUtility.FromJson<PlayerSaveData>(json);
                return data;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Dosya okuma hatasi ({path}): {ex.Message}");
                return null;
            }
        }

        // =====================================================================
        // Save
        // =====================================================================

        /// <summary>
        /// Lokal kayit (senkron). Mevcut ana kaydi backup'a kopyalar,
        /// sonra yeni veriyi ana kayit olarak yazar.
        /// </summary>
        public void SaveLocal()
        {
            if (Data == null)
            {
                Debug.LogError("[SaveManager] Kaydedilecek veri yok!");
                return;
            }

            try
            {
                // Meta verileri guncelle
                Data.LastSaveTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                Data.SaveVersion++;
                Data.GameVersion = Application.version;

                // Veri butunlugu hash'i olustur
                Data.DataHash = CalculateDataHash(Data);

                // Mevcut kaydi backup'a kopyala
                if (File.Exists(_savePath))
                {
                    File.Copy(_savePath, _backupPath, overwrite: true);
                }

                // Yeni veriyi yaz
                string json = JsonUtility.ToJson(Data, prettyPrint: false);
                File.WriteAllText(_savePath, json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Kayit hatasi: {ex.Message}");
            }
        }

        /// <summary>
        /// Async kayit. Simdilik lokal kaydi yapar, ileride cloud sync eklenecek.
        /// </summary>
        public async Task SaveAsync()
        {
            SaveLocal();
            // TODO: Cloud sync (FirestoreRepository) burada eklenecek
            await Task.CompletedTask;
        }

        // =====================================================================
        // Delete
        // =====================================================================

        /// <summary>
        /// Tum kayit dosyalarini siler ve yeni veri olusturur.
        /// Dikkatli kullanilmali -- geri alinamaz.
        /// </summary>
        public void DeleteSave()
        {
            try
            {
                if (File.Exists(_savePath)) File.Delete(_savePath);
                if (File.Exists(_backupPath)) File.Delete(_backupPath);

                // PlayerPrefs'teki hizli verileri de temizle
                PlayerPrefs.DeleteAll();
                PlayerPrefs.Save();

                Data = CreateNewSaveData();
                Debug.Log("[SaveManager] Tum kayitlar silindi, yeni veri olusturuldu.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Silme hatasi: {ex.Message}");
            }
        }

        // =====================================================================
        // Auto-Save Tick
        // =====================================================================

        /// <summary>
        /// Her frame cagirilir. AUTO_SAVE_INTERVAL'da bir otomatik kayit yapar.
        /// </summary>
        public void Tick(float deltaTime)
        {
            _autoSaveTimer += deltaTime;
            if (_autoSaveTimer >= AUTO_SAVE_INTERVAL)
            {
                _autoSaveTimer = 0f;
                SaveLocal();
            }
        }

        // =====================================================================
        // PlayerPrefs Hizli Erisim
        // =====================================================================

        /// <summary>Hizli PlayerPrefs kaydi (kucuk, sik degisen degerler icin).</summary>
        public static void SaveQuick(string key, int value)
        {
            PlayerPrefs.SetInt(key, value);
        }

        /// <summary>Hizli PlayerPrefs kaydi (float).</summary>
        public static void SaveQuick(string key, float value)
        {
            PlayerPrefs.SetFloat(key, value);
        }

        /// <summary>Hizli PlayerPrefs kaydi (string).</summary>
        public static void SaveQuick(string key, string value)
        {
            PlayerPrefs.SetString(key, value);
        }

        /// <summary>Hizli PlayerPrefs okuma (int).</summary>
        public static int LoadQuickInt(string key, int defaultValue = 0)
        {
            return PlayerPrefs.GetInt(key, defaultValue);
        }

        /// <summary>Hizli PlayerPrefs okuma (float).</summary>
        public static float LoadQuickFloat(string key, float defaultValue = 0f)
        {
            return PlayerPrefs.GetFloat(key, defaultValue);
        }

        /// <summary>Hizli PlayerPrefs okuma (string).</summary>
        public static string LoadQuickString(string key, string defaultValue = "")
        {
            return PlayerPrefs.GetString(key, defaultValue);
        }

        // =====================================================================
        // Veri Butunlugu
        // =====================================================================

        /// <summary>PlayerSaveData icin SHA256 hash hesaplar.</summary>
        private string CalculateDataHash(PlayerSaveData data)
        {
            // Hash hesaplamasi icin DataHash'i gecici olarak temizle
            string originalHash = data.DataHash;
            data.DataHash = "";

            string json = JsonUtility.ToJson(data);
            string salted = json + HASH_SALT;

            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(salted));
                var sb = new StringBuilder();
                foreach (byte b in bytes)
                {
                    sb.Append(b.ToString("x2"));
                }

                data.DataHash = originalHash; // Geri yukle
                return sb.ToString();
            }
        }

        /// <summary>Veri butunlugunu dogrular.</summary>
        private bool ValidateDataHash(PlayerSaveData data)
        {
            if (string.IsNullOrEmpty(data.DataHash)) return true; // Eski kayitlarda hash yok

            string expected = CalculateDataHash(data);
            return data.DataHash == expected;
        }

        // =====================================================================
        // Yeni Kayit Olusturma
        // =====================================================================

        /// <summary>Yeni oyuncu icin varsayilan save data olusturur.</summary>
        private PlayerSaveData CreateNewSaveData()
        {
            return new PlayerSaveData
            {
                PlayerId = Guid.NewGuid().ToString(),
                PlayerName = "",
                SaveVersion = 1,
                LastSaveTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                FirstPlayTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                GameVersion = Application.version,
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
                DataHash = ""
            };
        }
    }

    // =========================================================================
    // PlayerSaveData -- Oyuncu kayit veri modeli
    // =========================================================================

    /// <summary>
    /// Oyuncunun tum ilerlemesini, ekonomisini ve ayarlarini tutan
    /// merkezi veri sinifi. JSON serialize edilir.
    /// </summary>
    [System.Serializable]
    public partial class PlayerSaveData
    {
        // ---- Meta ----
        public string PlayerId;
        public string PlayerName;
        public int SaveVersion;
        public long LastSaveTimestamp;   // Unix timestamp (UTC)
        public long FirstPlayTimestamp;
        public string GameVersion;

        // ---- Ekonomi ----
        public double Coins;
        public int Gems;
        public int FranchisePoints;
        public int Reputation;
        public double TotalEarnings;         // Bu franchise donemi
        public double TotalLifetimeEarnings; // Tum zamanlarin toplami

        // ---- Ilerleme ----
        public int PlayerLevel;
        public int FranchiseCount;
        public string CurrentCityId;
        public bool HasBattlePass;
        public int BattlePassTier;

        // ---- Ayarlar ----
        public float MasterVolume;
        public float MusicVolume;
        public float SFXVolume;
        public string Language;
        public bool NotificationsEnabled;

        // ---- Anti-cheat ----
        public string DataHash;
    }
}
