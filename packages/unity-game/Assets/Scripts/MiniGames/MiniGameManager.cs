// =============================================================================
// MiniGameManager.cs
// Merkezi mini-game yoneticisi. Hangi mini-game'in ne zaman acilacagini,
// gunluk limitleri, cooldown'lari ve odul hesaplamasini yonetir.
// ServiceLocator'a IMiniGameManager olarak kaydolur.
// =============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using RiceFactory.Core;
using RiceFactory.Core.Events;

namespace RiceFactory.MiniGames
{
    // -------------------------------------------------------------------------
    // Interface
    // -------------------------------------------------------------------------

    /// <summary>
    /// Mini-game yonetici arayuzu. ServiceLocator ile erisilir.
    /// </summary>
    public interface IMiniGameManager
    {
        /// <summary>Mini-game baslatir. Basarili baslatma icin true doner.</summary>
        bool StartMiniGame(string miniGameId);

        /// <summary>Mini-game oynayabilir mi? (limit + cooldown kontrolu)</summary>
        bool CanPlay(string miniGameId);

        /// <summary>Belirli mini-game icin kalan gunluk hak sayisi.</summary>
        int GetRemainingPlays(string miniGameId);

        /// <summary>Cooldown bitis zamani. Cooldown yoksa DateTime.MinValue.</summary>
        DateTime GetCooldownEndTime();

        /// <summary>Belirli mini-game icin en yuksek skor.</summary>
        int GetHighScore(string miniGameId);

        /// <summary>Aktif mini-game var mi?</summary>
        bool IsPlaying { get; }

        /// <summary>Tum kayitli mini-game ID'lerini dondurur.</summary>
        IReadOnlyList<string> GetAllMiniGameIds();

        /// <summary>Mini-game konfigurasyonunu dondurur.</summary>
        MiniGameConfig GetConfig(string miniGameId);
    }

    // -------------------------------------------------------------------------
    // Oyun verisi — her mini-game icin durum
    // -------------------------------------------------------------------------

    [Serializable]
    public class MiniGamePlayData
    {
        public string MiniGameId;
        public int DailyPlaysUsed;
        public int HighScore;
        public MiniGameGrade BestGrade;
        public DateTime LastResetDate;

        public MiniGamePlayData(string id)
        {
            MiniGameId = id;
            DailyPlaysUsed = 0;
            HighScore = 0;
            BestGrade = MiniGameGrade.C;
            LastResetDate = DateTime.UtcNow.Date;
        }
    }

    // -------------------------------------------------------------------------
    // MiniGameManager Implementasyonu
    // -------------------------------------------------------------------------

    /// <summary>
    /// Merkezi mini-game yoneticisi.
    /// - Gunluk limit: her mini-game 3 kez oynanabilir
    /// - Cooldown: mini-game arasi 5 dakika bekleme
    /// - Grade bazli odul carpani: S=5x, A=3x, B=2x, C=1x
    /// - MiniGameCompletedEvent firlatin EventManager'a
    /// </summary>
    public class MiniGameManager : MonoBehaviour, IMiniGameManager
    {
        // =====================================================================
        // Sabitler
        // =====================================================================

        /// <summary>Her mini-game icin gunluk oynama limiti.</summary>
        private const int DAILY_PLAY_LIMIT = 3;

        /// <summary>Mini-game arasi cooldown suresi (saniye). 5 dakika.</summary>
        private const float COOLDOWN_SECONDS = 300f;

        /// <summary>Grade bazli odul carpanlari.</summary>
        private static readonly Dictionary<MiniGameGrade, float> GRADE_MULTIPLIERS = new()
        {
            { MiniGameGrade.S, 5f },
            { MiniGameGrade.A, 3f },
            { MiniGameGrade.B, 2f },
            { MiniGameGrade.C, 1f }
        };

        /// <summary>Grade bazli bonus suresi carpanlari.</summary>
        private static readonly Dictionary<MiniGameGrade, float> BOOST_DURATION_MULTIPLIERS = new()
        {
            { MiniGameGrade.S, 1800f },  // 30 dakika
            { MiniGameGrade.A, 1200f },  // 20 dakika
            { MiniGameGrade.B, 600f },   // 10 dakika
            { MiniGameGrade.C, 300f }    // 5 dakika
        };

        // =====================================================================
        // Serializasyon ve Durum
        // =====================================================================

        [Header("Mini-Game Konfigurasyonlari")]
        [SerializeField] private List<MiniGameConfig> _configs = new();

        // Oyun verileri — mini-game ID'ye gore
        private readonly Dictionary<string, MiniGamePlayData> _playData = new();

        // Cooldown zamani
        private DateTime _cooldownEndTime = DateTime.MinValue;

        // Aktif mini-game referansi
        private MiniGameBase _activeMiniGame;

        // Servis referanslari
        private IEventManager _eventManager;

        // =====================================================================
        // Ozellikler
        // =====================================================================

        public bool IsPlaying => _activeMiniGame != null && _activeMiniGame.State == MiniGameState.Playing;

        // =====================================================================
        // Unity Lifecycle
        // =====================================================================

        private void Awake()
        {
            // Varsayilan konfigurasyonlari olustur (editor'de atanmadiysa)
            if (_configs.Count == 0)
            {
                InitializeDefaultConfigs();
            }

            // Play data'lari hazirla
            foreach (var config in _configs)
            {
                if (!_playData.ContainsKey(config.MiniGameId))
                {
                    _playData[config.MiniGameId] = new MiniGamePlayData(config.MiniGameId);
                }
            }
        }

        private void Start()
        {
            // ServiceLocator'a kayit ol
            ServiceLocator.Register<IMiniGameManager>(this);

            // EventManager referansi
            if (ServiceLocator.TryGet(out IEventManager eventManager))
            {
                _eventManager = eventManager;
            }
            else
            {
                Debug.LogWarning("[MiniGameManager] EventManager bulunamadi. Event'ler firilatimayacak.");
            }
        }

        private void OnDestroy()
        {
            // ServiceLocator'dan kaydi kaldir
            if (ServiceLocator.IsRegistered<IMiniGameManager>())
            {
                ServiceLocator.Unregister<IMiniGameManager>();
            }
        }

        // =====================================================================
        // Varsayilan Konfigurasyonlar
        // =====================================================================

        private void InitializeDefaultConfigs()
        {
            _configs = new List<MiniGameConfig>
            {
                new MiniGameConfig
                {
                    MiniGameId = "harvest",
                    Duration = 15f,
                    ThresholdS = 50, ThresholdA = 35, ThresholdB = 20,
                    BaseReward = 100,
                    BoostDuration = 1800f
                },
                new MiniGameConfig
                {
                    MiniGameId = "quality_control",
                    Duration = 20f,
                    ThresholdS = 40, ThresholdA = 25, ThresholdB = 15,
                    BaseReward = 120,
                    BoostDuration = 1800f
                },
                new MiniGameConfig
                {
                    MiniGameId = "baking",
                    Duration = 25f,
                    ThresholdS = 45, ThresholdA = 30, ThresholdB = 20,
                    BaseReward = 130,
                    BoostDuration = 1800f
                },
                new MiniGameConfig
                {
                    MiniGameId = "order_rush",
                    Duration = 30f,
                    ThresholdS = 60, ThresholdA = 40, ThresholdB = 25,
                    BaseReward = 150,
                    BoostDuration = 1800f
                },
                new MiniGameConfig
                {
                    MiniGameId = "shelf_sort",
                    Duration = 20f,
                    ThresholdS = 45, ThresholdA = 30, ThresholdB = 18,
                    BaseReward = 110,
                    BoostDuration = 1800f
                },
                new MiniGameConfig
                {
                    MiniGameId = "logistics",
                    Duration = 25f,
                    ThresholdS = 90, ThresholdA = 77, ThresholdB = 63,
                    BaseReward = 140,
                    BoostDuration = 1800f
                }
            };
        }

        // =====================================================================
        // IMiniGameManager — Sorgu Metodlari
        // =====================================================================

        public bool CanPlay(string miniGameId)
        {
            // Cooldown kontrolu
            if (DateTime.UtcNow < _cooldownEndTime)
                return false;

            // Aktif oyun kontrolu
            if (IsPlaying)
                return false;

            // Gunluk limit kontrolu
            if (!_playData.TryGetValue(miniGameId, out var data))
                return false;

            // Gun sifirlama kontrolu
            CheckDailyReset(data);

            return data.DailyPlaysUsed < DAILY_PLAY_LIMIT;
        }

        public int GetRemainingPlays(string miniGameId)
        {
            if (!_playData.TryGetValue(miniGameId, out var data))
                return 0;

            CheckDailyReset(data);
            return Mathf.Max(0, DAILY_PLAY_LIMIT - data.DailyPlaysUsed);
        }

        public DateTime GetCooldownEndTime()
        {
            return _cooldownEndTime;
        }

        public int GetHighScore(string miniGameId)
        {
            return _playData.TryGetValue(miniGameId, out var data) ? data.HighScore : 0;
        }

        public IReadOnlyList<string> GetAllMiniGameIds()
        {
            var ids = new List<string>();
            foreach (var config in _configs)
            {
                ids.Add(config.MiniGameId);
            }
            return ids.AsReadOnly();
        }

        public MiniGameConfig GetConfig(string miniGameId)
        {
            return _configs.Find(c => c.MiniGameId == miniGameId);
        }

        // =====================================================================
        // IMiniGameManager — Oyun Baslatma
        // =====================================================================

        public bool StartMiniGame(string miniGameId)
        {
            if (!CanPlay(miniGameId))
            {
                Debug.LogWarning($"[MiniGameManager] {miniGameId} oynanamaz. " +
                    $"Limit veya cooldown aktif.");

                if (FeedbackManager.Instance != null)
                    FeedbackManager.Instance.PlayError();

                return false;
            }

            var config = GetConfig(miniGameId);
            if (config == null)
            {
                Debug.LogError($"[MiniGameManager] {miniGameId} konfigurasyonu bulunamadi.");
                return false;
            }

            // Mini-game prefab'ini sahneye ekle
            var miniGame = CreateMiniGameInstance(miniGameId);
            if (miniGame == null)
            {
                Debug.LogError($"[MiniGameManager] {miniGameId} instance olusturulamadi.");
                return false;
            }

            _activeMiniGame = miniGame;

            // Tamamlanma callback'ini dinle
            _activeMiniGame.OnGameCompleted += OnMiniGameCompleted;

            // Initialize ve baslat
            _activeMiniGame.Initialize(config);
            _activeMiniGame.StartGame();

            // Gunluk kullanimi artir
            var data = _playData[miniGameId];
            data.DailyPlaysUsed++;

            // Feedback
            if (FeedbackManager.Instance != null)
                FeedbackManager.Instance.PlayButtonClick();

            Debug.Log($"[MiniGameManager] {miniGameId} basladi. " +
                $"Kalan hak: {DAILY_PLAY_LIMIT - data.DailyPlaysUsed}");

            return true;
        }

        // =====================================================================
        // Mini-Game Tamamlanma
        // =====================================================================

        private void OnMiniGameCompleted(MiniGameGrade grade, int score)
        {
            if (_activeMiniGame == null) return;

            var miniGameId = _activeMiniGame.MiniGameId;
            var config = GetConfig(miniGameId);

            // Odul hesapla
            float gradeMultiplier = GRADE_MULTIPLIERS.TryGetValue(grade, out float mult) ? mult : 1f;
            int coinReward = Mathf.RoundToInt((config?.BaseReward ?? 100) * gradeMultiplier);
            float boostDuration = BOOST_DURATION_MULTIPLIERS.TryGetValue(grade, out float dur) ? dur : 300f;

            // En yuksek skoru guncelle
            if (_playData.TryGetValue(miniGameId, out var data))
            {
                if (score > data.HighScore)
                {
                    data.HighScore = score;
                    data.BestGrade = grade;
                }
            }

            // MiniGameCompletedEvent firlat
            _eventManager?.Publish(new MiniGameCompletedEvent
            {
                MiniGameId = miniGameId,
                Grade = grade,
                Score = score,
                BonusMultiplier = gradeMultiplier,
                CoinReward = coinReward,
                BoostDuration = boostDuration
            });

            // Cooldown baslat
            _cooldownEndTime = DateTime.UtcNow.AddSeconds(COOLDOWN_SECONDS);

            // Sonuc popup'ini goster
            ShowResultPopup(miniGameId, grade, score, coinReward, boostDuration);

            // Callback'i kaldir ve mini-game'i temizle
            _activeMiniGame.OnGameCompleted -= OnMiniGameCompleted;

            Debug.Log($"[MiniGameManager] {miniGameId} tamamlandi. " +
                $"Grade: {grade}, Score: {score}, Odul: {coinReward} coin, " +
                $"Boost: {boostDuration}s");
        }

        // =====================================================================
        // Sonuc Popup
        // =====================================================================

        private void ShowResultPopup(string miniGameId, MiniGameGrade grade, int score,
            int coinReward, float boostDuration)
        {
            var uiManager = RiceFactory.UI.UIManager.Instance;
            if (uiManager == null) return;

            // Popup data'sini hazirla
            var popupData = new MiniGameResultData
            {
                MiniGameId = miniGameId,
                Grade = grade,
                Score = score,
                CoinReward = coinReward,
                BoostDuration = boostDuration,
                CanReplay = CanPlay(miniGameId)
            };

            uiManager.ShowPopup<RiceFactory.UI.MiniGameResultPopup>(popupData);
        }

        // =====================================================================
        // Mini-Game Factory
        // =====================================================================

        /// <summary>
        /// Mini-game ID'sine gore uygun prefab'i yukler ve sahneye ekler.
        /// Resources/MiniGames/ altindan yukler.
        /// </summary>
        private MiniGameBase CreateMiniGameInstance(string miniGameId)
        {
            // Prefab yolu: Resources/MiniGames/{PrefabName}
            string prefabName = miniGameId switch
            {
                "harvest" => "HarvestMiniGame",
                "quality_control" => "QualityControlMiniGame",
                "baking" => "BakingMiniGame",
                "order_rush" => "OrderRushMiniGame",
                "shelf_sort" => "ShelfSortMiniGame",
                "logistics" => "LogisticsMiniGame",
                _ => null
            };

            if (string.IsNullOrEmpty(prefabName))
            {
                Debug.LogError($"[MiniGameManager] Bilinmeyen mini-game: {miniGameId}");
                return null;
            }

            var prefab = Resources.Load<MiniGameBase>($"MiniGames/{prefabName}");
            if (prefab == null)
            {
                Debug.LogError($"[MiniGameManager] Prefab bulunamadi: MiniGames/{prefabName}");
                return null;
            }

            // Canvas'a ekle
            var canvas = FindObjectOfType<Canvas>();
            var instance = Instantiate(prefab, canvas != null ? canvas.transform : transform);
            return instance;
        }

        // =====================================================================
        // Gunluk Sifirlama
        // =====================================================================

        /// <summary>
        /// Gun degismisse gunluk oynama sayacini sifirlar.
        /// </summary>
        private void CheckDailyReset(MiniGamePlayData data)
        {
            var today = DateTime.UtcNow.Date;
            if (data.LastResetDate < today)
            {
                data.DailyPlaysUsed = 0;
                data.LastResetDate = today;
            }
        }

        // =====================================================================
        // Aktif Mini-Game Temizleme
        // =====================================================================

        /// <summary>
        /// Aktif mini-game'i temizler. Sahne gecislerinde cagirilir.
        /// </summary>
        public void CleanupActiveMiniGame()
        {
            if (_activeMiniGame != null)
            {
                _activeMiniGame.OnGameCompleted -= OnMiniGameCompleted;

                if (_activeMiniGame.gameObject != null)
                    Destroy(_activeMiniGame.gameObject);

                _activeMiniGame = null;
            }
        }
    }

    // -------------------------------------------------------------------------
    // Sonuc Popup Data
    // -------------------------------------------------------------------------

    /// <summary>
    /// MiniGameResultPopup'a gonderilen veri sinifi.
    /// </summary>
    [Serializable]
    public class MiniGameResultData
    {
        public string MiniGameId;
        public MiniGameGrade Grade;
        public int Score;
        public int CoinReward;
        public float BoostDuration;
        public bool CanReplay;
    }
}
