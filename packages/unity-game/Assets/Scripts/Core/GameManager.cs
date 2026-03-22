// =============================================================================
// GameManager.cs
// Oyunun genel durumunu yoneten Singleton sinif.
// Oyun durumu gecisleri (Loading, Playing, Paused, Prestige vb.) yonetir.
// Diger manager'lara referans tutar ve uygulama yasam dongusunu kontrol eder.
// DontDestroyOnLoad ile sahne gecislerinde hayatta kalir.
// =============================================================================

using System;
using RiceFactory.Core.Events;
using UnityEngine;

namespace RiceFactory.Core
{
    // -------------------------------------------------------------------------
    // Oyun Durumu Enum'u
    // -------------------------------------------------------------------------

    /// <summary>Oyunun bulunabilecegi durumlar.</summary>
    public enum GameState
    {
        Loading,      // Baslangic yuklemesi, SDK init
        MainMenu,     // Ana menu ekrani
        Playing,      // Aktif oyun dongusu
        Paused,       // Duraklama (arka plan veya menu)
        MiniGame,     // Mini-game aktif
        Franchise,    // Prestige gecis ekrani
        Settings      // Ayarlar ekrani
    }

    // -------------------------------------------------------------------------
    // Game Manager Arayuzu
    // -------------------------------------------------------------------------

    /// <summary>GameManager icin interface. Durum yonetimi ve tick dongusu.</summary>
    public interface IGameManager
    {
        GameState CurrentState { get; }
        PlayerSaveData PlayerData { get; }
        float PlayTimeThisSession { get; }

        void ChangeState(GameState newState);
        void Tick(float deltaTime);
        void OnApplicationPause(bool paused);
        void OnApplicationQuit();
    }

    // -------------------------------------------------------------------------
    // Game Manager Implementasyonu
    // -------------------------------------------------------------------------

    /// <summary>
    /// Oyunun merkezi yoneticisi. Oyun durumunu, tick dongusunu ve
    /// uygulama yasam dongusunu yonetir.
    ///
    /// Not: GameManager bir MonoBehaviour degildir. MonoBehaviour proxy
    /// (GameManagerBehaviour) uzerinden Unity yaşam dongusu olaylari alinir.
    /// Bu sayede logic layer saf C# olarak kalir.
    /// </summary>
    public class GameManager : IGameManager
    {
        // =====================================================================
        // Properties
        // =====================================================================

        /// <summary>Oyunun mevcut durumu.</summary>
        public GameState CurrentState { get; private set; } = GameState.Loading;

        /// <summary>Aktif oyuncu verisi (SaveManager uzerinden).</summary>
        public PlayerSaveData PlayerData => _saveManager.Data;

        /// <summary>Bu oturumdaki toplam oynama suresi (saniye).</summary>
        public float PlayTimeThisSession { get; private set; }

        // =====================================================================
        // Bagimliliklar
        // =====================================================================

        private readonly ISaveManager _saveManager;
        private readonly IEventManager _eventManager;
        private readonly ITimeManager _timeManager;

        // =====================================================================
        // Constructor
        // =====================================================================

        public GameManager(ISaveManager saveManager, IEventManager eventManager, ITimeManager timeManager)
        {
            _saveManager = saveManager ?? throw new ArgumentNullException(nameof(saveManager));
            _eventManager = eventManager ?? throw new ArgumentNullException(nameof(eventManager));
            _timeManager = timeManager ?? throw new ArgumentNullException(nameof(timeManager));
        }

        // =====================================================================
        // Durum Yonetimi
        // =====================================================================

        /// <summary>
        /// Oyun durumunu degistirir ve GameStateChangedEvent yayinlar.
        /// Ayni duruma gecis yapilmaz.
        /// </summary>
        public void ChangeState(GameState newState)
        {
            if (CurrentState == newState) return;

            var oldState = CurrentState;
            CurrentState = newState;

            Debug.Log($"[GameManager] Durum degisimi: {oldState} -> {newState}");
            _eventManager.Publish(new GameStateChangedEvent(oldState, newState));
        }

        // =====================================================================
        // Tick Dongusu
        // =====================================================================

        /// <summary>
        /// Her frame MonoBehaviour proxy tarafindan cagirilir.
        /// Sadece Playing durumunda sistemleri gunceller.
        /// SaveManager auto-save tick'i her durumda calisir.
        /// </summary>
        public void Tick(float deltaTime)
        {
            // Auto-save her durumda calissin
            _saveManager.Tick(deltaTime);

            // Oyun mantigi sadece Playing durumunda
            if (CurrentState != GameState.Playing) return;

            PlayTimeThisSession += deltaTime;
            _timeManager.Tick(deltaTime);

            // Sistemler kendi guncelleme mantıklarini GameTickEvent uzerinden alir
            _eventManager.Publish(new GameTickEvent(deltaTime));
        }

        // =====================================================================
        // Uygulama Yasam Dongusu
        // =====================================================================

        /// <summary>
        /// Uygulama arka plana alindiginda veya on plana geldiginde cagirilir.
        /// Arka plana alinda: zaman kaydedilir, kayit yapilir.
        /// On plana gelince: offline sure hesaplanir, event yayinlanir.
        /// </summary>
        public void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                Debug.Log("[GameManager] Uygulama arka plana alindi. Kaydediliyor...");
                _timeManager.RecordPauseTime();
                _ = _saveManager.SaveAsync(); // Fire-and-forget, arka planda kaydet
            }
            else
            {
                Debug.Log("[GameManager] Uygulama on plana geldi.");
                var offlineDuration = _timeManager.GetTimeSincePause();
                _eventManager.Publish(new AppResumedEvent(offlineDuration));
            }
        }

        /// <summary>
        /// Uygulama tamamen kapatildiginda cagirilir.
        /// Son bir kayit yapilir.
        /// </summary>
        public void OnApplicationQuit()
        {
            Debug.Log("[GameManager] Uygulama kapatiliyor. Son kayit yapiliyor...");
            _timeManager.RecordPauseTime();
            _saveManager.SaveLocal(); // Senkron kayit (async guvenli degil quit sirasinda)
        }
    }

    // =========================================================================
    // MonoBehaviour Proxy -- Unity yasam dongusu koprusu
    // =========================================================================

    /// <summary>
    /// GameManager'in Unity yaşam dongusu olaylarini almasini saglayan
    /// MonoBehaviour proxy. DontDestroyOnLoad ile kalici olur.
    ///
    /// Boot sahnesinde bu component bir GameObject'e eklenir.
    /// GameManager pure C# olarak kalirken, Unity'nin Update, OnApplicationPause
    /// ve OnApplicationQuit olaylarini GameManager'a iletir.
    /// </summary>
    public class GameManagerBehaviour : MonoBehaviour
    {
        private IGameManager _gameManager;

        // Singleton erisimi
        public static GameManagerBehaviour Instance { get; private set; }

        private void Awake()
        {
            // Singleton kontrolu
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Boot sirasinda GameManager referansi verilir.
        /// ServiceLocator'dan otomatik olarak alinabilir.
        /// </summary>
        public void Initialize(IGameManager gameManager)
        {
            _gameManager = gameManager;
        }

        /// <summary>
        /// Initialize cagrilmadiysa ServiceLocator'dan otomatik cek.
        /// </summary>
        private void Start()
        {
            if (_gameManager == null && ServiceLocator.TryGet<IGameManager>(out var gm))
            {
                _gameManager = gm;
            }
        }

        private void Update()
        {
            _gameManager?.Tick(Time.deltaTime);
        }

        private void OnApplicationPause(bool paused)
        {
            _gameManager?.OnApplicationPause(paused);
        }

        private void OnApplicationQuit()
        {
            _gameManager?.OnApplicationQuit();
        }
    }
}
