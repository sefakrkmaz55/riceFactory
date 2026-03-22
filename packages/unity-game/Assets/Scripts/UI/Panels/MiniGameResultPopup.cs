// =============================================================================
// MiniGameResultPopup.cs
// Mini-Game Sonuc Popup — Grade, skor, odul ve aksiyonlar.
// Grade gosterimi (S/A/B/C — buyuk, renkli), skor, kazanilan odul.
// "Tekrar Oyna", "Kapat", "Reklam izle → 2x odul" butonlari.
// =============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RiceFactory.Core;
using RiceFactory.Core.Events;
using RiceFactory.MiniGames;

namespace RiceFactory.UI
{
    /// <summary>
    /// Mini-game sonuc popup'i. Oyun bittiginde gosterilir.
    /// - Grade (S/A/B/C) — buyuk, renkli gosterim
    /// - Skor
    /// - Kazanilan odul (coin + uretim boost suresi)
    /// - "Tekrar Oyna" butonu (hak varsa)
    /// - "Kapat" butonu
    /// - "Reklam izle → 2x odul" butonu
    /// </summary>
    public class MiniGameResultPopup : PopupBase
    {
        // =====================================================================
        // Grade Renkleri
        // =====================================================================

        private static readonly Color COLOR_GRADE_S = new(1f, 0.84f, 0f, 1f);       // Altin
        private static readonly Color COLOR_GRADE_A = new(0.24f, 0.7f, 0.95f, 1f);  // Mavi
        private static readonly Color COLOR_GRADE_B = new(0.56f, 0.79f, 0.28f, 1f); // Yesil
        private static readonly Color COLOR_GRADE_C = new(0.6f, 0.6f, 0.6f, 1f);    // Gri

        // =====================================================================
        // UI Referanslari
        // =====================================================================

        [Header("Sonuc Gosterimi")]
        [SerializeField] private TextMeshProUGUI _gradeText;         // Buyuk grade harfi
        [SerializeField] private TextMeshProUGUI _gradeSubText;      // Grade aciklamasi
        [SerializeField] private TextMeshProUGUI _scoreText;         // Skor
        [SerializeField] private TextMeshProUGUI _rewardCoinText;    // Kazanilan coin
        [SerializeField] private TextMeshProUGUI _rewardBoostText;   // Boost suresi

        [Header("Butonlar")]
        [SerializeField] private Button _replayButton;               // Tekrar Oyna
        [SerializeField] private Button _closeButton;                // Kapat
        [SerializeField] private Button _watchAdButton;              // Reklam izle → 2x odul
        [SerializeField] private TextMeshProUGUI _replayButtonText;
        [SerializeField] private TextMeshProUGUI _watchAdButtonText;

        // =====================================================================
        // Veri
        // =====================================================================

        private MiniGameResultData _data;

        // =====================================================================
        // PopupBase Override
        // =====================================================================

        protected override void OnInitialize(object data)
        {
            _data = data as MiniGameResultData;

            if (_data == null)
            {
                Debug.LogError("[MiniGameResultPopup] Gecersiz veri tipi.");
                return;
            }

            SetupGradeDisplay();
            SetupScoreDisplay();
            SetupRewardDisplay();
            SetupButtons();
        }

        // =====================================================================
        // Grade Gosterimi
        // =====================================================================

        private void SetupGradeDisplay()
        {
            if (_gradeText == null) return;

            string gradeLetter = _data.Grade.ToString();
            _gradeText.text = gradeLetter;
            _gradeText.fontSize = 96;
            _gradeText.fontStyle = TMPro.FontStyles.Bold;

            // Grade rengini ayarla
            Color gradeColor = _data.Grade switch
            {
                MiniGameGrade.S => COLOR_GRADE_S,
                MiniGameGrade.A => COLOR_GRADE_A,
                MiniGameGrade.B => COLOR_GRADE_B,
                _ => COLOR_GRADE_C
            };
            _gradeText.color = gradeColor;

            // Grade aciklamasi
            if (_gradeSubText != null)
            {
                _gradeSubText.text = _data.Grade switch
                {
                    MiniGameGrade.S => "Mukemmel!",
                    MiniGameGrade.A => "Harika!",
                    MiniGameGrade.B => "Iyi!",
                    _ => "Devam Et!"
                };
                _gradeSubText.color = gradeColor;
            }
        }

        // =====================================================================
        // Skor Gosterimi
        // =====================================================================

        private void SetupScoreDisplay()
        {
            if (_scoreText != null)
                _scoreText.text = $"Skor: {_data.Score}";
        }

        // =====================================================================
        // Odul Gosterimi
        // =====================================================================

        private void SetupRewardDisplay()
        {
            if (_rewardCoinText != null)
            {
                string formattedCoin = FeedbackManager.FormatNumber(_data.CoinReward);
                _rewardCoinText.text = $"+{formattedCoin} Coin";
                _rewardCoinText.color = new Color(1f, 0.84f, 0f, 1f); // Altin
            }

            if (_rewardBoostText != null)
            {
                int minutes = Mathf.RoundToInt(_data.BoostDuration / 60f);
                _rewardBoostText.text = $"Uretim Boost: {minutes} dk";
                _rewardBoostText.color = new Color(0.3f, 0.69f, 0.31f, 1f); // Yesil
            }
        }

        // =====================================================================
        // Buton Ayarlari
        // =====================================================================

        private void SetupButtons()
        {
            // Tekrar Oyna butonu
            if (_replayButton != null)
            {
                _replayButton.onClick.RemoveAllListeners();
                _replayButton.onClick.AddListener(OnReplayClicked);
                _replayButton.interactable = _data.CanReplay;

                if (_replayButtonText != null)
                {
                    _replayButtonText.text = _data.CanReplay
                        ? "Tekrar Oyna"
                        : "Hak Bitti";
                }
            }

            // Kapat butonu
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveAllListeners();
                _closeButton.onClick.AddListener(OnCloseClicked);
            }

            // Reklam izle butonu
            if (_watchAdButton != null)
            {
                _watchAdButton.onClick.RemoveAllListeners();
                _watchAdButton.onClick.AddListener(OnWatchAdClicked);

                if (_watchAdButtonText != null)
                    _watchAdButtonText.text = "Reklam Izle → 2x Odul";
            }
        }

        // =====================================================================
        // Buton Aksiyonlari
        // =====================================================================

        /// <summary>Tekrar Oyna butonuna tiklandiginda.</summary>
        private void OnReplayClicked()
        {
            if (FeedbackManager.Instance != null)
                FeedbackManager.Instance.PlayButtonClick();

            // Popup'i kapat ve mini-game'i tekrar baslat
            string miniGameId = _data.MiniGameId;
            Close();

            if (ServiceLocator.TryGet(out IMiniGameManager manager))
            {
                manager.StartMiniGame(miniGameId);
            }
        }

        /// <summary>Kapat butonuna tiklandiginda.</summary>
        private void OnCloseClicked()
        {
            if (FeedbackManager.Instance != null)
                FeedbackManager.Instance.PlayButtonClick();

            Close();
        }

        /// <summary>Reklam izle butonuna tiklandiginda.</summary>
        private void OnWatchAdClicked()
        {
            if (FeedbackManager.Instance != null)
                FeedbackManager.Instance.PlayButtonClick();

            // TODO: Reklam sistemi entegrasyonu
            // Reklam tamamlandiginda odulu 2x yap
            Debug.Log($"[MiniGameResultPopup] Reklam izlendi — " +
                $"Odul 2x: {_data.CoinReward * 2} coin");

            // Simdilik direkt 2x odulu ver ve butonu deaktif et
            Apply2xReward();
        }

        /// <summary>
        /// 2x odul uygular. Reklam tamamlandiktan sonra cagirilir.
        /// </summary>
        private void Apply2xReward()
        {
            int doubledReward = _data.CoinReward * 2;

            // Odul metnini guncelle
            if (_rewardCoinText != null)
            {
                string formatted = FeedbackManager.FormatNumber(doubledReward);
                _rewardCoinText.text = $"+{formatted} Coin (2x!)";
            }

            // Reklam butonunu deaktif et
            if (_watchAdButton != null)
                _watchAdButton.interactable = false;

            // Ek coin odulunu event olarak yayinla
            if (ServiceLocator.TryGet(out IEventManager eventManager))
            {
                eventManager.Publish(new CurrencyChangedEvent
                {
                    Type = CurrencyType.Coin,
                    OldAmount = 0, // Gercek deger CurrencySystem tarafindan yonetilir
                    NewAmount = _data.CoinReward, // Ek odul miktari (1x fazlasi)
                    Reason = "MiniGame_Ad_Bonus"
                });
            }

            if (FeedbackManager.Instance != null)
                FeedbackManager.Instance.PlayCoinEarned(doubledReward);
        }
    }
}
