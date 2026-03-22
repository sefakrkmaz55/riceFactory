// =============================================================================
// FriendSystem.cs
// Arkadas sistemi: ekleme, listeleme, fabrika ziyareti.
// Gunluk ziyaret limiti ve bonus kazanc mekanizmasi.
// Firebase callable function'lari uzerinden calisan, yokken dummy veri donduren yapi.
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
    /// Arkadas sistemi arayuzu.
    /// Arkadas ekleme, listeleme ve fabrika ziyareti islemleri.
    /// </summary>
    public interface IFriendSystem
    {
        /// <summary>ID ile arkadas ekleme istegi gonderir.</summary>
        Task<FriendRequestResult> AddFriendAsync(string friendId);

        /// <summary>Arkadas listesini dondurur.</summary>
        Task<List<FriendInfo>> GetFriendsAsync();

        /// <summary>Arkadasin fabrikasini ziyaret eder, bonus kazanc saglar.</summary>
        Task<VisitResult> VisitFriendFactoryAsync(string friendId);

        /// <summary>Bugun kalan ziyaret hakkini dondurur.</summary>
        int GetRemainingVisitsToday();

        /// <summary>Gunluk ziyaret sayacini sifirlar (yeni gun baslarken).</summary>
        void ResetDailyVisits();
    }

    // -------------------------------------------------------------------------
    // Veri Modelleri
    // -------------------------------------------------------------------------

    /// <summary>Arkadas ekleme sonucu.</summary>
    public enum FriendRequestResult
    {
        Success,
        AlreadyFriends,
        PlayerNotFound,
        RequestPending,
        Error
    }

    /// <summary>Bir arkadasin bilgilerini temsil eder.</summary>
    [Serializable]
    public class FriendInfo
    {
        public string PlayerId;
        public string PlayerName;
        public int Level;
        public int FranchiseCount;
        public string FactoryTheme;
        public bool IsOnline;
        public DateTime LastSeen;
    }

    /// <summary>Fabrika ziyaret sonucu.</summary>
    [Serializable]
    public class VisitResult
    {
        public bool Success;
        public string Message;
        public double CoinReward;
        public string FriendName;
        public int RemainingVisits;
    }

    // -------------------------------------------------------------------------
    // Event Tanimlamalari
    // -------------------------------------------------------------------------

    /// <summary>Arkadas ziyareti tamamlandiginda tetiklenir.</summary>
    public struct FriendVisitedEvent : IGameEvent
    {
        public string FriendId;
        public string FriendName;
        public double CoinReward;
        public int RemainingVisits;
    }

    /// <summary>Yeni arkadas eklendiginde tetiklenir.</summary>
    public struct FriendAddedEvent : IGameEvent
    {
        public string FriendId;
        public string FriendName;
    }

    // -------------------------------------------------------------------------
    // Implementasyon
    // -------------------------------------------------------------------------

    /// <summary>
    /// Arkadas sistemi implementasyonu.
    /// GDD 6.2: Gunluk max 3 ziyaret, her ziyarette coin odulu.
    /// Gorus notu: GDD'de 5 ziyaret yazsa da gorev tanimi 3 olarak belirlenmistir.
    /// </summary>
    public class FriendSystem : IFriendSystem
    {
        private const int DAILY_VISIT_LIMIT = 3;

        private readonly IEventManager _eventManager;
        private readonly ISaveManager _saveManager;

        // Gunluk ziyaret sayaci
        private int _visitsToday;
        private DateTime _lastVisitDate;

        // Arkadas listesi cache
        private List<FriendInfo> _cachedFriends;
        private float _friendsCacheTimestamp;
        private const float FRIENDS_CACHE_DURATION = 300f; // 5 dk

        public FriendSystem(IEventManager eventManager, ISaveManager saveManager)
        {
            _eventManager = eventManager ?? throw new ArgumentNullException(nameof(eventManager));
            _saveManager = saveManager ?? throw new ArgumentNullException(nameof(saveManager));

            _visitsToday = 0;
            _lastVisitDate = DateTime.UtcNow.Date;
        }

        // =====================================================================
        // ARKADAS EKLEME
        // =====================================================================

        public async Task<FriendRequestResult> AddFriendAsync(string friendId)
        {
            if (string.IsNullOrWhiteSpace(friendId))
            {
                Debug.LogWarning("[FriendSystem] AddFriend: Bos ID gonderildi.");
                return FriendRequestResult.Error;
            }

            friendId = friendId.Trim();

#if FIREBASE_ENABLED
            try
            {
                var data = new Dictionary<string, object>
                {
                    { "friendId", friendId }
                };

                var callable = Firebase.Functions.FirebaseFunctions.DefaultInstance
                    .GetHttpsCallable("addFriend");
                var result = await callable.CallAsync(data);

                if (result.Data is IDictionary<string, object> dict)
                {
                    var status = dict.GetValueOrDefault("status", "error")?.ToString();
                    var resultEnum = status switch
                    {
                        "success" => FriendRequestResult.Success,
                        "already_friends" => FriendRequestResult.AlreadyFriends,
                        "not_found" => FriendRequestResult.PlayerNotFound,
                        "pending" => FriendRequestResult.RequestPending,
                        _ => FriendRequestResult.Error
                    };

                    if (resultEnum == FriendRequestResult.Success)
                    {
                        string friendName = dict.GetValueOrDefault("friendName", "Oyuncu")?.ToString() ?? "Oyuncu";
                        _eventManager.Publish(new FriendAddedEvent
                        {
                            FriendId = friendId,
                            FriendName = friendName
                        });

                        // Cache'i gecersiz kil
                        _cachedFriends = null;
                    }

                    return resultEnum;
                }

                return FriendRequestResult.Error;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FriendSystem] Arkadas ekleme hatasi: {ex.Message}");
                return FriendRequestResult.Error;
            }
#else
            // Firebase yokken dummy — her zaman basarili
            Debug.Log($"[FriendSystem] (Dummy) Arkadas eklendi: {friendId}");

            _eventManager.Publish(new FriendAddedEvent
            {
                FriendId = friendId,
                FriendName = $"Oyuncu_{friendId}"
            });

            _cachedFriends = null;
            await Task.CompletedTask;
            return FriendRequestResult.Success;
#endif
        }

        // =====================================================================
        // ARKADAS LISTESI
        // =====================================================================

        public async Task<List<FriendInfo>> GetFriendsAsync()
        {
            // Cache kontrol
            if (_cachedFriends != null &&
                Time.realtimeSinceStartup - _friendsCacheTimestamp < FRIENDS_CACHE_DURATION)
            {
                return _cachedFriends;
            }

#if FIREBASE_ENABLED
            try
            {
                var callable = Firebase.Functions.FirebaseFunctions.DefaultInstance
                    .GetHttpsCallable("getFriendList");
                var result = await callable.CallAsync(null);

                _cachedFriends = ParseFriendList(result.Data);
                _friendsCacheTimestamp = Time.realtimeSinceStartup;
                return _cachedFriends;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FriendSystem] Arkadas listesi cekme hatasi: {ex.Message}");
                return _cachedFriends ?? new List<FriendInfo>();
            }
#else
            // Firebase yokken dummy
            _cachedFriends = GenerateDummyFriends();
            _friendsCacheTimestamp = Time.realtimeSinceStartup;
            await Task.CompletedTask;
            return _cachedFriends;
#endif
        }

        // =====================================================================
        // FABRIKA ZIYARETI
        // =====================================================================

        public async Task<VisitResult> VisitFriendFactoryAsync(string friendId)
        {
            // Yeni gun kontrolu
            CheckDayReset();

            // Limit kontrolu
            if (_visitsToday >= DAILY_VISIT_LIMIT)
            {
                return new VisitResult
                {
                    Success = false,
                    Message = $"Gunluk ziyaret limiti doldu ({DAILY_VISIT_LIMIT}/{DAILY_VISIT_LIMIT})",
                    CoinReward = 0,
                    RemainingVisits = 0
                };
            }

            if (string.IsNullOrWhiteSpace(friendId))
            {
                return new VisitResult
                {
                    Success = false,
                    Message = "Gecersiz arkadas ID",
                    CoinReward = 0,
                    RemainingVisits = GetRemainingVisitsToday()
                };
            }

#if FIREBASE_ENABLED
            try
            {
                var data = new Dictionary<string, object>
                {
                    { "friendId", friendId }
                };

                var callable = Firebase.Functions.FirebaseFunctions.DefaultInstance
                    .GetHttpsCallable("visitFriendFactory");
                var result = await callable.CallAsync(data);

                if (result.Data is IDictionary<string, object> dict)
                {
                    bool success = Convert.ToBoolean(dict.GetValueOrDefault("success", false));
                    if (success)
                    {
                        _visitsToday++;
                        double coinReward = Convert.ToDouble(dict.GetValueOrDefault("coinReward", 0));
                        string friendName = dict.GetValueOrDefault("friendName", "Oyuncu")?.ToString() ?? "Oyuncu";

                        _eventManager.Publish(new FriendVisitedEvent
                        {
                            FriendId = friendId,
                            FriendName = friendName,
                            CoinReward = coinReward,
                            RemainingVisits = GetRemainingVisitsToday()
                        });

                        return new VisitResult
                        {
                            Success = true,
                            Message = $"{friendName} fabrikasi ziyaret edildi!",
                            CoinReward = coinReward,
                            FriendName = friendName,
                            RemainingVisits = GetRemainingVisitsToday()
                        };
                    }
                }

                return new VisitResult
                {
                    Success = false,
                    Message = "Ziyaret basarisiz",
                    CoinReward = 0,
                    RemainingVisits = GetRemainingVisitsToday()
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FriendSystem] Ziyaret hatasi: {ex.Message}");
                return new VisitResult
                {
                    Success = false,
                    Message = "Baglanti hatasi",
                    CoinReward = 0,
                    RemainingVisits = GetRemainingVisitsToday()
                };
            }
#else
            // Firebase yokken dummy — ziyaret basarili, coin odulu hesapla
            _visitsToday++;

            int playerLevel = 1;
            if (ServiceLocator.TryGet<IEconomySystem>(out var economy))
            {
                // Basit seviye tahmini
                playerLevel = Mathf.Max(1, (int)(Math.Log10(_saveManager.Data.TotalEarnings + 1) * 5));
            }

            double reward = 100 + playerLevel * 10;
            string dummyName = $"Oyuncu_{friendId}";

            _eventManager.Publish(new FriendVisitedEvent
            {
                FriendId = friendId,
                FriendName = dummyName,
                CoinReward = reward,
                RemainingVisits = GetRemainingVisitsToday()
            });

            await Task.CompletedTask;
            return new VisitResult
            {
                Success = true,
                Message = $"{dummyName} fabrikasi ziyaret edildi!",
                CoinReward = reward,
                FriendName = dummyName,
                RemainingVisits = GetRemainingVisitsToday()
            };
#endif
        }

        // =====================================================================
        // GUNLUK LIMIT
        // =====================================================================

        public int GetRemainingVisitsToday()
        {
            CheckDayReset();
            return Mathf.Max(0, DAILY_VISIT_LIMIT - _visitsToday);
        }

        public void ResetDailyVisits()
        {
            _visitsToday = 0;
            _lastVisitDate = DateTime.UtcNow.Date;
            Debug.Log("[FriendSystem] Gunluk ziyaret sayaci sifirlandi.");
        }

        private void CheckDayReset()
        {
            var today = DateTime.UtcNow.Date;
            if (today > _lastVisitDate)
            {
                ResetDailyVisits();
            }
        }

        // =====================================================================
        // YARDIMCI
        // =====================================================================

#if FIREBASE_ENABLED
        private static List<FriendInfo> ParseFriendList(object data)
        {
            var friends = new List<FriendInfo>();

            if (data is IList<object> list)
            {
                foreach (var item in list)
                {
                    if (item is IDictionary<string, object> dict)
                    {
                        friends.Add(new FriendInfo
                        {
                            PlayerId = dict.GetValueOrDefault("playerId", "")?.ToString() ?? "",
                            PlayerName = dict.GetValueOrDefault("playerName", "Oyuncu")?.ToString() ?? "Oyuncu",
                            Level = Convert.ToInt32(dict.GetValueOrDefault("level", 1)),
                            FranchiseCount = Convert.ToInt32(dict.GetValueOrDefault("franchiseCount", 0)),
                            FactoryTheme = dict.GetValueOrDefault("factoryTheme", "default")?.ToString() ?? "default",
                            IsOnline = Convert.ToBoolean(dict.GetValueOrDefault("isOnline", false)),
                            LastSeen = DateTime.UtcNow // Basitlestirilmis
                        });
                    }
                }
            }

            return friends;
        }
#endif

        /// <summary>
        /// Firebase olmadan test icin dummy arkadas listesi olusturur.
        /// </summary>
        private static List<FriendInfo> GenerateDummyFriends()
        {
            return new List<FriendInfo>
            {
                new FriendInfo
                {
                    PlayerId = "friend_001",
                    PlayerName = "TontonUsta",
                    Level = 15,
                    FranchiseCount = 2,
                    FactoryTheme = "sakura",
                    IsOnline = true,
                    LastSeen = DateTime.UtcNow
                },
                new FriendInfo
                {
                    PlayerId = "friend_002",
                    PlayerName = "PirinçKralı",
                    Level = 22,
                    FranchiseCount = 4,
                    FactoryTheme = "neon",
                    IsOnline = false,
                    LastSeen = DateTime.UtcNow.AddHours(-3)
                },
                new FriendInfo
                {
                    PlayerId = "friend_003",
                    PlayerName = "SushiMaster",
                    Level = 8,
                    FranchiseCount = 1,
                    FactoryTheme = "default",
                    IsOnline = true,
                    LastSeen = DateTime.UtcNow
                },
                new FriendInfo
                {
                    PlayerId = "friend_004",
                    PlayerName = "NoodleKing",
                    Level = 30,
                    FranchiseCount = 6,
                    FactoryTheme = "steampunk",
                    IsOnline = false,
                    LastSeen = DateTime.UtcNow.AddDays(-1)
                },
                new FriendInfo
                {
                    PlayerId = "friend_005",
                    PlayerName = "ÇeltikÇılgın",
                    Level = 12,
                    FranchiseCount = 1,
                    FactoryTheme = "tropik",
                    IsOnline = false,
                    LastSeen = DateTime.UtcNow.AddHours(-8)
                }
            };
        }
    }
}
