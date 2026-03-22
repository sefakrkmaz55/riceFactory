// =============================================================================
// TimeManager.cs
// Offline sure hesaplama, anti-cheat zaman kontrolu ve aktif oyun suresi takibi.
// Kombo sistemi: aktif oynama suresi arttikca kazanc carpani artar.
// Sunucu saati ile lokal saat karsilastirmasi yaparak manipulasyonu tespit eder.
// =============================================================================

using System;
using System.Threading.Tasks;
using UnityEngine;

namespace RiceFactory.Core
{
    // -------------------------------------------------------------------------
    // Time Manager Arayuzu
    // -------------------------------------------------------------------------

    /// <summary>
    /// TimeManager icin interface. Zaman yonetimi ve offline hesaplamalar.
    /// </summary>
    public interface ITimeManager
    {
        bool IsTimeReliable { get; }
        float ActivePlayTime { get; }
        float ComboMultiplier { get; }

        Task SyncServerTimeAsync();
        void RecordPauseTime();
        TimeSpan GetTimeSincePause();
        void Tick(float deltaTime);
    }

    // -------------------------------------------------------------------------
    // Offline Kazanc Sonuc Modeli
    // -------------------------------------------------------------------------

    /// <summary>Offline kazanc hesaplama sonucunu tasir.</summary>
    [System.Serializable]
    public class OfflineEarningsResult
    {
        public TimeSpan Duration;
        public double TotalCoins;
        public int TotalProducts;
        public float Efficiency;
        public bool IsTimeReliable;
    }

    // -------------------------------------------------------------------------
    // Time Manager Implementasyonu
    // -------------------------------------------------------------------------

    /// <summary>
    /// Zaman yonetimi: offline sure hesaplama, anti-cheat kontrolleri,
    /// aktif oyun suresi takibi ve kombo carpani sistemi.
    ///
    /// Anti-cheat:
    /// - Boot'ta sunucu zamanini alir, lokal saatle karsilastirir
    /// - 5 dakikadan fazla fark varsa "guvenilmez" isaretler
    /// - Guvenilmez durumda offline kazanclar %50 ceza alir
    ///
    /// Kombo sistemi (GDD'den):
    /// - 0-2dk: x1.0, 2-5dk: x1.2, 5-10dk: x1.5, 10-20dk: x1.8, 20+dk: x2.0
    /// </summary>
    public class TimeManager : ITimeManager
    {
        // Sunucu zaman esitleme
        private long _lastKnownServerTime;
        private long _lastLocalTime;

        // Pause/Resume zamani
        private long _pauseTimestamp;

        // Kombo sistemi
        private float _comboTimer;
        private float _comboMultiplier = 1f;

        // =====================================================================
        // Public Properties
        // =====================================================================

        /// <summary>Cihaz zamaninin guvenilir olup olmadigini gosterir.</summary>
        public bool IsTimeReliable { get; private set; } = true;

        /// <summary>Bu oturumdaki aktif oyun suresi (saniye).</summary>
        public float ActivePlayTime { get; private set; }

        /// <summary>Aktif oyun suresine gore hesaplanan kombo carpani.</summary>
        public float ComboMultiplier => _comboMultiplier;

        // =====================================================================
        // Sunucu Zaman Esitleme
        // =====================================================================

        /// <summary>
        /// Boot sirasinda sunucu zamani ile cihaz zamanini karsilastirir.
        /// Fark 5 dakikayi gecerse cihaz saati "guvenilmez" isaretlenir.
        /// Firebase Server Timestamp kullanir, basarisiz olursa lokal zamani kabul eder.
        /// </summary>
        public async Task SyncServerTimeAsync()
        {
            _lastKnownServerTime = await FetchServerTimestamp();
            _lastLocalTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            long diff = Math.Abs(_lastKnownServerTime - _lastLocalTime);
            IsTimeReliable = diff < 300; // 5 dakikadan az fark

            if (!IsTimeReliable)
            {
                Debug.LogWarning(
                    $"[TimeManager] Cihaz saati sunucuyla uyumsuz. Fark: {diff}s. " +
                    "Offline kazanclar sinirlandirilacak."
                );
            }
            else
            {
                Debug.Log($"[TimeManager] Sunucu zamani esitlendi. Fark: {diff}s (guvenilir).");
            }
        }

        // =====================================================================
        // Pause / Resume
        // =====================================================================

        /// <summary>
        /// Uygulama arka plana alindiginda veya kapatildiginda cagirilir.
        /// Mevcut zamani kaydeder ve kombo sifirlar.
        /// </summary>
        public void RecordPauseTime()
        {
            _pauseTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            _comboTimer = 0f;
            _comboMultiplier = 1f;

            // Pause zamanini PlayerPrefs'e de yaz (uygulama crash durumunda kaybolmasin)
            PlayerPrefs.SetString("last_pause_timestamp", _pauseTimestamp.ToString());
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Uygulamaya donus sirasinda gecen sureyi hesaplar.
        /// Anti-cheat: negatif zaman veya 24 saatten uzun sureler sinirlandirilir.
        /// </summary>
        public TimeSpan GetTimeSincePause()
        {
            // Eger _pauseTimestamp sifirsa PlayerPrefs'ten oku
            if (_pauseTimestamp == 0)
            {
                string saved = PlayerPrefs.GetString("last_pause_timestamp", "0");
                long.TryParse(saved, out _pauseTimestamp);
            }

            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long elapsed = now - _pauseTimestamp;

            // Anti-cheat: negatif zaman veya asiri uzun sureler
            if (elapsed < 0)
            {
                Debug.LogWarning(
                    $"[TimeManager] Negatif offline sure tespit edildi: {elapsed}s. " +
                    "Zaman manipulasyonu olabilir."
                );
                IsTimeReliable = false;
                elapsed = 0;
            }
            else if (elapsed > 86400) // 24 saatten fazla
            {
                Debug.LogWarning(
                    $"[TimeManager] 24 saatten uzun offline sure: {elapsed}s. " +
                    "Maksimum 24 saat ile sinirlandiriliyor."
                );
                elapsed = 86400;
            }

            return TimeSpan.FromSeconds(elapsed);
        }

        // =====================================================================
        // Tick (Her Frame)
        // =====================================================================

        /// <summary>
        /// Her frame cagirilir. Aktif oyun suresini ve kombo carpanini gunceller.
        ///
        /// Kombo basamaklari (GDD):
        ///   0-2 dk   -> x1.0
        ///   2-5 dk   -> x1.2
        ///   5-10 dk  -> x1.5
        ///   10-20 dk -> x1.8
        ///   20+ dk   -> x2.0
        /// </summary>
        public void Tick(float deltaTime)
        {
            ActivePlayTime += deltaTime;
            _comboTimer += deltaTime;

            _comboMultiplier = _comboTimer switch
            {
                < 120f  => 1.0f,  // 0-2 dakika
                < 300f  => 1.2f,  // 2-5 dakika
                < 600f  => 1.5f,  // 5-10 dakika
                < 1200f => 1.8f,  // 10-20 dakika
                _       => 2.0f   // 20+ dakika
            };
        }

        // =====================================================================
        // Kombo Sifirlama
        // =====================================================================

        /// <summary>
        /// Kombo zamanlayicisini sifirlar (ornegin mini-game'e giris gibi kesintilerde).
        /// </summary>
        public void ResetCombo()
        {
            _comboTimer = 0f;
            _comboMultiplier = 1f;
        }

        // =====================================================================
        // Yardimci Metotlar
        // =====================================================================

        /// <summary>Simdi'nin Unix timestamp degerini dondurur (UTC).</summary>
        public static long GetUnixTimestampNow()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        /// <summary>Unix timestamp'i DateTime'a cevirir.</summary>
        public static DateTime UnixToDateTime(long unixTimestamp)
        {
            return DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).UtcDateTime;
        }

        /// <summary>Iki timestamp arasindaki farki TimeSpan olarak dondurur.</summary>
        public static TimeSpan GetElapsed(long fromTimestamp, long toTimestamp)
        {
            return TimeSpan.FromSeconds(Math.Abs(toTimestamp - fromTimestamp));
        }

        // =====================================================================
        // Sunucu Zamani Cekme (Firebase)
        // =====================================================================

        /// <summary>
        /// Firebase uzerinden sunucu zamanini alir.
        /// Basarisiz olursa lokal zamani dondurur (fallback).
        /// </summary>
        private async Task<long> FetchServerTimestamp()
        {
            try
            {
                // Firebase Server Timestamp
                // Not: Firebase SDK entegrasyonunda asagidaki kod aktif edilecek.
                // var snapshot = await FirebaseFirestore.DefaultInstance
                //     .Collection("server")
                //     .Document("time")
                //     .GetSnapshotAsync();
                // return snapshot.GetValue<long>("timestamp");

                // Simdilik lokal zamani dondur (Firebase entegrasyonu sonrasi guncellenecek)
                await Task.Delay(1); // Async uyumluluk
                return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[TimeManager] Sunucu zamani alinamadi, lokal zaman kullaniliyor: {ex.Message}");
                return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            }
        }
    }
}
