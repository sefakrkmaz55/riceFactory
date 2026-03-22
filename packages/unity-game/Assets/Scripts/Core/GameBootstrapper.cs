// =============================================================================
// GameBootstrapper.cs
// Boot sahnesine eklenen MonoBehaviour. Tum servisleri dogru sirada baslatir,
// ServiceLocator'a kaydeder ve oyunu baslangic durumuna getirir.
//
// Baslangic sirasi bagimliliklara gore belirlenmistir:
//   EventManager -> IBalanceConfig -> SaveManager -> TimeManager -> GameManager
//   -> CurrencySystem -> PriceCalculator -> ProductionManager -> PrestigeSystem
//   -> UpgradeSystem -> ResearchSystem -> OrderSystem -> BattlePassSystem
//   -> LeaderboardSystem -> FriendSystem -> AdManager -> IAPManager
//   -> MonoBehaviour servisler (UIManager, AudioManager, AnalyticsManager, AnalyticsBridge)
//
// Referans: docs/TECH_ARCHITECTURE.md — Boot sequence
// =============================================================================

using System;
using System.Threading.Tasks;
using UnityEngine;
using RiceFactory.Data.Save;
using RiceFactory.Economy;
using RiceFactory.Production;
using RiceFactory.Social;
using RiceFactory.Ads;
using RiceFactory.UI;
using RiceFactory.Core.Events;

namespace RiceFactory.Core
{
    /// <summary>
    /// Boot sahnesinde tek bir GameObject'e eklenir.
    /// Awake'te tum servisleri olusturur, kaydeder ve oyunu baslatir.
    /// DontDestroyOnLoad ile sahne gecislerinde hayatta kalir.
    /// </summary>
    public class GameBootstrapper : MonoBehaviour
    {
        // =====================================================================
        // Inspector Referanslari (MonoBehaviour servisler sahnede hazir olmali)
        // =====================================================================

        [Header("Sahne Referanslari (MonoBehaviour Servisler)")]
        [Tooltip("Sahnedeki AudioManager referansi")]
        [SerializeField] private AudioManager _audioManager;

        [Tooltip("Sahnedeki UIManager referansi")]
        [SerializeField] private UIManager _uiManager;

        [Tooltip("Sahnedeki AnalyticsManager referansi")]
        [SerializeField] private AnalyticsManager _analyticsManager;

        [Tooltip("Sahnedeki AnalyticsBridge referansi")]
        [SerializeField] private AnalyticsBridge _analyticsBridge;

        [Header("Boot Ayarlari")]
        [Tooltip("Boot sonrasi direkt Game sahnesine mi gecilsin (true) yoksa MainMenu'ye mi (false)?")]
        [SerializeField] private bool _skipMainMenu = false;

        // =====================================================================
        // Dahili Durum
        // =====================================================================

        private bool _isBooted;
        private static GameBootstrapper _instance;

        // =====================================================================
        // Unity Lifecycle
        // =====================================================================

        private async void Awake()
        {
            // Singleton kontrolu
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            await BootAsync();
        }

        // =====================================================================
        // Boot Sirasi
        // =====================================================================

        /// <summary>
        /// Tum servisleri sirasyla olusturur ve ServiceLocator'a kaydeder.
        /// Bagimliliklara gore belirlenmis kritik sira.
        /// </summary>
        private async Task BootAsync()
        {
            if (_isBooted)
            {
                Debug.LogWarning("[GameBootstrapper] Zaten boot edildi, atlanıyor.");
                return;
            }

            Debug.Log("[GameBootstrapper] Boot basladi...");
            var bootStart = Time.realtimeSinceStartup;

            try
            {
                // ---- 1. ServiceLocator temizle ----
                ServiceLocator.Reset();

                // ---- 2. EventManager ----
                var eventManager = new EventManager();
                ServiceLocator.Register<IEventManager>(eventManager);
                Debug.Log("[GameBootstrapper] [1/21] EventManager kayitlandi.");

                // ---- 3. Firebase (IBalanceConfig bagimli) ----
                var firebaseManager = FirebaseManager.Instance;
                await firebaseManager.InitializeAsync();
                ServiceLocator.Register<IFirebaseManager>(firebaseManager);
                Debug.Log("[GameBootstrapper] [2/21] FirebaseManager kayitlandi.");

                // ---- 3b. RemoteConfigManager (IBalanceConfig implementasyonu) ----
                var remoteConfig = new RemoteConfigManager(firebaseManager);
                await remoteConfig.FetchAndActivateAsync();
                ServiceLocator.Register<IRemoteConfigManager>(remoteConfig);
                ServiceLocator.Register<IBalanceConfig>(remoteConfig);
                Debug.Log("[GameBootstrapper] [3/21] RemoteConfigManager + IBalanceConfig kayitlandi.");

                // ---- 4. SaveManager ----
                var saveManager = new SaveManager();
                await saveManager.LoadAsync();
                ServiceLocator.Register<ISaveManager>(saveManager);
                Debug.Log("[GameBootstrapper] [4/21] SaveManager kayitlandi.");

                // ---- 5. TimeManager ----
                var timeManager = new TimeManager();
                await timeManager.SyncServerTimeAsync();
                ServiceLocator.Register<ITimeManager>(timeManager);
                Debug.Log("[GameBootstrapper] [5/21] TimeManager kayitlandi.");

                // ---- 6. GameManager ----
                var gameManager = new GameManager(saveManager, eventManager, timeManager);
                ServiceLocator.Register<IGameManager>(gameManager);
                Debug.Log("[GameBootstrapper] [6/21] GameManager kayitlandi.");

                // GameManagerBehaviour proxy'sini olustur
                var gmBehaviourObj = new GameObject("[GameManagerBehaviour]");
                gmBehaviourObj.transform.SetParent(transform);
                var gmBehaviour = gmBehaviourObj.AddComponent<GameManagerBehaviour>();
                gmBehaviour.Initialize(gameManager);

                // ---- 7. CurrencySystem ----
                var currencySystem = new CurrencySystem(saveManager, eventManager);
                ServiceLocator.Register<IEconomySystem>(currencySystem);
                Debug.Log("[GameBootstrapper] [7/21] CurrencySystem kayitlandi.");

                // ---- 8. PriceCalculator ----
                var priceCalculator = new PriceCalculator(remoteConfig);
                ServiceLocator.Register<PriceCalculator>(priceCalculator);
                Debug.Log("[GameBootstrapper] [8/21] PriceCalculator kayitlandi.");

                // ---- 9. ProductionManager ----
                var productionManager = new ProductionManager(
                    remoteConfig, saveManager, eventManager, priceCalculator, currencySystem);
                ServiceLocator.Register<ProductionManager>(productionManager);
                Debug.Log("[GameBootstrapper] [9/21] ProductionManager kayitlandi.");

                // ---- 10. PrestigeSystem ----
                var prestigeSystem = new PrestigeSystem(remoteConfig, saveManager, eventManager);
                ServiceLocator.Register<IPrestigeSystem>(prestigeSystem);
                Debug.Log("[GameBootstrapper] [10/21] PrestigeSystem kayitlandi.");

                // ---- 11. UpgradeSystem ----
                var upgradeSystem = new UpgradeSystem(
                    priceCalculator, productionManager, currencySystem, remoteConfig);
                ServiceLocator.Register<IUpgradeSystem>(upgradeSystem);
                Debug.Log("[GameBootstrapper] [11/21] UpgradeSystem kayitlandi.");

                // ---- 12. ResearchSystem ----
                var researchSystem = new ResearchSystem(
                    remoteConfig, saveManager, eventManager, currencySystem);
                ServiceLocator.Register<ResearchSystem>(researchSystem);
                Debug.Log("[GameBootstrapper] [12/21] ResearchSystem kayitlandi.");

                // ---- 13. OrderSystem ----
                var orderSystem = new OrderSystem(
                    remoteConfig, saveManager, eventManager, currencySystem);
                ServiceLocator.Register<OrderSystem>(orderSystem);
                Debug.Log("[GameBootstrapper] [13/21] OrderSystem kayitlandi.");

                // ---- 14. BattlePassSystem ----
                var battlePassSystem = new BattlePassSystem(eventManager, saveManager);
                ServiceLocator.Register<IBattlePassSystem>(battlePassSystem);
                Debug.Log("[GameBootstrapper] [14/21] BattlePassSystem kayitlandi.");

                // ---- 15. AuthManager ----
                var authManager = new AuthManager(firebaseManager);
                await authManager.InitializeAsync();
                ServiceLocator.Register<IAuthManager>(authManager);
                Debug.Log("[GameBootstrapper] [15/21] AuthManager kayitlandi.");

                // ---- 16. CloudSaveManager ----
                var cloudSaveManager = new CloudSaveManager(firebaseManager, authManager, saveManager);
                await cloudSaveManager.SyncAsync();
                ServiceLocator.Register<ICloudSaveManager>(cloudSaveManager);
                Debug.Log("[GameBootstrapper] [16/21] CloudSaveManager kayitlandi.");

                // ---- 17. LeaderboardSystem ----
                var leaderboardSystem = new LeaderboardSystem(eventManager);
                ServiceLocator.Register<ILeaderboardSystem>(leaderboardSystem);
                Debug.Log("[GameBootstrapper] [17/21] LeaderboardSystem kayitlandi.");

                // ---- 18. FriendSystem ----
                var friendSystem = new FriendSystem(eventManager, saveManager);
                ServiceLocator.Register<IFriendSystem>(friendSystem);
                Debug.Log("[GameBootstrapper] [18/21] FriendSystem kayitlandi.");

                // ---- 19. AdManager ----
                var adManager = new AdManager(eventManager);
                ServiceLocator.Register<IAdManager>(adManager);
                Debug.Log("[GameBootstrapper] [19/21] AdManager kayitlandi.");

                // ---- 20. IAPManager ----
                var iapManager = new IAPManager(eventManager, saveManager);
                ServiceLocator.Register<IIAPManager>(iapManager);
                Debug.Log("[GameBootstrapper] [20/21] IAPManager kayitlandi.");

                // ---- 21. MonoBehaviour Servisler ----
                // UIManager — sahnede hazir olmali, fallback: FindFirstObjectByType
                if (_uiManager == null)
                    _uiManager = FindFirstObjectByType<UIManager>();
                if (_uiManager != null)
                    Debug.Log("[GameBootstrapper] [21a] UIManager bulundu.");
                else
                    Debug.LogWarning("[GameBootstrapper] UIManager sahnede bulunamadi!");

                // AudioManager — sahnede hazir olmali
                if (_audioManager == null)
                    _audioManager = FindFirstObjectByType<AudioManager>();
                if (_audioManager != null)
                {
                    ServiceLocator.Register<IAudioManager>(_audioManager);
                    Debug.Log("[GameBootstrapper] [21b] AudioManager kayitlandi.");
                }
                else
                {
                    Debug.LogWarning("[GameBootstrapper] AudioManager sahnede bulunamadi!");
                }

                // AnalyticsManager — Awake'te kendini ServiceLocator'a kaydeder
                if (_analyticsManager == null)
                    _analyticsManager = FindFirstObjectByType<AnalyticsManager>();
                if (_analyticsManager != null)
                {
                    _analyticsManager.StartSession();
                    Debug.Log("[GameBootstrapper] [21c] AnalyticsManager hazir.");
                }

                // AnalyticsBridge — Start'ta kendi bagimliklarini ServiceLocator'dan alir
                if (_analyticsBridge == null)
                    _analyticsBridge = FindFirstObjectByType<AnalyticsBridge>();

                Debug.Log("[GameBootstrapper] [21/21] MonoBehaviour servisler tamamlandi.");

                // =====================================================================
                // Boot Sonrasi: Offline Kazanc ve Sahne Gecisi
                // =====================================================================

                _isBooted = true;
                float bootDuration = Time.realtimeSinceStartup - bootStart;
                Debug.Log($"[GameBootstrapper] Boot tamamlandi. Sure: {bootDuration:F2}s");

                // Offline sure hesapla
                var offlineDuration = timeManager.GetTimeSincePause();
                if (offlineDuration.TotalSeconds > 60) // 1 dakikadan fazla offline kaldiysa
                {
                    Debug.Log($"[GameBootstrapper] Offline sure: {offlineDuration.TotalMinutes:F1} dk");

                    var offlineResult = productionManager.CalculateOfflineProduction(offlineDuration);

                    if (offlineResult.TotalCoins > 0)
                    {
                        // Offline kazanci uygula
                        currencySystem.AddCoins(offlineResult.TotalCoins, "offline_earnings");

                        // Offline kazanc eventini firlat (OfflineEarningsPanel dinler)
                        eventManager.Publish(new OfflineEarningsCalculatedEvent
                        {
                            TotalCoins = offlineResult.TotalCoins,
                            TotalProducts = offlineResult.TotalProducts,
                            Duration = offlineResult.Duration,
                            Efficiency = offlineResult.Efficiency
                        });

                        Debug.Log($"[GameBootstrapper] Offline kazanc: {offlineResult.TotalCoins:N0} coin, " +
                                  $"{offlineResult.TotalProducts} urun");
                    }

                    // Offline surede devam eden arastirmayi hesapla
                    researchSystem.ProcessOfflineTime((float)offlineDuration.TotalSeconds);
                }

                // Sahne gecisi
                bool hasSave = saveManager.Data != null && saveManager.Data.SaveVersion > 1;

                if (_skipMainMenu || hasSave)
                {
                    // Direkt oyuna gir
                    gameManager.ChangeState(GameState.Playing);
                    SceneController.LoadScene("Game");
                }
                else
                {
                    // Ana menuye git
                    gameManager.ChangeState(GameState.MainMenu);
                    SceneController.LoadScene("MainMenu");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameBootstrapper] Boot sirasinda kritik hata: {ex}");

                // Hata durumunda en azindan ana menuye gecmeyi dene
                try
                {
                    SceneController.LoadScene("MainMenu");
                }
                catch
                {
                    Debug.LogError("[GameBootstrapper] Ana menu sahnesine de gecilemedi!");
                }
            }
        }
    }
}
