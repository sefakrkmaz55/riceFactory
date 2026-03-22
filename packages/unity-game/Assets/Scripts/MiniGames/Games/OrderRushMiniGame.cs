// =============================================================================
// OrderRushMiniGame.cs
// Siparis Hizi Mini-Game — Restoran
// Sure: 30 saniye. Musteri siparisleri gelir, dogru malzemeleri sirayla sec.
// 4 malzeme butonu: pirinc, ekmek, et, sebze.
// Dogru sirada basilirsa +5, yanlis basarsa siparis sifirlanir (-2).
// Grade: S>=60, A>=40, B>=25, C>=0
// =============================================================================

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RiceFactory.Core;
using RiceFactory.Core.Events;

namespace RiceFactory.MiniGames
{
    /// <summary>
    /// Siparis Hizi mini-game. Musteri siparis kartinda malzeme kombinasyonu gosterilir.
    /// Oyuncu dogru sirada malzeme butonlarina basarak siparisi tamamlar.
    /// - Dogru sira: +5 puan
    /// - Yanlis basma: siparis sifirlanir, -2 puan
    /// </summary>
    public class OrderRushMiniGame : MiniGameBase
    {
        // =====================================================================
        // Sabitler
        // =====================================================================

        private const int CORRECT_ORDER_POINTS = 5;       // Dogru siparis tamamlama puani
        private const int WRONG_PENALTY = 2;               // Yanlis basma cezasi
        private const int MIN_INGREDIENTS = 2;             // Minimum malzeme sayisi
        private const int MAX_INGREDIENTS = 4;             // Maksimum malzeme sayisi

        // =====================================================================
        // Malzeme Tanimlari
        // =====================================================================

        /// <summary>Malzeme tipleri.</summary>
        public enum IngredientType
        {
            Rice,       // Pirinc
            Bread,      // Ekmek
            Meat,       // Et
            Vegetable   // Sebze
        }

        /// <summary>Malzeme gorsel bilgileri.</summary>
        private static readonly Dictionary<IngredientType, string> INGREDIENT_NAMES = new()
        {
            { IngredientType.Rice, "Pirinc" },
            { IngredientType.Bread, "Ekmek" },
            { IngredientType.Meat, "Et" },
            { IngredientType.Vegetable, "Sebze" }
        };

        private static readonly Dictionary<IngredientType, Color> INGREDIENT_COLORS = new()
        {
            { IngredientType.Rice, new Color(1f, 1f, 0.8f, 1f) },       // Acik sari
            { IngredientType.Bread, new Color(0.82f, 0.62f, 0.37f, 1f) }, // Kahverengi
            { IngredientType.Meat, new Color(0.8f, 0.2f, 0.2f, 1f) },    // Kirmizi
            { IngredientType.Vegetable, new Color(0.2f, 0.7f, 0.3f, 1f) } // Yesil
        };

        // =====================================================================
        // UI Referanslari
        // =====================================================================

        [Header("Siparis Hizi UI")]
        [SerializeField] private TextMeshProUGUI _scoreText;
        [SerializeField] private TextMeshProUGUI _timerText;
        [SerializeField] private TextMeshProUGUI _orderText;         // Siparis karti metni
        [SerializeField] private RectTransform _orderCardArea;       // Siparis karti alani
        [SerializeField] private TextMeshProUGUI _feedbackText;      // Anlik geri bildirim
        [SerializeField] private TextMeshProUGUI _progressText;      // "2/4 malzeme"

        [Header("Malzeme Butonlari")]
        [SerializeField] private Button _riceButton;
        [SerializeField] private Button _breadButton;
        [SerializeField] private Button _meatButton;
        [SerializeField] private Button _vegetableButton;

        // =====================================================================
        // Oyun Durumu
        // =====================================================================

        // Mevcut siparis
        private readonly List<IngredientType> _currentOrder = new();
        private int _currentIngredientIndex;      // Sipariste kacinci malzemedeyiz
        private int _ordersCompleted;              // Tamamlanan siparis sayisi

        // Tum malzeme tipleri — rastgele secim icin
        private static readonly IngredientType[] ALL_INGREDIENTS =
        {
            IngredientType.Rice,
            IngredientType.Bread,
            IngredientType.Meat,
            IngredientType.Vegetable
        };

        // =====================================================================
        // MiniGameBase Overrides
        // =====================================================================

        protected override void OnInitialize(MiniGameConfig config)
        {
            if (_scoreText != null) _scoreText.text = "0";
            if (_timerText != null) _timerText.text = config.Duration.ToString("F0");
            if (_feedbackText != null) _feedbackText.gameObject.SetActive(false);
            if (_progressText != null) _progressText.text = "";

            _currentOrder.Clear();
            _currentIngredientIndex = 0;
            _ordersCompleted = 0;

            // Butonlari bagla
            SetupButtons();
        }

        protected override void OnGameStart()
        {
            GenerateNewOrder();
        }

        protected override void OnGameUpdate(float deltaTime)
        {
            if (_timerText != null)
                _timerText.text = Mathf.CeilToInt(_timeRemaining).ToString();
        }

        protected override void OnGameEnd(MiniGameGrade grade, int score)
        {
            // Butonlari deaktif et
            SetButtonsInteractable(false);
        }

        protected override void OnScoreChanged(int newScore)
        {
            if (_scoreText != null)
                _scoreText.text = newScore.ToString();
        }

        // =====================================================================
        // Buton Baglantisi
        // =====================================================================

        private void SetupButtons()
        {
            if (_riceButton != null)
            {
                _riceButton.onClick.RemoveAllListeners();
                _riceButton.onClick.AddListener(() => OnIngredientPressed(IngredientType.Rice));
            }
            if (_breadButton != null)
            {
                _breadButton.onClick.RemoveAllListeners();
                _breadButton.onClick.AddListener(() => OnIngredientPressed(IngredientType.Bread));
            }
            if (_meatButton != null)
            {
                _meatButton.onClick.RemoveAllListeners();
                _meatButton.onClick.AddListener(() => OnIngredientPressed(IngredientType.Meat));
            }
            if (_vegetableButton != null)
            {
                _vegetableButton.onClick.RemoveAllListeners();
                _vegetableButton.onClick.AddListener(() => OnIngredientPressed(IngredientType.Vegetable));
            }
        }

        private void SetButtonsInteractable(bool interactable)
        {
            if (_riceButton != null) _riceButton.interactable = interactable;
            if (_breadButton != null) _breadButton.interactable = interactable;
            if (_meatButton != null) _meatButton.interactable = interactable;
            if (_vegetableButton != null) _vegetableButton.interactable = interactable;
        }

        // =====================================================================
        // Siparis Olusturma
        // =====================================================================

        /// <summary>Yeni rastgele siparis olusturur.</summary>
        private void GenerateNewOrder()
        {
            _currentOrder.Clear();
            _currentIngredientIndex = 0;

            // Malzeme sayisi — ilerledikce artar
            int count = Mathf.Min(MIN_INGREDIENTS + (_ordersCompleted / 2), MAX_INGREDIENTS);

            for (int i = 0; i < count; i++)
            {
                var ingredient = ALL_INGREDIENTS[Random.Range(0, ALL_INGREDIENTS.Length)];
                _currentOrder.Add(ingredient);
            }

            // Siparis kartini guncelle
            UpdateOrderDisplay();
            UpdateProgressDisplay();

            SetButtonsInteractable(true);
        }

        // =====================================================================
        // Malzeme Butonu Isleme
        // =====================================================================

        /// <summary>Malzeme butonuna basildiginda.</summary>
        private void OnIngredientPressed(IngredientType ingredient)
        {
            if (_state != MiniGameState.Playing) return;
            if (_currentIngredientIndex >= _currentOrder.Count) return;

            var expectedIngredient = _currentOrder[_currentIngredientIndex];

            if (ingredient == expectedIngredient)
            {
                // Dogru malzeme
                _currentIngredientIndex++;
                UpdateOrderDisplay();
                UpdateProgressDisplay();

                if (FeedbackManager.Instance != null)
                    FeedbackManager.Instance.PlayButtonClick();

                // Siparis tamamlandi mi?
                if (_currentIngredientIndex >= _currentOrder.Count)
                {
                    OnOrderCompleted();
                }
            }
            else
            {
                // Yanlis malzeme — siparis sifirla
                OnOrderFailed();
            }
        }

        /// <summary>Siparis basariyla tamamlandiginda.</summary>
        private void OnOrderCompleted()
        {
            AddScore(CORRECT_ORDER_POINTS);
            _ordersCompleted++;

            ShowFeedback(true, $"+{CORRECT_ORDER_POINTS}");

            if (FeedbackManager.Instance != null)
                FeedbackManager.Instance.PlayCoinEarned(CORRECT_ORDER_POINTS);

            // Yeni siparis olustur
            GenerateNewOrder();
        }

        /// <summary>Yanlis malzeme secildiginde.</summary>
        private void OnOrderFailed()
        {
            SubtractScore(WRONG_PENALTY);
            _currentIngredientIndex = 0; // Siparisi bastan basla

            ShowFeedback(false, $"-{WRONG_PENALTY}");
            UpdateOrderDisplay();
            UpdateProgressDisplay();

            if (FeedbackManager.Instance != null)
                FeedbackManager.Instance.PlayError();
        }

        // =====================================================================
        // Siparis Gosterimi
        // =====================================================================

        private void UpdateOrderDisplay()
        {
            if (_orderText == null) return;

            // Siparis kartini goster — tamamlanmis malzemeler farkli renkte
            var sb = new System.Text.StringBuilder();
            sb.Append("Siparis: ");

            for (int i = 0; i < _currentOrder.Count; i++)
            {
                var ingredientName = INGREDIENT_NAMES[_currentOrder[i]];

                if (i < _currentIngredientIndex)
                {
                    // Tamamlanmis — yesil ve ustunu cizili
                    sb.Append($"<color=#4CAF50><s>{ingredientName}</s></color>");
                }
                else if (i == _currentIngredientIndex)
                {
                    // Siradaki — kalin ve parlak
                    sb.Append($"<color=#FFEB3B><b>[{ingredientName}]</b></color>");
                }
                else
                {
                    // Bekleyen — soluk
                    sb.Append($"<color=#888888>{ingredientName}</color>");
                }

                if (i < _currentOrder.Count - 1)
                    sb.Append(" → ");
            }

            _orderText.text = sb.ToString();
        }

        private void UpdateProgressDisplay()
        {
            if (_progressText == null) return;
            _progressText.text = $"{_currentIngredientIndex}/{_currentOrder.Count} malzeme";
        }

        // =====================================================================
        // Geri Bildirim
        // =====================================================================

        private void ShowFeedback(bool positive, string message)
        {
            if (_feedbackText == null) return;

            _feedbackText.gameObject.SetActive(true);
            _feedbackText.text = message;
            _feedbackText.color = positive
                ? new Color(0.2f, 0.8f, 0.2f, 1f)
                : new Color(0.9f, 0.2f, 0.2f, 1f);

            CancelInvoke(nameof(HideFeedback));
            Invoke(nameof(HideFeedback), 0.6f);
        }

        private void HideFeedback()
        {
            if (_feedbackText != null)
                _feedbackText.gameObject.SetActive(false);
        }
    }
}
