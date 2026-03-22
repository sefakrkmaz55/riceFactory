# riceFactory -- Teknik Mimari Dokumani

**Versiyon:** 1.0
**Tarih:** 2026-03-22
**Yazar:** Teknik Mimar (tech-architect agent)
**Referans:** `docs/GDD.md` v1.0, `docs/ECONOMY_BALANCE.md` v1.0

---

## Icindekiler

1. [Mimari Genel Bakis](#1-mimari-genel-bakis)
2. [Unity Proje Yapisi](#2-unity-proje-yapisi)
3. [Core Sistemler](#3-core-sistemler)
4. [Oyun Sistemleri](#4-oyun-sistemleri)
5. [Veri Yapilari](#5-veri-yapilari)
6. [Firebase Entegrasyonu](#6-firebase-entegrasyonu)
7. [Performans Stratejisi](#7-performans-stratejisi)
8. [Build Pipeline](#8-build-pipeline)
9. [Guvenlik](#9-guvenlik)
10. [Ucuncu Parti Kutuphaneler](#10-ucuncu-parti-kutuphaneler)

---

## 1. Mimari Genel Bakis

### 1.1 Sistem Diyagrami (Istemci <-> Sunucu)

```
+------------------------------------------------------------------+
|                        ISTEMCI (Unity)                            |
|                                                                  |
|  +------------------+  +------------------+  +-----------------+ |
|  |  Presentation    |  |     Logic        |  |     Data        | |
|  |  (UI, Animasyon, |  |  (GameManager,   |  |  (ScriptableObj,| |
|  |   Input, VFX)    |  |   Sistemler,     |  |   SaveData,     | |
|  |                  |  |   Event Bus)     |  |   RemoteConfig) | |
|  +--------+---------+  +--------+---------+  +--------+--------+ |
|           |                     |                      |          |
|           +----------+----------+----------+-----------+          |
|                      |                     |                      |
|              +-------v-------+     +-------v--------+            |
|              | Firebase SDK  |     | Local Storage  |            |
|              | (Auth, Store, |     | (PlayerPrefs,  |            |
|              |  Analytics)   |     |  JSON Files)   |            |
|              +-------+-------+     +----------------+            |
+--------------------------|---------------------------------------+
                           | HTTPS / gRPC
                           v
+------------------------------------------------------------------+
|                    SUNUCU (Firebase)                              |
|                                                                  |
|  +-----------------+  +-----------------+  +------------------+  |
|  | Authentication  |  |   Firestore     |  | Cloud Functions  |  |
|  | (Anonim, Google,|  |   (Oyuncu veri, |  | (Anti-cheat,     |  |
|  |  Apple, Email)  |  |    Liderboard,  |  |  Ekonomi valid., |  |
|  |                 |  |    Ticaret)     |  |  Push trigger)   |  |
|  +-----------------+  +-----------------+  +------------------+  |
|                                                                  |
|  +-----------------+  +-----------------+  +------------------+  |
|  | Remote Config   |  |   Analytics     |  | Cloud Messaging  |  |
|  | (Ekonomi param.,|  |   (Olay takip,  |  | (Push bildirim,  |  |
|  |  A/B test,      |  |    funnel,      |  |  geri donus,     |  |
|  |  ozellik flag)  |  |    retention)   |  |  etkinlik duyuru)|  |
|  +-----------------+  +-----------------+  +------------------+  |
+------------------------------------------------------------------+
```

### 1.2 Katmanli Mimari

Proje uc ana katmandan olusur. Bagimlilik yonu her zaman icten disa dogrudur: **Data <- Logic <- Presentation**.

```
+---------------------------------------------------------------+
|                    PRESENTATION LAYER                          |
|  UI Panelleri, Animasyonlar, Input, VFX, Ses                  |
|  (MonoBehaviour'lar, UI Controller'lar)                       |
+----------------------------+----------------------------------+
                             | Bagimsiz arayuzler (interface)
+----------------------------v----------------------------------+
|                      LOGIC LAYER                              |
|  GameManager, ProductionSystem, EconomySystem,                |
|  UpgradeSystem, PrestigeSystem, QuestSystem, TimeManager      |
|  (Pure C# siniflar + ScriptableObject referanslari)           |
+----------------------------+----------------------------------+
                             | Data modelleri ve repository'ler
+----------------------------v----------------------------------+
|                       DATA LAYER                              |
|  ScriptableObject'ler, SaveManager, RemoteConfigManager,      |
|  FirestoreRepository, LocalRepository                         |
|  (Kalicilik, seri/deseri, Firebase SDK wrapper)               |
+---------------------------------------------------------------+
```

### 1.3 Namespace Yapisi

```
RiceFactory
  RiceFactory.Core            // GameManager, EventManager, TimeManager
  RiceFactory.Core.Events     // Event tanimlamalari
  RiceFactory.Data            // ScriptableObject'ler, veri modelleri
  RiceFactory.Data.Save       // Save/Load siniflari
  RiceFactory.Data.Config     // Remote Config wrapper
  RiceFactory.Systems         // Tum oyun sistemleri
  RiceFactory.Systems.Production
  RiceFactory.Systems.Economy
  RiceFactory.Systems.Upgrade
  RiceFactory.Systems.Prestige
  RiceFactory.Systems.Quest
  RiceFactory.Systems.Social
  RiceFactory.UI              // Tum UI siniflari
  RiceFactory.UI.Panels
  RiceFactory.UI.Components
  RiceFactory.UI.Popups
  RiceFactory.Audio           // Ses yonetimi
  RiceFactory.Utils           // Yardimci siniflar, extension'lar
  RiceFactory.Firebase        // Firebase SDK wrapper'lar
  RiceFactory.MiniGames       // Mini-game controller'lar
```

---

## 2. Unity Proje Yapisi

### 2.1 Klasor Yapisi ve Sorumluluklar

```
packages/unity-game/
  Assets/
    _Project/                         # Ana proje klasoru
      Scenes/
        Boot.unity                    # Baslangic, SDK init, splash
        MainMenu.unity                # Ana menu, ayarlar
        Game.unity                    # Ana oyun sahnesi
        MiniGame_Harvest.unity        # Hasat Kosusu mini-game
        MiniGame_Quality.unity        # Kalite Kontrol mini-game
        MiniGame_Oven.unity           # Firin Zamanlama mini-game
        MiniGame_Chef.unity           # Sef Ustasi mini-game
        MiniGame_Shelf.unity          # Raf Duzeni mini-game
        MiniGame_Bargain.unity        # Pazarlik mini-game
      Scripts/
        Core/                         # Tekil yoneticiler
          GameManager.cs
          EventManager.cs
          TimeManager.cs
          AudioManager.cs
          SaveManager.cs
          UIManager.cs
          ServiceLocator.cs
        Data/
          ScriptableObjects/          # SO tanimlari
            FacilityData.cs
            ProductData.cs
            MachineData.cs
            WorkerData.cs
            UpgradeData.cs
            ResearchData.cs
            OrderData.cs
            CityData.cs
          Models/                     # Runtime veri modelleri
            PlayerSaveData.cs
            FacilityState.cs
            ProductionState.cs
            InventoryState.cs
          Config/
            RemoteConfigManager.cs
            BalanceConfig.cs
          Repository/
            IDataRepository.cs
            LocalRepository.cs
            FirestoreRepository.cs
        Systems/
          Production/
            ProductionSystem.cs
            ProductionPipeline.cs
            QualityCalculator.cs
          Economy/
            EconomySystem.cs
            PriceCalculator.cs
            DemandSystem.cs
          Upgrade/
            UpgradeSystem.cs
            MachineUpgradeHandler.cs
            WorkerUpgradeHandler.cs
            StarUpgradeHandler.cs
          Prestige/
            PrestigeSystem.cs
            FranchiseCalculator.cs
          Quest/
            QuestSystem.cs
            OrderManager.cs
            DailyQuestManager.cs
          Social/
            SocialSystem.cs
            LeaderboardManager.cs
            FriendManager.cs
            TradeManager.cs
          Research/
            ResearchSystem.cs
            ResearchTree.cs
        UI/
          Panels/
            MainGamePanel.cs
            FacilityPanel.cs
            UpgradePanel.cs
            ResearchPanel.cs
            ShopPanel.cs
            SocialPanel.cs
            SettingsPanel.cs
            FranchisePanel.cs
            OfflineRewardPanel.cs
          Components/
            ProductionSlotUI.cs
            ResourceBarUI.cs
            OrderCardUI.cs
            LeaderboardEntryUI.cs
            StarRatingUI.cs
          Popups/
            ConfirmPopup.cs
            RewardPopup.cs
            AdOfferPopup.cs
            MilestonePopup.cs
          Base/
            PanelBase.cs
            PopupBase.cs
            UIAnimator.cs
        MiniGames/
          MiniGameBase.cs
          HarvestRunGame.cs
          QualityControlGame.cs
          OvenTimingGame.cs
          ChefMasterGame.cs
          ShelfArrangeGame.cs
          BargainGame.cs
        Audio/
          AudioClipDatabase.cs
          MusicController.cs
          SFXController.cs
        Firebase/
          FirebaseInitializer.cs
          AuthManager.cs
          FirestoreManager.cs
          AnalyticsManager.cs
          CloudMessagingManager.cs
          RemoteConfigFetcher.cs
        Utils/
          BigNumber.cs              # Buyuk sayi gosterimi (1.5M, 2.3B)
          TimeUtils.cs
          MathUtils.cs
          Extensions.cs
          ObjectPool.cs
          Coroutines.cs
      Prefabs/
        Facilities/                  # Tesis prefab'lari
        UI/                          # UI prefab'lari
        Effects/                     # VFX prefab'lari
        MiniGames/                   # Mini-game prefab'lari
      Art/
        Sprites/
          Facilities/
          Products/
          UI/
          Characters/
          Cities/
        Animations/
        SpriteAtlases/
      Audio/
        Music/
        SFX/
      Data/
        ScriptableObjects/           # SO instance'lari (.asset dosyalari)
          Facilities/
          Products/
          Research/
          Cities/
        Config/
          balance_config.json
      Addressables/                  # Addressable asset gruplari
      Plugins/                       # Native plugin'ler
    ThirdParty/                      # Ucuncu parti asset'ler
```

### 2.2 Sahne Yapisi

```
                    BOOT
                     |
              (SDK Init, Auth,
               Remote Config,
               Save Load)
                     |
                     v
                 MAIN MENU
               /          \
              v            v
           GAME        SETTINGS
          /    \
   (Ana Oyun)  (Mini-Game'ler
                 additive load)
```

**Boot Sahnesi:**
- Firebase SDK baslat
- Auth kontrolu (anonim giris veya mevcut oturum)
- Remote Config fetch
- Save data yukle (lokal + bulut merge)
- Splash / loading ekrani
- MainMenu'ye gecis

**MainMenu Sahnesi:**
- Baslat butonu
- Ayarlar
- Sosyal panel (liderboard, arkadaslar)
- Dukkan (IAP, kozmetik)
- Battle Pass goruntuleme
- Offline reward paneli (eger offline kazanc varsa)

**Game Sahnesi:**
- Ana oyun dongusu
- Tesis gorunumleri (scroll)
- Ust bar (para, elmas, seviye)
- Alt bar (navigasyon)
- Mini-game'ler additive olarak yuklenir

### 2.3 Manager Siniflari ve Yasam Dongusu

Manager'lar **ServiceLocator** pattern ile yonetilir. Hepsi `Boot` sahnesinde `DontDestroyOnLoad` ile olusturulur.

```csharp
namespace RiceFactory.Core
{
    /// <summary>
    /// Tum core servisleri baslatan ve yasam dongusunu yoneten sinif.
    /// Boot sahnesinde calisir, DontDestroyOnLoad ile kalici olur.
    /// </summary>
    public class GameBootstrapper : MonoBehaviour
    {
        [SerializeField] private GameConfig _gameConfig;

        private async void Awake()
        {
            DontDestroyOnLoad(gameObject);

            // 1. Data Layer -- once veri kaynaklari
            var remoteConfig = new RemoteConfigManager();
            await remoteConfig.FetchAndActivateAsync();

            var balanceConfig = new BalanceConfig(remoteConfig);
            ServiceLocator.Register<IBalanceConfig>(balanceConfig);

            // 2. Core Layer -- temel servisler
            var eventManager = new EventManager();
            ServiceLocator.Register<IEventManager>(eventManager);

            var timeManager = new TimeManager();
            ServiceLocator.Register<ITimeManager>(timeManager);

            var saveManager = new SaveManager();
            await saveManager.LoadAsync();
            ServiceLocator.Register<ISaveManager>(saveManager);

            var audioManager = new AudioManager(_gameConfig.AudioConfig);
            ServiceLocator.Register<IAudioManager>(audioManager);

            var uiManager = new UIManager();
            ServiceLocator.Register<IUIManager>(uiManager);

            // 3. Game Systems -- oyun sistemleri
            var economySystem = new EconomySystem(balanceConfig, saveManager);
            ServiceLocator.Register<IEconomySystem>(economySystem);

            var productionSystem = new ProductionSystem(balanceConfig, saveManager, economySystem);
            ServiceLocator.Register<IProductionSystem>(productionSystem);

            var upgradeSystem = new UpgradeSystem(balanceConfig, saveManager, economySystem);
            ServiceLocator.Register<IUpgradeSystem>(upgradeSystem);

            var prestigeSystem = new PrestigeSystem(balanceConfig, saveManager);
            ServiceLocator.Register<IPrestigeSystem>(prestigeSystem);

            var questSystem = new QuestSystem(balanceConfig, saveManager, economySystem);
            ServiceLocator.Register<IQuestSystem>(questSystem);

            var researchSystem = new ResearchSystem(balanceConfig, saveManager, timeManager);
            ServiceLocator.Register<IResearchSystem>(researchSystem);

            var socialSystem = new SocialSystem();
            ServiceLocator.Register<ISocialSystem>(socialSystem);

            // 4. Offline kazanc hesapla
            var offlineEarnings = timeManager.CalculateOfflineEarnings(
                saveManager.Data, balanceConfig, productionSystem
            );

            // 5. Sahne gecisi
            if (offlineEarnings.TotalCoins > 0)
            {
                // Offline reward ekranini goster, sonra MainMenu veya Game
                uiManager.ShowOfflineReward(offlineEarnings);
            }

            SceneManager.LoadScene("MainMenu");
        }
    }
}
```

### 2.4 Dependency Injection Yaklasimi

Proje, hafif bir **Service Locator** pattern kullanir. Tam DI framework'u (Zenject/VContainer) yerine tercih edilmesinin sebebi: mobilde performans ve karmasiklik dengesi.

```csharp
namespace RiceFactory.Core
{
    /// <summary>
    /// Basit Service Locator. Test edilebilirlik icin interface tabanli calisir.
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> _services = new();

        public static void Register<T>(T service) where T : class
        {
            var type = typeof(T);
            if (_services.ContainsKey(type))
            {
                Debug.LogWarning($"[ServiceLocator] {type.Name} zaten kayitli, uzerine yaziliyor.");
            }
            _services[type] = service;
        }

        public static T Get<T>() where T : class
        {
            var type = typeof(T);
            if (_services.TryGetValue(type, out var service))
            {
                return (T)service;
            }
            throw new InvalidOperationException(
                $"[ServiceLocator] {type.Name} kayitli degil. Boot sirasini kontrol edin."
            );
        }

        public static bool TryGet<T>(out T service) where T : class
        {
            var type = typeof(T);
            if (_services.TryGetValue(type, out var obj))
            {
                service = (T)obj;
                return true;
            }
            service = null;
            return false;
        }

        /// <summary>Test ortaminda servisleri temizlemek icin.</summary>
        public static void Reset() => _services.Clear();
    }
}
```

**MonoBehaviour'lardan erisim:**

```csharp
public class FacilityPanel : PanelBase
{
    private IProductionSystem _production;
    private IUpgradeSystem _upgrade;

    protected override void OnInitialize()
    {
        _production = ServiceLocator.Get<IProductionSystem>();
        _upgrade = ServiceLocator.Get<IUpgradeSystem>();
    }
}
```

---

## 3. Core Sistemler

### 3.1 GameManager (Oyun Durumu)

GameManager oyunun genel durumunu yonetir: hangi asamada oldugumuz, pause/resume, uygulama yasam dongusu.

```csharp
namespace RiceFactory.Core
{
    public enum GameState
    {
        Loading,
        MainMenu,
        Playing,
        Paused,
        MiniGame,
        Franchise,  // Prestige gecis ekrani
        Settings
    }

    public class GameManager : IGameManager
    {
        public GameState CurrentState { get; private set; }
        public PlayerSaveData PlayerData => _saveManager.Data;
        public float PlayTimeThisSession { get; private set; }

        private readonly ISaveManager _saveManager;
        private readonly IEventManager _eventManager;
        private readonly ITimeManager _timeManager;

        public GameManager(ISaveManager saveManager, IEventManager eventManager, ITimeManager timeManager)
        {
            _saveManager = saveManager;
            _eventManager = eventManager;
            _timeManager = timeManager;
        }

        public void ChangeState(GameState newState)
        {
            var oldState = CurrentState;
            CurrentState = newState;
            _eventManager.Publish(new GameStateChangedEvent(oldState, newState));
        }

        /// <summary>
        /// Her frame cagirilir (MonoBehaviour proxy uzerinden).
        /// Tum sistemlerin tick'ini yonetir.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (CurrentState != GameState.Playing) return;

            PlayTimeThisSession += deltaTime;
            _timeManager.Tick(deltaTime);

            // Sistemler kendi Update'lerini EventManager uzerinden alir
            _eventManager.Publish(new GameTickEvent(deltaTime));
        }

        /// <summary>Uygulama arka plana alindiginda.</summary>
        public void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                _timeManager.RecordPauseTime();
                _saveManager.SaveAsync();
            }
            else
            {
                var offlineDuration = _timeManager.GetTimeSincePause();
                _eventManager.Publish(new AppResumedEvent(offlineDuration));
            }
        }
    }
}
```

### 3.2 SaveManager (Lokal + Bulut Kayit)

Save sistemi **iki katmanli** calisir: her zaman oncelikli lokal JSON kayit + periyodik Firestore senkronizasyonu.

```csharp
namespace RiceFactory.Data.Save
{
    public class SaveManager : ISaveManager
    {
        public PlayerSaveData Data { get; private set; }

        private readonly LocalRepository _local;
        private readonly FirestoreRepository _cloud;
        private readonly IEventManager _eventManager;

        private float _autoSaveTimer;
        private const float AUTO_SAVE_INTERVAL = 30f; // 30 saniyede bir lokal kayit
        private const float CLOUD_SYNC_INTERVAL = 300f; // 5 dakikada bir bulut sync

        public SaveManager()
        {
            _local = new LocalRepository();
            _cloud = new FirestoreRepository();
            _eventManager = ServiceLocator.Get<IEventManager>();
        }

        /// <summary>Oyun basinda: once lokal, sonra buluttan oku, en yenisini al.</summary>
        public async Task LoadAsync()
        {
            var localData = _local.Load();
            var cloudData = await _cloud.LoadAsync();

            if (cloudData == null)
            {
                Data = localData ?? new PlayerSaveData();
            }
            else if (localData == null)
            {
                Data = cloudData;
            }
            else
            {
                // Timestamp karsilastirmasi: en guncel olan kazanir
                Data = localData.LastSaveTimestamp > cloudData.LastSaveTimestamp
                    ? localData
                    : cloudData;
            }
        }

        /// <summary>Lokal kayit (aninda, senkron).</summary>
        public void SaveLocal()
        {
            Data.LastSaveTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            Data.SaveVersion++;
            _local.Save(Data);
        }

        /// <summary>Bulut kayit (async, arka plan).</summary>
        public async Task SaveCloudAsync()
        {
            SaveLocal(); // Oncelikle lokali guncelle
            await _cloud.SaveAsync(Data);
        }

        /// <summary>Periyodik kayit kontrolu, GameManager.Tick'ten cagirilir.</summary>
        public void Tick(float deltaTime)
        {
            _autoSaveTimer += deltaTime;
            if (_autoSaveTimer >= AUTO_SAVE_INTERVAL)
            {
                _autoSaveTimer = 0f;
                SaveLocal();
            }
        }

        public async Task SaveAsync()
        {
            await SaveCloudAsync();
        }
    }
}
```

**Lokal depolama formati:** Application.persistentDataPath altinda sifreli JSON dosyasi.

```
{platform_data_path}/
  save_v1.json          # Ana kayit
  save_v1.backup.json   # Onceki kayit (yedek)
```

### 3.3 TimeManager (Offline Sure Hesaplama, Anti-Cheat)

TimeManager, offline sureleri hesaplar ve zaman manipulasyonuna karsi koruma saglar.

```csharp
namespace RiceFactory.Core
{
    public class TimeManager : ITimeManager
    {
        private long _lastKnownServerTime;
        private long _lastLocalTime;
        private long _pauseTimestamp;
        private float _comboTimer;
        private float _comboMultiplier = 1f;

        /// <summary>Cihaz zamaninin guvenilir olup olmadigini kontrol eder.</summary>
        public bool IsTimeReliable { get; private set; } = true;

        /// <summary>Aktif oyun suresi (kombo sistemi icin).</summary>
        public float ActivePlayTime { get; private set; }
        public float ComboMultiplier => _comboMultiplier;

        /// <summary>
        /// Boot sirasinda sunucu zamani ile yerel zamani karsilastirir.
        /// Fark > 5 dakika ise "guvenilmez" isaretler.
        /// </summary>
        public async Task SyncServerTimeAsync()
        {
            _lastKnownServerTime = await FetchServerTimestamp();
            _lastLocalTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            long diff = Math.Abs(_lastKnownServerTime - _lastLocalTime);
            IsTimeReliable = diff < 300; // 5 dakikadan az fark

            if (!IsTimeReliable)
            {
                Debug.LogWarning("[TimeManager] Cihaz saati sunucuyla uyumsuz. " +
                    $"Fark: {diff}s. Offline kazanclar sinirlandirilacak.");
            }
        }

        /// <summary>Uygulamadan cikis ani kaydi.</summary>
        public void RecordPauseTime()
        {
            _pauseTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            _comboTimer = 0f;
            _comboMultiplier = 1f;
        }

        /// <summary>Uygulamaya donus sirasinda gecen sureyi hesaplar.</summary>
        public TimeSpan GetTimeSincePause()
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long elapsed = now - _pauseTimestamp;

            // Anti-cheat: negatif zaman veya asiri uzun sureler
            if (elapsed < 0 || elapsed > 86400) // 24 saatten fazla
            {
                elapsed = Math.Clamp(elapsed, 0, 86400);
                IsTimeReliable = false;
            }

            return TimeSpan.FromSeconds(elapsed);
        }

        /// <summary>
        /// Offline kazanc hesabi.
        /// GDD'ye gore: temel %30, arastirma ile %80'e kadar, franchise ile max %180.
        /// Max sure: 8 saat (ucretsiz), 12 saat (Battle Pass).
        /// </summary>
        public OfflineEarningsResult CalculateOfflineEarnings(
            PlayerSaveData data,
            IBalanceConfig config,
            IProductionSystem production)
        {
            var elapsed = GetTimeSincePause();
            float maxHours = data.HasBattlePass
                ? config.GetFloat("battlepass_offline_bonus_hours", 12f)
                : config.GetFloat("economy_offline_max_hours", 8f);

            double cappedSeconds = Math.Min(elapsed.TotalSeconds, maxHours * 3600);

            // Offline verim hesapla
            float baseEfficiency = config.GetFloat("economy_offline_base_efficiency", 0.30f);
            float automationBonus = CalculateAutomationBonus(data);
            float franchiseBonus = data.FranchiseBonuses.OfflineEarningBonus;
            float totalEfficiency = Math.Min(
                baseEfficiency + automationBonus + franchiseBonus, 1.80f
            );

            // Her tesis icin kazanc hesapla
            double totalCoins = 0;
            int totalProducts = 0;
            foreach (var facility in data.Facilities)
            {
                if (!facility.IsUnlocked) continue;
                var rate = production.CalculateProductionRate(facility);
                double facilityEarnings = rate.CoinsPerSecond * cappedSeconds * totalEfficiency;
                totalCoins += facilityEarnings;
                totalProducts += (int)(rate.ProductsPerSecond * cappedSeconds * totalEfficiency);
            }

            // Guvenilmez zaman durumunda %50 ceza
            if (!IsTimeReliable)
            {
                totalCoins *= 0.5;
                totalProducts = (int)(totalProducts * 0.5);
            }

            return new OfflineEarningsResult
            {
                Duration = TimeSpan.FromSeconds(cappedSeconds),
                TotalCoins = totalCoins,
                TotalProducts = totalProducts,
                Efficiency = totalEfficiency,
                IsTimeReliable = IsTimeReliable
            };
        }

        /// <summary>Kombo sistemi -- aktif oyun suresi arttikca carpan artar.</summary>
        public void Tick(float deltaTime)
        {
            ActivePlayTime += deltaTime;
            _comboTimer += deltaTime;

            // GDD: 0-2dk x1.0, 2-5dk x1.2, 5-10dk x1.5, 10-20dk x1.8, 20+dk x2.0
            _comboMultiplier = _comboTimer switch
            {
                < 120f => 1.0f,
                < 300f => 1.2f,
                < 600f => 1.5f,
                < 1200f => 1.8f,
                _ => 2.0f
            };
        }

        private float CalculateAutomationBonus(PlayerSaveData data)
        {
            // Otomasyon arastirma seviyelerine gore ek offline verim
            int automationLevel = data.Research.GetBranchLevel("automation");
            return automationLevel switch
            {
                0 => 0f,
                <= 3 => 0.10f + (automationLevel * 0.033f), // Lv1-3: %40-50
                <= 6 => 0.25f + ((automationLevel - 3) * 0.05f), // Lv4-6: %55-70
                7 => 0.50f, // Tam Otomasyon: %80
                8 => 0.50f, // Singularite: 2x (ayri hesaplanir)
                _ => 0.50f
            };
        }

        private async Task<long> FetchServerTimestamp()
        {
            // Firebase Server Timestamp kullanarak sunucu zamanini al
            // Fallback: lokal zamani kullan
            try
            {
                var snapshot = await FirebaseFirestore.DefaultInstance
                    .Collection("server")
                    .Document("time")
                    .GetSnapshotAsync();
                return snapshot.GetValue<long>("timestamp");
            }
            catch
            {
                return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            }
        }
    }
}
```

### 3.4 EventManager (Event Bus Pattern)

Sistemler arasi iletisim **Event Bus** ile saglanir. Tight coupling onlenir.

```csharp
namespace RiceFactory.Core
{
    public interface IGameEvent { }

    public class EventManager : IEventManager
    {
        private readonly Dictionary<Type, List<Delegate>> _listeners = new();

        public void Subscribe<T>(Action<T> listener) where T : IGameEvent
        {
            var type = typeof(T);
            if (!_listeners.ContainsKey(type))
                _listeners[type] = new List<Delegate>();

            _listeners[type].Add(listener);
        }

        public void Unsubscribe<T>(Action<T> listener) where T : IGameEvent
        {
            var type = typeof(T);
            if (_listeners.ContainsKey(type))
                _listeners[type].Remove(listener);
        }

        public void Publish<T>(T gameEvent) where T : IGameEvent
        {
            var type = typeof(T);
            if (!_listeners.ContainsKey(type)) return;

            // Kopya liste ile iterate et (listener icerisinden subscribe/unsubscribe olabilir)
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

        public void Clear() => _listeners.Clear();
    }
}
```

**Kullanilan event tipleri:**

```csharp
namespace RiceFactory.Core.Events
{
    // Oyun durumu
    public struct GameStateChangedEvent : IGameEvent
    {
        public GameState OldState;
        public GameState NewState;
        public GameStateChangedEvent(GameState old, GameState n) { OldState = old; NewState = n; }
    }

    public struct GameTickEvent : IGameEvent
    {
        public float DeltaTime;
        public GameTickEvent(float dt) { DeltaTime = dt; }
    }

    public struct AppResumedEvent : IGameEvent
    {
        public TimeSpan OfflineDuration;
        public AppResumedEvent(TimeSpan d) { OfflineDuration = d; }
    }

    // Ekonomi
    public struct CurrencyChangedEvent : IGameEvent
    {
        public CurrencyType Type; // Coin, Gem, FP, Reputation
        public double OldAmount;
        public double NewAmount;
        public string Reason;
    }

    // Uretim
    public struct ProductionCompletedEvent : IGameEvent
    {
        public string FacilityId;
        public string ProductId;
        public int Quantity;
        public int Quality; // 1-5
    }

    public struct ProductSoldEvent : IGameEvent
    {
        public string ProductId;
        public int Quantity;
        public double Revenue;
    }

    // Upgrade
    public struct UpgradeCompletedEvent : IGameEvent
    {
        public UpgradeType Type; // Machine, Worker, Star, Research
        public string TargetId;
        public int NewLevel;
    }

    // Prestige
    public struct FranchiseStartedEvent : IGameEvent
    {
        public int FranchiseNumber;
        public int EarnedFP;
        public string NewCityId;
    }

    // Siparis
    public struct OrderCompletedEvent : IGameEvent
    {
        public string OrderId;
        public OrderType Type;
        public double Reward;
    }

    public struct OrderExpiredEvent : IGameEvent
    {
        public string OrderId;
        public int ReputationLoss;
    }

    // Mini-game
    public struct MiniGameCompletedEvent : IGameEvent
    {
        public string MiniGameId;
        public MiniGameGrade Grade; // Bronze, Silver, Gold
        public float BonusMultiplier;
    }

    // Milestone
    public struct MilestoneUnlockedEvent : IGameEvent
    {
        public string MilestoneId;
        public string RewardDescription;
    }
}
```

### 3.5 AudioManager

```csharp
namespace RiceFactory.Audio
{
    public class AudioManager : IAudioManager
    {
        private AudioSource _musicSource;
        private readonly ObjectPool<AudioSource> _sfxPool;
        private float _masterVolume = 1f;
        private float _musicVolume = 0.7f;
        private float _sfxVolume = 1f;

        public void PlayMusic(string clipName, bool loop = true, float fadeIn = 1f)
        {
            var clip = AudioClipDatabase.GetMusic(clipName);
            if (clip == null) return;
            // Crossfade ile gecis
            DOTween.To(() => _musicSource.volume, v => _musicSource.volume = v, 0f, fadeIn * 0.5f)
                .OnComplete(() =>
                {
                    _musicSource.clip = clip;
                    _musicSource.loop = loop;
                    _musicSource.Play();
                    DOTween.To(() => _musicSource.volume, v => _musicSource.volume = v,
                        _musicVolume * _masterVolume, fadeIn * 0.5f);
                });
        }

        public void PlaySFX(string clipName, float pitchVariation = 0.05f)
        {
            var clip = AudioClipDatabase.GetSFX(clipName);
            if (clip == null) return;

            var source = _sfxPool.Get();
            source.clip = clip;
            source.volume = _sfxVolume * _masterVolume;
            source.pitch = 1f + UnityEngine.Random.Range(-pitchVariation, pitchVariation);
            source.Play();

            // Bittikten sonra havuza geri ver
            DOVirtual.DelayedCall(clip.length + 0.1f, () => _sfxPool.Release(source));
        }

        public void SetVolume(AudioChannel channel, float volume)
        {
            volume = Mathf.Clamp01(volume);
            switch (channel)
            {
                case AudioChannel.Master: _masterVolume = volume; break;
                case AudioChannel.Music: _musicVolume = volume; break;
                case AudioChannel.SFX: _sfxVolume = volume; break;
            }
            // Aktif kaynaklari guncelle
            _musicSource.volume = _musicVolume * _masterVolume;
        }
    }
}
```

### 3.6 UIManager (Panel Stack Sistemi)

UIManager, panel'lerin acilip kapanmasini stack (yigin) mantigi ile yonetir. Geri butonu son paneli kapatir.

```csharp
namespace RiceFactory.UI
{
    public class UIManager : IUIManager
    {
        private readonly Stack<PanelBase> _panelStack = new();
        private readonly Dictionary<Type, PanelBase> _panelRegistry = new();
        private Transform _panelContainer;
        private Transform _popupContainer;

        /// <summary>Bir panel tipini ac. Onceki panel gizlenir (stack).</summary>
        public T OpenPanel<T>() where T : PanelBase
        {
            var panel = GetOrCreatePanel<T>();

            // Mevcut ust paneli gizle
            if (_panelStack.Count > 0)
            {
                var current = _panelStack.Peek();
                current.Hide();
            }

            _panelStack.Push(panel);
            panel.Show();
            return panel;
        }

        /// <summary>Ust paneli kapat, altindakini goster.</summary>
        public void CloseTopPanel()
        {
            if (_panelStack.Count <= 1) return; // Ana panel kapanamaz

            var top = _panelStack.Pop();
            top.Hide();

            if (_panelStack.Count > 0)
            {
                _panelStack.Peek().Show();
            }
        }

        /// <summary>Popup goster (stack'i etkilemez, uzerine biner).</summary>
        public T ShowPopup<T>(object data = null) where T : PopupBase
        {
            var popup = GetOrCreatePopup<T>();
            popup.Initialize(data);
            popup.Show();
            return popup;
        }

        /// <summary>Android geri butonu veya ESC icin.</summary>
        public void OnBackPressed()
        {
            // Oncelikle aktif popup var mi?
            var activePopup = _popupContainer.GetComponentInChildren<PopupBase>();
            if (activePopup != null)
            {
                activePopup.Close();
                return;
            }

            CloseTopPanel();
        }

        private T GetOrCreatePanel<T>() where T : PanelBase
        {
            if (_panelRegistry.TryGetValue(typeof(T), out var existing))
                return (T)existing;

            // Prefab'dan olustur (Addressables ile)
            var prefab = AddressablesHelper.LoadSync<T>($"Panel_{typeof(T).Name}");
            var instance = UnityEngine.Object.Instantiate(prefab, _panelContainer);
            _panelRegistry[typeof(T)] = instance;
            instance.Initialize();
            return instance;
        }

        private T GetOrCreatePopup<T>() where T : PopupBase
        {
            var prefab = AddressablesHelper.LoadSync<T>($"Popup_{typeof(T).Name}");
            var instance = UnityEngine.Object.Instantiate(prefab, _popupContainer);
            return instance;
        }
    }
}
```

**PanelBase ve PopupBase:**

```csharp
namespace RiceFactory.UI
{
    public abstract class PanelBase : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;

        private bool _isInitialized;

        public void Initialize()
        {
            if (_isInitialized) return;
            _isInitialized = true;
            OnInitialize();
        }

        public void Show()
        {
            gameObject.SetActive(true);
            _canvasGroup.DOFade(1f, 0.2f);
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
            OnShow();
        }

        public void Hide()
        {
            _canvasGroup.DOFade(0f, 0.15f).OnComplete(() =>
            {
                gameObject.SetActive(false);
            });
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            OnHide();
        }

        protected abstract void OnInitialize();
        protected virtual void OnShow() { }
        protected virtual void OnHide() { }
    }

    public abstract class PopupBase : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _popupBody;

        public void Initialize(object data) => OnInitialize(data);

        public void Show()
        {
            gameObject.SetActive(true);
            _canvasGroup.alpha = 0f;
            _popupBody.localScale = Vector3.one * 0.8f;

            DOTween.Sequence()
                .Append(_canvasGroup.DOFade(1f, 0.2f))
                .Join(_popupBody.DOScale(1f, 0.25f).SetEase(Ease.OutBack));
        }

        public void Close()
        {
            DOTween.Sequence()
                .Append(_canvasGroup.DOFade(0f, 0.15f))
                .Join(_popupBody.DOScale(0.8f, 0.15f))
                .OnComplete(() => Destroy(gameObject));
        }

        protected abstract void OnInitialize(object data);
    }
}
```

---

## 4. Oyun Sistemleri

### 4.1 ProductionSystem (Fabrika, Makine, Uretim Zinciri)

```csharp
namespace RiceFactory.Systems.Production
{
    /// <summary>
    /// Uretim sisteminin kalbi. Her tesisin uretim dongulerini yonetir.
    /// GDD'deki uretim zincirini (Tarla -> Fabrika -> Firin -> Restoran -> Market -> Kuresel) uygular.
    /// </summary>
    public class ProductionSystem : IProductionSystem
    {
        private readonly IBalanceConfig _config;
        private readonly ISaveManager _save;
        private readonly IEconomySystem _economy;
        private readonly IEventManager _events;

        private readonly Dictionary<string, ProductionPipeline> _activePipelines = new();

        public ProductionSystem(IBalanceConfig config, ISaveManager save, IEconomySystem economy)
        {
            _config = config;
            _save = save;
            _economy = economy;
            _events = ServiceLocator.Get<IEventManager>();

            _events.Subscribe<GameTickEvent>(OnTick);
            InitializePipelines();
        }

        private void InitializePipelines()
        {
            foreach (var facility in _save.Data.Facilities)
            {
                if (!facility.IsUnlocked) continue;
                _activePipelines[facility.Id] = new ProductionPipeline(facility, _config);
            }
        }

        private void OnTick(GameTickEvent e)
        {
            foreach (var kvp in _activePipelines)
            {
                var pipeline = kvp.Value;
                pipeline.Tick(e.DeltaTime);

                if (pipeline.HasOutput)
                {
                    var output = pipeline.CollectOutput();
                    ProcessOutput(kvp.Key, output);
                }
            }
        }

        private void ProcessOutput(string facilityId, ProductionOutput output)
        {
            // Envanteri guncelle
            _save.Data.Inventory.Add(output.ProductId, output.Quantity);

            // Otomatik satis acik mi?
            var facility = _save.Data.GetFacility(facilityId);
            if (facility.AutoSellEnabled)
            {
                double revenue = _economy.SellProduct(output.ProductId, output.Quantity, output.Quality);
                _events.Publish(new ProductSoldEvent
                {
                    ProductId = output.ProductId,
                    Quantity = output.Quantity,
                    Revenue = revenue
                });
            }

            _events.Publish(new ProductionCompletedEvent
            {
                FacilityId = facilityId,
                ProductId = output.ProductId,
                Quantity = output.Quantity,
                Quality = output.Quality
            });
        }

        /// <summary>
        /// Uretim hizi hesaplama.
        /// Formul: TemelHiz x MakineCarpani x CalisanBonus x YildizBonus x ArastirmaBonus x KomboBonus
        /// </summary>
        public ProductionRate CalculateProductionRate(FacilityState facility)
        {
            var facilityData = _config.GetFacilityData(facility.FacilityType);
            var activeProduct = facilityData.Products[facility.ActiveProductIndex];

            float baseRate = 1f / activeProduct.BaseProductionTime; // birim/saniye

            // Makine carpani: [1.0, 1.5, 2.2, 3.5, 5.0]
            float machineMultiplier = _config.MachineSpeedMultipliers[facility.MachineLevel - 1];

            // Calisan bonusu: 1 + (level x 0.02)
            float workerBonus = 1f + (facility.WorkerLevel * _config.WorkerEfficiencyPerLevel);

            // Yildiz bonusu: [0, 0.25, 0.50, 1.00, 2.00]
            float starBonus = 1f + _config.FacilityStarBonuses[facility.StarLevel - 1];

            // Arastirma bonuslari
            float researchBonus = CalculateResearchProductionBonus(facility.FacilityType);

            // Kombo
            var timeManager = ServiceLocator.Get<ITimeManager>();
            float comboMultiplier = timeManager.ComboMultiplier;

            float totalRate = baseRate * machineMultiplier * workerBonus *
                              starBonus * researchBonus * comboMultiplier;

            return new ProductionRate
            {
                ProductsPerSecond = totalRate,
                CoinsPerSecond = totalRate * CalculateSellPrice(activeProduct, facility)
            };
        }

        /// <summary>Satis fiyati hesapla: TemelFiyat x KaliteCarpani x TalepCarpani x ItibarBonusu</summary>
        private double CalculateSellPrice(ProductData product, FacilityState facility)
        {
            float basePrice = product.BaseSellPrice;
            float qualityMultiplier = _config.QualityPriceMultipliers[facility.AverageQuality - 1];
            float demandMultiplier = _economy.GetDemandMultiplier(product.Id);
            float reputationBonus = 1f + (_save.Data.Reputation / 10000f);

            return basePrice * qualityMultiplier * demandMultiplier * reputationBonus;
        }

        private float CalculateResearchProductionBonus(string facilityType)
        {
            // Hiz dali + kapasite dali + otomasyon dali bonuslari topla
            float bonus = 1f;
            var research = _save.Data.Research;

            // Genel hiz bonusu (Kuantum Uretim, Zaman Bukucu)
            int speedLevel = research.GetBranchLevel("speed");
            bonus += speedLevel * 0.10f; // Basitlestirilmis

            return bonus;
        }
    }
}
```

**ProductionPipeline (tek bir tesisin uretim hatti):**

```csharp
namespace RiceFactory.Systems.Production
{
    public class ProductionPipeline
    {
        private readonly FacilityState _facility;
        private readonly IBalanceConfig _config;

        private float _productionTimer;
        private float _currentCycleDuration;
        private readonly Queue<ProductionOutput> _outputQueue = new();

        public bool HasOutput => _outputQueue.Count > 0;

        public ProductionPipeline(FacilityState facility, IBalanceConfig config)
        {
            _facility = facility;
            _config = config;
            RecalculateCycleDuration();
        }

        public void Tick(float deltaTime)
        {
            // Girdi kontrolu: yeterli malzeme var mi?
            if (!HasRequiredInputs()) return;

            _productionTimer += deltaTime;

            while (_productionTimer >= _currentCycleDuration)
            {
                _productionTimer -= _currentCycleDuration;
                ProduceItem();
            }
        }

        private void ProduceItem()
        {
            int quality = QualityCalculator.Calculate(
                _facility.MachineLevel,
                _facility.WorkerQualityLevel,
                _config
            );

            _outputQueue.Enqueue(new ProductionOutput
            {
                ProductId = _facility.ActiveProductId,
                Quantity = _facility.BaseOutputAmount,
                Quality = quality
            });
        }

        public ProductionOutput CollectOutput() => _outputQueue.Dequeue();

        private bool HasRequiredInputs()
        {
            // Tarla icin girdi yok (otomatik buyume)
            if (_facility.FacilityType == "field") return true;

            // Diger tesisler icin girdi gereksinimi kontrolu
            var product = _config.GetProductData(_facility.ActiveProductId);
            foreach (var input in product.InputRequirements)
            {
                var inventory = ServiceLocator.Get<ISaveManager>().Data.Inventory;
                if (inventory.GetAmount(input.ProductId) < input.Quantity)
                    return false;
            }
            return true;
        }

        private void RecalculateCycleDuration()
        {
            var product = _config.GetProductData(_facility.ActiveProductId);
            float baseTime = product.BaseProductionTime;
            float machineMultiplier = _config.MachineSpeedMultipliers[_facility.MachineLevel - 1];
            float workerBonus = 1f + (_facility.WorkerSpeedLevel * _config.WorkerEfficiencyPerLevel);
            _currentCycleDuration = baseTime / (machineMultiplier * workerBonus);
        }
    }
}
```

**QualityCalculator:**

```csharp
namespace RiceFactory.Systems.Production
{
    /// <summary>
    /// Urun kalitesi (1-5 yildiz) hesaplama.
    /// Kalite: MakineSeviyesi + CalisanBecerisi + GirdiKalitesi + MiniGameBonus
    /// </summary>
    public static class QualityCalculator
    {
        public static int Calculate(int machineLevel, int workerQualityLevel, IBalanceConfig config)
        {
            int minQuality = config.MachineQualityFloors[machineLevel - 1];
            int maxQuality = config.MachineQualityCeilings[machineLevel - 1];

            // Agirlikli olasilik dagilimi
            float[] weights = config.QualityDropWeights;

            // Calisan kalite becerisi ile agirliklari kaydir
            float qualityShift = workerQualityLevel * 0.01f;
            float roll = UnityEngine.Random.value - qualityShift;

            float cumulative = 0f;
            for (int i = 0; i < weights.Length; i++)
            {
                cumulative += weights[i];
                if (roll <= cumulative)
                {
                    return Mathf.Clamp(i + 1, minQuality, maxQuality);
                }
            }

            return maxQuality;
        }
    }
}
```

### 4.2 EconomySystem (Para, Elmas, Fiyat Hesaplama)

```csharp
namespace RiceFactory.Systems.Economy
{
    public class EconomySystem : IEconomySystem
    {
        private readonly IBalanceConfig _config;
        private readonly ISaveManager _save;
        private readonly IEventManager _events;
        private readonly DemandSystem _demand;

        public double Coins => _save.Data.Coins;
        public int Gems => _save.Data.Gems;
        public int FranchisePoints => _save.Data.FranchisePoints;
        public int Reputation => _save.Data.Reputation;

        public EconomySystem(IBalanceConfig config, ISaveManager save)
        {
            _config = config;
            _save = save;
            _events = ServiceLocator.Get<IEventManager>();
            _demand = new DemandSystem(config);
        }

        /// <summary>
        /// Para harca. Yetmezse false doner.
        /// </summary>
        public bool SpendCoins(double amount, string reason)
        {
            if (_save.Data.Coins < amount) return false;

            double old = _save.Data.Coins;
            _save.Data.Coins -= amount;

            _events.Publish(new CurrencyChangedEvent
            {
                Type = CurrencyType.Coin,
                OldAmount = old,
                NewAmount = _save.Data.Coins,
                Reason = reason
            });

            return true;
        }

        /// <summary>Para kazan.</summary>
        public void EarnCoins(double amount, string reason)
        {
            double old = _save.Data.Coins;
            _save.Data.Coins += amount;
            _save.Data.TotalEarnings += amount;

            _events.Publish(new CurrencyChangedEvent
            {
                Type = CurrencyType.Coin,
                OldAmount = old,
                NewAmount = _save.Data.Coins,
                Reason = reason
            });
        }

        /// <summary>
        /// Urun sat. SatisFiyati = TemelFiyat x KaliteCarpani x TalepCarpani x ItibarBonusu
        /// </summary>
        public double SellProduct(string productId, int quantity, int quality)
        {
            var product = _config.GetProductData(productId);
            float basePrice = product.BaseSellPrice;
            float qualityMultiplier = _config.QualityPriceMultipliers[quality - 1];
            float demandMultiplier = _demand.GetMultiplier(productId);
            float reputationBonus = 1f + (_save.Data.Reputation / 10000f);
            float globalMultiplier = _config.GetFloat("globalSellPriceMultiplier", 1f);

            double totalRevenue = basePrice * qualityMultiplier * demandMultiplier *
                                  reputationBonus * globalMultiplier * quantity;

            EarnCoins(totalRevenue, $"Satis:{productId}x{quantity}");
            return totalRevenue;
        }

        /// <summary>
        /// Upgrade maliyet hesaplama.
        /// Makine: BaseCost x 5^(level-1)
        /// Calisan: 50 x level^2.2
        /// Arastirma: BaseCost x 3^(level-1)
        /// Yildiz: TesisAcmaMaliyeti x 3^(yildiz-1)
        /// </summary>
        public double CalculateUpgradeCost(UpgradeType type, string targetId, int currentLevel)
        {
            double globalMultiplier = _config.GetFloat("globalUpgradeCostMultiplier", 1f);

            return type switch
            {
                UpgradeType.Machine => CalculateMachineCost(targetId, currentLevel) * globalMultiplier,
                UpgradeType.Worker => CalculateWorkerCost(currentLevel) * globalMultiplier,
                UpgradeType.Research => CalculateResearchCost(targetId, currentLevel) * globalMultiplier,
                UpgradeType.Star => CalculateStarCost(targetId, currentLevel) * globalMultiplier,
                _ => 0
            };
        }

        private double CalculateMachineCost(string facilityType, int currentLevel)
        {
            // BaseCost x 5^(level-1)
            float baseCost = _config.GetFacilityData(facilityType).MachineBaseCost;
            float exponent = _config.GetFloat("machineCostExponent", 5f);
            return baseCost * Math.Pow(exponent, currentLevel - 1);
        }

        private double CalculateWorkerCost(int currentLevel)
        {
            // 50 x level^2.2
            float baseCost = _config.GetFloat("workerCostBase", 50f);
            float exponent = _config.GetFloat("workerCostExponent", 2.2f);
            return baseCost * Math.Pow(currentLevel, exponent);
        }

        private double CalculateResearchCost(string branchId, int currentLevel)
        {
            // BaseCost x 3^(level-1)
            float baseCost = _config.GetResearchData(branchId).BaseCost;
            float exponent = _config.GetFloat("researchCostExponent", 3f);
            return baseCost * Math.Pow(exponent, currentLevel - 1);
        }

        private double CalculateStarCost(string facilityType, int currentStar)
        {
            // TesisAcmaMaliyeti x 3^(yildiz-1)
            float unlockCost = _config.GetFacilityData(facilityType).UnlockCost;
            float exponent = _config.GetFloat("facilityStarCostExponent", 3f);
            return unlockCost * Math.Pow(exponent, currentStar - 1);
        }

        public float GetDemandMultiplier(string productId) => _demand.GetMultiplier(productId);
    }
}
```

**DemandSystem (dinamik pazar talebi):**

```csharp
namespace RiceFactory.Systems.Economy
{
    /// <summary>
    /// Pazar talebi 0.8 - 1.5 arasi dalgalanir.
    /// Sinusoidal dongu + rastgele gurultu.
    /// </summary>
    public class DemandSystem
    {
        private readonly IBalanceConfig _config;
        private readonly Dictionary<string, float> _demandPhases = new();

        public DemandSystem(IBalanceConfig config)
        {
            _config = config;
            // Her urun icin rastgele faz
            foreach (var product in config.AllProducts)
            {
                _demandPhases[product.Id] = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            }
        }

        public float GetMultiplier(string productId)
        {
            float fluctuation = _config.GetFloat("demandFluctuation", 0.3f);
            float cycleDuration = _config.GetFloat("demandCycleDurationMinutes", 60f) * 60f;
            float phase = _demandPhases.GetValueOrDefault(productId, 0f);

            float time = Time.time;
            float sinValue = Mathf.Sin((time / cycleDuration) * Mathf.PI * 2f + phase);

            // 0.8 ile 1.5 arasi
            float baseMultiplier = 1.15f; // ortanca
            return baseMultiplier + sinValue * fluctuation;
        }
    }
}
```

### 4.3 UpgradeSystem (Yildiz, Arastirma Agaci)

```csharp
namespace RiceFactory.Systems.Upgrade
{
    public class UpgradeSystem : IUpgradeSystem
    {
        private readonly IBalanceConfig _config;
        private readonly ISaveManager _save;
        private readonly IEconomySystem _economy;
        private readonly IEventManager _events;

        public UpgradeSystem(IBalanceConfig config, ISaveManager save, IEconomySystem economy)
        {
            _config = config;
            _save = save;
            _economy = economy;
            _events = ServiceLocator.Get<IEventManager>();
        }

        /// <summary>Makine seviye atlama. Max seviye: 5.</summary>
        public bool TryUpgradeMachine(string facilityId)
        {
            var facility = _save.Data.GetFacility(facilityId);
            if (facility.MachineLevel >= _config.MaxMachineLevel) return false;

            double cost = _economy.CalculateUpgradeCost(
                UpgradeType.Machine, facility.FacilityType, facility.MachineLevel + 1
            );

            if (!_economy.SpendCoins(cost, $"MachineUpgrade:{facilityId}")) return false;

            facility.MachineLevel++;

            _events.Publish(new UpgradeCompletedEvent
            {
                Type = UpgradeType.Machine,
                TargetId = facilityId,
                NewLevel = facility.MachineLevel
            });

            // Analytics
            ServiceLocator.Get<IAnalytics>()?.LogEvent("machine_upgrade", new Dictionary<string, object>
            {
                { "facility", facility.FacilityType },
                { "new_level", facility.MachineLevel },
                { "cost", cost }
            });

            return true;
        }

        /// <summary>
        /// Tesis yildiz atlama.
        /// Kosullar: Tum makineler belirli seviyede + urun satisi sayisi + arastirma sayisi
        /// </summary>
        public bool TryUpgradeStar(string facilityId)
        {
            var facility = _save.Data.GetFacility(facilityId);
            if (facility.StarLevel >= 5) return false;

            // Kosullari kontrol et
            int requiredMachineLevel = facility.StarLevel + 1; // Yildiz 2 icin Makine Lv.2
            if (facility.MachineLevel < requiredMachineLevel) return false;

            int requiredSales = GetRequiredSalesForStar(facility.StarLevel + 1);
            if (facility.TotalProductsSold < requiredSales) return false;

            // Maliyet
            double cost = _economy.CalculateUpgradeCost(
                UpgradeType.Star, facility.FacilityType, facility.StarLevel + 1
            );
            if (!_economy.SpendCoins(cost, $"StarUpgrade:{facilityId}")) return false;

            facility.StarLevel++;

            _events.Publish(new UpgradeCompletedEvent
            {
                Type = UpgradeType.Star,
                TargetId = facilityId,
                NewLevel = facility.StarLevel
            });

            // Yeni urun tarifleri acildi mi kontrol et
            CheckNewRecipeUnlocks(facility);

            return true;
        }

        /// <summary>GDD: 500, 5000, 50000, 500000 urun satisi</summary>
        private int GetRequiredSalesForStar(int star)
        {
            return star switch
            {
                2 => 500,
                3 => 5_000,
                4 => 50_000,
                5 => 500_000,
                _ => int.MaxValue
            };
        }

        private void CheckNewRecipeUnlocks(FacilityState facility)
        {
            // Her yildiz seviyesinde yeni urun tarifleri acilir
            var recipes = _config.GetRecipesForStarLevel(facility.FacilityType, facility.StarLevel);
            foreach (var recipe in recipes)
            {
                if (!facility.UnlockedRecipes.Contains(recipe.Id))
                {
                    facility.UnlockedRecipes.Add(recipe.Id);
                    _events.Publish(new MilestoneUnlockedEvent
                    {
                        MilestoneId = $"recipe_{recipe.Id}",
                        RewardDescription = $"Yeni tarif: {recipe.DisplayName}"
                    });
                }
            }
        }
    }
}
```

### 4.4 PrestigeSystem (Franchise)

```csharp
namespace RiceFactory.Systems.Prestige
{
    /// <summary>
    /// Franchise (prestige) sistemi.
    /// Formul: FP = floor( sqrt(ToplamKazanc / 1,000,000) x (1 + BonusCarpan) )
    /// BonusCarpan = (5-yildiz tesis sayisi) x 0.1
    /// </summary>
    public class PrestigeSystem : IPrestigeSystem
    {
        private readonly IBalanceConfig _config;
        private readonly ISaveManager _save;
        private readonly IEventManager _events;

        public PrestigeSystem(IBalanceConfig config, ISaveManager save)
        {
            _config = config;
            _save = save;
            _events = ServiceLocator.Get<IEventManager>();
        }

        /// <summary>Franchise yapilabilir mi?</summary>
        public bool CanPrestige()
        {
            double threshold = _config.GetFloat("economy_franchise_threshold", 1_000_000f);
            return _save.Data.TotalEarnings >= threshold;
        }

        /// <summary>Kazanilacak FP hesapla (onizleme).</summary>
        public int CalculateFP()
        {
            double totalEarnings = _save.Data.TotalEarnings;
            double divisor = _config.GetFloat("economy_fp_formula_divisor", 1_000_000f);

            int fiveStarCount = _save.Data.Facilities.Count(f => f.StarLevel >= 5);
            float bonusMultiplier = fiveStarCount * 0.1f;

            return (int)Math.Floor(Math.Sqrt(totalEarnings / divisor) * (1 + bonusMultiplier));
        }

        /// <summary>Franchise islemini gerceklestir.</summary>
        public FranchiseResult ExecuteFranchise(string selectedCityId)
        {
            if (!CanPrestige()) return null;

            int earnedFP = CalculateFP();
            int franchiseNumber = _save.Data.FranchiseCount + 1;

            // Kalici verileri kaydet
            var persistentData = new PersistentFranchiseData
            {
                FranchisePoints = _save.Data.FranchisePoints + earnedFP,
                FranchiseCount = franchiseNumber,
                FranchiseBonuses = _save.Data.FranchiseBonuses, // Harcanan FP bonuslari
                TotalLifetimeEarnings = _save.Data.TotalLifetimeEarnings + _save.Data.TotalEarnings,
                UnlockedCities = _save.Data.UnlockedCities,
                Achievements = _save.Data.Achievements,
                CosmeticInventory = _save.Data.CosmeticInventory
            };

            // Sifirlanan veriler
            _save.Data.ResetForFranchise(persistentData);

            // Baslangic parasi bonusu uygula
            double startingCoins = CalculateStartingCoins(persistentData.FranchiseBonuses);
            _save.Data.Coins = startingCoins;

            // Sehir temasi ayarla
            _save.Data.CurrentCityId = selectedCityId;

            _events.Publish(new FranchiseStartedEvent
            {
                FranchiseNumber = franchiseNumber,
                EarnedFP = earnedFP,
                NewCityId = selectedCityId
            });

            return new FranchiseResult
            {
                FranchiseNumber = franchiseNumber,
                EarnedFP = earnedFP,
                TotalFP = persistentData.FranchisePoints,
                StartingCoins = startingCoins,
                CityId = selectedCityId
            };
        }

        /// <summary>FP harcayarak kalici bonus al.</summary>
        public bool PurchaseFranchiseBonus(FranchiseBonusType type)
        {
            var bonusInfo = GetBonusInfo(type);
            int currentLevel = _save.Data.FranchiseBonuses.GetLevel(type);

            if (currentLevel >= bonusInfo.MaxLevel) return false;
            if (_save.Data.FranchisePoints < bonusInfo.FPCost) return false;

            _save.Data.FranchisePoints -= bonusInfo.FPCost;
            _save.Data.FranchiseBonuses.SetLevel(type, currentLevel + 1);

            return true;
        }

        private double CalculateStartingCoins(FranchiseBonuses bonuses)
        {
            // Her seviye +%50
            int level = bonuses.GetLevel(FranchiseBonusType.StartingCoins);
            return 0 + (level > 0 ? 500 * Math.Pow(1.5, level) : 0);
        }

        private FranchiseBonusInfo GetBonusInfo(FranchiseBonusType type)
        {
            return type switch
            {
                FranchiseBonusType.ProductionSpeed => new(5, 20, "+%10 Uretim Hizi"),
                FranchiseBonusType.StartingCoins => new(3, 10, "+%50 Baslangic Parasi"),
                FranchiseBonusType.OfflineEarnings => new(4, 20, "+%5 Offline Kazanc"),
                FranchiseBonusType.FacilityCostReduction => new(6, 8, "-%10 Tesis Maliyeti"),
                FranchiseBonusType.CriticalProduction => new(8, 10, "+%2 Kritik Uretim"),
                FranchiseBonusType.SpecialWorker => new(15, 1, "Efsanevi Calisanlar"),
                _ => new(10, 1, "Bilinmeyen")
            };
        }
    }
}
```

### 4.5 QuestSystem (Siparisler, Gunluk Gorevler)

```csharp
namespace RiceFactory.Systems.Quest
{
    /// <summary>
    /// Siparis sistemi. 4 tur siparis: Normal, Acil, VIP, Toplu, Efsanevi.
    /// Siparis tahtasinda 3 siparis gorunur, 15 dk'da bir yenilenir.
    /// </summary>
    public class QuestSystem : IQuestSystem
    {
        private readonly IBalanceConfig _config;
        private readonly ISaveManager _save;
        private readonly IEconomySystem _economy;
        private readonly IEventManager _events;

        private List<ActiveOrder> _activeOrders = new();
        private float _refreshTimer;

        public IReadOnlyList<ActiveOrder> ActiveOrders => _activeOrders;

        public QuestSystem(IBalanceConfig config, ISaveManager save, IEconomySystem economy)
        {
            _config = config;
            _save = save;
            _economy = economy;
            _events = ServiceLocator.Get<IEventManager>();

            _events.Subscribe<GameTickEvent>(OnTick);
            LoadOrders();
        }

        private void OnTick(GameTickEvent e)
        {
            // Siparis surelerini guncelle
            for (int i = _activeOrders.Count - 1; i >= 0; i--)
            {
                _activeOrders[i].RemainingTime -= e.DeltaTime;
                if (_activeOrders[i].RemainingTime <= 0)
                {
                    ExpireOrder(_activeOrders[i]);
                    _activeOrders.RemoveAt(i);
                }
            }

            // Yenileme
            _refreshTimer += e.DeltaTime;
            float refreshMinutes = _config.GetFloat("economy_order_refresh_minutes", 15f);
            if (_refreshTimer >= refreshMinutes * 60f && _activeOrders.Count < 3)
            {
                _refreshTimer = 0f;
                GenerateNewOrder();
            }
        }

        /// <summary>Siparisi kabul et ve teslim etmeyi dene.</summary>
        public OrderResult TryCompleteOrder(string orderId)
        {
            var order = _activeOrders.Find(o => o.Id == orderId);
            if (order == null) return OrderResult.NotFound;

            // Envanterde yeterli urun var mi?
            foreach (var req in order.Requirements)
            {
                if (_save.Data.Inventory.GetAmount(req.ProductId) < req.Quantity)
                    return OrderResult.InsufficientItems;

                // Kalite kosulu
                if (req.MinQuality > 0 && _save.Data.Inventory.GetQuality(req.ProductId) < req.MinQuality)
                    return OrderResult.InsufficientQuality;
            }

            // Urunleri dusuyor ve odulleri ver
            foreach (var req in order.Requirements)
            {
                _save.Data.Inventory.Remove(req.ProductId, req.Quantity);
            }

            double reward = order.BaseReward * order.RewardMultiplier;
            _economy.EarnCoins(reward, $"Order:{orderId}");

            // Itibar kazanc
            _save.Data.Reputation += order.ReputationReward;

            _activeOrders.Remove(order);

            _events.Publish(new OrderCompletedEvent
            {
                OrderId = orderId,
                Type = order.Type,
                Reward = reward
            });

            return OrderResult.Completed;
        }

        private void ExpireOrder(ActiveOrder order)
        {
            _save.Data.Reputation = Math.Max(0, _save.Data.Reputation - 10);
            _events.Publish(new OrderExpiredEvent
            {
                OrderId = order.Id,
                ReputationLoss = 10
            });
        }

        private void GenerateNewOrder()
        {
            // Oyuncu seviyesine gore siparis turu sec
            var orderType = DetermineOrderType();
            var order = OrderGenerator.Generate(orderType, _save.Data, _config);
            _activeOrders.Add(order);
        }

        private OrderType DetermineOrderType()
        {
            var data = _save.Data;
            var available = new List<OrderType> { OrderType.Normal };

            if (data.HasFacility("factory")) available.Add(OrderType.Urgent);
            if (data.HasFacility("restaurant")) available.Add(OrderType.VIP);
            if (data.HasFacility("market")) available.Add(OrderType.Bulk);
            if (data.HasFacility("global")) available.Add(OrderType.Legendary);

            return available[UnityEngine.Random.Range(0, available.Count)];
        }

        private void LoadOrders()
        {
            _activeOrders = _save.Data.ActiveOrders?.ToList() ?? new List<ActiveOrder>();
        }
    }
}
```

### 4.6 SocialSystem (Liderboard, Arkadas)

```csharp
namespace RiceFactory.Systems.Social
{
    public class SocialSystem : ISocialSystem
    {
        private readonly FirestoreManager _firestore;

        public SocialSystem()
        {
            _firestore = ServiceLocator.Get<FirestoreManager>();
        }

        /// <summary>Haftalik liderboard'u getir.</summary>
        public async Task<List<LeaderboardEntry>> GetWeeklyLeaderboardAsync(
            LeaderboardCategory category, int limit = 50)
        {
            string collection = $"leaderboards/weekly_{GetCurrentWeekId()}/{category}";
            var snapshot = await _firestore.Collection(collection)
                .OrderByDescending("score")
                .Limit(limit)
                .GetSnapshotAsync();

            return snapshot.Documents.Select(doc => new LeaderboardEntry
            {
                PlayerId = doc.Id,
                PlayerName = doc.GetValue<string>("name"),
                Score = doc.GetValue<long>("score"),
                Rank = doc.GetValue<int>("rank"),
                AvatarUrl = doc.GetValue<string>("avatar")
            }).ToList();
        }

        /// <summary>Arkadas fabrikasini ziyaret et (read-only veri).</summary>
        public async Task<FriendFactoryData> VisitFriendFactoryAsync(string friendId)
        {
            var doc = await _firestore.Collection("players")
                .Document(friendId)
                .Collection("public_data")
                .Document("factory")
                .GetSnapshotAsync();

            if (!doc.Exists) return null;

            return doc.ConvertTo<FriendFactoryData>();
        }

        /// <summary>Arkadasa yardim ver (+%10 uretim boost, 1 saat).</summary>
        public async Task<bool> HelpFriendAsync(string friendId)
        {
            var save = ServiceLocator.Get<ISaveManager>();
            if (save.Data.DailyVisitCount >= 5) return false;

            await _firestore.Collection("players")
                .Document(friendId)
                .Collection("boosts")
                .AddAsync(new
                {
                    type = "friend_help",
                    multiplier = 1.10f,
                    durationSeconds = 3600,
                    fromPlayer = save.Data.PlayerId,
                    createdAt = FieldValue.ServerTimestamp
                });

            save.Data.DailyVisitCount++;

            // Ziyaretci odulu
            var economy = ServiceLocator.Get<IEconomySystem>();
            int level = save.Data.PlayerLevel;
            double reward = 100 + level * 10;
            economy.EarnCoins(reward, "FriendVisit");

            return true;
        }

        private string GetCurrentWeekId()
        {
            var now = DateTime.UtcNow;
            var cal = System.Globalization.CultureInfo.InvariantCulture.Calendar;
            int week = cal.GetWeekOfYear(now, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Monday);
            return $"{now.Year}_W{week:D2}";
        }
    }
}
```

---

## 5. Veri Yapilari

### 5.1 ScriptableObject Tanimlari

```csharp
namespace RiceFactory.Data
{
    /// <summary>Tesis tanimlamasi (Tarla, Fabrika, Firin, Restoran, Market, Kuresel Dagitim)</summary>
    [CreateAssetMenu(fileName = "New Facility", menuName = "RiceFactory/Data/Facility")]
    public class FacilityData : ScriptableObject
    {
        [Header("Temel Bilgi")]
        public string Id;
        public string DisplayName;
        public string Description;
        public Sprite Icon;
        public Sprite[] StarVisuals; // 5 yildiz icin 5 farkli gorsel

        [Header("Acilma Kosullari")]
        public int UnlockOrder; // 1-6 arasi sira
        public float UnlockCost;
        public string RequiredFacilityId; // Onceki tesis
        public int RequiredStarLevel; // Onceki tesisin yildiz seviyesi
        public double RequiredTotalEarnings;

        [Header("Makine")]
        public float MachineBaseCost;
        public int MaxMachineLevel; // Varsayilan: 5

        [Header("Urunler")]
        public ProductData[] Products; // Bu tesiste uretilebilecek urunler
    }

    /// <summary>Urun tanimlamasi</summary>
    [CreateAssetMenu(fileName = "New Product", menuName = "RiceFactory/Data/Product")]
    public class ProductData : ScriptableObject
    {
        [Header("Temel Bilgi")]
        public string Id;
        public string DisplayName;
        public Sprite Icon;
        public Sprite[] QualityVisuals; // 5 kalite seviyesi

        [Header("Uretim")]
        public float BaseProductionTime; // saniye
        public int BaseOutputAmount;
        public InputRequirement[] InputRequirements;

        [Header("Satis")]
        public float BaseSellPrice;
        public int MinQuality; // Minimum uretim kalitesi
        public int MaxQuality; // Maksimum uretim kalitesi

        [Header("Acilma")]
        public int RequiredStarLevel; // Bu urun kacinci yildizda acilir
    }

    [System.Serializable]
    public class InputRequirement
    {
        public string ProductId;
        public int Quantity;
    }

    /// <summary>Upgrade tanimlamasi</summary>
    [CreateAssetMenu(fileName = "New Upgrade", menuName = "RiceFactory/Data/Upgrade")]
    public class UpgradeData : ScriptableObject
    {
        public string Id;
        public string DisplayName;
        public string Description;
        public Sprite Icon;
        public UpgradeType Type;
        public int MaxLevel;
        public float BaseCost;
        public float CostExponent;
        public float BonusPerLevel;
        public string AffectedStat; // "production_speed", "quality", vb.
    }

    /// <summary>Arastirma tanimlamasi</summary>
    [CreateAssetMenu(fileName = "New Research", menuName = "RiceFactory/Data/Research")]
    public class ResearchData : ScriptableObject
    {
        public string Id;
        public string BranchId; // "automation", "quality", "speed", "capacity"
        public string DisplayName;
        public string Description;
        public Sprite Icon;
        public int Level; // Bu arastirmanin seviyesi (1-8)
        public float BaseCost;
        public float BaseTimeSeconds;
        public string Effect; // Etki aciklamasi
        public float EffectValue;
    }

    /// <summary>Sehir tanimlamasi (franchise temalari)</summary>
    [CreateAssetMenu(fileName = "New City", menuName = "RiceFactory/Data/City")]
    public class CityData : ScriptableObject
    {
        public string Id;
        public string DisplayName;
        public string Description;
        public Sprite Background;
        public Sprite[] FacilityOverrides; // Sehre ozel tesis gorselleri
        public AudioClip ThemeMusic;
        public int RequiredFranchiseNumber; // Kacinci franchise'da acilir
        public int FPCost; // Satin alma maliyeti (tek seferlik)
    }

    /// <summary>Siparis tanimlamasi</summary>
    [CreateAssetMenu(fileName = "New Order", menuName = "RiceFactory/Data/Order")]
    public class OrderData : ScriptableObject
    {
        public string Id;
        public OrderType Type;
        public string CustomerName;
        public Sprite CustomerAvatar;
        public float BaseDurationMinutes;
        public float BaseReward;
        public float RewardMultiplier;
        public int ReputationReward;
        public InputRequirement[] Requirements;
    }
}
```

### 5.2 Oyuncu Save Data Semasi (JSON)

```csharp
namespace RiceFactory.Data.Save
{
    [System.Serializable]
    public class PlayerSaveData
    {
        // Meta
        public string PlayerId;
        public string PlayerName;
        public int SaveVersion;
        public long LastSaveTimestamp; // Unix timestamp
        public long FirstPlayTimestamp;
        public string GameVersion;

        // Ekonomi
        public double Coins;
        public int Gems;
        public int FranchisePoints;
        public int Reputation;
        public double TotalEarnings; // Bu franchise donemi
        public double TotalLifetimeEarnings; // Tum zamanlarin toplami

        // Ilerleme
        public int PlayerLevel;
        public int FranchiseCount;
        public string CurrentCityId;
        public bool HasBattlePass;
        public int BattlePassTier;

        // Tesisler
        public List<FacilityState> Facilities;

        // Envanter
        public InventoryState Inventory;

        // Arastirma
        public ResearchState Research;

        // Franchise Bonuslari
        public FranchiseBonuses FranchiseBonuses;

        // Siparisler
        public List<ActiveOrder> ActiveOrders;

        // Basarimlar
        public List<string> Achievements;
        public List<string> CompletedMilestones;

        // Sosyal
        public List<string> FriendIds;
        public int DailyVisitCount;
        public long LastDailyResetTimestamp;

        // Kozmetik
        public List<string> CosmeticInventory;
        public Dictionary<string, string> EquippedCosmetics;

        // Ayarlar
        public float MasterVolume;
        public float MusicVolume;
        public float SFXVolume;
        public string Language;
        public bool NotificationsEnabled;

        // Anti-cheat
        public string DataHash; // Veri butunlugu kontrolu
    }

    [System.Serializable]
    public class FacilityState
    {
        public string Id; // Benzersiz instance ID
        public string FacilityType; // "field", "factory", "bakery", "restaurant", "market", "global"
        public bool IsUnlocked;
        public int StarLevel; // 1-5
        public int MachineLevel; // 1-5
        public int WorkerLevel; // 1-50
        public int WorkerSpeedLevel;
        public int WorkerQualityLevel;
        public int WorkerCapacityLevel;
        public int WorkerAutomationLevel;
        public int ActiveProductIndex;
        public string ActiveProductId;
        public bool AutoSellEnabled;
        public int TotalProductsSold;
        public int AverageQuality;
        public List<string> UnlockedRecipes;
    }

    [System.Serializable]
    public class InventoryState
    {
        public Dictionary<string, int> Items; // productId -> miktar
        public Dictionary<string, int> QualityMap; // productId -> ortalama kalite

        public int GetAmount(string productId) =>
            Items.TryGetValue(productId, out int val) ? val : 0;

        public int GetQuality(string productId) =>
            QualityMap.TryGetValue(productId, out int val) ? val : 1;

        public void Add(string productId, int quantity)
        {
            if (!Items.ContainsKey(productId)) Items[productId] = 0;
            Items[productId] += quantity;
        }

        public bool Remove(string productId, int quantity)
        {
            if (GetAmount(productId) < quantity) return false;
            Items[productId] -= quantity;
            return true;
        }
    }

    [System.Serializable]
    public class ResearchState
    {
        public Dictionary<string, int> BranchLevels; // branchId -> seviye
        public string ActiveResearchId;
        public float ActiveResearchProgress; // 0-1 arasi
        public long ResearchStartTimestamp;

        public int GetBranchLevel(string branchId) =>
            BranchLevels.TryGetValue(branchId, out int val) ? val : 0;
    }

    [System.Serializable]
    public class FranchiseBonuses
    {
        public int ProductionSpeedLevel;
        public int StartingCoinsLevel;
        public int OfflineEarningsLevel;
        public int FacilityCostReductionLevel;
        public int CriticalProductionLevel;
        public bool SpecialWorkerUnlocked;

        public float ProductionSpeedBonus => ProductionSpeedLevel * 0.10f;
        public float OfflineEarningBonus => OfflineEarningsLevel * 0.05f;
        public float FacilityCostReduction => FacilityCostReductionLevel * 0.10f;
        public float CriticalProductionChance => CriticalProductionLevel * 0.02f;

        public int GetLevel(FranchiseBonusType type) => type switch
        {
            FranchiseBonusType.ProductionSpeed => ProductionSpeedLevel,
            FranchiseBonusType.StartingCoins => StartingCoinsLevel,
            FranchiseBonusType.OfflineEarnings => OfflineEarningsLevel,
            FranchiseBonusType.FacilityCostReduction => FacilityCostReductionLevel,
            FranchiseBonusType.CriticalProduction => CriticalProductionLevel,
            _ => 0
        };

        public void SetLevel(FranchiseBonusType type, int level)
        {
            switch (type)
            {
                case FranchiseBonusType.ProductionSpeed: ProductionSpeedLevel = level; break;
                case FranchiseBonusType.StartingCoins: StartingCoinsLevel = level; break;
                case FranchiseBonusType.OfflineEarnings: OfflineEarningsLevel = level; break;
                case FranchiseBonusType.FacilityCostReduction: FacilityCostReductionLevel = level; break;
                case FranchiseBonusType.CriticalProduction: CriticalProductionLevel = level; break;
                case FranchiseBonusType.SpecialWorker: SpecialWorkerUnlocked = true; break;
            }
        }
    }
}
```

**Ornek JSON ciktisi:**

```json
{
  "playerId": "abc123",
  "playerName": "PirinçUstasi42",
  "saveVersion": 147,
  "lastSaveTimestamp": 1774243200,
  "coins": 284500.0,
  "gems": 73,
  "franchisePoints": 12,
  "reputation": 1850,
  "totalEarnings": 5420000.0,
  "franchiseCount": 2,
  "currentCityId": "istanbul",
  "facilities": [
    {
      "id": "field_01",
      "facilityType": "field",
      "isUnlocked": true,
      "starLevel": 4,
      "machineLevel": 4,
      "workerLevel": 28,
      "activeProductId": "rice",
      "autoSellEnabled": false,
      "totalProductsSold": 45200,
      "unlockedRecipes": ["paddy", "rice"]
    },
    {
      "id": "factory_01",
      "facilityType": "factory",
      "isUnlocked": true,
      "starLevel": 3,
      "machineLevel": 3,
      "workerLevel": 20,
      "activeProductId": "rice_flour",
      "autoSellEnabled": true,
      "totalProductsSold": 12400,
      "unlockedRecipes": ["rice_flour", "rice_starch", "rice_vinegar", "rice_milk"]
    }
  ],
  "inventory": {
    "items": {
      "paddy": 450,
      "rice": 1200,
      "rice_flour": 320,
      "bread": 85
    }
  },
  "research": {
    "branchLevels": {
      "automation": 3,
      "quality": 2,
      "speed": 4,
      "capacity": 1
    },
    "activeResearchId": "quality_4",
    "activeResearchProgress": 0.35
  },
  "franchiseBonuses": {
    "productionSpeedLevel": 2,
    "startingCoinsLevel": 1,
    "offlineEarningsLevel": 1
  }
}
```

### 5.3 Runtime Data Modelleri

```csharp
namespace RiceFactory.Data.Models
{
    public struct ProductionRate
    {
        public float ProductsPerSecond;
        public double CoinsPerSecond;
    }

    public struct OfflineEarningsResult
    {
        public TimeSpan Duration;
        public double TotalCoins;
        public int TotalProducts;
        public float Efficiency;
        public bool IsTimeReliable;
    }

    public struct FranchiseResult
    {
        public int FranchiseNumber;
        public int EarnedFP;
        public int TotalFP;
        public double StartingCoins;
        public string CityId;
    }

    public class ActiveOrder
    {
        public string Id;
        public OrderType Type;
        public string CustomerName;
        public List<InputRequirement> Requirements;
        public float RemainingTime; // saniye
        public double BaseReward;
        public float RewardMultiplier;
        public int ReputationReward;
    }

    public class LeaderboardEntry
    {
        public string PlayerId;
        public string PlayerName;
        public long Score;
        public int Rank;
        public string AvatarUrl;
    }

    public class FriendFactoryData
    {
        public string PlayerName;
        public string CityId;
        public List<FacilityVisualInfo> Facilities;
        public List<string> Cosmetics;
        public int Level;
        public int FranchiseCount;
    }

    public enum CurrencyType { Coin, Gem, FP, Reputation }
    public enum UpgradeType { Machine, Worker, Star, Research }
    public enum OrderType { Normal, Urgent, VIP, Bulk, Legendary }
    public enum MiniGameGrade { Bronze, Silver, Gold }
    public enum FranchiseBonusType
    {
        ProductionSpeed, StartingCoins, OfflineEarnings,
        FacilityCostReduction, CriticalProduction, SpecialWorker
    }
    public enum AudioChannel { Master, Music, SFX }
}
```

---

## 6. Firebase Entegrasyonu

### 6.1 Auth Akisi (Anonim -> Baglama)

```
+------------------+     +-------------------+     +---------------------+
|  Ilk Acilis      |     |   Anonim Hesap    |     |  Bagli Hesap        |
|  (Yeni oyuncu)   | --> |   Olusturuldu     | --> |  (Google/Apple/     |
|                  |     |   (auto)          |     |   Email baglandi)   |
+------------------+     +-------------------+     +---------------------+
                               |                          |
                               v                          v
                          Gecici UID                  Kalici UID
                          (cihaz kaybolursa          (cihaz degisse
                           veri kaybedilebilir)       veri korunur)
```

```csharp
namespace RiceFactory.Firebase
{
    public class AuthManager
    {
        private FirebaseAuth _auth;

        public string UserId => _auth.CurrentUser?.UserId;
        public bool IsAnonymous => _auth.CurrentUser?.IsAnonymous ?? true;

        public async Task InitializeAsync()
        {
            _auth = FirebaseAuth.DefaultInstance;

            if (_auth.CurrentUser == null)
            {
                // Ilk acilis: anonim giris
                await _auth.SignInAnonymouslyAsync();
                Debug.Log($"[Auth] Anonim giris basarili. UID: {UserId}");
            }
            else
            {
                Debug.Log($"[Auth] Mevcut oturum. UID: {UserId}, Anonim: {IsAnonymous}");
            }
        }

        /// <summary>Anonim hesabi Google hesabina bagla.</summary>
        public async Task<bool> LinkWithGoogleAsync()
        {
            try
            {
                // Google Sign-In SDK ile token al
                string idToken = await GoogleSignIn.GetIdTokenAsync();
                var credential = GoogleAuthProvider.GetCredential(idToken, null);
                await _auth.CurrentUser.LinkWithCredentialAsync(credential);
                Debug.Log("[Auth] Google baglama basarili.");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Auth] Google baglama hatasi: {ex.Message}");
                return false;
            }
        }

        /// <summary>Anonim hesabi Apple hesabina bagla.</summary>
        public async Task<bool> LinkWithAppleAsync()
        {
            try
            {
                var credential = await GetAppleCredentialAsync();
                await _auth.CurrentUser.LinkWithCredentialAsync(credential);
                Debug.Log("[Auth] Apple baglama basarili.");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Auth] Apple baglama hatasi: {ex.Message}");
                return false;
            }
        }

        private async Task<Credential> GetAppleCredentialAsync()
        {
            // Sign in with Apple SDK kullanimi
            // Apple'in nonce + authorization code akisi
            var rawNonce = GenerateNonce();
            var appleResult = await SignInWithApple.LoginAsync(rawNonce);
            return OAuthProvider.GetCredential(
                "apple.com", appleResult.IdToken, rawNonce, appleResult.AuthorizationCode
            );
        }

        private string GenerateNonce()
        {
            var bytes = new byte[32];
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }
    }
}
```

### 6.2 Firestore Koleksiyon Yapisi

```
firestore-root/
|
+-- players/                          # Oyuncu koleksiyonu
|   +-- {userId}/                     # Her oyuncu bir document
|       |-- displayName: string
|       |-- level: number
|       |-- franchiseCount: number
|       |-- totalLifetimeEarnings: number
|       |-- lastOnline: timestamp
|       |-- createdAt: timestamp
|       |-- platform: string           # "ios" | "android"
|       |-- gameVersion: string
|       |
|       +-- save_data/                 # Alt koleksiyon: save
|       |   +-- current               # Tek document, tam save JSON
|       |       |-- data: map          # PlayerSaveData serializasyonu
|       |       |-- version: number
|       |       |-- timestamp: timestamp
|       |       |-- checksum: string   # Veri butunlugu hash
|       |
|       +-- public_data/              # Diger oyuncularin gorebilecegi veri
|       |   +-- profile
|       |   |   |-- name: string
|       |   |   |-- level: number
|       |   |   |-- avatar: string
|       |   |   |-- frame: string
|       |   +-- factory
|       |       |-- cityId: string
|       |       |-- facilities: array
|       |       |-- cosmetics: array
|       |
|       +-- boosts/                   # Aktif boost'lar
|       |   +-- {boostId}
|       |       |-- type: string
|       |       |-- multiplier: number
|       |       |-- durationSeconds: number
|       |       |-- fromPlayer: string
|       |       |-- createdAt: timestamp
|       |
|       +-- transactions/            # IAP ve ekonomi loglari (audit trail)
|           +-- {transactionId}
|               |-- type: string      # "iap", "reward", "spend"
|               |-- amount: number
|               |-- currency: string
|               |-- reason: string
|               |-- timestamp: timestamp
|
+-- leaderboards/                     # Liderboard koleksiyonu
|   +-- weekly_{yearWeek}/            # Haftalik
|   |   +-- earnings/
|   |   |   +-- {userId}
|   |   |       |-- name: string
|   |   |       |-- score: number
|   |   |       |-- avatar: string
|   |   +-- production/
|   |   +-- orders/
|   |   +-- quality/
|   +-- monthly_{yearMonth}/          # Aylik
|       +-- emperor/
|       +-- franchise_master/
|
+-- trades/                           # Ticaret pazari
|   +-- {tradeId}
|       |-- sellerId: string
|       |-- productId: string
|       |-- quantity: number
|       |-- quality: number
|       |-- price: number
|       |-- status: string            # "open", "sold", "cancelled"
|       |-- createdAt: timestamp
|       |-- expiresAt: timestamp
|
+-- events/                           # Sezonluk etkinlikler
|   +-- {eventId}
|       |-- name: string
|       |-- startDate: timestamp
|       |-- endDate: timestamp
|       |-- config: map
|       |-- rewards: array
|
+-- server/                           # Sunucu meta verisi
    +-- time
    |   |-- timestamp: number         # Server timestamp (anti-cheat)
    +-- config
        |-- maintenanceMode: boolean
        |-- minGameVersion: string
        |-- announcement: string
```

### 6.3 Cloud Functions Listesi ve Tetikleyicileri

| Fonksiyon | Tetikleyici | Aciklama |
|-----------|-------------|----------|
| `onUserCreated` | Auth: onCreate | Yeni oyuncu Firestore document'i olustur, ilk veriyi yaz |
| `validatePurchase` | HTTPS callable | IAP makbuzu dogrula (App Store / Google Play) |
| `submitLeaderboardScore` | HTTPS callable | Liderboard skorunu sunucu tarafli dogrula ve yaz |
| `processFranchise` | HTTPS callable | Franchise islemini sunucu tarafli dogrula, FP hesapla |
| `executeTrade` | Firestore: trades/{id} onWrite | Ticaret islemini dogrula, atomik transfer |
| `resetWeeklyLeaderboard` | Cloud Scheduler (Pazartesi 00:00 UTC) | Haftalik liderboard'u arsivle ve sifirla |
| `resetMonthlyLeaderboard` | Cloud Scheduler (Ayin 1'i 00:00 UTC) | Aylik liderboard'u arsivle ve sifirla |
| `sendRetentionNotification` | Cloud Scheduler (her gun 10:00) | 24+ saat offline oyunculara push bildirim |
| `validateSaveData` | HTTPS callable | Save data checksum dogrulama, anti-cheat |
| `distributeEventRewards` | Cloud Scheduler (etkinlik bitis) | Sezonluk etkinlik odullerini dagit |
| `cleanupExpiredTrades` | Cloud Scheduler (her 6 saat) | Suresi dolan ticaret ilanlarini temizle |
| `syncServerTime` | HTTPS callable | Istemciye sunucu zamanini dondur (anti-cheat) |
| `banCheck` | Firestore: players/{id} onUpdate | Anormal veri degisikliklerini tespit et, flag'le |

### 6.4 Remote Config Anahtar Listesi

Tum ekonomi parametreleri sunucu tarafli ayarlanabilir (A/B test, live ops):

| Anahtar | Varsayilan | Tip | Aciklama |
|---------|-----------|-----|----------|
| `economy_upgrade_cost_base_multiplier` | 1.0 | float | Global upgrade maliyet carpani |
| `economy_sell_price_multiplier` | 1.0 | float | Global satis fiyati carpani |
| `economy_offline_base_efficiency` | 0.30 | float | Temel offline verim |
| `economy_offline_max_hours` | 8 | int | Max offline birikim suresi |
| `economy_franchise_threshold` | 1000000 | int | Minimum franchise kazanci |
| `economy_fp_formula_divisor` | 1000000 | int | FP formulundeki bolen |
| `economy_daily_free_gems` | 10 | int | Gunluk giris elmas odulu |
| `economy_ad_reward_multiplier` | 2.0 | float | Reklam izleme odul carpani |
| `economy_order_refresh_minutes` | 15 | int | Siparis yenileme suresi |
| `economy_minigame_cooldown_hours` | 2 | int | Mini-game bekleme suresi |
| `economy_combo_max_multiplier` | 2.0 | float | Maksimum kombo carpani |
| `economy_reputation_bonus_per_100` | 0.01 | float | Her 100 itibar basina bonus |
| `economy_star_cost_exponent` | 3 | float | Yildiz atlama maliyet ussu |
| `economy_worker_cost_exponent` | 2.2 | float | Calisan seviye maliyet ussu |
| `economy_research_cost_exponent` | 3 | float | Arastirma maliyet ussu |
| `economy_machine_cost_exponent` | 5 | float | Makine upgrade maliyet ussu |
| `event_production_multiplier` | 1.0 | float | Etkinlik doneminde uretim carpani |
| `event_special_order_multiplier` | 1.0 | float | Etkinlik ozel siparis carpani |
| `battlepass_offline_bonus_hours` | 4 | int | Battle Pass ek offline saat |
| `feature_flag_trade_enabled` | false | bool | Ticaret sistemi acik/kapali |
| `feature_flag_global_facility` | false | bool | Kuresel Dagitim tesisi acik/kapali |
| `ad_max_daily_count` | 12 | int | Gunluk maksimum reklam sayisi |
| `ad_min_interval_seconds` | 180 | int | Reklamlar arasi minimum sure |

### 6.5 Analytics Event Semasi

| Event | Parametreler | Tetiklenme Ani |
|-------|-------------|----------------|
| `session_start` | `platform`, `version`, `franchise_count` | Uygulama acilisi |
| `session_end` | `duration_seconds`, `coins_earned` | Uygulama kapanisi |
| `tutorial_step` | `step_id`, `step_name` | Tutorial asamalari |
| `tutorial_complete` | `duration_seconds` | Tutorial bitisi |
| `facility_unlock` | `facility_type`, `cost`, `player_level` | Tesis acma |
| `machine_upgrade` | `facility_type`, `new_level`, `cost` | Makine yukseltme |
| `worker_upgrade` | `facility_type`, `skill_type`, `new_level`, `cost` | Calisan yukseltme |
| `star_upgrade` | `facility_type`, `new_star`, `cost` | Yildiz atlama |
| `research_start` | `branch`, `level`, `cost` | Arastirma baslatma |
| `research_complete` | `branch`, `level`, `duration_seconds` | Arastirma tamamlama |
| `product_sell` | `product_id`, `quantity`, `quality`, `revenue` | Urun satisi |
| `order_complete` | `order_type`, `reward`, `reputation_gain` | Siparis tamamlama |
| `order_expire` | `order_type`, `reputation_loss` | Siparis suresi dolma |
| `minigame_play` | `minigame_id`, `grade`, `bonus_multiplier` | Mini-game oynama |
| `franchise_execute` | `franchise_number`, `earned_fp`, `total_earnings`, `city_id` | Franchise yapma |
| `fp_spend` | `bonus_type`, `new_level`, `fp_cost` | FP harcama |
| `ad_watched` | `placement`, `reward_type`, `reward_amount` | Reklam izleme |
| `iap_purchase` | `product_id`, `price`, `currency`, `gem_amount` | Uygulama ici satin alma |
| `friend_visit` | `friend_id`, `helped` | Arkadas ziyareti |
| `trade_create` | `product_id`, `quantity`, `price` | Ticaret ilani olusturma |
| `trade_complete` | `trade_id`, `buyer_id`, `revenue` | Ticaret tamamlama |
| `milestone_unlock` | `milestone_id`, `reward` | Milestone acma |
| `offline_return` | `duration_hours`, `earnings`, `ad_doubled` | Offline donus |

### 6.6 Cloud Messaging Yapisi

```csharp
namespace RiceFactory.Firebase
{
    public class CloudMessagingManager
    {
        public async Task InitializeAsync()
        {
            // FCM token al ve Firestore'a kaydet
            var messaging = FirebaseMessaging.DefaultInstance;
            messaging.TokenReceived += OnTokenReceived;
            messaging.MessageReceived += OnMessageReceived;

            // Konu abonelikleri
            await messaging.SubscribeAsync("global_announcements");
            await messaging.SubscribeAsync("events");
        }

        private void OnTokenReceived(object sender, TokenReceivedEventArgs e)
        {
            // Token'i Firestore'a kaydet
            var userId = ServiceLocator.Get<AuthManager>().UserId;
            FirebaseFirestore.DefaultInstance
                .Collection("players").Document(userId)
                .UpdateAsync("fcmToken", e.Token);
        }

        private void OnMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            var data = e.Message.Data;
            string type = data.GetValueOrDefault("type", "");

            switch (type)
            {
                case "offline_reminder":
                    // "Fabrikan seni bekliyor! X coin biriktirdin."
                    ShowLocalNotification(data["title"], data["body"]);
                    break;
                case "event_start":
                    // "Bahar Festivali basladi!"
                    ShowLocalNotification(data["title"], data["body"]);
                    break;
                case "friend_help":
                    // "Arkadasin senin fabrikana hayran kaldi!"
                    ShowLocalNotification(data["title"], data["body"]);
                    break;
                case "order_expiring":
                    // "Siparisin 10 dakika icinde surecek!"
                    ShowLocalNotification(data["title"], data["body"]);
                    break;
            }
        }
    }
}
```

**Bildirim turleri ve zamanlama:**

| Bildirim | Kosul | Zamanlama |
|----------|-------|----------|
| Offline hatirlatma | 4+ saat offline | Gunluk 10:00 ve 18:00 |
| Uretim dolu | Stok kapasitesi %90+ | Aninda |
| Siparis suresi | Kabul edilen siparis 10 dk kala | Aninda |
| Etkinlik baslangici | Yeni sezonluk etkinlik | Etkinlik baslangicinda |
| Arastirma tamam | Arastirma suresi doldu | Aninda |
| Arkadas yardimi | Bir arkadas boost gonderdi | Aninda |
| Liderboard odulu | Haftalik donem sonu | Pazartesi 00:00 |

---

## 7. Performans Stratejisi

### 7.1 Object Pooling

Sik olusturulan/yok edilen nesneler icin havuz sistemi:

```csharp
namespace RiceFactory.Utils
{
    public class ObjectPool<T> where T : Component
    {
        private readonly T _prefab;
        private readonly Transform _parent;
        private readonly Queue<T> _available = new();
        private readonly HashSet<T> _inUse = new();
        private readonly int _maxSize;

        public ObjectPool(T prefab, Transform parent, int initialSize = 10, int maxSize = 100)
        {
            _prefab = prefab;
            _parent = parent;
            _maxSize = maxSize;

            // On-olusturma
            for (int i = 0; i < initialSize; i++)
            {
                var obj = CreateNew();
                obj.gameObject.SetActive(false);
                _available.Enqueue(obj);
            }
        }

        public T Get()
        {
            T obj;
            if (_available.Count > 0)
            {
                obj = _available.Dequeue();
            }
            else if (_inUse.Count < _maxSize)
            {
                obj = CreateNew();
            }
            else
            {
                Debug.LogWarning($"[ObjectPool] Max boyuta ulasildi ({_maxSize}). Yeniden kullaniliyor.");
                // En eski kullanimdakini geri al
                obj = _inUse.First();
                _inUse.Remove(obj);
            }

            obj.gameObject.SetActive(true);
            _inUse.Add(obj);
            return obj;
        }

        public void Release(T obj)
        {
            if (!_inUse.Remove(obj)) return;
            obj.gameObject.SetActive(false);
            _available.Enqueue(obj);
        }

        public void ReleaseAll()
        {
            foreach (var obj in _inUse.ToList())
            {
                Release(obj);
            }
        }

        private T CreateNew()
        {
            return UnityEngine.Object.Instantiate(_prefab, _parent);
        }
    }
}
```

**Havuzlanan nesneler:**

| Nesne | Havuz Boyutu | Kullanim |
|-------|-------------|----------|
| Urun ikonu (UI) | 50 | Uretim animasyonlari, envanter gorunumu |
| Para metin efekti (+100) | 20 | Kazanc gostergeleri |
| Uretim partikul | 30 | Tesis uretim efektleri |
| Siparis karti | 10 | Siparis paneli |
| Liderboard satiri | 50 | Liderboard listesi |
| Bildirim toast | 5 | Ekran ustu bildirimler |

### 7.2 Sprite Atlas Yonetimi

```
SpriteAtlases/
  Atlas_UI_Common.spriteatlas        # Genel UI oge'leri (butonlar, ikonlar, cerceveler)
  Atlas_UI_Currencies.spriteatlas    # Para birimleri ikonlari
  Atlas_Facility_Field.spriteatlas   # Tarla gorselleri (5 yildiz x 5 makine)
  Atlas_Facility_Factory.spriteatlas # Fabrika gorselleri
  Atlas_Facility_Bakery.spriteatlas  # Firin gorselleri
  Atlas_Facility_Restaurant.spriteatlas
  Atlas_Facility_Market.spriteatlas
  Atlas_Facility_Global.spriteatlas
  Atlas_Products.spriteatlas         # Tum urun ikonlari (tek atlas, kucuk ikonlar)
  Atlas_Characters.spriteatlas       # Calisan ve musteri gorselleri
  Atlas_Effects.spriteatlas          # Partikul ve efekt sprite'lari
  Atlas_MiniGames.spriteatlas        # Mini-game ozel gorselleri
```

**Kurallar:**
- Her atlas max **2048x2048** piksel (mobil uyumluluk).
- Tesis atlas'lari sadece o tesis acildiginda yuklenir (lazy load).
- Ortak UI atlas'i her zaman bellekte kalir.
- Sprite'lar `[PackingTag]` ile otomatik atlas'a atanir.

### 7.3 Draw Call Optimizasyonu

| Strateji | Uygulama | Beklenen Etki |
|----------|----------|---------------|
| Sprite Batching | Ayni atlas'tan gelen sprite'lar otomatik batch'lenir | Draw call %60 azalma |
| UI Canvas bolme | StaticCanvas (nadiren degisen) + DynamicCanvas (sik degisen) | Rebuild maliyeti %70 azalma |
| TextMeshPro atlas | Tek font atlas tum metinler icin | Font draw call'lari 1'e duser |
| Shader varyant eleme | `#pragma skip_variants` ile gereksiz varyantlar cikarilir | Shader derleme suresi azalir |
| Kamera optimizasyonu | Tek kamera, katmanli culling | Overdraw azalir |

**Canvas stratejisi:**

```
MainCanvas (Screen Space - Overlay)
  +-- StaticLayer (UI elements that rarely change)
  |   +-- TopBar (coins, gems, level)
  |   +-- BottomNav (navigation buttons)
  |
  +-- DynamicLayer (frequently updated)
  |   +-- FacilityView (production animations)
  |   +-- FloatingNumbers (coin gain text)
  |
  +-- PopupLayer (modals, overlays)
      +-- Popups, Tooltips
```

### 7.4 Memory Yonetimi

| Strateji | Detay |
|----------|-------|
| **Addressables** | Tesis gorselleri, sehir temalari, mini-game asset'leri lazy load |
| **Texture sıkistirma** | iOS: ASTC 6x6, Android: ETC2 (fallback: ETC1+Alpha) |
| **Audio sıkistirma** | Muzik: Vorbis/AAC streaming, SFX: ADPCM decompress on load |
| **GC allokasyon azaltma** | String concatenation yerine StringBuilder, LINQ yerine for dongusu, struct yerine class dikkatli kullanimi |
| **Profiling** | Unity Profiler + Memory Profiler ile duzenli kontrol |
| **Max bellek budgesi** | iOS: 200MB, Android: 150MB (eski cihaz uyumu) |

```csharp
// GC allokasyonunu en aza indiren ornek:
// KOTU:
string display = $"Coin: {coins.ToString("N0")}"; // Her frame allokasyon

// IYI:
private readonly StringBuilder _sb = new(32);
private void UpdateCoinDisplay(double coins)
{
    _sb.Clear();
    _sb.Append("Coin: ");
    BigNumber.Format(coins, _sb); // Allokasyonsuz format
    _coinText.SetText(_sb);
}
```

### 7.5 Batching ve LOD

2D oyun oldugu icin klasik LOD yerine **detay seviyesi** yaklasimi:

| Durum | Detay | Aciklama |
|-------|-------|----------|
| Odaklanmis tesis | Tam detay | Tum animasyonlar, partkuller, calisan hareketleri |
| Gorunur ama uzak | Orta detay | Animasyonlar yavaslatilmis, partikuller kapatilmis |
| Ekran disi | Minimum | Sadece mantik isler, gorsel guncelleme yok |

```csharp
public class FacilityRenderer : MonoBehaviour
{
    private enum DetailLevel { Full, Reduced, Hidden }
    private DetailLevel _currentDetail;

    private void OnBecameVisible()
    {
        SetDetailLevel(DetailLevel.Full);
    }

    private void OnBecameInvisible()
    {
        SetDetailLevel(DetailLevel.Hidden);
    }

    private void SetDetailLevel(DetailLevel level)
    {
        if (_currentDetail == level) return;
        _currentDetail = level;

        switch (level)
        {
            case DetailLevel.Full:
                _animator.speed = 1f;
                _particleSystem.Play();
                _workerAnimator.enabled = true;
                break;
            case DetailLevel.Reduced:
                _animator.speed = 0.5f;
                _particleSystem.Stop();
                _workerAnimator.enabled = false;
                break;
            case DetailLevel.Hidden:
                _animator.speed = 0f;
                _particleSystem.Stop();
                _workerAnimator.enabled = false;
                break;
        }
    }
}
```

---

## 8. Build Pipeline

### 8.1 iOS Build Ayarlari

| Ayar | Deger | Aciklama |
|------|-------|----------|
| Target SDK | iOS 15.0+ | Min iOS versiyon |
| Architecture | ARM64 | Tek mimari (32-bit destek kaldirildi) |
| Scripting Backend | IL2CPP | Performans ve guvenlik |
| API Compatibility | .NET Standard 2.1 | Firebase SDK uyumu |
| Texture Compression | ASTC 6x6 | En iyi boyut/kalite dengesi |
| Strip Engine Code | ON | Kullanilmayan Unity modulleri cikarilir |
| Managed Stripping Level | Medium | IL2CPP boyut optimizasyonu |
| Bundle Identifier | com.riceFactory.game | App Store ID |
| Signing | Automatic | Xcode otomatik imzalama |
| Capabilities | Push Notifications, Sign in with Apple, In-App Purchase | Gerekli yetenekler |
| App Transport Security | Firebase domainleri icin exception | HTTPS zorunlulugu |

**Entitlements:**

```xml
<?xml version="1.0" encoding="UTF-8"?>
<plist version="1.0">
<dict>
    <key>aps-environment</key>
    <string>production</string>
    <key>com.apple.developer.applesignin</key>
    <array>
        <string>Default</string>
    </array>
</dict>
</plist>
```

### 8.2 Android Build Ayarlari

| Ayar | Deger | Aciklama |
|------|-------|----------|
| Min API Level | API 24 (Android 7.0) | Firebase minimum gereksinim |
| Target API Level | API 34 (Android 14) | Google Play gereksinim |
| Scripting Backend | IL2CPP | Performans ve guvenlik |
| Target Architecture | ARM64 | Tek mimari (Google Play zorunlulugu) |
| Texture Compression | ETC2 | GLES 3.0+ standart |
| Internet Access | Require | Firebase icin zorunlu |
| Write Permission | External (SDCard) | Save dosyalari |
| Custom Gradle Template | ON | Firebase dependecy'leri icin |
| Minify | Release: Proguard/R8 | APK boyut kucultme |
| App Bundle | ON | Google Play AAB formati |
| Keystore | release.keystore | Imzalama anahtari (CI/CD'de secret) |

**build.gradle eklemeleri:**

```groovy
dependencies {
    implementation platform('com.google.firebase:firebase-bom:32.7.0')
    implementation 'com.google.firebase:firebase-analytics'
    implementation 'com.google.firebase:firebase-auth'
    implementation 'com.google.firebase:firebase-firestore'
    implementation 'com.google.firebase:firebase-config'
    implementation 'com.google.firebase:firebase-messaging'
    implementation 'com.google.android.gms:play-services-ads:22.6.0'
    implementation 'com.android.billingclient:billing:6.1.0'
}
```

### 8.3 CI/CD Onerisi

```
+-------------------+     +------------------+     +------------------+
|   Git Push        |     |   CI Pipeline    |     |   Distribution   |
|   (main/develop)  | --> |   (GitHub        | --> |   (TestFlight /  |
|                   |     |    Actions)      |     |    Firebase      |
|                   |     |                  |     |    App Dist.)    |
+-------------------+     +------------------+     +------------------+
```

**GitHub Actions Workflow:**

```yaml
# .github/workflows/build.yml
name: Build & Deploy
on:
  push:
    branches: [main, develop]
    tags: ['v*']

jobs:
  build-android:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
        with:
          lfs: true
      - uses: game-ci/unity-builder@v4
        with:
          targetPlatform: Android
          buildMethod: BuildScript.BuildAndroid
          androidAppBundle: true
          androidKeystoreName: release.keystore
          androidKeystoreBase64: ${{ secrets.ANDROID_KEYSTORE_BASE64 }}
          androidKeystorePass: ${{ secrets.ANDROID_KEYSTORE_PASS }}
      - uses: r0adkll/upload-google-play@v1
        if: startsWith(github.ref, 'refs/tags/v')
        with:
          serviceAccountJsonPlainText: ${{ secrets.GOOGLE_PLAY_SERVICE_ACCOUNT }}
          packageName: com.riceFactory.game
          releaseFiles: build/Android/*.aab
          track: internal

  build-ios:
    runs-on: macos-latest
    steps:
      - uses: actions/checkout@v4
        with:
          lfs: true
      - uses: game-ci/unity-builder@v4
        with:
          targetPlatform: iOS
          buildMethod: BuildScript.BuildIOS
      - uses: yukiarrr/ios-build-action@v1.12.0
        with:
          project-path: build/iOS/Unity-iPhone.xcodeproj
          export-method: app-store
      - uses: apple-actions/upload-testflight-build@v1
        if: startsWith(github.ref, 'refs/tags/v')
```

### 8.4 Versiyonlama Stratejisi

**Semantic Versioning:** `MAJOR.MINOR.PATCH+BUILD`

| Kisim | Aciklama | Artis Kosulu |
|-------|----------|-------------|
| MAJOR | Buyuk degisiklik, save uyumsuzlugu | Save format degisikligi, buyuk ozellik |
| MINOR | Yeni ozellik, geriye uyumlu | Yeni tesis, yeni sistem |
| PATCH | Bug fix, kucuk iyilestirme | Hata duzeltme, denge ayari |
| BUILD | CI build numarasi | Her build'de otomatik artar |

**Ornek:** `1.2.5+147`

**Version Code (Android) / Build Number (iOS):**

```
VersionCode = MAJOR * 10000 + MINOR * 100 + PATCH
Ornek: 1.2.5 -> 10205
```

**Save data uyumluluk:**

```csharp
public class SaveMigrator
{
    public static PlayerSaveData Migrate(PlayerSaveData data, string currentVersion)
    {
        if (data.GameVersion == currentVersion) return data;

        // Versiyon zinciri ile migrasyon
        if (CompareVersions(data.GameVersion, "1.1.0") < 0)
            data = MigrateTo_1_1_0(data);
        if (CompareVersions(data.GameVersion, "1.2.0") < 0)
            data = MigrateTo_1_2_0(data);

        data.GameVersion = currentVersion;
        return data;
    }
}
```

---

## 9. Guvenlik

### 9.1 Anti-Cheat Yaklasimi

#### Zaman Manipulasyonu Onleme

```csharp
namespace RiceFactory.Core
{
    /// <summary>
    /// Cihaz saatini ileri alarak offline kazanc manipulasyonunu onler.
    /// </summary>
    public class AntiCheatTimeValidator
    {
        private long _lastValidServerTime;
        private long _lastLocalTime;
        private int _suspicionScore;

        private const int SUSPICION_THRESHOLD = 3;
        private const int MAX_TIME_DRIFT_SECONDS = 300; // 5 dakika tolerans

        /// <summary>Zaman tutarliligini kontrol et.</summary>
        public TimeValidationResult ValidateTime()
        {
            long localNow = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long expectedElapsed = localNow - _lastLocalTime;

            // Negatif zaman farki = saat geri alinmis
            if (expectedElapsed < -60)
            {
                _suspicionScore += 2;
                return new TimeValidationResult
                {
                    IsValid = false,
                    Reason = "Cihaz saati geri alinmis",
                    PenaltyMultiplier = 0.1f // %90 kazanc cezasi
                };
            }

            // Gercekci olmayan ileri zaman = saat ileri alinmis
            // Sunucu zamani ile karsilastir
            if (_lastValidServerTime > 0)
            {
                long serverDrift = Math.Abs(localNow - _lastValidServerTime);
                if (serverDrift > MAX_TIME_DRIFT_SECONDS)
                {
                    _suspicionScore++;
                    if (_suspicionScore >= SUSPICION_THRESHOLD)
                    {
                        return new TimeValidationResult
                        {
                            IsValid = false,
                            Reason = "Tekrarlayan zaman uyumsuzlugu",
                            PenaltyMultiplier = 0f, // Tamamen engelle
                            ShouldReport = true
                        };
                    }
                    return new TimeValidationResult
                    {
                        IsValid = true,
                        Reason = "Zaman farki tespit edildi",
                        PenaltyMultiplier = 0.5f // %50 ceza
                    };
                }
            }

            _lastLocalTime = localNow;
            return new TimeValidationResult { IsValid = true, PenaltyMultiplier = 1f };
        }
    }
}
```

#### Deger Manipulasyonu Onleme

```csharp
namespace RiceFactory.Core
{
    /// <summary>
    /// Memory editor (GameGuardian, Cheat Engine) gibi araclara karsi
    /// hassas degerleri sifreli tutar.
    /// </summary>
    public struct SecureDouble
    {
        private long _encrypted;
        private readonly long _key;

        public SecureDouble(double value)
        {
            _key = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            _encrypted = Encrypt(value, _key);
        }

        public double Value
        {
            get => Decrypt(_encrypted, _key);
            set => _encrypted = Encrypt(value, _key);
        }

        private static long Encrypt(double value, long key)
        {
            long bits = BitConverter.DoubleToInt64Bits(value);
            return bits ^ key;
        }

        private static double Decrypt(long encrypted, long key)
        {
            long bits = encrypted ^ key;
            return BitConverter.Int64BitsToDouble(bits);
        }

        // Implicit operatorler ile normal double gibi kullanim
        public static implicit operator double(SecureDouble s) => s.Value;
    }

    /// <summary>Ayni yaklasim int icin.</summary>
    public struct SecureInt
    {
        private int _encrypted;
        private readonly int _key;

        public SecureInt(int value)
        {
            _key = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            _encrypted = value ^ _key;
        }

        public int Value
        {
            get => _encrypted ^ _key;
            set => _encrypted = value ^ _key;
        }

        public static implicit operator int(SecureInt s) => s.Value;
    }
}
```

**Kullanim:**

```csharp
// PlayerSaveData icinde hassas degerler SecureDouble/SecureInt kullanir
public SecureDouble Coins;
public SecureInt Gems;
```

### 9.2 Sunucu Tarafli Dogrulama Noktalari

| Islem | Dogrulama | Cloud Function |
|-------|-----------|----------------|
| IAP satin alma | Makbuz dogrulama (Apple/Google receipt validation) | `validatePurchase` |
| Liderboard skor gonderme | Skor tutarliligi kontrolu (max kazanilabilir/saat siniri) | `submitLeaderboardScore` |
| Franchise islemi | Toplam kazanc dogrulama, FP hesap kontrolu | `processFranchise` |
| Ticaret islemi | Fiyat araligi kontrolu, envanter dogrulama, atomik transfer | `executeTrade` |
| Save data kayit | Checksum dogrulama, anormal deger tespiti | `validateSaveData` |
| Offline kazanc | Sunucu zamani ile karsilastirma, max kazanc siniri | Istemci + sunucu |

**Anormallik tespiti (Cloud Function):**

```typescript
// packages/firebase-backend/functions/src/antiCheat.ts
export const validateSaveData = functions.https.onCall(async (data, context) => {
  const userId = context.auth?.uid;
  if (!userId) throw new HttpsError('unauthenticated', 'Giris yapilmali.');

  const saveData = data.saveData;
  const previousSave = await getPreviousSave(userId);

  // 1. Checksum dogrulama
  const expectedHash = computeHash(saveData);
  if (saveData.dataHash !== expectedHash) {
    await flagPlayer(userId, 'checksum_mismatch');
    throw new HttpsError('invalid-argument', 'Veri butunlugu hatasi.');
  }

  // 2. Kazanc hizi kontrolu
  const timeDiff = saveData.lastSaveTimestamp - previousSave.lastSaveTimestamp;
  const earningsDiff = saveData.totalEarnings - previousSave.totalEarnings;
  const maxPossibleEarnings = calculateMaxEarnings(previousSave, timeDiff);

  if (earningsDiff > maxPossibleEarnings * 1.5) { // %50 tolerans
    await flagPlayer(userId, 'abnormal_earnings', {
      expected: maxPossibleEarnings,
      actual: earningsDiff,
      timeSeconds: timeDiff
    });
    return { valid: false, reason: 'Anormal kazanc orani.' };
  }

  // 3. Elmas tutarliligi
  const gemDiff = saveData.gems - previousSave.gems;
  const maxFreeGems = 100; // Gunluk max ucretsiz elmas
  if (gemDiff > maxFreeGems && !hasPurchaseRecord(userId, timeDiff)) {
    await flagPlayer(userId, 'abnormal_gems');
    return { valid: false, reason: 'Anormal elmas artisi.' };
  }

  return { valid: true };
});
```

### 9.3 Obfuscation

| Katman | Arac | Kapsam |
|--------|------|--------|
| IL2CPP | Unity yerlesik | C# kodunu C++ -> native derleme. Basli basina guclu koruma. |
| .NET obfuscation | Obfuscar (ucretsiz) veya Beebyte (Unity icin) | IL2CPP oncesi isim karistirma (debug symbol'leri) |
| String sifreleme | Ozel cozum | Hassas string'ler (API anahtarlari, endpoint URL'leri) runtime'da cozulur |
| PlayerPrefs sifreleme | AES-256 | Lokal kayit dosyasi sifrelenir |
| Sabitler gizleme | SecureInt/SecureDouble | Bellekteki degerler XOR ile sifreli tutulur |

**Save data sifreleme:**

```csharp
namespace RiceFactory.Data.Save
{
    public static class SaveEncryption
    {
        // Anahtar: cihaz ID + sabit tuz. Her cihazda farkli.
        private static byte[] GetKey()
        {
            string deviceId = SystemInfo.deviceUniqueIdentifier;
            string salt = "rF_2026_s4lt"; // Obfuscation ile gizlenir
            byte[] combined = Encoding.UTF8.GetBytes(deviceId + salt);
            using var sha = System.Security.Cryptography.SHA256.Create();
            return sha.ComputeHash(combined); // 256-bit anahtar
        }

        public static string Encrypt(string json)
        {
            byte[] key = GetKey();
            byte[] data = Encoding.UTF8.GetBytes(json);

            using var aes = Aes.Create();
            aes.Key = key;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            byte[] encrypted = encryptor.TransformFinalBlock(data, 0, data.Length);

            // IV + encrypted data
            byte[] result = new byte[aes.IV.Length + encrypted.Length];
            Array.Copy(aes.IV, 0, result, 0, aes.IV.Length);
            Array.Copy(encrypted, 0, result, aes.IV.Length, encrypted.Length);

            return Convert.ToBase64String(result);
        }

        public static string Decrypt(string base64)
        {
            byte[] key = GetKey();
            byte[] fullData = Convert.FromBase64String(base64);

            using var aes = Aes.Create();
            aes.Key = key;

            byte[] iv = new byte[16];
            Array.Copy(fullData, 0, iv, 0, 16);
            aes.IV = iv;

            byte[] encrypted = new byte[fullData.Length - 16];
            Array.Copy(fullData, 16, encrypted, 0, encrypted.Length);

            using var decryptor = aes.CreateDecryptor();
            byte[] decrypted = decryptor.TransformFinalBlock(encrypted, 0, encrypted.Length);

            return Encoding.UTF8.GetString(decrypted);
        }
    }
}
```

---

## 10. Ucuncu Parti Kutuphaneler

### 10.1 Onerilen Unity Paketleri

| Paket | Versiyon | Amac | Gereklilik |
|-------|----------|------|------------|
| **DOTween Pro** | 1.2.7+ | Animasyon, tweening, UI gecisleri | ZORUNLU |
| **TextMeshPro** | 3.2+ (Unity yerlesik) | Metin render, emoji, rich text | ZORUNLU |
| **Addressables** | 1.21+ | Asset lazy loading, bellek yonetimi | ZORUNLU |
| **Newtonsoft JSON** | 13.0+ (Unity paketi) | JSON serialization/deserialization | ZORUNLU |
| **UniTask** | 2.5+ | Allokasyonsuz async/await, coroutine alternatifi | ONERILEN |
| **NaughtyAttributes** | 2.1+ | Inspector iyilestirme, editor araclari | ONERILEN |
| **Sprite Atlas Packer** | Unity yerlesik | Sprite atlas otomatik paketleme | ZORUNLU |
| **Unity Localization** | 1.4+ | Coklu dil destegi | ONERILEN |
| **Unity IAP** | 4.9+ | Uygulama ici satin alma | ZORUNLU |

### 10.2 Firebase SDK'lar

| SDK | Versiyon | Amac |
|-----|----------|------|
| Firebase Auth | 12.0+ | Kimlik dogrulama |
| Firebase Firestore | 12.0+ | NoSQL veritabani |
| Firebase Remote Config | 12.0+ | Sunucu tarafli yapilandirma |
| Firebase Analytics | 12.0+ | Olay takibi |
| Firebase Cloud Messaging | 12.0+ | Push bildirimler |
| Firebase Crashlytics | 12.0+ | Crash raporlama |
| Firebase App Distribution | - | Beta test dagilimi |

### 10.3 Ad SDK'lar

| SDK | Amac | Entegrasyon |
|-----|------|-------------|
| **Google AdMob** | Ana reklam aglari (rewarded, banner) | Unity Mediation uzerinden |
| **Unity Ads** | Yedek ag, Unity Mediation icinden | Unity Mediation |
| **IronSource / LevelPlay** | Mediation alternatifi (yuksek eCPM) | Direkt entegrasyon |
| **AppLovin MAX** | Mediation platformu (A/B test) | Opsiyonel alternatif |

**Reklam yerlesimleri (GDD uyumlu):**

| Yerlesim | Tur | Odul |
|----------|-----|------|
| Offline donus ekrani | Rewarded | Kazanc x2 |
| Uretim boost | Rewarded | Tum uretim x2, 30 dk |
| Arastirma hizlandirma | Rewarded | Arastirma suresi -%30 |
| Siparis yenileme | Rewarded | 3 yeni siparis |
| Mini-game sifirlama | Rewarded | Cooldown sifirla |
| Cark cevirme | Rewarded | Rastgele odul |

**Reklam kurallari:**
- Gunluk max 12 reklam.
- Reklamlar arasi min 3 dakika.
- Interstitial (zorla gosterilen) reklam **YOK**.
- Tum reklamlar opsiyonel ve odullu.

### 10.4 Analytics SDK'lar

| SDK | Amac | Kullanim |
|-----|------|----------|
| **Firebase Analytics** | Birincil analytics | Olay takibi, funnel, retention |
| **Firebase Crashlytics** | Crash raporlama | Hata tespit ve cozum |
| **Unity Analytics** | Unity ekosistem metrikleri | Opsiyonel |
| **Adjust / AppsFlyer** | Attribution, UA (User Acquisition) | Reklam kampanya takibi |

**Temel izlenen metrikler:**

| Metrik | Olcum | Hedef |
|--------|-------|-------|
| D1 Retention | Gun 1 geri donus | >%45 |
| D7 Retention | Gun 7 geri donus | >%20 |
| D30 Retention | Gun 30 geri donus | >%10 |
| Session Length | Ortalama oturum suresi | 5-8 dk |
| Sessions/Day | Gunluk oturum sayisi | 3-5 |
| ARPDAU | Gunluk kullanici basina gelir | Platform bagimli |
| Ad eCPM | 1000 gosterim basina gelir | >$15 (rewarded) |
| Conversion Rate | Tutorial tamamlama | >%85 |
| Franchise Rate | Ilk franchise orani (D14 icinde) | >%30 |

---

## Ek: Sistem Etkilesim Diyagrami

```
                              EventManager
                                  |
                    +-------------+-------------+
                    |             |             |
              GameTickEvent  CurrencyChanged  UpgradeCompleted
                    |             |             |
        +-----------+-------+    |    +--------+--------+
        |           |       |    |    |        |        |
   Production   Research  Quest  |  UI Update  Audio  Analytics
   System       System   System  |  (Panel     (SFX   (Event
   (Tick        (Timer   (Order  |   refresh)   play)   log)
    uretim)      ilerle)  sure)  |
        |                        |
        v                        v
   EconomySystem <----------> SaveManager
   (Coin islemleri)           (Periyodik kayit)
        |
        v
   PrestigeSystem
   (Franchise kontrolu)
```

---

**Son guncelleme:** 2026-03-22
**Sonraki adimlar:** Prototip fazinda bu mimari iskelet uygulanacak, `Boot` + `Game` sahneleri ve core manager'lar oncelikli olusturulacak.
