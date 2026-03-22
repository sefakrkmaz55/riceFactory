// =============================================================================
// BakingMiniGame.cs
// Firincilk Mini-Game — Firin
// Sure: 25 saniye. Zamanlama oyunu — ekmek firinda, dogru anda cikar.
// Isi bari yukselir: yesil bolgede mukemmel (+10), sarida iyi (+5), kirmizida yanik (+1).
// Art arda 5 ekmek pisirme. Grade: S>=45, A>=30, B>=20, C>=0
// =============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RiceFactory.Core;
using RiceFactory.Core.Events;

namespace RiceFactory.MiniGames
{
    /// <summary>
    /// Firincilk mini-game. Isi bari surekli yukselir, dogru zamanda "Cikar" butonuna basarak
    /// ekmegi ideal noktada cikarmalsin.
    /// - Yesil bolge: mukemmel (+10 puan)
    /// - Sari bolge: iyi (+5 puan)
    /// - Kirmizi bolge: yanik (+1 puan)
    /// Toplam 5 ekmek pisirilir.
    /// </summary>
    public class BakingMiniGame : MiniGameBase
    {
        // =====================================================================
        // Sabitler
        // =====================================================================

        private const int TOTAL_BREADS = 5;                // Toplam ekmek sayisi
        private const float HEAT_SPEED_BASE = 0.6f;        // Isi artis hizi (0-1 arasi/saniye)
        private const float HEAT_SPEED_INCREMENT = 0.08f;   // Her ekmekte hiz artisi

        // Bolge sinirlari (0-1 arasi normalize edilmis)
        private const float GREEN_ZONE_MIN = 0.45f;         // Yesil bolge baslangici
        private const float GREEN_ZONE_MAX = 0.65f;         // Yesil bolge bitisi
        private const float YELLOW_ZONE_MIN = 0.30f;        // Sari bolge baslangici
        private const float YELLOW_ZONE_MAX = 0.80f;        // Sari bolge bitisi
        // Kirmizi: 0.80 - 1.0

        // Puanlama
        private const int PERFECT_POINTS = 10;              // Yesil — mukemmel
        private const int GOOD_POINTS = 5;                  // Sari — iyi
        private const int BURNT_POINTS = 1;                 // Kirmizi — yanik

        // Otomatik yanik suresi
        private const float AUTO_BURN_THRESHOLD = 1.0f;     // Bari tamamen doldugunda otomatik cikar

        // =====================================================================
        // UI Referanslari
        // =====================================================================

        [Header("Firincilk Mini-Game UI")]
        [SerializeField] private RectTransform _heatBar;       // Isi bari arka plan
        [SerializeField] private Image _heatFillImage;         // Isi bari dolgu
        [SerializeField] private RectTransform _greenZone;     // Yesil bolge gorseli
        [SerializeField] private RectTransform _yellowZone;    // Sari bolge gorseli
        [SerializeField] private Button _extractButton;        // "Cikar" butonu
        [SerializeField] private TextMeshProUGUI _scoreText;
        [SerializeField] private TextMeshProUGUI _timerText;
        [SerializeField] private TextMeshProUGUI _breadCountText; // "Ekmek 1/5"
        [SerializeField] private TextMeshProUGUI _resultText;     // "Mukemmel!", "Iyi", "Yanik"

        // =====================================================================
        // Oyun Durumu
        // =====================================================================

        private float _heatLevel;        // Mevcut isi seviyesi (0-1)
        private float _heatSpeed;        // Mevcut isi artis hizi
        private int _breadsBaked;        // Pisirilmis ekmek sayisi
        private bool _isHeating;         // Isi bari aktif mi?
        private bool _waitingForNext;    // Sonraki ekmek icin bekleniyor mu?
        private float _waitTimer;        // Bekleme zamanlayicisi

        // =====================================================================
        // MiniGameBase Overrides
        // =====================================================================

        protected override void OnInitialize(MiniGameConfig config)
        {
            if (_scoreText != null) _scoreText.text = "0";
            if (_timerText != null) _timerText.text = config.Duration.ToString("F0");
            if (_breadCountText != null) _breadCountText.text = $"Ekmek 0/{TOTAL_BREADS}";
            if (_resultText != null) _resultText.gameObject.SetActive(false);

            _heatLevel = 0f;
            _heatSpeed = HEAT_SPEED_BASE;
            _breadsBaked = 0;
            _isHeating = false;
            _waitingForNext = false;

            // Buton baglantisi
            if (_extractButton != null)
            {
                _extractButton.onClick.RemoveAllListeners();
                _extractButton.onClick.AddListener(OnExtractPressed);
            }

            UpdateHeatBarVisual();
        }

        protected override void OnGameStart()
        {
            StartNextBread();
        }

        protected override void OnGameUpdate(float deltaTime)
        {
            // Zamanlayici
            if (_timerText != null)
                _timerText.text = Mathf.CeilToInt(_timeRemaining).ToString();

            if (_waitingForNext)
            {
                // Sonraki ekmek icin kisa bekleme
                _waitTimer -= deltaTime;
                if (_waitTimer <= 0f)
                {
                    _waitingForNext = false;
                    StartNextBread();
                }
                return;
            }

            if (!_isHeating) return;

            // Isi barini artir
            _heatLevel += _heatSpeed * deltaTime;
            _heatLevel = Mathf.Clamp01(_heatLevel);

            UpdateHeatBarVisual();

            // Otomatik yanik — bar tamamen dolduysa
            if (_heatLevel >= AUTO_BURN_THRESHOLD)
            {
                ExtractBread();
            }
        }

        protected override void OnGameEnd(MiniGameGrade grade, int score)
        {
            _isHeating = false;
            _waitingForNext = false;

            if (_extractButton != null)
                _extractButton.interactable = false;
        }

        protected override void OnScoreChanged(int newScore)
        {
            if (_scoreText != null)
                _scoreText.text = newScore.ToString();
        }

        // =====================================================================
        // Ekmek Yonetimi
        // =====================================================================

        /// <summary>Sonraki ekmegi pisirmeye basla.</summary>
        private void StartNextBread()
        {
            if (_breadsBaked >= TOTAL_BREADS)
            {
                // Tum ekmekler pisirildi — oyunu bitir
                var grade = CalculateGrade(_score);
                EndGame(grade, _score);
                return;
            }

            _heatLevel = 0f;
            _heatSpeed = HEAT_SPEED_BASE + (_breadsBaked * HEAT_SPEED_INCREMENT);
            _isHeating = true;

            UpdateHeatBarVisual();

            if (_extractButton != null)
                _extractButton.interactable = true;

            if (_resultText != null)
                _resultText.gameObject.SetActive(false);
        }

        /// <summary>"Cikar" butonuna basildiginda.</summary>
        private void OnExtractPressed()
        {
            if (!_isHeating || _state != MiniGameState.Playing) return;
            ExtractBread();
        }

        /// <summary>Ekmegi cikarir ve puanlandirir.</summary>
        private void ExtractBread()
        {
            _isHeating = false;

            // Puanlama — hangi bolgede cikarildi?
            int points;
            string resultMessage;
            Color resultColor;

            if (_heatLevel >= GREEN_ZONE_MIN && _heatLevel <= GREEN_ZONE_MAX)
            {
                // Mukemmel — yesil bolge
                points = PERFECT_POINTS;
                resultMessage = "Mukemmel!";
                resultColor = new Color(0.2f, 0.8f, 0.2f, 1f);

                if (FeedbackManager.Instance != null)
                    FeedbackManager.Instance.PlayCoinEarned(points);
            }
            else if (_heatLevel >= YELLOW_ZONE_MIN && _heatLevel <= YELLOW_ZONE_MAX)
            {
                // Iyi — sari bolge
                points = GOOD_POINTS;
                resultMessage = "Iyi!";
                resultColor = new Color(1f, 0.8f, 0f, 1f);

                if (FeedbackManager.Instance != null)
                    FeedbackManager.Instance.PlayButtonClick();
            }
            else
            {
                // Yanik veya cig — kirmizi bolge
                points = BURNT_POINTS;
                resultMessage = _heatLevel >= YELLOW_ZONE_MAX ? "Yanik!" : "Cig!";
                resultColor = new Color(0.9f, 0.2f, 0.2f, 1f);

                if (FeedbackManager.Instance != null)
                    FeedbackManager.Instance.PlayError();
            }

            AddScore(points);
            _breadsBaked++;

            // Sonuc metni goster
            if (_resultText != null)
            {
                _resultText.gameObject.SetActive(true);
                _resultText.text = $"{resultMessage} +{points}";
                _resultText.color = resultColor;
            }

            // Ekmek sayacini guncelle
            if (_breadCountText != null)
                _breadCountText.text = $"Ekmek {_breadsBaked}/{TOTAL_BREADS}";

            // Buton deaktif
            if (_extractButton != null)
                _extractButton.interactable = false;

            // 5 ekmek pisirilmisse ve sure bitmemisse sonraki ekmege gec
            if (_breadsBaked < TOTAL_BREADS)
            {
                _waitingForNext = true;
                _waitTimer = 0.8f; // 0.8 saniye bekleme
            }
            else
            {
                // Tum ekmekler pisirildi
                var grade = CalculateGrade(_score);
                EndGame(grade, _score);
            }
        }

        // =====================================================================
        // Isi Bari Gorseli
        // =====================================================================

        private void UpdateHeatBarVisual()
        {
            if (_heatFillImage == null) return;

            _heatFillImage.fillAmount = _heatLevel;

            // Renk degisimi — bolgelerine gore
            if (_heatLevel >= GREEN_ZONE_MIN && _heatLevel <= GREEN_ZONE_MAX)
                _heatFillImage.color = new Color(0.2f, 0.8f, 0.2f, 1f);    // Yesil
            else if (_heatLevel >= YELLOW_ZONE_MIN && _heatLevel <= YELLOW_ZONE_MAX)
                _heatFillImage.color = new Color(1f, 0.8f, 0f, 1f);         // Sari
            else if (_heatLevel > YELLOW_ZONE_MAX)
                _heatFillImage.color = new Color(0.9f, 0.2f, 0.2f, 1f);     // Kirmizi
            else
                _heatFillImage.color = new Color(0.5f, 0.5f, 0.5f, 1f);     // Gri (dusuk)
        }
    }
}
