// =============================================================================
// AnalyticsBridge.cs
// EventManager'daki oyun event'lerini dinleyerek otomatik analytics event'i
// olusturur. Oyun sistemi <-> Analytics arasindaki kopru.
// Tum mevcut IGameEvent'leri subscribe eder ve uygun parametrelerle
// AnalyticsManager'a iletir.
// =============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using RiceFactory.Core.Events;

namespace RiceFactory.Core
{
    /// <summary>
    /// EventManager'daki oyun event'lerini dinleyerek Firebase Analytics'e
    /// otomatik olarak ileten kopru sinifi.
    ///
    /// Sorumluluk: Oyun event'lerini analytics event'lerine donusturmek.
    /// Her event icin uygun parametre haritalamasini yapar.
    ///
    /// Singleton olarak calisir ve ServiceLocator'a kaydedilir.
    /// </summary>
    public class AnalyticsBridge : MonoBehaviour
    {
        // ---- Singleton ----
        private static AnalyticsBridge _instance;

        // ---- Bagimliliklar ----
        private IAnalyticsManager _analytics;
        private IEventManager _eventManager;

        // ---- Sabitler ----
        private const string Tag = "[AnalyticsBridge]";

        // =====================================================================
        // Unity Yasam Dongusu
        // =====================================================================

        private void Awake()
        {
            // Singleton kontrol
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Start'ta bagimliliklar alinir ve event'lere abone olunur.
        /// Awake yerine Start kullanilir cunku AnalyticsManager ve EventManager'in
        /// onceden kayit olmus olmasi gerekir.
        /// </summary>
        private void Start()
        {
            // ServiceLocator'dan bagimliliklari al
            if (!ServiceLocator.TryGet(out _analytics))
            {
                Debug.LogError($"{Tag} IAnalyticsManager bulunamadi! Analytics bridge devre disi.");
                return;
            }

            if (!ServiceLocator.TryGet(out _eventManager))
            {
                Debug.LogError($"{Tag} IEventManager bulunamadi! Analytics bridge devre disi.");
                return;
            }

            SubscribeAll();
            Debug.Log($"{Tag} Tum event'lere abone olundu.");
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                UnsubscribeAll();
                _instance = null;
            }
        }

        // =====================================================================
        // Event Abonelikleri
        // =====================================================================

        /// <summary>Tum oyun event'lerine abone olur.</summary>
        private void SubscribeAll()
        {
            _eventManager.Subscribe<CurrencyChangedEvent>(OnCurrencyChanged);
            _eventManager.Subscribe<ProductionCompletedEvent>(OnProductionCompleted);
            _eventManager.Subscribe<UpgradeCompletedEvent>(OnUpgradeCompleted);
            _eventManager.Subscribe<FranchiseStartedEvent>(OnFranchiseStarted);
            _eventManager.Subscribe<OrderCompletedEvent>(OnOrderCompleted);
            _eventManager.Subscribe<MiniGameCompletedEvent>(OnMiniGameCompleted);
            _eventManager.Subscribe<MilestoneUnlockedEvent>(OnMilestoneUnlocked);
            _eventManager.Subscribe<OfflineEarningsCalculatedEvent>(OnOfflineEarningsCalculated);
        }

        /// <summary>Tum abonelikleri iptal eder.</summary>
        private void UnsubscribeAll()
        {
            if (_eventManager == null) return;

            _eventManager.Unsubscribe<CurrencyChangedEvent>(OnCurrencyChanged);
            _eventManager.Unsubscribe<ProductionCompletedEvent>(OnProductionCompleted);
            _eventManager.Unsubscribe<UpgradeCompletedEvent>(OnUpgradeCompleted);
            _eventManager.Unsubscribe<FranchiseStartedEvent>(OnFranchiseStarted);
            _eventManager.Unsubscribe<OrderCompletedEvent>(OnOrderCompleted);
            _eventManager.Unsubscribe<MiniGameCompletedEvent>(OnMiniGameCompleted);
            _eventManager.Unsubscribe<MilestoneUnlockedEvent>(OnMilestoneUnlocked);
            _eventManager.Unsubscribe<OfflineEarningsCalculatedEvent>(OnOfflineEarningsCalculated);
        }

        // =====================================================================
        // Event Handler'lar
        // =====================================================================

        /// <summary>
        /// Para birimi degisikligi. Kazanc ve harcama ayri event olarak loglanir.
        /// CurrencyType'a gore coin/gem ayrimi yapilir.
        /// </summary>
        private void OnCurrencyChanged(CurrencyChangedEvent e)
        {
            double delta = e.NewAmount - e.OldAmount;
            bool isEarned = delta > 0;

            string eventName;
            switch (e.Type)
            {
                case CurrencyType.Coin:
                    eventName = isEarned ? AnalyticsEvents.CoinEarned : AnalyticsEvents.CoinSpent;
                    break;
                case CurrencyType.Gem:
                    eventName = isEarned ? AnalyticsEvents.GemEarned : AnalyticsEvents.GemSpent;
                    break;
                default:
                    // FP ve Reputation icin su an ayri event tanimlanmamis, genel log
                    return;
            }

            var parameters = new Dictionary<string, object>
            {
                { AnalyticsParams.Amount, Math.Abs(delta) },
                { AnalyticsParams.Source, e.Reason ?? "unknown" }
            };

            _analytics.LogEvent(eventName, parameters);
        }

        /// <summary>
        /// Uretim tamamlandi. Fabrika, urun ve kalite bilgileri loglanir.
        /// </summary>
        private void OnProductionCompleted(ProductionCompletedEvent e)
        {
            _analytics.LogEvent(AnalyticsEvents.ProductionComplete, new Dictionary<string, object>
            {
                { AnalyticsParams.FactoryId, e.FacilityId },
                { AnalyticsParams.ProductId, e.ProductId },
                { AnalyticsParams.Quality, e.Quality },
                { AnalyticsParams.Quantity, e.Quantity }
            });
        }

        /// <summary>
        /// Yukseltme tamamlandi. UpgradeType'a gore uygun event secilir.
        /// Machine, Worker, Star ve Research ayri event olarak loglanir.
        /// </summary>
        private void OnUpgradeCompleted(UpgradeCompletedEvent e)
        {
            string eventName;
            switch (e.Type)
            {
                case UpgradeType.Machine:
                    eventName = AnalyticsEvents.UpgradeMachine;
                    break;
                case UpgradeType.Worker:
                    eventName = AnalyticsEvents.UpgradeWorker;
                    break;
                case UpgradeType.Star:
                    eventName = AnalyticsEvents.UpgradeStar;
                    break;
                case UpgradeType.Research:
                    eventName = AnalyticsEvents.ResearchCompleted;
                    break;
                default:
                    return;
            }

            var parameters = new Dictionary<string, object>
            {
                { AnalyticsParams.FactoryId, e.TargetId },
                { AnalyticsParams.Level, e.NewLevel }
            };

            // Star upgrade icin star_level parametresi ekle
            if (e.Type == UpgradeType.Star)
            {
                parameters[AnalyticsParams.StarLevel] = e.NewLevel;
            }

            _analytics.LogEvent(eventName, parameters);
        }

        /// <summary>
        /// Franchise (prestige) baslatildi. FP, franchise sayisi ve sehir bilgisi loglanir.
        /// </summary>
        private void OnFranchiseStarted(FranchiseStartedEvent e)
        {
            _analytics.LogEvent(AnalyticsEvents.FranchiseStarted, new Dictionary<string, object>
            {
                { AnalyticsParams.FpEarned, e.EarnedFP },
                { AnalyticsParams.FranchiseCount, e.FranchiseNumber }
            });
        }

        /// <summary>
        /// Siparis tamamlandi. Siparis tipi ve odul loglanir.
        /// </summary>
        private void OnOrderCompleted(OrderCompletedEvent e)
        {
            _analytics.LogEvent(AnalyticsEvents.OrderCompleted, new Dictionary<string, object>
            {
                { AnalyticsParams.OrderType, e.Type.ToString().ToLowerInvariant() },
                { AnalyticsParams.Reward, e.Reward }
            });
        }

        /// <summary>
        /// Mini-game tamamlandi. Oyun tipi, derece ve bonus carpani loglanir.
        /// </summary>
        private void OnMiniGameCompleted(MiniGameCompletedEvent e)
        {
            _analytics.LogEvent(AnalyticsEvents.MiniGamePlayed, new Dictionary<string, object>
            {
                { AnalyticsParams.GameType, e.MiniGameId },
                { AnalyticsParams.Grade, e.Grade.ToString().ToLowerInvariant() },
                { AnalyticsParams.BonusMultiplier, e.BonusMultiplier }
            });
        }

        /// <summary>
        /// Milestone acildi. Milestone kimlik ve odul aciklamasi loglanir.
        /// </summary>
        private void OnMilestoneUnlocked(MilestoneUnlockedEvent e)
        {
            _analytics.LogEvent(AnalyticsEvents.MilestoneUnlocked, new Dictionary<string, object>
            {
                { AnalyticsParams.MilestoneId, e.MilestoneId },
                { AnalyticsParams.RewardDescription, e.RewardDescription }
            });
        }

        /// <summary>
        /// Offline kazanc hesaplandi. Toplam coin, sure ve verimlilik loglanir.
        /// </summary>
        private void OnOfflineEarningsCalculated(OfflineEarningsCalculatedEvent e)
        {
            _analytics.LogEvent(AnalyticsEvents.OfflineEarningsCollected, new Dictionary<string, object>
            {
                { AnalyticsParams.Amount, e.TotalCoins },
                { AnalyticsParams.HoursAway, Math.Round(e.Duration.TotalHours, 1) }
            });
        }
    }
}
