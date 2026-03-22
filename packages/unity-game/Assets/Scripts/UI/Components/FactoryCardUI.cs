// =============================================================================
// FactoryCardUI.cs
// Her fabrika icin kart bilesen. Fabrika adi, yildiz seviyesi, uretim hizi,
// gelir/dk, progress bar ve kilit durumunu gosterir.
// Dokunuldigunda UpgradePanel acar.
// =============================================================================

using UnityEngine;
using UnityEngine.UI;
using RiceFactory.Core;
using RiceFactory.Production;

namespace RiceFactory.UI
{
    /// <summary>
    /// Fabrika kart UI bileseni. Her fabrika icin bir kart gosterilir.
    /// Acik fabrikalar: ad, yildiz, uretim hizi, gelir, progress bar.
    /// Kilitli fabrikalar: kilit ikonu + acma maliyeti.
    /// Dokunuldigunda UpgradePanel acilir.
    /// </summary>
    public class FactoryCardUI : MonoBehaviour
    {
        // =====================================================================
        // Inspector Referanslari
        // =====================================================================

        [Header("Temel Bilgi")]
        [SerializeField] private TMPro.TMP_Text _factoryNameText;
        [SerializeField] private TMPro.TMP_Text _starText;
        [SerializeField] private TMPro.TMP_Text _productionRateText;
        [SerializeField] private TMPro.TMP_Text _revenueText;

        [Header("Progress Bar")]
        [SerializeField] private Slider _progressBar;
        [SerializeField] private Image _progressFill;

        [Header("Kilit Durumu")]
        [SerializeField] private GameObject _unlockedContent;
        [SerializeField] private GameObject _lockedContent;
        [SerializeField] private TMPro.TMP_Text _unlockCostText;
        [SerializeField] private Image _lockIcon;

        [Header("Etkilesim")]
        [SerializeField] private Button _cardButton;
        [SerializeField] private Button _unlockButton;

        // =====================================================================
        // Dahili Durum
        // =====================================================================

        private Factory _factory;
        private FactoryConfigData _configData;
        private bool _isUnlocked;

        // =====================================================================
        // Public API
        // =====================================================================

        /// <summary>
        /// Acik bir fabrika icin karti kurar.
        /// </summary>
        /// <param name="factory">Aktif Factory nesnesi</param>
        /// <param name="configData">Fabrika konfigurasyonu (isim, maliyet vb.)</param>
        public void Setup(Factory factory, FactoryConfigData configData)
        {
            _factory = factory;
            _configData = configData;
            _isUnlocked = true;

            // Gorsellik
            if (_unlockedContent != null) _unlockedContent.SetActive(true);
            if (_lockedContent != null) _lockedContent.SetActive(false);

            // Isim
            if (_factoryNameText != null)
            {
                _factoryNameText.text = configData?.Name ?? factory?.Data?.FactoryName ?? "Fabrika";
            }

            // Buton binding
            if (_cardButton != null)
            {
                _cardButton.onClick.RemoveAllListeners();
                _cardButton.onClick.AddListener(OnCardClicked);
            }

            UpdateDisplay(factory);
        }

        /// <summary>
        /// Kilitli bir fabrika icin karti kurar.
        /// Kilit ikonu ve acma maliyeti gosterilir.
        /// </summary>
        /// <param name="configData">Fabrika konfigurasyonu</param>
        public void SetupLocked(FactoryConfigData configData)
        {
            _factory = null;
            _configData = configData;
            _isUnlocked = false;

            // Gorsellik
            if (_unlockedContent != null) _unlockedContent.SetActive(false);
            if (_lockedContent != null) _lockedContent.SetActive(true);

            // Isim
            if (_factoryNameText != null)
            {
                _factoryNameText.text = configData?.Name ?? "Kilitli";
            }

            // Acma maliyeti
            if (_unlockCostText != null && configData != null)
            {
                if (configData.UnlockCost <= 0)
                {
                    _unlockCostText.text = "Ucretsiz";
                }
                else
                {
                    _unlockCostText.text = FormatNumber(configData.UnlockCost);
                }
            }

            // Unlock butonu
            if (_unlockButton != null)
            {
                _unlockButton.onClick.RemoveAllListeners();
                _unlockButton.onClick.AddListener(OnUnlockClicked);
            }
        }

        /// <summary>
        /// Fabrika kartinin gorsel bilgilerini gunceller.
        /// Her frame (veya tick) cagirilabilir.
        /// </summary>
        /// <param name="factory">Guncel Factory nesnesi</param>
        public void UpdateDisplay(Factory factory)
        {
            if (factory == null || !_isUnlocked) return;

            _factory = factory;

            // Yildiz gosterimi
            if (_starText != null)
            {
                _starText.text = GetStarDisplay(factory.StarLevel);
            }

            // Uretim hizi
            if (_productionRateText != null)
            {
                float rate = factory.CurrentProductionRate;
                if (rate >= 1f)
                {
                    _productionRateText.text = $"{rate:F1}/sn";
                }
                else
                {
                    float perMinute = rate * 60f;
                    _productionRateText.text = $"{perMinute:F0}/dk";
                }
            }

            // Gelir/dakika
            if (_revenueText != null)
            {
                double revPerMin = factory.CurrentRevenuePerMinute;
                _revenueText.text = $"{FormatNumber(revPerMin)}/dk";
            }

            // Progress bar
            if (_progressBar != null)
            {
                _progressBar.value = factory.ProductionProgress;
            }
        }

        // =====================================================================
        // Buton Handler'lar
        // =====================================================================

        /// <summary>
        /// Acik fabrika kartina dokunuldugunda UpgradePanel acar.
        /// </summary>
        private void OnCardClicked()
        {
            if (_factory == null || !_isUnlocked)
            {
                Debug.Log("[FactoryCardUI] Kilitli fabrikaya dokunuldu, islem yok.");
                return;
            }

            Debug.Log($"[FactoryCardUI] Fabrika karti tiklandi: {_factory.Data?.FactoryName}");

            // UpgradePanel ac — factory instance ID ile
            // UIManager.Instance?.OpenPanel<UpgradePanel>();
            // Not: UpgradePanel henuz tanimlanmadiysa log basilir.
            // UpgradePanel acildiginda factory ID'si parametre olarak verilir.
        }

        /// <summary>
        /// Kilitli fabrika kartindaki "Ac" butonuna basildiginda
        /// fabrikayi acmaya calisir.
        /// </summary>
        private void OnUnlockClicked()
        {
            if (_configData == null) return;

            Debug.Log($"[FactoryCardUI] Fabrika acma deneniyor: {_configData.Name}");

            if (ServiceLocator.TryGet<ProductionManager>(out var pm))
            {
                var factory = pm.UnlockFactory(_configData.Id);
                if (factory != null)
                {
                    // Basarili — karti acik duruma gecir
                    Setup(factory, _configData);
                    Debug.Log($"[FactoryCardUI] {_configData.Name} basariyla acildi!");
                }
                else
                {
                    Debug.Log($"[FactoryCardUI] {_configData.Name} acilamadi (yetersiz bakiye veya kosul).");
                }
            }
        }

        // =====================================================================
        // Yardimci
        // =====================================================================

        /// <summary>
        /// Yildiz seviyesini gorsel yildiz karakter dizisine cevirir.
        /// Ornek: 3 -> "★★★☆☆"
        /// </summary>
        private static string GetStarDisplay(int starLevel)
        {
            const int maxStars = 5;
            int filled = Mathf.Clamp(starLevel, 0, maxStars);
            int empty = maxStars - filled;

            return new string('\u2605', filled) + new string('\u2606', empty);
        }

        /// <summary>
        /// Buyuk sayilari okunabilir formata cevirir.
        /// 1,500 -> "1.5K", 2,500,000 -> "2.5M"
        /// </summary>
        private static string FormatNumber(double value)
        {
            if (value >= 1_000_000_000) return $"{value / 1_000_000_000:F1}B";
            if (value >= 1_000_000) return $"{value / 1_000_000:F1}M";
            if (value >= 1_000) return $"{value / 1_000:F1}K";
            return $"{value:F0}";
        }
    }
}
