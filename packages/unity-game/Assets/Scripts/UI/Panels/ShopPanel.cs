// =============================================================================
// ShopPanel.cs
// Magaza UI paneli.
// Elmas paketleri, Starter Pack, reklamsiz paket, kozmetik magaza.
// Referans: docs/MONETIZATION.md Bolum 3 — IAP
// =============================================================================

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RiceFactory.Core;
using RiceFactory.Ads;

namespace RiceFactory.UI
{
    /// <summary>
    /// Magaza paneli.
    /// Elmas paketleri, starter pack (ilk 48 saat), reklamsiz paket
    /// ve kozmetik magaza (placeholder) gosterimi.
    /// </summary>
    public class ShopPanel : PanelBase
    {
        // ---------------------------------------------------------------
        // UI Referanslari
        // ---------------------------------------------------------------

        [Header("Starter Pack")]
        [SerializeField] private GameObject _starterPackSection;
        [SerializeField] private TextMeshProUGUI _starterPackTimerText;
        [SerializeField] private Button _starterPackButton;

        [Header("Elmas Paketleri")]
        [SerializeField] private Transform _gemPackContainer;
        [SerializeField] private GameObject _gemPackPrefab;

        [Header("Reklamsiz Paket")]
        [SerializeField] private GameObject _adFreeSection;
        [SerializeField] private Button _adFreeButton;
        [SerializeField] private TextMeshProUGUI _adFreePriceText;
        [SerializeField] private GameObject _adFreeOwnedBadge;

        [Header("Kozmetik Magaza")]
        [SerializeField] private GameObject _cosmeticSection;
        [SerializeField] private TextMeshProUGUI _cosmeticPlaceholderText;

        [Header("Kontrol")]
        [SerializeField] private Button _restoreButton;
        [SerializeField] private Button _closeButton;
        [SerializeField] private GameObject _purchaseLoadingOverlay;

        // ---------------------------------------------------------------
        // Durum
        // ---------------------------------------------------------------

        private IIAPManager _iapManager;
        private readonly List<GameObject> _spawnedGemPacks = new();

        // ---------------------------------------------------------------
        // PanelBase overrides
        // ---------------------------------------------------------------

        protected override void OnInitialize()
        {
            _iapManager = ServiceLocator.Get<IIAPManager>();

            _starterPackButton?.onClick.AddListener(OnStarterPackClicked);
            _adFreeButton?.onClick.AddListener(OnAdFreeClicked);
            _restoreButton?.onClick.AddListener(OnRestoreClicked);
            _closeButton?.onClick.AddListener(() => UIManager.Instance?.CloseTopPanel());
        }

        protected override void OnShow()
        {
            RefreshShop();
        }

        protected override void OnHide() { }

        private void OnDestroy()
        {
            _starterPackButton?.onClick.RemoveAllListeners();
            _adFreeButton?.onClick.RemoveAllListeners();
            _restoreButton?.onClick.RemoveAllListeners();
            _closeButton?.onClick.RemoveAllListeners();
        }

        // ---------------------------------------------------------------
        // Magaza Yenileme
        // ---------------------------------------------------------------

        private void RefreshShop()
        {
            SetupStarterPack();
            SetupGemPacks();
            SetupAdFree();
            SetupCosmeticSection();
        }

        // ---------------------------------------------------------------
        // Starter Pack
        // ---------------------------------------------------------------

        private void SetupStarterPack()
        {
            if (_starterPackSection == null) return;

            bool available = _iapManager.IsStarterPackAvailable;
            _starterPackSection.SetActive(available);

            if (available && _starterPackTimerText != null)
            {
                // TODO: Geri sayim zamanlayicisi (Update'de)
                _starterPackTimerText.text = "Sinirli sure!";
            }
        }

        private async void OnStarterPackClicked()
        {
            SetPurchaseLoading(true);

            var result = await _iapManager.PurchaseAsync("com.ricefactory.starter_pack");
            HandlePurchaseResult(result, "Starter Pack");

            SetPurchaseLoading(false);
            RefreshShop();
        }

        // ---------------------------------------------------------------
        // Elmas Paketleri
        // ---------------------------------------------------------------

        private void SetupGemPacks()
        {
            // Mevcut paketleri temizle
            foreach (var obj in _spawnedGemPacks)
            {
                if (obj != null) Destroy(obj);
            }
            _spawnedGemPacks.Clear();

            if (_gemPackPrefab == null || _gemPackContainer == null) return;

            var products = _iapManager.GetProducts();
            foreach (var product in products)
            {
                // Sadece consumable elmas paketlerini goster
                if (product.Type != IAPProductType.Consumable) continue;
                if (!product.IsAvailable) continue;

                var packObj = Instantiate(_gemPackPrefab, _gemPackContainer);
                _spawnedGemPacks.Add(packObj);

                // Isim
                var nameText = packObj.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
                if (nameText != null) nameText.text = product.DisplayName;

                // Elmas miktari
                var amountText = packObj.transform.Find("AmountText")?.GetComponent<TextMeshProUGUI>();
                if (amountText != null) amountText.text = $"{product.GemAmount:N0}";

                // Bonus
                var bonusText = packObj.transform.Find("BonusText")?.GetComponent<TextMeshProUGUI>();
                if (bonusText != null)
                {
                    bonusText.gameObject.SetActive(product.BonusPercent > 0);
                    bonusText.text = $"+%{product.BonusPercent} deger";
                }

                // Fiyat
                var priceText = packObj.transform.Find("PriceText")?.GetComponent<TextMeshProUGUI>();
                if (priceText != null) priceText.text = product.PriceUSD;

                // Satin al butonu
                var buyButton = packObj.transform.Find("BuyButton")?.GetComponent<Button>();
                if (buyButton != null)
                {
                    string capturedId = product.ProductId;
                    string capturedName = product.DisplayName;
                    buyButton.onClick.AddListener(() => OnGemPackClicked(capturedId, capturedName));
                }

                packObj.SetActive(true);
            }
        }

        private async void OnGemPackClicked(string productId, string displayName)
        {
            SetPurchaseLoading(true);

            var result = await _iapManager.PurchaseAsync(productId);
            HandlePurchaseResult(result, displayName);

            SetPurchaseLoading(false);
        }

        // ---------------------------------------------------------------
        // Reklamsiz Paket
        // ---------------------------------------------------------------

        private void SetupAdFree()
        {
            if (_adFreeSection == null) return;

            bool owned = _iapManager.IsAdFreeUnlocked;

            if (_adFreeButton != null) _adFreeButton.interactable = !owned;
            if (_adFreeOwnedBadge != null) _adFreeOwnedBadge.SetActive(owned);
            if (_adFreePriceText != null)
            {
                _adFreePriceText.text = owned ? "Satin Alindi" : "$5.99";
            }
        }

        private async void OnAdFreeClicked()
        {
            SetPurchaseLoading(true);

            var result = await _iapManager.PurchaseAsync("com.ricefactory.ad_free");
            HandlePurchaseResult(result, "Reklamsiz Deneyim");

            SetPurchaseLoading(false);
            RefreshShop();
        }

        // ---------------------------------------------------------------
        // Kozmetik Magaza (Placeholder)
        // ---------------------------------------------------------------

        private void SetupCosmeticSection()
        {
            if (_cosmeticPlaceholderText != null)
            {
                _cosmeticPlaceholderText.text = "Kozmetik magaza yakinda geliyor!";
            }
        }

        // ---------------------------------------------------------------
        // Restore
        // ---------------------------------------------------------------

        private async void OnRestoreClicked()
        {
            SetPurchaseLoading(true);

            var result = await _iapManager.RestorePurchasesAsync();

            if (result.Success)
            {
                Debug.Log($"[ShopPanel] Restore basarili: {result.RestoredCount} urun geri yuklendi.");
            }
            else
            {
                Debug.LogWarning($"[ShopPanel] Restore basarisiz: {result.Message}");
            }

            SetPurchaseLoading(false);
            RefreshShop();
        }

        // ---------------------------------------------------------------
        // Yardimci
        // ---------------------------------------------------------------

        private void HandlePurchaseResult(PurchaseResult result, string productName)
        {
            switch (result)
            {
                case PurchaseResult.Success:
                    Debug.Log($"[ShopPanel] Satin alma basarili: {productName}");
                    break;
                case PurchaseResult.Cancelled:
                    Debug.Log($"[ShopPanel] Satin alma iptal: {productName}");
                    break;
                case PurchaseResult.AlreadyOwned:
                    Debug.Log($"[ShopPanel] Zaten satin alinmis: {productName}");
                    break;
                case PurchaseResult.NotAvailable:
                    Debug.LogWarning($"[ShopPanel] Urun mevcut degil: {productName}");
                    break;
                default:
                    Debug.LogError($"[ShopPanel] Satin alma basarisiz: {productName}");
                    break;
            }
        }

        private void SetPurchaseLoading(bool loading)
        {
            if (_purchaseLoadingOverlay != null)
                _purchaseLoadingOverlay.SetActive(loading);
        }
    }
}
