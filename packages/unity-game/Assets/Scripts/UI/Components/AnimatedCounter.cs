using UnityEngine;
using TMPro;

namespace RiceFactory.UI
{
    /// <summary>
    /// Animasyonlu sayac komponenti.
    /// Eski degerden yeni degere yumusak gecis yapar (lerp).
    /// Buyuk sayilari formatlar: 1K, 1M, 1B, 1T...
    /// TextMeshPro ile calisir.
    ///
    /// Kullanim:
    ///   animatedCounter.SetValue(newValue);         // animasyonlu
    ///   animatedCounter.SetValueImmediate(newValue); // aninda
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class AnimatedCounter : MonoBehaviour
    {
        [Header("Animasyon Ayarlari")]
        [Tooltip("Sayacin hedef degere ulasma suresi (saniye)")]
        [SerializeField] private float _duration = 0.6f;

        [Tooltip("Minimum animasyon suresi — cok kucuk degisikliklerde bile his vermesi icin")]
        [SerializeField] private float _minDuration = 0.1f;

        [Tooltip("Opsiyonel on ek (orn: '$' veya 'FP ')")]
        [SerializeField] private string _prefix = "";

        [Tooltip("Opsiyonel son ek")]
        [SerializeField] private string _suffix = "";

        [Tooltip("Kucuk sayilarda ondalik gosterilsin mi (orn: 1.2K)")]
        [SerializeField] private bool _showDecimalForSmall = true;

        private TextMeshProUGUI _text;

        private double _currentDisplayValue;
        private double _targetValue;
        private double _startValue;
        private float _elapsedTime;
        private float _currentDuration;
        private bool _isAnimating;

        // ---------------------------------------------------------------
        // Lifecycle
        // ---------------------------------------------------------------

        private void Awake()
        {
            _text = GetComponent<TextMeshProUGUI>();
        }

        private void Update()
        {
            if (!_isAnimating) return;

            _elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsedTime / _currentDuration);

            // EaseOutQuad — basta hizli, sona dogru yavaslar
            float easedT = 1f - (1f - t) * (1f - t);

            _currentDisplayValue = Lerp(_startValue, _targetValue, easedT);
            UpdateText(_currentDisplayValue);

            if (t >= 1f)
            {
                _isAnimating = false;
                _currentDisplayValue = _targetValue;
                UpdateText(_currentDisplayValue);
            }
        }

        // ---------------------------------------------------------------
        // Public API
        // ---------------------------------------------------------------

        /// <summary>
        /// Animasyonlu gecis ile yeni deger ata.
        /// Onceki animasyon devam ediyorsa mevcut gorunen degerden baslar.
        /// </summary>
        public void SetValue(double newValue)
        {
            _startValue = _currentDisplayValue;
            _targetValue = newValue;
            _elapsedTime = 0f;

            // Degisim miktarina gore sure ayarla
            double diff = System.Math.Abs(newValue - _startValue);
            if (diff < 1.0)
            {
                // Cok kucuk degisikliklerde animasyon gereksiz
                _currentDisplayValue = newValue;
                UpdateText(_currentDisplayValue);
                _isAnimating = false;
                return;
            }

            _currentDuration = Mathf.Max(_minDuration, _duration);
            _isAnimating = true;
        }

        /// <summary>
        /// Animasyonsuz, aninda deger ata (ilk yukleme vb. icin).
        /// </summary>
        public void SetValueImmediate(double value)
        {
            _isAnimating = false;
            _currentDisplayValue = value;
            _targetValue = value;
            UpdateText(value);
        }

        /// <summary>Mevcut gorunen degeri dondurur.</summary>
        public double CurrentDisplayValue => _currentDisplayValue;

        /// <summary>Hedef degeri dondurur.</summary>
        public double TargetValue => _targetValue;

        // ---------------------------------------------------------------
        // Buyuk Sayi Formatlama
        // ---------------------------------------------------------------

        /// <summary>
        /// Buyuk sayilari okunabilir formata cevirir.
        /// 999 ve alti: tam sayi olarak gosterilir.
        /// 1,000+:     1K, 1M, 1B, 1T, 1Qa, 1Qi...
        /// </summary>
        public static string FormatNumber(double value, bool showDecimal = true)
        {
            if (value < 0)
                return "-" + FormatNumber(-value, showDecimal);

            // Kucuk sayilar: tam goster
            if (value < 1_000)
                return ((long)value).ToString("N0");

            // Buyuk sayi suffixleri
            // K=Kilo, M=Mega, B=Billion, T=Trillion, Qa=Quadrillion, Qi=Quintillion
            string[] suffixes = { "", "K", "M", "B", "T", "Qa", "Qi", "Sx", "Sp", "Oc", "No", "Dc" };

            int tier = 0;
            double scaled = value;

            while (scaled >= 1_000 && tier < suffixes.Length - 1)
            {
                scaled /= 1_000;
                tier++;
            }

            if (showDecimal && scaled < 100)
            {
                // 1.23K, 12.3M gibi — 3 anlamli basamak
                if (scaled < 10)
                    return scaled.ToString("F2") + suffixes[tier];
                else
                    return scaled.ToString("F1") + suffixes[tier];
            }

            return ((int)scaled).ToString() + suffixes[tier];
        }

        // ---------------------------------------------------------------
        // Internal
        // ---------------------------------------------------------------

        private void UpdateText(double value)
        {
            if (_text == null) return;
            _text.text = $"{_prefix}{FormatNumber(value, _showDecimalForSmall)}{_suffix}";
        }

        private static double Lerp(double a, double b, float t)
        {
            return a + (b - a) * t;
        }
    }
}
