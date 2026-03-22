// =============================================================================
// MiniGamePanel.cs
// Mini-Game Secim Paneli — 6 mini-game karti gosterir.
// Her kart: ikon + isim + en yuksek skor + kalan hak.
// Kilitli mini-game'ler (tesis acilmamissa), cooldown gostergesi.
// =============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RiceFactory.Core;
using RiceFactory.MiniGames;

namespace RiceFactory.UI
{
    /// <summary>
    /// Mini-game secim paneli. 6 mini-game kartini listeler.
    /// Oyuncu kartlara tiklayarak mini-game baslatir.
    /// Cooldown gostergesi ve kilitli oyun destegi vardir.
    /// </summary>
    public class MiniGamePanel : PanelBase
    {
        // =====================================================================
        // Kart Bilgileri
        // =====================================================================

        /// <summary>Mini-game kart verileri — Inspector'da atanir.</summary>
        [Serializable]
        public class MiniGameCardData
        {
            public string MiniGameId;
            public string DisplayName;
            public string FacilityName;       // Bagla oldugu tesis
            public Sprite Icon;
            public Color CardColor = Color.white;
        }

        // =====================================================================
        // UI Referanslari
        // =====================================================================

        [Header("Panel Ayarlari")]
        [SerializeField] private Transform _cardContainer;         // Kartlarin yerlestirilecegi alan
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _cooldownText;    // Cooldown gostergesi

        [Header("Mini-Game Kartlari")]
        [SerializeField] private List<MiniGameCardData> _cardDataList = new();

        // =====================================================================
        // Calisma Zamani
        // =====================================================================

        private IMiniGameManager _miniGameManager;
        private readonly List<MiniGameCardUI> _cards = new();

        // Cooldown guncelleme zamanlayicisi
        private float _cooldownUpdateTimer;

        // =====================================================================
        // PanelBase Overrides
        // =====================================================================

        protected override void OnInitialize()
        {
            if (_titleText != null)
                _titleText.text = "Mini-Game'ler";

            // Varsayilan kart verileri (Inspector'da atanmadiysa)
            if (_cardDataList.Count == 0)
            {
                _cardDataList = new List<MiniGameCardData>
                {
                    new() { MiniGameId = "harvest", DisplayName = "Hasat Kosusu", FacilityName = "Tarla", CardColor = new Color(0.56f, 0.79f, 0.28f) },
                    new() { MiniGameId = "quality_control", DisplayName = "Kalite Kontrol", FacilityName = "Fabrika", CardColor = new Color(0.38f, 0.65f, 0.87f) },
                    new() { MiniGameId = "baking", DisplayName = "Firin Zamanlama", FacilityName = "Firin", CardColor = new Color(0.82f, 0.62f, 0.37f) },
                    new() { MiniGameId = "order_rush", DisplayName = "Siparis Hizi", FacilityName = "Restoran", CardColor = new Color(0.95f, 0.46f, 0.19f) },
                    new() { MiniGameId = "shelf_sort", DisplayName = "Raf Duzeni", FacilityName = "Market", CardColor = new Color(0.55f, 0.43f, 0.75f) },
                    new() { MiniGameId = "logistics", DisplayName = "Lojistik Rota", FacilityName = "Dagitim", CardColor = new Color(0.3f, 0.69f, 0.84f) }
                };
            }

            CreateCards();
        }

        protected override void OnShow()
        {
            // MiniGameManager referansini al
            if (ServiceLocator.TryGet(out IMiniGameManager mgr))
                _miniGameManager = mgr;

            RefreshAllCards();
        }

        // =====================================================================
        // Update — Cooldown Gosterge Guncellemesi
        // =====================================================================

        private void Update()
        {
            if (_miniGameManager == null) return;

            _cooldownUpdateTimer -= Time.deltaTime;
            if (_cooldownUpdateTimer <= 0f)
            {
                _cooldownUpdateTimer = 0.5f; // Yarim saniyede bir guncelle
                UpdateCooldownDisplay();
            }
        }

        // =====================================================================
        // Kart Olusturma
        // =====================================================================

        private void CreateCards()
        {
            if (_cardContainer == null) return;

            foreach (var cardData in _cardDataList)
            {
                var cardUI = CreateCardUI(cardData);
                _cards.Add(cardUI);
            }
        }

        /// <summary>
        /// Tek bir mini-game karti olusturur.
        /// </summary>
        private MiniGameCardUI CreateCardUI(MiniGameCardData data)
        {
            // Kart ana nesnesi
            var cardObj = new GameObject($"Card_{data.MiniGameId}");
            cardObj.transform.SetParent(_cardContainer, false);

            var cardRect = cardObj.AddComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(300f, 160f);

            // Arka plan
            var bgImage = cardObj.AddComponent<Image>();
            bgImage.color = data.CardColor;

            // Buton
            var button = cardObj.AddComponent<Button>();
            var miniGameId = data.MiniGameId; // Closure icin kopyala
            button.onClick.AddListener(() => OnCardClicked(miniGameId));

            // CanvasGroup (kilitli gorunum icin)
            var canvasGroup = cardObj.AddComponent<CanvasGroup>();

            // Ikon (sol taraf)
            var iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(cardObj.transform, false);
            var iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0f, 0.2f);
            iconRect.anchorMax = new Vector2(0.3f, 0.8f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;
            var iconImage = iconObj.AddComponent<Image>();
            iconImage.sprite = data.Icon;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;

            // Isim metni
            var nameObj = new GameObject("Name");
            nameObj.transform.SetParent(cardObj.transform, false);
            var nameRect = nameObj.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0.32f, 0.55f);
            nameRect.anchorMax = new Vector2(1f, 0.95f);
            nameRect.offsetMin = Vector2.zero;
            nameRect.offsetMax = Vector2.zero;
            var nameText = nameObj.AddComponent<TextMeshProUGUI>();
            nameText.text = data.DisplayName;
            nameText.fontSize = 22;
            nameText.fontStyle = TMPro.FontStyles.Bold;
            nameText.alignment = TMPro.TextAlignmentOptions.Left;
            nameText.color = Color.white;
            nameText.raycastTarget = false;

            // Tesis adi
            var facilityObj = new GameObject("Facility");
            facilityObj.transform.SetParent(cardObj.transform, false);
            var facilityRect = facilityObj.AddComponent<RectTransform>();
            facilityRect.anchorMin = new Vector2(0.32f, 0.35f);
            facilityRect.anchorMax = new Vector2(1f, 0.55f);
            facilityRect.offsetMin = Vector2.zero;
            facilityRect.offsetMax = Vector2.zero;
            var facilityText = facilityObj.AddComponent<TextMeshProUGUI>();
            facilityText.text = data.FacilityName;
            facilityText.fontSize = 16;
            facilityText.color = new Color(1f, 1f, 1f, 0.7f);
            facilityText.raycastTarget = false;

            // En yuksek skor
            var scoreObj = new GameObject("HighScore");
            scoreObj.transform.SetParent(cardObj.transform, false);
            var scoreRect = scoreObj.AddComponent<RectTransform>();
            scoreRect.anchorMin = new Vector2(0.32f, 0.05f);
            scoreRect.anchorMax = new Vector2(0.65f, 0.35f);
            scoreRect.offsetMin = Vector2.zero;
            scoreRect.offsetMax = Vector2.zero;
            var scoreText = scoreObj.AddComponent<TextMeshProUGUI>();
            scoreText.text = "En Yuksek: 0";
            scoreText.fontSize = 14;
            scoreText.color = new Color(1f, 1f, 1f, 0.6f);
            scoreText.raycastTarget = false;

            // Kalan hak
            var playsObj = new GameObject("RemainingPlays");
            playsObj.transform.SetParent(cardObj.transform, false);
            var playsRect = playsObj.AddComponent<RectTransform>();
            playsRect.anchorMin = new Vector2(0.65f, 0.05f);
            playsRect.anchorMax = new Vector2(1f, 0.35f);
            playsRect.offsetMin = Vector2.zero;
            playsRect.offsetMax = Vector2.zero;
            var playsText = playsObj.AddComponent<TextMeshProUGUI>();
            playsText.text = "Hak: 3";
            playsText.fontSize = 14;
            playsText.alignment = TMPro.TextAlignmentOptions.Right;
            playsText.color = new Color(1f, 1f, 1f, 0.6f);
            playsText.raycastTarget = false;

            // Kilit gorseli (baslangicta gizli)
            var lockObj = new GameObject("Lock");
            lockObj.transform.SetParent(cardObj.transform, false);
            var lockRect = lockObj.AddComponent<RectTransform>();
            lockRect.anchorMin = Vector2.zero;
            lockRect.anchorMax = Vector2.one;
            lockRect.offsetMin = Vector2.zero;
            lockRect.offsetMax = Vector2.zero;
            var lockImage = lockObj.AddComponent<Image>();
            lockImage.color = new Color(0f, 0f, 0f, 0.6f);
            lockImage.raycastTarget = false;
            var lockLabelObj = new GameObject("LockLabel");
            lockLabelObj.transform.SetParent(lockObj.transform, false);
            var lockLabelRect = lockLabelObj.AddComponent<RectTransform>();
            lockLabelRect.anchorMin = new Vector2(0.2f, 0.3f);
            lockLabelRect.anchorMax = new Vector2(0.8f, 0.7f);
            lockLabelRect.offsetMin = Vector2.zero;
            lockLabelRect.offsetMax = Vector2.zero;
            var lockLabel = lockLabelObj.AddComponent<TextMeshProUGUI>();
            lockLabel.text = "KILITLI";
            lockLabel.fontSize = 24;
            lockLabel.fontStyle = TMPro.FontStyles.Bold;
            lockLabel.alignment = TMPro.TextAlignmentOptions.Center;
            lockLabel.color = new Color(1f, 1f, 1f, 0.8f);
            lockLabel.raycastTarget = false;
            lockObj.SetActive(false); // Baslangicta acik

            return new MiniGameCardUI
            {
                MiniGameId = data.MiniGameId,
                CardObject = cardObj,
                Button = button,
                CanvasGroup = canvasGroup,
                HighScoreText = scoreText,
                RemainingPlaysText = playsText,
                LockOverlay = lockObj
            };
        }

        // =====================================================================
        // Kart Guncelleme
        // =====================================================================

        private void RefreshAllCards()
        {
            foreach (var card in _cards)
            {
                RefreshCard(card);
            }
            UpdateCooldownDisplay();
        }

        private void RefreshCard(MiniGameCardUI card)
        {
            if (_miniGameManager == null) return;

            int highScore = _miniGameManager.GetHighScore(card.MiniGameId);
            int remaining = _miniGameManager.GetRemainingPlays(card.MiniGameId);
            bool canPlay = _miniGameManager.CanPlay(card.MiniGameId);

            // Skorları guncelle
            if (card.HighScoreText != null)
                card.HighScoreText.text = $"En Yuksek: {highScore}";

            if (card.RemainingPlaysText != null)
                card.RemainingPlaysText.text = $"Hak: {remaining}";

            // Buton durumu
            if (card.Button != null)
                card.Button.interactable = canPlay;

            // Oynanamazsa solgunlastir
            if (card.CanvasGroup != null)
                card.CanvasGroup.alpha = canPlay ? 1f : 0.5f;

            // TODO: Tesis acik/kapali kontrolu — kilitli gosterim
            // Simdilik tum kartlar acik
            if (card.LockOverlay != null)
                card.LockOverlay.SetActive(false);
        }

        // =====================================================================
        // Cooldown Gosterge
        // =====================================================================

        private void UpdateCooldownDisplay()
        {
            if (_cooldownText == null || _miniGameManager == null) return;

            var cooldownEnd = _miniGameManager.GetCooldownEndTime();
            var remaining = cooldownEnd - DateTime.UtcNow;

            if (remaining.TotalSeconds > 0)
            {
                int minutes = (int)remaining.TotalMinutes;
                int seconds = remaining.Seconds;
                _cooldownText.text = $"Sonraki mini-game: {minutes}:{seconds:D2}";
                _cooldownText.gameObject.SetActive(true);
            }
            else
            {
                _cooldownText.gameObject.SetActive(false);
            }
        }

        // =====================================================================
        // Kart Tiklama
        // =====================================================================

        private void OnCardClicked(string miniGameId)
        {
            if (_miniGameManager == null)
            {
                Debug.LogWarning("[MiniGamePanel] MiniGameManager bulunamadi.");
                return;
            }

            if (!_miniGameManager.CanPlay(miniGameId))
            {
                if (FeedbackManager.Instance != null)
                    FeedbackManager.Instance.PlayError();
                return;
            }

            // Mini-game'i baslat
            bool started = _miniGameManager.StartMiniGame(miniGameId);

            if (started)
            {
                // Paneli gizle — mini-game Canvas uzerinde gosterilecek
                Hide();
            }
        }

        // =====================================================================
        // Kart UI Sinifi
        // =====================================================================

        private class MiniGameCardUI
        {
            public string MiniGameId;
            public GameObject CardObject;
            public Button Button;
            public CanvasGroup CanvasGroup;
            public TextMeshProUGUI HighScoreText;
            public TextMeshProUGUI RemainingPlaysText;
            public GameObject LockOverlay;
        }
    }
}
