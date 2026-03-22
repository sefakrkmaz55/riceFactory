// =============================================================================
// IAPManager.cs
// Uygulama ici satin alma yonetimi.
// Elmas paketleri, starter pack, reklamsiz paket tanimlamalari.
// #if IAP_ENABLED ile kosullu derleme. IAP yokken satin almayi simule eder.
// Referans: docs/MONETIZATION.md Bolum 3 — IAP
// =============================================================================

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using RiceFactory.Core;
using RiceFactory.Core.Events;

namespace RiceFactory.Ads
{
    // -------------------------------------------------------------------------
    // Interface
    // -------------------------------------------------------------------------

    /// <summary>
    /// Uygulama ici satin alma sistemi arayuzu.
    /// Urun listeleme, satin alma, dogrulama ve restore islemleri.
    /// </summary>
    public interface IIAPManager
    {
        /// <summary>Tum urun tanimlarini dondurur.</summary>
        List<IAPProduct> GetProducts();

        /// <summary>Belirtilen urunu satin alma akisini baslatir.</summary>
        Task<PurchaseResult> PurchaseAsync(string productId);

        /// <summary>Onceki satin almalari geri yukler (restore).</summary>
        Task<RestoreResult> RestorePurchasesAsync();

        /// <summary>Reklamsiz paketin satin alinmis olup olmadigini dondurur.</summary>
        bool IsAdFreeUnlocked { get; }

        /// <summary>Starter Pack'in mevcut olup olmadigini dondurur (ilk 48 saat).</summary>
        bool IsStarterPackAvailable { get; }

        /// <summary>Starter Pack'in zaten satin alinmis olup olmadigini dondurur.</summary>
        bool IsStarterPackPurchased { get; }
    }

    // -------------------------------------------------------------------------
    // Veri Modelleri
    // -------------------------------------------------------------------------

    /// <summary>IAP urun tipleri.</summary>
    public enum IAPProductType
    {
        /// <summary>Tek seferlik tuketilebilir (elmas paketi).</summary>
        Consumable,

        /// <summary>Kalici satin alma (reklamsiz, starter pack).</summary>
        NonConsumable
    }

    /// <summary>Bir IAP urununu temsil eder.</summary>
    [Serializable]
    public class IAPProduct
    {
        public string ProductId;
        public string DisplayName;
        public string Description;
        public IAPProductType Type;
        public string PriceUSD;
        public int GemAmount;
        public int BonusPercent;
        public bool IsAvailable;
    }

    /// <summary>Satin alma sonucu.</summary>
    public enum PurchaseResult
    {
        Success,
        Cancelled,
        Failed,
        AlreadyOwned,
        NotAvailable
    }

    /// <summary>Restore sonucu.</summary>
    [Serializable]
    public class RestoreResult
    {
        public bool Success;
        public int RestoredCount;
        public string Message;
    }

    // -------------------------------------------------------------------------
    // Event Tanimlamalari
    // -------------------------------------------------------------------------

    /// <summary>Basarili satin alma sonrasi tetiklenir.</summary>
    public struct IAPPurchaseCompletedEvent : IGameEvent
    {
        public string ProductId;
        public int GemAmount;
        public bool IsAdFree;
    }

    // -------------------------------------------------------------------------
    // Implementasyon
    // -------------------------------------------------------------------------

    /// <summary>
    /// IAP yoneticisi. Urun tanimlari MONETIZATION.md'den alinmistir.
    /// IAP_ENABLED tanimli degilse satin almayi simule eder (test modu).
    /// </summary>
    public class IAPManager : IIAPManager
    {
        private readonly IEventManager _eventManager;
        private readonly ISaveManager _saveManager;

        private bool _isAdFreeUnlocked;
        private bool _isStarterPackPurchased;
        private DateTime _gameFirstOpenTime;

        // Urun katalogu
        private readonly List<IAPProduct> _products;

        public bool IsAdFreeUnlocked => _isAdFreeUnlocked;
        public bool IsStarterPackPurchased => _isStarterPackPurchased;

        public bool IsStarterPackAvailable
        {
            get
            {
                if (_isStarterPackPurchased) return false;
                // Ilk 48 saat kontrolu
                var elapsed = DateTime.UtcNow - _gameFirstOpenTime;
                return elapsed.TotalHours <= 48;
            }
        }

        // =====================================================================
        // URUN TANIMLARI (MONETIZATION.md 3.2, 3.3, 3.5)
        // =====================================================================

        private static readonly IAPProduct[] ProductDefinitions =
        {
            // --- Elmas Paketleri ---
            new IAPProduct
            {
                ProductId = "com.ricefactory.gems_80",
                DisplayName = "Bir Avuc Elmas",
                Description = "80 Elmas",
                Type = IAPProductType.Consumable,
                PriceUSD = "$0.99",
                GemAmount = 80,
                BonusPercent = 0,
                IsAvailable = true
            },
            new IAPProduct
            {
                ProductId = "com.ricefactory.gems_500",
                DisplayName = "Elmas Kesesi",
                Description = "500 Elmas (+%20 deger)",
                Type = IAPProductType.Consumable,
                PriceUSD = "$4.99",
                GemAmount = 500,
                BonusPercent = 20,
                IsAvailable = true
            },
            new IAPProduct
            {
                ProductId = "com.ricefactory.gems_1200",
                DisplayName = "Elmas Sandigi",
                Description = "1,200 Elmas (+%50 deger)",
                Type = IAPProductType.Consumable,
                PriceUSD = "$9.99",
                GemAmount = 1200,
                BonusPercent = 50,
                IsAvailable = true
            },
            new IAPProduct
            {
                ProductId = "com.ricefactory.gems_2800",
                DisplayName = "Elmas Hazinesi",
                Description = "2,800 Elmas (+%75 deger)",
                Type = IAPProductType.Consumable,
                PriceUSD = "$19.99",
                GemAmount = 2800,
                BonusPercent = 75,
                IsAvailable = true
            },
            new IAPProduct
            {
                ProductId = "com.ricefactory.gems_6500",
                DisplayName = "Elmas Madeni",
                Description = "6,500 Elmas (+%100 deger)",
                Type = IAPProductType.Consumable,
                PriceUSD = "$49.99",
                GemAmount = 6500,
                BonusPercent = 100,
                IsAvailable = true
            },
            new IAPProduct
            {
                ProductId = "com.ricefactory.gems_15000",
                DisplayName = "Efsanevi Hazine",
                Description = "15,000 Elmas (+%130 deger)",
                Type = IAPProductType.Consumable,
                PriceUSD = "$99.99",
                GemAmount = 15000,
                BonusPercent = 130,
                IsAvailable = true
            },

            // --- Starter Pack ---
            new IAPProduct
            {
                ProductId = "com.ricefactory.starter_pack",
                DisplayName = "Starter Pack",
                Description = "500 Elmas + 10,000 Coin + Ozel Cerceve + Boost Token + Premium Calisan",
                Type = IAPProductType.NonConsumable,
                PriceUSD = "$2.99",
                GemAmount = 500,
                BonusPercent = 0,
                IsAvailable = true
            },

            // --- Reklamsiz Paket ---
            new IAPProduct
            {
                ProductId = "com.ricefactory.ad_free",
                DisplayName = "Reklamsiz Deneyim",
                Description = "Tum reklam odulleri otomatik verilir. Reklam izleme gerekmez.",
                Type = IAPProductType.NonConsumable,
                PriceUSD = "$5.99",
                GemAmount = 0,
                BonusPercent = 0,
                IsAvailable = true
            }
        };

        // =====================================================================
        // CONSTRUCTOR
        // =====================================================================

        public IAPManager(IEventManager eventManager, ISaveManager saveManager)
        {
            _eventManager = eventManager ?? throw new ArgumentNullException(nameof(eventManager));
            _saveManager = saveManager ?? throw new ArgumentNullException(nameof(saveManager));

            _products = new List<IAPProduct>(ProductDefinitions);

            // Oyunun ilk acilma zamanini save'den al (yoksa simdi)
            _gameFirstOpenTime = _saveManager.Data.FirstOpenTime != default
                ? _saveManager.Data.FirstOpenTime
                : DateTime.UtcNow;

            // Restore durumlarini save'den yukle
            _isAdFreeUnlocked = _saveManager.Data.IsAdFree;
            _isStarterPackPurchased = _saveManager.Data.IsStarterPackPurchased;

#if IAP_ENABLED
            InitializeNativeIAP();
#endif
        }

        // =====================================================================
        // URUN LISTELEME
        // =====================================================================

        public List<IAPProduct> GetProducts()
        {
            // Starter Pack uygunlugunu guncelle
            foreach (var product in _products)
            {
                if (product.ProductId == "com.ricefactory.starter_pack")
                {
                    product.IsAvailable = IsStarterPackAvailable;
                }
                else if (product.ProductId == "com.ricefactory.ad_free")
                {
                    product.IsAvailable = !_isAdFreeUnlocked;
                }
            }

            return _products;
        }

        // =====================================================================
        // SATIN ALMA
        // =====================================================================

        public async Task<PurchaseResult> PurchaseAsync(string productId)
        {
            var product = _products.Find(p => p.ProductId == productId);
            if (product == null)
            {
                Debug.LogError($"[IAPManager] Urun bulunamadi: {productId}");
                return PurchaseResult.NotAvailable;
            }

            if (!product.IsAvailable)
            {
                Debug.LogWarning($"[IAPManager] Urun mevcut degil: {productId}");
                return PurchaseResult.NotAvailable;
            }

            // Non-consumable zaten satin alinmis mi?
            if (product.Type == IAPProductType.NonConsumable)
            {
                if (productId == "com.ricefactory.ad_free" && _isAdFreeUnlocked)
                    return PurchaseResult.AlreadyOwned;
                if (productId == "com.ricefactory.starter_pack" && _isStarterPackPurchased)
                    return PurchaseResult.AlreadyOwned;
            }

#if IAP_ENABLED
            return await PurchaseNativeAsync(product);
#else
            // IAP yokken simule et — direkt basarili
            Debug.Log($"[IAPManager] (Test) Satin alma simulasyonu: {product.DisplayName}");
            await Task.CompletedTask;
            ProcessSuccessfulPurchase(product);
            return PurchaseResult.Success;
#endif
        }

        // =====================================================================
        // RESTORE
        // =====================================================================

        public async Task<RestoreResult> RestorePurchasesAsync()
        {
#if IAP_ENABLED
            return await RestoreNativeAsync();
#else
            Debug.Log("[IAPManager] (Test) Restore simulasyonu.");
            await Task.CompletedTask;
            return new RestoreResult
            {
                Success = true,
                RestoredCount = 0,
                Message = "Test modunda restore yapildi."
            };
#endif
        }

        // =====================================================================
        // SATIN ALMA SONRASI ISLEM
        // =====================================================================

        private void ProcessSuccessfulPurchase(IAPProduct product)
        {
            bool isAdFreeGrant = false;

            switch (product.ProductId)
            {
                // Elmas paketleri
                case "com.ricefactory.gems_80":
                case "com.ricefactory.gems_500":
                case "com.ricefactory.gems_1200":
                case "com.ricefactory.gems_2800":
                case "com.ricefactory.gems_6500":
                case "com.ricefactory.gems_15000":
                    GrantGems(product.GemAmount, product.ProductId);
                    break;

                // Starter Pack
                case "com.ricefactory.starter_pack":
                    GrantGems(500, "starter_pack");
                    GrantCoins(10000, "starter_pack");
                    _isStarterPackPurchased = true;
                    _saveManager.Data.IsStarterPackPurchased = true;
                    // TODO: Ozel cerceve, boost token, premium calisan unlock
                    break;

                // Reklamsiz Paket
                case "com.ricefactory.ad_free":
                    _isAdFreeUnlocked = true;
                    _saveManager.Data.IsAdFree = true;
                    isAdFreeGrant = true;
                    break;
            }

            _eventManager.Publish(new IAPPurchaseCompletedEvent
            {
                ProductId = product.ProductId,
                GemAmount = product.GemAmount,
                IsAdFree = isAdFreeGrant
            });

            _saveManager.SaveLocal();
            Debug.Log($"[IAPManager] Satin alma tamamlandi: {product.DisplayName}");
        }

        // =====================================================================
        // YARDIMCI
        // =====================================================================

        private void GrantGems(int amount, string reason)
        {
            if (ServiceLocator.TryGet<IEconomySystem>(out var economy))
            {
                economy.AddCurrency(CurrencyType.Gem, amount, $"iap_{reason}");
            }
        }

        private void GrantCoins(double amount, string reason)
        {
            if (ServiceLocator.TryGet<IEconomySystem>(out var economy))
            {
                economy.AddCurrency(CurrencyType.Coin, amount, $"iap_{reason}");
            }
        }

        // =====================================================================
        // NATIVE IAP KATMANI (IAP_ENABLED)
        // =====================================================================

#if IAP_ENABLED
        private void InitializeNativeIAP()
        {
            // TODO: Unity IAP SDK baslatma
            // var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
            // foreach (var product in _products)
            // {
            //     var type = product.Type == IAPProductType.Consumable
            //         ? UnityEngine.Purchasing.ProductType.Consumable
            //         : UnityEngine.Purchasing.ProductType.NonConsumable;
            //     builder.AddProduct(product.ProductId, type);
            // }
            // UnityPurchasing.Initialize(this, builder);

            Debug.Log("[IAPManager] Native IAP SDK baslatiliyor...");
        }

        private async Task<PurchaseResult> PurchaseNativeAsync(IAPProduct product)
        {
            // TODO: Unity IAP SDK uzerinden satin alma
            // var storeProduct = _storeController.products.WithID(product.ProductId);
            // _storeController.InitiatePurchase(storeProduct);

            Debug.LogWarning($"[IAPManager] Native IAP henuz entegre edilmedi: {product.ProductId}");
            await Task.CompletedTask;
            return PurchaseResult.Failed;
        }

        private async Task<RestoreResult> RestoreNativeAsync()
        {
            // TODO: Platform bazli restore
            // Apple: otomatik, Google: manuel restore gerekebilir

            Debug.LogWarning("[IAPManager] Native restore henuz entegre edilmedi.");
            await Task.CompletedTask;
            return new RestoreResult
            {
                Success = false,
                RestoredCount = 0,
                Message = "Native IAP henuz entegre edilmedi."
            };
        }
#endif
    }
}
