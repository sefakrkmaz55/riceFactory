// =============================================================================
// ShelfSortMiniGame.cs
// Raf Duzenleme Mini-Game — Market
// Sure: 20 saniye. Karisik urunleri dogru raflara surukle-birak.
// 3 raf kategorisi: tahillar, unlu mamuller, hazir yemek.
// Dogru yerlestirme +3, yanlis -1. Ilk 5 saniyede 2x bonus.
// Grade: S>=45, A>=30, B>=18, C>=0
// =============================================================================

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using RiceFactory.Core;
using RiceFactory.Core.Events;

namespace RiceFactory.MiniGames
{
    /// <summary>
    /// Raf Duzenleme mini-game. Karisik urunleri dogru kategorideki rafa surukle-birak.
    /// - Dogru: +3 puan
    /// - Yanlis: -1 puan
    /// - Hiz bonusu: ilk 5 saniyedeki yerlesimler 2x puan
    /// </summary>
    public class ShelfSortMiniGame : MiniGameBase
    {
        // =====================================================================
        // Sabitler
        // =====================================================================

        private const int CORRECT_POINTS = 3;            // Dogru yerlestirme puani
        private const int WRONG_PENALTY = 1;              // Yanlis yerlestirme cezasi
        private const float SPEED_BONUS_DURATION = 5f;    // Hiz bonusu suresi (saniye)
        private const float SPEED_BONUS_MULTIPLIER = 2f;  // Hiz bonusu carpani
        private const float SPAWN_INTERVAL = 1.2f;        // Urun olusma araligi
        private const int MAX_ACTIVE_PRODUCTS = 6;        // Ayni anda ekrandaki max urun

        // =====================================================================
        // Raf Kategorileri
        // =====================================================================

        /// <summary>Urun kategorileri.</summary>
        public enum ProductCategory
        {
            Grains,         // Tahillar (pirinc, bugday, misir)
            BakedGoods,     // Unlu mamuller (ekmek, pasta, borek)
            ReadyMeals      // Hazir yemek (pilav, sandvic, corba)
        }

        /// <summary>Kategori isimleri.</summary>
        private static readonly Dictionary<ProductCategory, string> CATEGORY_NAMES = new()
        {
            { ProductCategory.Grains, "Tahillar" },
            { ProductCategory.BakedGoods, "Unlu Mamuller" },
            { ProductCategory.ReadyMeals, "Hazir Yemek" }
        };

        /// <summary>Kategori renkleri.</summary>
        private static readonly Dictionary<ProductCategory, Color> CATEGORY_COLORS = new()
        {
            { ProductCategory.Grains, new Color(0.93f, 0.86f, 0.51f, 1f) },     // Buğday sarisi
            { ProductCategory.BakedGoods, new Color(0.82f, 0.62f, 0.37f, 1f) },  // Kahverengi
            { ProductCategory.ReadyMeals, new Color(0.95f, 0.46f, 0.19f, 1f) }   // Turuncu
        };

        /// <summary>Her kategorideki urun isimleri.</summary>
        private static readonly Dictionary<ProductCategory, string[]> PRODUCT_NAMES = new()
        {
            { ProductCategory.Grains, new[] { "Pirinc", "Bugday", "Misir", "Arpa" } },
            { ProductCategory.BakedGoods, new[] { "Ekmek", "Pasta", "Borek", "Kurabiye" } },
            { ProductCategory.ReadyMeals, new[] { "Pilav", "Sandvic", "Corba", "Makarna" } }
        };

        // =====================================================================
        // UI Referanslari
        // =====================================================================

        [Header("Raf Duzenleme UI")]
        [SerializeField] private RectTransform _productArea;       // Urunlerin belirdigi alan
        [SerializeField] private TextMeshProUGUI _scoreText;
        [SerializeField] private TextMeshProUGUI _timerText;
        [SerializeField] private TextMeshProUGUI _feedbackText;
        [SerializeField] private TextMeshProUGUI _bonusText;       // "2X HIZ BONUSU!" uyarisi

        [Header("Raf Alanlari (Drop Zone)")]
        [SerializeField] private RectTransform _grainsShelf;       // Tahillar rafi
        [SerializeField] private RectTransform _bakedGoodsShelf;   // Unlu mamuller rafi
        [SerializeField] private RectTransform _readyMealsShelf;   // Hazir yemek rafi

        // =====================================================================
        // Oyun Durumu
        // =====================================================================

        private readonly List<DraggableProduct> _activeProducts = new();
        private float _spawnTimer;

        // Surukleme durumu
        private DraggableProduct _draggedProduct;
        private Vector2 _dragOffset;
        private bool _isDragging;

        // Nesne havuzu
        private readonly List<GameObject> _productPool = new();

        // =====================================================================
        // Urun Verisi
        // =====================================================================

        private class DraggableProduct
        {
            public GameObject GameObject;
            public RectTransform RectTransform;
            public Image Image;
            public TextMeshProUGUI NameText;
            public ProductCategory Category;
            public Vector2 OriginalPosition;
            public bool IsActive;
        }

        // =====================================================================
        // MiniGameBase Overrides
        // =====================================================================

        protected override void OnInitialize(MiniGameConfig config)
        {
            if (_scoreText != null) _scoreText.text = "0";
            if (_timerText != null) _timerText.text = config.Duration.ToString("F0");
            if (_feedbackText != null) _feedbackText.gameObject.SetActive(false);
            if (_bonusText != null) _bonusText.gameObject.SetActive(false);

            _activeProducts.Clear();
            _spawnTimer = 0f;
            _isDragging = false;
            _draggedProduct = null;
        }

        protected override void OnGameStart()
        {
            // Baslangicta birkac urun spawn et
            for (int i = 0; i < 3; i++)
            {
                SpawnProduct();
            }

            // Hiz bonusu gostergesi
            if (_bonusText != null)
            {
                _bonusText.gameObject.SetActive(true);
                _bonusText.text = "2X HIZ BONUSU!";
                _bonusText.color = new Color(1f, 0.84f, 0f, 1f);
            }
        }

        protected override void OnGameUpdate(float deltaTime)
        {
            // Zamanlayici
            if (_timerText != null)
                _timerText.text = Mathf.CeilToInt(_timeRemaining).ToString();

            // Hiz bonusu gostergesi — ilk 5 saniye
            if (_bonusText != null)
            {
                float elapsed = _config.Duration - _timeRemaining;
                _bonusText.gameObject.SetActive(elapsed < SPEED_BONUS_DURATION);
            }

            // Urun spawn
            if (_activeProducts.Count < MAX_ACTIVE_PRODUCTS)
            {
                _spawnTimer -= deltaTime;
                if (_spawnTimer <= 0f)
                {
                    SpawnProduct();
                    _spawnTimer = SPAWN_INTERVAL;
                }
            }

            // Drag & Drop input
            HandleDragInput();
        }

        protected override void OnGameEnd(MiniGameGrade grade, int score)
        {
            foreach (var product in _activeProducts)
            {
                if (product.GameObject != null)
                    product.GameObject.SetActive(false);
            }
            _activeProducts.Clear();
            _isDragging = false;
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
            if (_productArea == null) return;

            // Rastgele kategori
            var categories = new[] { ProductCategory.Grains, ProductCategory.BakedGoods, ProductCategory.ReadyMeals };
            var category = categories[Random.Range(0, categories.Length)];

            // Rastgele urun ismi
            var names = PRODUCT_NAMES[category];
            var productName = names[Random.Range(0, names.Length)];

            var productObj = GetOrCreateProductObject();
            var rectTransform = productObj.GetComponent<RectTransform>();
            var image = productObj.GetComponentInChildren<Image>();

            // Rastgele pozisyon — urun alaninda
            var areaRect = _productArea.rect;
            float x = Random.Range(areaRect.xMin + 50f, areaRect.xMax - 50f);
            float y = Random.Range(areaRect.yMin + 30f, areaRect.yMax - 30f);
            rectTransform.anchoredPosition = new Vector2(x, y);

            // Gorsel
            if (image != null)
                image.color = CATEGORY_COLORS[category];

            // Isim metni
            var nameText = productObj.GetComponentInChildren<TextMeshProUGUI>();
            if (nameText != null)
                nameText.text = productName;

            productObj.SetActive(true);

            var draggable = new DraggableProduct
            {
                GameObject = productObj,
                RectTransform = rectTransform,
                Image = image,
                NameText = nameText,
                Category = category,
                OriginalPosition = new Vector2(x, y),
                IsActive = true
            };

            _activeProducts.Add(draggable);
        }

        // =====================================================================
        // Drag & Drop Input
        // =====================================================================

        private void HandleDragInput()
        {
            Vector2 inputPos = Vector2.zero;
            bool inputDown = false;
            bool inputHeld = false;
            bool inputUp = false;

            // Touch input
            if (Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);
                inputPos = touch.position;
                inputDown = touch.phase == TouchPhase.Began;
                inputHeld = touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary;
                inputUp = touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled;
            }
            // Mouse input
            else
            {
                inputPos = Input.mousePosition;
                inputDown = Input.GetMouseButtonDown(0);
                inputHeld = Input.GetMouseButton(0);
                inputUp = Input.GetMouseButtonUp(0);
            }

            if (inputDown && !_isDragging)
            {
                TryStartDrag(inputPos);
            }
            else if (inputHeld && _isDragging)
            {
                UpdateDrag(inputPos);
            }
            else if (inputUp && _isDragging)
            {
                EndDrag(inputPos);
            }
        }

        private void TryStartDrag(Vector2 screenPos)
        {
            if (_productArea == null) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _productArea, screenPos, null, out Vector2 localPos);

            // En yakin urunu bul
            DraggableProduct closest = null;
            float closestDist = 60f;

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

            if (closest == null) return;

            _draggedProduct = closest;
            _isDragging = true;
            _dragOffset = closest.RectTransform.anchoredPosition - localPos;
        }

        private void UpdateDrag(Vector2 screenPos)
        {
            if (_draggedProduct == null || _productArea == null) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _productArea, screenPos, null, out Vector2 localPos);

            _draggedProduct.RectTransform.anchoredPosition = localPos + _dragOffset;
        }

        private void EndDrag(Vector2 screenPos)
        {
            if (_draggedProduct == null)
            {
                _isDragging = false;
                return;
            }

            // Hangi rafa birakildi?
            ProductCategory? droppedOnShelf = GetShelfAtPosition(screenPos);

            if (droppedOnShelf.HasValue)
            {
                ProcessDrop(_draggedProduct, droppedOnShelf.Value);
            }
            else
            {
                // Rafa birakilmadi — orijinal pozisyona geri don
                _draggedProduct.RectTransform.anchoredPosition = _draggedProduct.OriginalPosition;
            }

            _isDragging = false;
            _draggedProduct = null;
        }

        // =====================================================================
        // Raf Algilama
        // =====================================================================

        /// <summary>
        /// Ekran pozisyonunun hangi rafin uzerinde oldugunu belirler.
        /// </summary>
        private ProductCategory? GetShelfAtPosition(Vector2 screenPos)
        {
            if (_grainsShelf != null && RectTransformUtility.RectangleContainsScreenPoint(_grainsShelf, screenPos))
                return ProductCategory.Grains;

            if (_bakedGoodsShelf != null && RectTransformUtility.RectangleContainsScreenPoint(_bakedGoodsShelf, screenPos))
                return ProductCategory.BakedGoods;

            if (_readyMealsShelf != null && RectTransformUtility.RectangleContainsScreenPoint(_readyMealsShelf, screenPos))
                return ProductCategory.ReadyMeals;

            return null;
        }

        // =====================================================================
        // Drop Isleme
        // =====================================================================

        private void ProcessDrop(DraggableProduct product, ProductCategory shelf)
        {
            bool correct = product.Category == shelf;

            // Hiz bonusu kontrolu — ilk 5 saniye
            float elapsed = _config.Duration - _timeRemaining;
            bool hasSpeedBonus = elapsed < SPEED_BONUS_DURATION;

            if (correct)
            {
                int points = CORRECT_POINTS;
                if (hasSpeedBonus)
                    points = Mathf.RoundToInt(points * SPEED_BONUS_MULTIPLIER);

                AddScore(points);
                ShowFeedback(true, $"+{points}");

                if (FeedbackManager.Instance != null)
                    FeedbackManager.Instance.PlayCoinEarned(points);
            }
            else
            {
                SubtractScore(WRONG_PENALTY);
                ShowFeedback(false, $"-{WRONG_PENALTY}");

                if (FeedbackManager.Instance != null)
                    FeedbackManager.Instance.PlayError();
            }

            // Urunu kaldir
            product.IsActive = false;
            product.GameObject.SetActive(false);
            _activeProducts.Remove(product);
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

            // Yeni urun nesnesi olustur
            var newObj = new GameObject("ShelfProduct");
            newObj.transform.SetParent(_productArea, false);

            var rect = newObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(90f, 70f);

            // Arka plan
            var img = newObj.AddComponent<Image>();
            img.raycastTarget = false;
            img.color = Color.white;

            // Isim metni
            var textObj = new GameObject("Name");
            textObj.transform.SetParent(newObj.transform, false);

            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 14;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            tmp.color = Color.black;

            _productPool.Add(newObj);
            return newObj;
        }
    }
}
