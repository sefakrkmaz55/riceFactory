// =============================================================================
// EventManager.cs
// Event Bus pattern ile sistemler arasi gevşek bagli iletisim.
// Generic Subscribe/Unsubscribe/Publish mekanizmasi sunar.
// Tum oyun event'leri bu sistem uzerinden iletilir.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RiceFactory.Core
{
    // -------------------------------------------------------------------------
    // Event Arayuzu
    // -------------------------------------------------------------------------

    /// <summary>
    /// Tum oyun event'lerinin uygulamasi gereken temel arayuz.
    /// Struct olarak tanimlanan event'ler GC basincinı azaltir.
    /// </summary>
    public interface IGameEvent { }

    // -------------------------------------------------------------------------
    // Event Manager Arayuzu
    // -------------------------------------------------------------------------

    /// <summary>
    /// EventManager icin interface. Test edilebilirlik ve ServiceLocator uyumu icin.
    /// </summary>
    public interface IEventManager
    {
        void Subscribe<T>(Action<T> listener) where T : IGameEvent;
        void Unsubscribe<T>(Action<T> listener) where T : IGameEvent;
        void Publish<T>(T gameEvent) where T : IGameEvent;
        void Clear();
    }

    // -------------------------------------------------------------------------
    // Event Manager Implementasyonu
    // -------------------------------------------------------------------------

    /// <summary>
    /// Merkezi event bus. Sistemler birbirine dogrudan referans olmadan
    /// event'ler uzerinden haberlesir.
    ///
    /// Kullanim:
    ///   eventManager.Subscribe&lt;CurrencyChangedEvent&gt;(OnCurrencyChanged);
    ///   eventManager.Publish(new CurrencyChangedEvent { ... });
    ///   eventManager.Unsubscribe&lt;CurrencyChangedEvent&gt;(OnCurrencyChanged);
    /// </summary>
    public class EventManager : IEventManager
    {
        private readonly Dictionary<Type, List<Delegate>> _listeners = new();

        /// <summary>
        /// Bir event tipine listener kaydeder.
        /// Ayni listener birden fazla kez kaydedilmez.
        /// </summary>
        public void Subscribe<T>(Action<T> listener) where T : IGameEvent
        {
            if (listener == null)
            {
                Debug.LogWarning("[EventManager] Null listener kaydi reddedildi.");
                return;
            }

            var type = typeof(T);
            if (!_listeners.ContainsKey(type))
            {
                _listeners[type] = new List<Delegate>();
            }

            // Ayni listener'in tekrar eklenmesini onle
            if (_listeners[type].Contains(listener))
            {
                Debug.LogWarning($"[EventManager] {type.Name} icin ayni listener zaten kayitli.");
                return;
            }

            _listeners[type].Add(listener);
        }

        /// <summary>
        /// Bir event tipinden listener'i kaldirir.
        /// </summary>
        public void Unsubscribe<T>(Action<T> listener) where T : IGameEvent
        {
            if (listener == null) return;

            var type = typeof(T);
            if (_listeners.ContainsKey(type))
            {
                _listeners[type].Remove(listener);

                // Bos listeyi temizle
                if (_listeners[type].Count == 0)
                {
                    _listeners.Remove(type);
                }
            }
        }

        /// <summary>
        /// Bir event'i tum kayitli listener'lara yayinlar.
        /// Listener icerisinden Subscribe/Unsubscribe yapilabilmesi icin
        /// kopya liste uzerinde iterate eder.
        /// </summary>
        public void Publish<T>(T gameEvent) where T : IGameEvent
        {
            var type = typeof(T);
            if (!_listeners.ContainsKey(type)) return;

            // Kopya liste: listener icerisinden subscribe/unsubscribe guvenli olsun
            var snapshot = _listeners[type].ToList();
            foreach (var listener in snapshot)
            {
                try
                {
                    ((Action<T>)listener).Invoke(gameEvent);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[EventManager] {type.Name} isleme hatasi: {ex}");
                }
            }
        }

        /// <summary>
        /// Tum listener'lari temizler. Sahne gecisleri ve test icin.
        /// </summary>
        public void Clear()
        {
            _listeners.Clear();
        }

        /// <summary>
        /// Belirli bir event tipi icin kayitli listener sayisini dondurur.
        /// Debug ve test amacli.
        /// </summary>
        public int GetListenerCount<T>() where T : IGameEvent
        {
            var type = typeof(T);
            return _listeners.ContainsKey(type) ? _listeners[type].Count : 0;
        }
    }
}

// =============================================================================
// Oyun Event Tanimlamalari
// =============================================================================

namespace RiceFactory.Core.Events
{
    using System;

    // ---- Oyun Durumu Event'leri ----

    /// <summary>Oyun durumu degistiginde tetiklenir.</summary>
    public struct GameStateChangedEvent : IGameEvent
    {
        public GameState OldState;
        public GameState NewState;

        public GameStateChangedEvent(GameState oldState, GameState newState)
        {
            OldState = oldState;
            NewState = newState;
        }
    }

    /// <summary>Her frame tetiklenir. Sistemlerin guncellenmesi icin.</summary>
    public struct GameTickEvent : IGameEvent
    {
        public float DeltaTime;

        public GameTickEvent(float deltaTime)
        {
            DeltaTime = deltaTime;
        }
    }

    /// <summary>Uygulama arka plandan donduguunde tetiklenir.</summary>
    public struct AppResumedEvent : IGameEvent
    {
        public TimeSpan OfflineDuration;

        public AppResumedEvent(TimeSpan duration)
        {
            OfflineDuration = duration;
        }
    }

    // ---- Ekonomi Event'leri ----

    /// <summary>Para birimi degistiginde tetiklenir (Coin, Gem, FP, Reputation).</summary>
    public struct CurrencyChangedEvent : IGameEvent
    {
        public CurrencyType Type;
        public double OldAmount;
        public double NewAmount;
        public string Reason;
    }

    // ---- Uretim Event'leri ----

    /// <summary>Bir uretim dongüsu tamamlandiginda tetiklenir.</summary>
    public struct ProductionCompletedEvent : IGameEvent
    {
        public string FacilityId;
        public string ProductId;
        public int Quantity;
        public int Quality; // 1-5 yildiz
    }

    /// <summary>Urun satildiginda tetiklenir.</summary>
    public struct ProductSoldEvent : IGameEvent
    {
        public string ProductId;
        public int Quantity;
        public double Revenue;
    }

    // ---- Upgrade Event'leri ----

    /// <summary>Herhangi bir yukseltme tamamlandiginda tetiklenir.</summary>
    public struct UpgradeCompletedEvent : IGameEvent
    {
        public UpgradeType Type; // Machine, Worker, Star, Research
        public string TargetId;
        public int NewLevel;
    }

    // ---- Prestige Event'leri ----

    /// <summary>Franchise (prestige) baslatidiginda tetiklenir.</summary>
    public struct FranchiseStartedEvent : IGameEvent
    {
        public int FranchiseNumber;
        public int EarnedFP;
        public string NewCityId;
    }

    // ---- Siparis Event'leri ----

    /// <summary>Bir siparis tamamlandiginda tetiklenir.</summary>
    public struct OrderCompletedEvent : IGameEvent
    {
        public string OrderId;
        public OrderType Type;
        public double Reward;
    }

    /// <summary>Bir siparisin suresi dolduğunda tetiklenir.</summary>
    public struct OrderExpiredEvent : IGameEvent
    {
        public string OrderId;
        public int ReputationLoss;
    }

    // ---- Mini-Game Event'leri ----

    /// <summary>Mini-game tamamlandiginda tetiklenir.</summary>
    public struct MiniGameCompletedEvent : IGameEvent
    {
        public string MiniGameId;
        public MiniGameGrade Grade; // S, A, B, C
        public int Score;
        public float BonusMultiplier;
        public int CoinReward;
        public float BoostDuration; // Saniye cinsinden uretim boost suresi
    }

    // ---- Milestone Event'leri ----

    /// <summary>Yeni bir milestone acildiginda tetiklenir.</summary>
    public struct MilestoneUnlockedEvent : IGameEvent
    {
        public string MilestoneId;
        public string RewardDescription;
    }

    // ---- Offline Kazanc Event'leri ----

    /// <summary>Offline kazanc hesaplandiktan sonra tetiklenir.</summary>
    public struct OfflineEarningsCalculatedEvent : IGameEvent
    {
        public double TotalCoins;
        public int TotalProducts;
        public TimeSpan Duration;
        public float Efficiency;
    }

    // -------------------------------------------------------------------------
    // Yardimci Enum'lar
    // -------------------------------------------------------------------------

    /// <summary>Para birimi tipleri.</summary>
    public enum CurrencyType
    {
        Coin,
        Gem,
        FranchisePoint,
        FP = FranchisePoint, // CurrencySystem ve PrestigeSystem uyumlulugu icin alias
        Reputation
    }

    /// <summary>Upgrade tipleri.</summary>
    public enum UpgradeType
    {
        Machine,
        Worker,
        Star,
        Research
    }

    /// <summary>Siparis tipleri.</summary>
    public enum OrderType
    {
        Normal,
        Special,
        Daily,
        Weekly
    }

    /// <summary>Mini-game basari dereceleri.</summary>
    public enum MiniGameGrade
    {
        C,      // Bronz — minimum basari
        B,      // Gumus — orta basari
        A,      // Altin — iyi basari
        S       // Elmas — mukemmel basari
    }
}
