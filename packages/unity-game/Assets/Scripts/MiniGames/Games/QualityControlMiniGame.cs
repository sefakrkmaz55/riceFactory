// =============================================================================
// QualityControlMiniGame.cs
// Kalite Kontrol Mini-Game — Fabrika
// Sure: 20 saniye. Konveyor bant uzerinde gelen urunleri sirala.
// Iyi urunler saga, kotu urunler sola kaydır.
// Grade: S>=40, A>=25, B>=15, C>=0
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
    /// Kalite Kontrol mini-game.
    /// Konveyor bant uzerinde gelen urunleri swipe ile sirala:
    /// - Iyi urun saga kaydır: +2 puan (dogru), -1 puan (yanlis)
    /// - Kotu urun sola kaydır: +3 puan (dogru), -2 puan (yanlis)
    /// </summary>
    public class QualityControlMiniGame : MiniGameBase
    {
        // =====================================================================
        // Sabitler
        // =====================================================================

        private const float PRODUCT_SPEED = 150f;             // Urun hareket hizi (piksel/sn)
        private const float SPAWN_INTERVAL = 1.5f;            // Urun spawn araligi (saniye)
        private const float BAD_PRODUCT_CHANCE = 0.35f;        // Kotu urun olasiligi (%35)
        private const float SWIPE_THRESHOLD = 50f;             // Minimum swipe mesafesi (piksel)
        private const int GOOD_CORRECT_POINTS = 2;             // Iyi urun dogru siralama puani
        private const int GOOD_WRONG_PENALTY = 1;              // Iyi urun yanlis siralama cezasi
        private const int BAD_CORRECT_POINTS = 3;              // Kotu urun dogru siralama puani
        private const int BAD_WRONG_PENALTY = 2;               // Kotu urun yanlis siralama cezasi

        // =====================================================================
        // UI Referanslari
        // =====================================================================

        [Header("Kalite Kontrol UI")]
        [SerializeField] private RectTransform _conveyorArea;  // Konveyor bant alani
        [SerializeField] private TextMeshProUGUI _scoreText;
        [SerializeField] private TextMeshProUGUI _timerText;
        [SerializeField] private TextMeshProUGUI _feedbackText; // "+2" / "-1" gibi anlık geri bildirim

        // =====================================================================
        // Oyun Durumu
        // =====================================================================

        private readonly List<ProductData> _activeProducts = new();
        private float _spawnTimer;

        // Swipe algilama
        private Vector2 _swipeStartPos;
        private bool _isSwiping;
        private ProductData _swipeTarget;

        // Nesne havuzu
        private readonly List<GameObject> _productPool = new();

        // =====================================================================
        // Urun Verisi
        // =====================================================================

        private class ProductData
        {
            public GameObject GameObject;
            public RectTransform RectTransform;
            public Image Image;
            public bool IsBad;           // Kotu urun mu?
            public bool IsActive;
            public float XPosition;      // Mevcut X pozisyonu
        }

        // =====================================================================
        // MiniGameBase Overrides
        // =====================================================================

        protected override void OnInitialize(MiniGameConfig config)
        {
            if (_scoreText != null) _scoreText.text = "0";
            if (_timerText != null) _timerText.text = config.Duration.ToString("F0");
            if (_feedbackText != null) _feedbackText.gameObject.SetActive(false);

            _activeProducts.Clear();
            _spawnTimer = 0f;
            _isSwiping = false;
        }

        protected override void OnGameStart()
        {
            _spawnTimer = 0.5f; // Ilk urun hemen gelsin
        }

        protected override void OnGameUpdate(float deltaTime)
        {
            // Zamanlayici
            if (_timerText != null)
                _timerText.text = Mathf.CeilToInt(_timeRemaining).ToString();

            // Urun spawn
            _spawnTimer -= deltaTime;
            if (_spawnTimer <= 0f)
            {
                SpawnProduct();
                _spawnTimer = SPAWN_INTERVAL;
            }

            // Urunleri hareket ettir (soldan saga)
            MoveProducts(deltaTime);

            // Ekran disina cikan urunleri kaldir (siralama yapilmadi — ceza yok)
            RemoveOffscreenProducts();

            // Input — swipe algilama
            HandleSwipeInput();
        }

        protected override void OnGameEnd(MiniGameGrade grade, int score)
        {
            foreach (var product in _activeProducts)
            {
                if (product.GameObject != null)
                    product.GameObject.SetActive(false);
            }
            _activeProducts.Clear();
        }

        protected override void OnScoreChanged(int newScore)
        {
            if (_scoreText != null)
                _scoreText.text = newScore.ToString();
        }

        // =====================================================================
        // Urun Spawn
        // =====================================================================

        private void SpawnProduct()
        {
            if (_conveyorArea == null) return;

            var productObj = GetOrCreateProductObject();
            var rectTransform = productObj.GetComponent<RectTransform>();
            var image = productObj.GetComponent<Image>();

            // Sol taraftan baslat
            var areaRect = _conveyorArea.rect;
            float startX = areaRect.xMin - 40f;
            float y = areaRect.center.y + Random.Range(-30f, 30f); // Hafif dikey varyasyon
            rectTransform.anchoredPosition = new Vector2(startX, y);

            // Kotu urun mu?
            bool isBad = Random.value < BAD_PRODUCT_CHANCE;

            // Gorsel — kotu urunler farkli renk ve seklide
            if (image != null)
            {
                image.color = isBad
                    ? new Color(0.6f, 0.2f, 0.2f, 1f)   // Kirmizimsi — kotu
                    : new Color(0.2f, 0.7f, 0.3f, 1f);   // Yesil — iyi
            }

            productObj.SetActive(true);

            var productData = new ProductData
            {
                GameObject = productObj,
                RectTransform = rectTransform,
                Image = image,
                IsBad = isBad,
                IsActive = true,
                XPosition = startX
            };

            _activeProducts.Add(productData);
        }

        // =====================================================================
        // Urun Hareketi
        // =====================================================================

        private void MoveProducts(float deltaTime)
        {
            float speed = PRODUCT_SPEED * (_config?.DifficultyMultiplier ?? 1f);

            foreach (var product in _activeProducts)
            {
                if (!product.IsActive) continue;

                product.XPosition += speed * deltaTime;
                product.RectTransform.anchoredPosition = new Vector2(
                    product.XPosition,
                    product.RectTransform.anchoredPosition.y);
            }
        }

        // =====================================================================
        // Ekran Disi Kontrol
        // =====================================================================

        private void RemoveOffscreenProducts()
        {
            if (_conveyorArea == null) return;
            float maxX = _conveyorArea.rect.xMax + 60f;

            for (int i = _activeProducts.Count - 1; i >= 0; i--)
            {
                var product = _activeProducts[i];
                if (!product.IsActive) continue;

                if (product.XPosition > maxX)
                {
                    product.IsActive = false;
                    product.GameObject.SetActive(false);
                    _activeProducts.RemoveAt(i);
                }
            }
        }

        // =====================================================================
        // Swipe Input
        // =====================================================================

        private void HandleSwipeInput()
        {
            // Touch input
            if (Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);

                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        BeginSwipe(touch.position);
                        break;
                    case TouchPhase.Ended:
                    case TouchPhase.Canceled:
                        EndSwipe(touch.position);
                        break;
                }
            }
            // Mouse input (editor)
            else
            {
                if (Input.GetMouseButtonDown(0))
                    BeginSwipe(Input.mousePosition);
                else if (Input.GetMouseButtonUp(0) && _isSwiping)
                    EndSwipe(Input.mousePosition);
            }
        }

        private void BeginSwipe(Vector2 screenPos)
        {
            _swipeStartPos = screenPos;
            _isSwiping = true;

            // Dokunulan urunu bul
            _swipeTarget = FindProductAtPosition(screenPos);
        }

        private void EndSwipe(Vector2 screenPos)
        {
            if (!_isSwiping || _swipeTarget == null)
            {
                _isSwiping = false;
                _swipeTarget = null;
                return;
            }

            _isSwiping = false;

            float swipeDelta = screenPos.x - _swipeStartPos.x;

            if (Mathf.Abs(swipeDelta) < SWIPE_THRESHOLD)
            {
                _swipeTarget = null;
                return; // Yeterli swipe yapilmadi
            }

            bool swipedRight = swipeDelta > 0;
            ProcessSort(_swipeTarget, swipedRight);

            // Urunu kaldir
            _swipeTarget.IsActive = false;
            _swipeTarget.GameObject.SetActive(false);
            _activeProducts.Remove(_swipeTarget);
            _swipeTarget = null;
        }

        // =====================================================================
        // Urun Bulma (pozisyona gore)
        // =====================================================================

        private ProductData FindProductAtPosition(Vector2 screenPos)
        {
            if (_conveyorArea == null) return null;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _conveyorArea, screenPos, null, out Vector2 localPos);

            ProductData closest = null;
            float closestDist = 80f; // Maksimum dokunma mesafesi

            foreach (var product in _activeProducts)
            {
                if (!product.IsActive) continue;

                float dist = Vector2.Distance(localPos, product.RectTransform.anchoredPosition);
                if (dist < closestDist)
                {
                    closest = product;
                    closestDist = dist;
                }
            }

            return closest;
        }

        // =====================================================================
        // Siralama Islemesi
        // =====================================================================

        /// <summary>
        /// Urunu siralanma yonune gore degerlendirir.
        /// Iyi urun saga = dogru (+2), sola = yanlis (-1)
        /// Kotu urun sola = dogru (+3), saga = yanlis (-2)
        /// </summary>
        private void ProcessSort(ProductData product, bool swipedRight)
        {
            bool correct;

            if (product.IsBad)
            {
                // Kotu urun sola gitmeli
                correct = !swipedRight;

                if (correct)
                    AddScore(BAD_CORRECT_POINTS);
                else
                    SubtractScore(BAD_WRONG_PENALTY);
            }
            else
            {
                // Iyi urun saga gitmeli
                correct = swipedRight;

                if (correct)
                    AddScore(GOOD_CORRECT_POINTS);
                else
                    SubtractScore(GOOD_WRONG_PENALTY);
            }

            // Anlik geri bildirim
            ShowFeedback(correct, product.IsBad);

            // Haptic feedback
            if (FeedbackManager.Instance != null)
            {
                if (correct)
                    FeedbackManager.Instance.PlayButtonClick();
                else
                    FeedbackManager.Instance.PlayError();
            }
        }

        // =====================================================================
        // Geri Bildirim UI
        // =====================================================================

        private void ShowFeedback(bool correct, bool wasBad)
        {
            if (_feedbackText == null) return;

            _feedbackText.gameObject.SetActive(true);

            if (correct)
            {
                int points = wasBad ? BAD_CORRECT_POINTS : GOOD_CORRECT_POINTS;
                _feedbackText.text = $"+{points}";
                _feedbackText.color = new Color(0.2f, 0.8f, 0.2f, 1f); // Yesil
            }
            else
            {
                int penalty = wasBad ? BAD_WRONG_PENALTY : GOOD_WRONG_PENALTY;
                _feedbackText.text = $"-{penalty}";
                _feedbackText.color = new Color(0.9f, 0.2f, 0.2f, 1f); // Kirmizi
            }

            // 0.5 saniye sonra gizle
            CancelInvoke(nameof(HideFeedback));
            Invoke(nameof(HideFeedback), 0.5f);
        }

        private void HideFeedback()
        {
            if (_feedbackText != null)
                _feedbackText.gameObject.SetActive(false);
        }

        // =====================================================================
        // Nesne Havuzu
        // =====================================================================

        private GameObject GetOrCreateProductObject()
        {
            foreach (var obj in _productPool)
            {
                if (obj != null && !obj.activeSelf)
                    return obj;
            }

            var newObj = new GameObject("Product");
            newObj.transform.SetParent(_conveyorArea, false);

            var rect = newObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(70f, 70f);

            var img = newObj.AddComponent<Image>();
            img.raycastTarget = false;

            _productPool.Add(newObj);
            return newObj;
        }
    }
}
