// =============================================================================
// ScreenEffects.cs
// Ekran efektleri: flash, shake, confetti/particle.
// Singleton, Canvas uzerinde overlay olarak calisir.
// =============================================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RiceFactory.Core;

namespace RiceFactory.UI
{
    /// <summary>
    /// Ekran geneli gorsel efektler: flash, shake, confetti.
    /// Sahne yuklenirken Canvas uzerinde otomatik kurulur.
    /// </summary>
    public class ScreenEffects : MonoBehaviour
    {
        // =====================================================================
        // Singleton
        // =====================================================================

        public static ScreenEffects Instance { get; private set; }

        // =====================================================================
        // Ayarlar
        // =====================================================================

        private const int CONFETTI_POOL_SIZE = 60;
        private const float CONFETTI_FALL_SPEED = 400f;
        private const float CONFETTI_LIFETIME = 2f;
        private const float CONFETTI_PIECE_SIZE = 12f;

        // =====================================================================
        // Referanslar
        // =====================================================================

        private Canvas _overlayCanvas;
        private Image _flashImage;
        private RectTransform _canvasRect;
        private Transform _confettiContainer;

        // Confetti pool
        private readonly Queue<RectTransform> _confettiPool = new();
        private readonly List<ConfettiPiece> _activeConfetti = new();

        // Shake
        private Transform _mainCameraTransform;
        private Vector3 _originalCameraPosition;
        private bool _isShaking;

        // Confetti renkleri (ART_GUIDE renk paletinden)
        private static readonly Color[] CONFETTI_COLORS =
        {
            new Color(0.3f, 0.69f, 0.31f, 1f),   // Yesil #4CAF50
            new Color(1f, 0.84f, 0.31f, 1f),       // Sari #FFD54F
            new Color(1f, 0.44f, 0.26f, 1f),       // Turuncu #FF7043
            new Color(0.13f, 0.59f, 0.95f, 1f),    // Mavi #2196F3
            new Color(0.96f, 0.26f, 0.21f, 1f),    // Kirmizi #F44336
            new Color(0.48f, 0.12f, 0.64f, 1f),    // Mor #7B1FA2
            new Color(1f, 0.6f, 0f, 1f),            // Amber #FF9800
        };

        // =====================================================================
        // Unity Lifecycle
        // =====================================================================

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            SetupOverlayCanvas();
            SetupFlashImage();
            SetupConfettiPool();
        }

        private void Start()
        {
            // Ana kamera referansi
            if (Camera.main != null)
            {
                _mainCameraTransform = Camera.main.transform;
                _originalCameraPosition = _mainCameraTransform.position;
            }
        }

        private void Update()
        {
            UpdateConfetti();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        // =====================================================================
        // Kurulum
        // =====================================================================

        private void SetupOverlayCanvas()
        {
            // Overlay canvas olustur (en ust katman)
            var canvasObj = new GameObject("ScreenEffectsCanvas");
            canvasObj.transform.SetParent(transform);

            _overlayCanvas = canvasObj.AddComponent<Canvas>();
            _overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _overlayCanvas.sortingOrder = 999; // En ustte

            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            _canvasRect = canvasObj.GetComponent<RectTransform>();

            // Raycast engellememesi icin GraphicRaycaster ekleme
        }

        private void SetupFlashImage()
        {
            var flashObj = new GameObject("FlashOverlay");
            flashObj.transform.SetParent(_overlayCanvas.transform, false);

            var rect = flashObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            _flashImage = flashObj.AddComponent<Image>();
            _flashImage.color = Color.clear;
            _flashImage.raycastTarget = false;
        }

        private void SetupConfettiPool()
        {
            var containerObj = new GameObject("ConfettiContainer");
            containerObj.transform.SetParent(_overlayCanvas.transform, false);

            var containerRect = containerObj.AddComponent<RectTransform>();
            containerRect.anchorMin = Vector2.zero;
            containerRect.anchorMax = Vector2.one;
            containerRect.offsetMin = Vector2.zero;
            containerRect.offsetMax = Vector2.zero;

            _confettiContainer = containerObj.transform;

            for (int i = 0; i < CONFETTI_POOL_SIZE; i++)
            {
                var piece = CreateConfettiPiece();
                piece.gameObject.SetActive(false);
                _confettiPool.Enqueue(piece);
            }
        }

        private RectTransform CreateConfettiPiece()
        {
            var obj = new GameObject("Confetti");
            obj.transform.SetParent(_confettiContainer, false);

            var rect = obj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(CONFETTI_PIECE_SIZE, CONFETTI_PIECE_SIZE);

            var image = obj.AddComponent<Image>();
            image.raycastTarget = false;

            return rect;
        }

        // =====================================================================
        // Screen Flash
        // =====================================================================

        /// <summary>
        /// Ekrani verilen renkte flash yapar ve verilen surede solar.
        /// </summary>
        /// <param name="color">Flash rengi (alpha dahil).</param>
        /// <param name="duration">Solma suresi (saniye).</param>
        public void FlashScreen(Color color, float duration)
        {
            if (_flashImage == null) return;

            StopCoroutine(nameof(FlashRoutine));
            StartCoroutine(FlashRoutine(color, duration));
        }

        private IEnumerator FlashRoutine(Color color, float duration)
        {
            _flashImage.color = color;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float alpha = Mathf.Lerp(color.a, 0f, t);
                _flashImage.color = new Color(color.r, color.g, color.b, alpha);
                yield return null;
            }

            _flashImage.color = Color.clear;
        }

        // =====================================================================
        // Screen Shake
        // =====================================================================

        /// <summary>
        /// Kamerayi hafifce sallar.
        /// </summary>
        /// <param name="intensity">Sallama siddeti (piksel).</param>
        /// <param name="duration">Sure (saniye).</param>
        public void ShakeScreen(float intensity = 3f, float duration = 0.2f)
        {
            if (_isShaking) return;

            // Kamera referansini guncelle
            if (_mainCameraTransform == null && Camera.main != null)
            {
                _mainCameraTransform = Camera.main.transform;
                _originalCameraPosition = _mainCameraTransform.position;
            }

            if (_mainCameraTransform != null)
            {
                StartCoroutine(ShakeRoutine(intensity, duration));
            }
        }

        private IEnumerator ShakeRoutine(float intensity, float duration)
        {
            _isShaking = true;
            _originalCameraPosition = _mainCameraTransform.position;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Azalan siddet
                float currentIntensity = intensity * (1f - t);

                float offsetX = Random.Range(-currentIntensity, currentIntensity);
                float offsetY = Random.Range(-currentIntensity, currentIntensity);

                _mainCameraTransform.position = _originalCameraPosition +
                    new Vector3(offsetX * 0.01f, offsetY * 0.01f, 0f);

                yield return null;
            }

            _mainCameraTransform.position = _originalCameraPosition;
            _isShaking = false;
        }

        // =====================================================================
        // Confetti / Particle
        // =====================================================================

        /// <summary>
        /// Ekranin ustunden rastgele renkli kucuk kareler dusurur.
        /// </summary>
        /// <param name="count">Confetti parcasi sayisi.</param>
        public void SpawnConfetti(int count)
        {
            for (int i = 0; i < count; i++)
            {
                RectTransform piece;
                if (_confettiPool.Count > 0)
                {
                    piece = _confettiPool.Dequeue();
                }
                else
                {
                    piece = CreateConfettiPiece();
                }

                // Rastgele renk
                var color = CONFETTI_COLORS[Random.Range(0, CONFETTI_COLORS.Length)];
                piece.GetComponent<Image>().color = color;

                // Rastgele boyut
                float size = Random.Range(6f, 16f);
                piece.sizeDelta = new Vector2(size, size);

                // Rastgele baslangic pozisyonu (ustten)
                float canvasWidth = _canvasRect != null ? _canvasRect.rect.width : 1080f;
                float canvasHeight = _canvasRect != null ? _canvasRect.rect.height : 1920f;
                float startX = Random.Range(-canvasWidth * 0.5f, canvasWidth * 0.5f);
                float startY = canvasHeight * 0.5f + Random.Range(0f, 100f);

                piece.anchoredPosition = new Vector2(startX, startY);
                piece.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
                piece.gameObject.SetActive(true);

                // Rastgele hiz ve donus
                var confettiData = new ConfettiPiece
                {
                    Rect = piece,
                    FallSpeed = CONFETTI_FALL_SPEED * Random.Range(0.5f, 1.5f),
                    HorizontalDrift = Random.Range(-80f, 80f),
                    RotationSpeed = Random.Range(-360f, 360f),
                    Lifetime = CONFETTI_LIFETIME * Random.Range(0.8f, 1.2f),
                    Elapsed = 0f,
                    CanvasHeight = canvasHeight
                };

                _activeConfetti.Add(confettiData);
            }
        }

        private void UpdateConfetti()
        {
            for (int i = _activeConfetti.Count - 1; i >= 0; i--)
            {
                var piece = _activeConfetti[i];
                piece.Elapsed += Time.unscaledDeltaTime;

                if (piece.Elapsed >= piece.Lifetime || piece.Rect == null)
                {
                    if (piece.Rect != null)
                    {
                        piece.Rect.gameObject.SetActive(false);
                        _confettiPool.Enqueue(piece.Rect);
                    }
                    _activeConfetti.RemoveAt(i);
                    continue;
                }

                // Hareket
                float dt = Time.unscaledDeltaTime;
                var pos = piece.Rect.anchoredPosition;
                pos.y -= piece.FallSpeed * dt;
                pos.x += piece.HorizontalDrift * dt * Mathf.Sin(piece.Elapsed * 3f); // Sallanti

                piece.Rect.anchoredPosition = pos;

                // Donus
                piece.Rect.Rotate(0f, 0f, piece.RotationSpeed * dt);

                // Fade out (son %30)
                float lifeT = piece.Elapsed / piece.Lifetime;
                if (lifeT > 0.7f)
                {
                    float alpha = 1f - (lifeT - 0.7f) / 0.3f;
                    var image = piece.Rect.GetComponent<Image>();
                    if (image != null)
                    {
                        var c = image.color;
                        c.a = alpha;
                        image.color = c;
                    }
                }

                // Ekranin altina dustuyse temizle
                if (pos.y < -piece.CanvasHeight * 0.6f)
                {
                    piece.Rect.gameObject.SetActive(false);
                    _confettiPool.Enqueue(piece.Rect);
                    _activeConfetti.RemoveAt(i);
                    continue;
                }

                _activeConfetti[i] = piece;
            }
        }

        // =====================================================================
        // Confetti Veri Yapisi
        // =====================================================================

        private struct ConfettiPiece
        {
            public RectTransform Rect;
            public float FallSpeed;
            public float HorizontalDrift;
            public float RotationSpeed;
            public float Lifetime;
            public float Elapsed;
            public float CanvasHeight;
        }
    }
}
