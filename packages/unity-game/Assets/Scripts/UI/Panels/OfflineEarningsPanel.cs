using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RiceFactory.Core;
using RiceFactory.Core.Events;

namespace RiceFactory.UI
{
    /// <summary>
    /// Offline kazanc popup paneli — oyuna geri donuldugunde otomatik acilir.
    ///
    /// Gosterir:
    /// - Offline gecen sure
    /// - Kazanilan coin miktari (animasyonlu sayac)
    /// - Kazanilan urun miktari
    /// - Verimlilik yuzdesi
    /// - "Topla" butonu (1x kazanc)
    /// - "Reklam Izle x2" butonu (2x kazanc — placeholder)
    ///
    /// Boot sirasinda TimeManager.CalculateOfflineEarnings sonucu varsa
    /// UIManager.ShowPopup<OfflineEarningsPanel>(offlineResult) ile acilir.
    ///
    /// ART_GUIDE 4.9 Popup Stilleri ve 4.10 Tek Elle Kullanim Kurallarina uygun.
    /// Butonlar popup'in alt kisminda, minimum 44x44pt dokunma alani.
    /// </summary>
    public class OfflineEarningsPanel : PopupBase
    {
        // ---------------------------------------------------------------
        // UI Referanslari
        // ---------------------------------------------------------------

        [Header("Baslik")]
        [SerializeField] private TextMeshProUGUI _titleText;

        [Header("Sure Gosterimi")]
        [SerializeField] private TextMeshProUGUI _durationText;

        [Header("Kazanc Gosterimi")]
        [SerializeField] private AnimatedCounter _coinEarningsCounter;
        [SerializeField] private TextMeshProUGUI _productCountText;
        [SerializeField] private TextMeshProUGUI _efficiencyText;

        [Header("Butonlar")]
        [Tooltip("1x kazanci topla")]
        [SerializeField] private Button _collectButton;
        [SerializeField] private TextMeshProUGUI _collectButtonText;

        [Tooltip("Reklam izleyerek 2x kazanc — placeholder")]
        [SerializeField] private Button _watchAdButton;
        [SerializeField] private TextMeshProUGUI _watchAdButtonText;

        [Header("Gorsel")]
        [SerializeField] private Image _coinIcon;

        // ---------------------------------------------------------------
        // State
        // ---------------------------------------------------------------

        private OfflineEarningsResult _earningsData;
        private bool _isCollected;

        // ---------------------------------------------------------------
        // PopupBase override
        // ---------------------------------------------------------------

        /// <summary>
        /// OfflineEarningsResult verisi ile baslatilir.
        /// data null veya yanlis tip ise popup kapanir.
        /// </summary>
        protected override void OnInitialize(object data)
        {
            if (data is OfflineEarningsResult result)
            {
                _earningsData = result;
            }
            else
            {
                Debug.LogWarning("[OfflineEarningsPanel] Gecersiz data tipi, popup kapatiliyor.");
                Close();
                return;
            }

            _isCollected = false;

            // UI icerigi doldur
            PopulateUI();

            // Buton listener'lari
            _collectButton?.onClick.AddListener(OnCollectClicked);
            _watchAdButton?.onClick.AddListener(OnWatchAdClicked);
        }

        private void OnDestroy()
        {
            _collectButton?.onClick.RemoveAllListeners();
            _watchAdButton?.onClick.RemoveAllListeners();
        }

        // ---------------------------------------------------------------
        // UI Doldurma
        // ---------------------------------------------------------------

        private void PopulateUI()
        {
            // Baslik
            if (_titleText != null)
                _titleText.text = "Yokken Kazandiklarin!";

            // Offline sure — okunabilir format
            if (_durationText != null)
                _durationText.text = FormatDuration(_earningsData.Duration);

            // Kazanc miktari — animasyonlu sayac (0'dan hedefe)
            if (_coinEarningsCounter != null)
            {
                _coinEarningsCounter.SetValueImmediate(0);
                // Kisa gecikme ile animasyonu baslat (popup acilma animasyonu bitsin)
                SimpleTween.DelayedCall(0.4f, () =>
                {
                    _coinEarningsCounter.SetValue(_earningsData.TotalCoins);
                });
            }

            // Urun sayisi
            if (_productCountText != null)
                _productCountText.text = $"{AnimatedCounter.FormatNumber(_earningsData.TotalProducts)} urun uretildi";

            // Verimlilik
            if (_efficiencyText != null)
                _efficiencyText.text = $"Verimlilik: %{_earningsData.Efficiency * 100:F0}";

            // Buton metinleri
            if (_collectButtonText != null)
                _collectButtonText.text = $"Topla: {AnimatedCounter.FormatNumber(_earningsData.TotalCoins)}";

            if (_watchAdButtonText != null)
                _watchAdButtonText.text = $"Reklam Izle x2: {AnimatedCounter.FormatNumber(_earningsData.TotalCoins * 2)}";

            // Zaman guvenilir degilse uyari goster
            if (!_earningsData.IsTimeReliable && _efficiencyText != null)
            {
                _efficiencyText.text += " (sinirli)";
            }
        }

        // ---------------------------------------------------------------
        // Buton Aksiyonlari
        // ---------------------------------------------------------------

        /// <summary>1x kazanci oyuncuya ekle ve popup'i kapat.</summary>
        private void OnCollectClicked()
        {
            if (_isCollected) return;
            _isCollected = true;

            CollectEarnings(1.0);
            Close();
        }

        /// <summary>
        /// Reklam izleyerek 2x kazanc.
        /// TODO: Gercek reklam SDK entegrasyonu yapilacak.
        /// Su an placeholder — direkt 2x kazanc verir.
        /// </summary>
        private void OnWatchAdClicked()
        {
            if (_isCollected) return;
            _isCollected = true;

            // Placeholder: reklam gosterim simule et
            // Gercek entegrasyonda:
            // AdManager.ShowRewardedAd(onSuccess: () => CollectEarnings(2.0), onFail: () => _isCollected = false);
            Debug.Log("[OfflineEarningsPanel] Reklam placeholder — 2x kazanc veriliyor.");
            CollectEarnings(2.0);
            Close();
        }

        /// <summary>
        /// Kazanci EconomySystem uzerinden oyuncu bakiyesine ekler.
        /// </summary>
        private void CollectEarnings(double multiplier)
        {
            var economySystem = ServiceLocator.Get<IEconomySystem>();
            if (economySystem == null)
            {
                Debug.LogError("[OfflineEarningsPanel] EconomySystem bulunamadi!");
                return;
            }

            double finalAmount = _earningsData.TotalCoins * multiplier;
            economySystem.AddCurrency(CurrencyType.Coin, finalAmount, "offline_earnings");

            Debug.Log($"[OfflineEarningsPanel] Offline kazanc toplandi: {AnimatedCounter.FormatNumber(finalAmount)} (x{multiplier})");
        }

        // ---------------------------------------------------------------
        // Yardimci — Sure Formatlama
        // ---------------------------------------------------------------

        /// <summary>
        /// TimeSpan'i okunabilir Turkce formata cevirir.
        /// Ornekler: "2 saat 15 dk", "45 dk", "8 saat"
        /// </summary>
        private static string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalHours >= 1)
            {
                int hours = (int)duration.TotalHours;
                int minutes = duration.Minutes;

                if (minutes > 0)
                    return $"{hours} saat {minutes} dk";
                else
                    return $"{hours} saat";
            }

            if (duration.TotalMinutes >= 1)
            {
                return $"{(int)duration.TotalMinutes} dk";
            }

            return "1 dk'dan az";
        }
    }
}
