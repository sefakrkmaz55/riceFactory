// =============================================================================
// FloatingText.cs
// Havaya yukselip solan floating text efekti ("+1,234 coin").
// Object pool ile calisir — animasyon bitince FeedbackManager'a geri doner.
// SimpleTween ile animasyon: yukari hareket + fade out, 1s.
// =============================================================================

using UnityEngine;
using TMPro;
using RiceFactory.Core;

namespace RiceFactory.UI
{
    /// <summary>
    /// Havaya yukselen ve kaybolan text efekti.
    /// FeedbackManager object pool'undan alinir, animasyon bitince geri doner.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class FloatingText : MonoBehaviour
    {
        // =====================================================================
        // Ayarlar
        // =====================================================================

        private const float DURATION = 1.0f;
        private const float RISE_DISTANCE = 120f; // piksel
        private const float DEFAULT_FONT_SIZE = 28f;

        // Renk sabitleri (ART_GUIDE'a uygun)
        public static readonly Color COLOR_COIN = new Color(0.3f, 0.69f, 0.31f, 1f);   // Yesil #4CAF50
        public static readonly Color COLOR_GEM = new Color(0.13f, 0.59f, 0.95f, 1f);    // Mavi #2196F3
        public static readonly Color COLOR_XP = new Color(1f, 0.84f, 0.31f, 1f);         // Sari #FFD54F
        public static readonly Color COLOR_PRESTIGE = new Color(1f, 0.84f, 0f, 1f);      // Altin #FFD700

        // =====================================================================
        // Referanslar
        // =====================================================================

        private RectTransform _rectTransform;
        private CanvasGroup _canvasGroup;
        private TextMeshProUGUI _text;

        // Animasyon durumu
        private float _elapsed;
        private bool _isAnimating;
        private Vector2 _startPosition;
        private float _targetY;

        // =====================================================================
        // Unity Lifecycle
        // =====================================================================

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _canvasGroup = GetComponent<CanvasGroup>();
            _text = GetComponent<TextMeshProUGUI>();

            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
        }

        private void Update()
        {
            if (!_isAnimating) return;

            _elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(_elapsed / DURATION);

            // Ease: OutQuad — hizli baslar, yavaslayarak durur
            float easedT = SimpleTween.EaseOutQuad(t);

            // Yukari hareket
            float currentY = Mathf.Lerp(_startPosition.y, _targetY, easedT);
            _rectTransform.anchoredPosition = new Vector2(_startPosition.x, currentY);

            // Fade out (son %40'ta baslar)
            if (t > 0.6f)
            {
                float fadeT = (t - 0.6f) / 0.4f;
                _canvasGroup.alpha = 1f - fadeT;
            }
            else
            {
                _canvasGroup.alpha = 1f;
            }

            // Scale punch — baslangicta hafif buyume sonra normal
            if (t < 0.2f)
            {
                float scaleT = t / 0.2f;
                float scale = 1f + 0.3f * (1f - scaleT);
                _rectTransform.localScale = Vector3.one * scale;
            }
            else
            {
                _rectTransform.localScale = Vector3.one;
            }

            // Bitti mi?
            if (t >= 1f)
            {
                _isAnimating = false;
                ReturnToPool();
            }
        }

        // =====================================================================
        // Public API
        // =====================================================================

        /// <summary>
        /// Floating text'i gosterir ve animasyonu baslatir.
        /// </summary>
        /// <param name="message">Gosterilecek metin ("+1,234").</param>
        /// <param name="color">Metin rengi.</param>
        /// <param name="fontSize">Opsiyonel font boyutu.</param>
        public void Show(string message, Color color, float fontSize = 0f)
        {
            if (_text == null)
                _text = GetComponent<TextMeshProUGUI>();
            if (_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();
            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>();

            _text.text = message;
            _text.color = color;
            _text.fontSize = fontSize > 0f ? fontSize : DEFAULT_FONT_SIZE;

            // Ekranin ortasinda veya hafif rastgele konumda baslat
            float randomX = Random.Range(-50f, 50f);
            float randomY = Random.Range(-20f, 20f);
            _startPosition = new Vector2(randomX, randomY);
            _targetY = _startPosition.y + RISE_DISTANCE;

            _rectTransform.anchoredPosition = _startPosition;
            _rectTransform.localScale = Vector3.one;
            _canvasGroup.alpha = 1f;

            _elapsed = 0f;
            _isAnimating = true;
        }

        /// <summary>
        /// Belirli bir ekran pozisyonunda floating text gosterir.
        /// </summary>
        /// <param name="message">Metin.</param>
        /// <param name="color">Renk.</param>
        /// <param name="screenPosition">Baslangic ekran pozisyonu (anchored).</param>
        /// <param name="fontSize">Font boyutu.</param>
        public void ShowAtPosition(string message, Color color, Vector2 screenPosition, float fontSize = 0f)
        {
            if (_text == null)
                _text = GetComponent<TextMeshProUGUI>();
            if (_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();
            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>();

            _text.text = message;
            _text.color = color;
            _text.fontSize = fontSize > 0f ? fontSize : DEFAULT_FONT_SIZE;

            _startPosition = screenPosition;
            _targetY = _startPosition.y + RISE_DISTANCE;

            _rectTransform.anchoredPosition = _startPosition;
            _rectTransform.localScale = Vector3.one;
            _canvasGroup.alpha = 1f;

            _elapsed = 0f;
            _isAnimating = true;
        }

        // =====================================================================
        // Pool Geri Donus
        // =====================================================================

        private void ReturnToPool()
        {
            _isAnimating = false;
            _canvasGroup.alpha = 0f;

            if (FeedbackManager.Instance != null)
            {
                FeedbackManager.Instance.ReturnFloatingText(this);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}
