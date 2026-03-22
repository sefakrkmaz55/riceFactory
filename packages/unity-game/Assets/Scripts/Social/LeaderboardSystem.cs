// =============================================================================
// LeaderboardSystem.cs
// Haftalik ve aylik liderboard yonetimi.
// Firebase callable function'lari uzerinden skor gonderme ve siralama cekme.
// Cache mekanizmasi ile gereksiz network cagrilarini onler.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using RiceFactory.Core;
using RiceFactory.Core.Events;

namespace RiceFactory.Social
{
    // -------------------------------------------------------------------------
    // Interface
    // -------------------------------------------------------------------------

    /// <summary>
    /// Liderboard sistemi arayuzu.
    /// Haftalik/aylik skor gonderme, siralama cekme ve oyuncunun kendi sirasini sorgulama.
    /// </summary>
    public interface ILeaderboardSystem
    {
        /// <summary>Belirtilen kategoride skor gonderir.</summary>
        Task SubmitScoreAsync(LeaderboardCategory category, double score);

        /// <summary>Belirtilen periyod ve kategorideki siralamayı dondurur.</summary>
        Task<List<LeaderboardEntry>> GetLeaderboardAsync(LeaderboardPeriod period, LeaderboardCategory category, int limit = 50);

        /// <summary>Oyuncunun belirtilen liderboard'daki sirasini dondurur.</summary>
        Task<LeaderboardEntry> GetPlayerRankAsync(LeaderboardPeriod period, LeaderboardCategory category);

        /// <summary>Cache'i temizler, bir sonraki cagri taze veri ceker.</summary>
        void InvalidateCache();
    }

    // -------------------------------------------------------------------------
    // Veri Modelleri
    // -------------------------------------------------------------------------

    /// <summary>Liderboard zaman periyodu.</summary>
    public enum LeaderboardPeriod
    {
        Weekly,
        Monthly
    }

    /// <summary>Liderboard kategorileri (GDD 6.1).</summary>
    public enum LeaderboardCategory
    {
        /// <summary>Toplam coin kazanci.</summary>
        TopEarner,

        /// <summary>Toplam uretim miktari.</summary>
        TopProducer,

        /// <summary>Tamamlanan siparis sayisi.</summary>
        OrderKing,

        /// <summary>5 yildiz urun sayisi.</summary>
        QualityChampion,

        /// <summary>Aylik toplam kazanc (sadece Monthly).</summary>
        Emperor,

        /// <summary>Toplam franchise sayisi (sadece Monthly).</summary>
        FranchiseMaster
    }

    /// <summary>Tek bir liderboard satirini temsil eder.</summary>
    [Serializable]
    public class LeaderboardEntry
    {
        public int Rank;
        public string PlayerId;
        public string PlayerName;
        public double Score;
        public bool IsCurrentPlayer;
    }

    // -------------------------------------------------------------------------
    // Implementasyon
    // -------------------------------------------------------------------------

    /// <summary>
    /// Liderboard sistemi. Firebase callable function'lari cagirarak skor gonderir
    /// ve siralama cekar. Firebase yokken dummy veri dondurur.
    /// Cache suresi: 5 dakika.
    /// </summary>
    public class LeaderboardSystem : ILeaderboardSystem
    {
        private const float CACHE_DURATION_SECONDS = 300f; // 5 dakika

        private readonly IEventManager _eventManager;

        // Cache: key = "period_category", value = (entries, timestamp)
        private readonly Dictionary<string, (List<LeaderboardEntry> Entries, float Timestamp)> _cache = new();

        // Oyuncunun kendi sira cache'i
        private readonly Dictionary<string, (LeaderboardEntry Entry, float Timestamp)> _playerRankCache = new();

        public LeaderboardSystem(IEventManager eventManager)
        {
            _eventManager = eventManager ?? throw new ArgumentNullException(nameof(eventManager));
        }

        // =====================================================================
        // SKOR GONDERME
        // =====================================================================

        public async Task SubmitScoreAsync(LeaderboardCategory category, double score)
        {
            if (score <= 0)
            {
                Debug.LogWarning($"[LeaderboardSystem] SubmitScore: Gecersiz skor ({score}). Islem iptal.");
                return;
            }

#if FIREBASE_ENABLED
            try
            {
                var data = new Dictionary<string, object>
                {
                    { "category", category.ToString() },
                    { "score", score }
                };

                var callable = Firebase.Functions.FirebaseFunctions.DefaultInstance
                    .GetHttpsCallable("submitLeaderboardScore");
                await callable.CallAsync(data);

                Debug.Log($"[LeaderboardSystem] Skor gonderildi: {category} = {score}");

                // Skor gonderildikten sonra ilgili cache'leri gecersiz kil
                InvalidateCacheForCategory(category);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LeaderboardSystem] Skor gonderme hatasi: {ex.Message}");
            }
#else
            // Firebase yokken sadece logla
            Debug.Log($"[LeaderboardSystem] (Dummy) Skor gonderildi: {category} = {score}");
            InvalidateCacheForCategory(category);
            await Task.CompletedTask;
#endif
        }

        // =====================================================================
        // SIRALAMA CEKME
        // =====================================================================

        public async Task<List<LeaderboardEntry>> GetLeaderboardAsync(
            LeaderboardPeriod period, LeaderboardCategory category, int limit = 50)
        {
            string cacheKey = BuildCacheKey(period, category);

            // Cache kontrol
            if (_cache.TryGetValue(cacheKey, out var cached))
            {
                if (Time.realtimeSinceStartup - cached.Timestamp < CACHE_DURATION_SECONDS)
                {
                    return cached.Entries;
                }
            }

#if FIREBASE_ENABLED
            try
            {
                var data = new Dictionary<string, object>
                {
                    { "period", period.ToString() },
                    { "category", category.ToString() },
                    { "limit", limit }
                };

                var callable = Firebase.Functions.FirebaseFunctions.DefaultInstance
                    .GetHttpsCallable("getLeaderboard");
                var result = await callable.CallAsync(data);

                var entries = ParseLeaderboardResult(result.Data);
                _cache[cacheKey] = (entries, Time.realtimeSinceStartup);
                return entries;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LeaderboardSystem] Liderboard cekme hatasi: {ex.Message}");
                // Hata durumunda eski cache varsa onu dondur
                if (_cache.TryGetValue(cacheKey, out var stale))
                    return stale.Entries;
                return new List<LeaderboardEntry>();
            }
#else
            // Firebase yokken dummy veri
            var dummyEntries = GenerateDummyLeaderboard(category, limit);
            _cache[cacheKey] = (dummyEntries, Time.realtimeSinceStartup);
            await Task.CompletedTask;
            return dummyEntries;
#endif
        }

        // =====================================================================
        // OYUNCUNUN KENDI SIRASI
        // =====================================================================

        public async Task<LeaderboardEntry> GetPlayerRankAsync(
            LeaderboardPeriod period, LeaderboardCategory category)
        {
            string cacheKey = BuildCacheKey(period, category);

            // Cache kontrol
            if (_playerRankCache.TryGetValue(cacheKey, out var cached))
            {
                if (Time.realtimeSinceStartup - cached.Timestamp < CACHE_DURATION_SECONDS)
                {
                    return cached.Entry;
                }
            }

#if FIREBASE_ENABLED
            try
            {
                var data = new Dictionary<string, object>
                {
                    { "period", period.ToString() },
                    { "category", category.ToString() }
                };

                var callable = Firebase.Functions.FirebaseFunctions.DefaultInstance
                    .GetHttpsCallable("getPlayerRank");
                var result = await callable.CallAsync(data);

                var entry = ParsePlayerRankResult(result.Data);
                entry.IsCurrentPlayer = true;
                _playerRankCache[cacheKey] = (entry, Time.realtimeSinceStartup);
                return entry;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LeaderboardSystem] Oyuncu sirasi cekme hatasi: {ex.Message}");
                if (_playerRankCache.TryGetValue(cacheKey, out var stale))
                    return stale.Entry;
                return CreateDefaultPlayerEntry();
            }
#else
            // Firebase yokken dummy
            var dummyEntry = CreateDefaultPlayerEntry();
            dummyEntry.Rank = UnityEngine.Random.Range(50, 500);
            dummyEntry.Score = UnityEngine.Random.Range(1000, 50000);
            _playerRankCache[cacheKey] = (dummyEntry, Time.realtimeSinceStartup);
            await Task.CompletedTask;
            return dummyEntry;
#endif
        }

        // =====================================================================
        // CACHE YONETIMI
        // =====================================================================

        public void InvalidateCache()
        {
            _cache.Clear();
            _playerRankCache.Clear();
            Debug.Log("[LeaderboardSystem] Tum cache temizlendi.");
        }

        private void InvalidateCacheForCategory(LeaderboardCategory category)
        {
            // Hem weekly hem monthly cache'ini temizle
            string weeklyKey = BuildCacheKey(LeaderboardPeriod.Weekly, category);
            string monthlyKey = BuildCacheKey(LeaderboardPeriod.Monthly, category);
            _cache.Remove(weeklyKey);
            _cache.Remove(monthlyKey);
            _playerRankCache.Remove(weeklyKey);
            _playerRankCache.Remove(monthlyKey);
        }

        private static string BuildCacheKey(LeaderboardPeriod period, LeaderboardCategory category)
        {
            return $"{period}_{category}";
        }

        // =====================================================================
        // YARDIMCI: Parse & Dummy
        // =====================================================================

#if FIREBASE_ENABLED
        private static List<LeaderboardEntry> ParseLeaderboardResult(object data)
        {
            var entries = new List<LeaderboardEntry>();

            if (data is IList<object> list)
            {
                foreach (var item in list)
                {
                    if (item is IDictionary<string, object> dict)
                    {
                        entries.Add(new LeaderboardEntry
                        {
                            Rank = Convert.ToInt32(dict.GetValueOrDefault("rank", 0)),
                            PlayerId = dict.GetValueOrDefault("playerId", "")?.ToString() ?? "",
                            PlayerName = dict.GetValueOrDefault("playerName", "Oyuncu")?.ToString() ?? "Oyuncu",
                            Score = Convert.ToDouble(dict.GetValueOrDefault("score", 0)),
                            IsCurrentPlayer = false
                        });
                    }
                }
            }

            return entries;
        }

        private static LeaderboardEntry ParsePlayerRankResult(object data)
        {
            if (data is IDictionary<string, object> dict)
            {
                return new LeaderboardEntry
                {
                    Rank = Convert.ToInt32(dict.GetValueOrDefault("rank", 0)),
                    PlayerId = dict.GetValueOrDefault("playerId", "")?.ToString() ?? "",
                    PlayerName = dict.GetValueOrDefault("playerName", "Ben")?.ToString() ?? "Ben",
                    Score = Convert.ToDouble(dict.GetValueOrDefault("score", 0)),
                    IsCurrentPlayer = true
                };
            }

            return CreateDefaultPlayerEntry();
        }
#endif

        private static LeaderboardEntry CreateDefaultPlayerEntry()
        {
            return new LeaderboardEntry
            {
                Rank = 0,
                PlayerId = "local_player",
                PlayerName = "Ben",
                Score = 0,
                IsCurrentPlayer = true
            };
        }

        /// <summary>
        /// Firebase baglantisi olmadan test icin dummy liderboard verisi olusturur.
        /// </summary>
        private static List<LeaderboardEntry> GenerateDummyLeaderboard(LeaderboardCategory category, int limit)
        {
            var entries = new List<LeaderboardEntry>();
            var names = new[]
            {
                "TontonUsta", "PirinçKralı", "FabrikaBey", "SushiMaster",
                "RiceQueen", "NoodleKing", "ÇeltikÇılgın", "PilâvPaşa",
                "UnMüdürü", "EkmekEfe", "SandviçSelin", "RestoranReis",
                "TarlaAga", "HasatHanım", "MakineMemet", "KaliteKadir",
                "SiparişSena", "FranchiseFatih", "YıldızYusuf", "ÜretimÜmit"
            };

            int count = Mathf.Min(limit, 20);
            double baseScore = category switch
            {
                LeaderboardCategory.TopEarner => 100000,
                LeaderboardCategory.TopProducer => 5000,
                LeaderboardCategory.OrderKing => 200,
                LeaderboardCategory.QualityChampion => 100,
                LeaderboardCategory.Emperor => 500000,
                LeaderboardCategory.FranchiseMaster => 10,
                _ => 10000
            };

            for (int i = 0; i < count; i++)
            {
                entries.Add(new LeaderboardEntry
                {
                    Rank = i + 1,
                    PlayerId = $"dummy_{i}",
                    PlayerName = names[i % names.Length],
                    Score = baseScore * (1.0 - i * 0.05),
                    IsCurrentPlayer = false
                });
            }

            // Oyuncunun kendisini rastgele bir siraya ekle
            int playerIndex = Mathf.Min(count / 2, count - 1);
            if (playerIndex >= 0 && playerIndex < entries.Count)
            {
                entries[playerIndex].PlayerId = "local_player";
                entries[playerIndex].PlayerName = "Ben";
                entries[playerIndex].IsCurrentPlayer = true;
            }

            return entries;
        }
    }
}
