// =============================================================================
// OrderSystem.cs
// Siparis (Quest) sistemi — 5 siparis turu, itibar entegrasyonu.
//
// Referans: docs/GDD.md Bolum 3.6 — Siparis Sistemi
// Referans: docs/ECONOMY_BALANCE.md — Siparis parametreleri
// Parametreler: balance_config.json orders.*
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using RiceFactory.Core;
using RiceFactory.Core.Events;
using RiceFactory.Data.Save;
using UnityEngine;

namespace RiceFactory.Economy
{
    /// <summary>
    /// Siparis (Quest) sistemi.
    /// 5 siparis turu: Normal, Acil, VIP, Toplu, Efsanevi.
    ///
    /// Siparis tahtasinda 3 siparis gorunur (balance_config.json: orders.boardSize = 3).
    /// Yenileme suresi: 15 dk (balance_config.json: orders.refreshMinutes = 15).
    /// Basarili tamamlama -> Itibar puani kazanilir.
    ///
    /// Referans: docs/GDD.md Bolum 3.6
    /// </summary>
    public class OrderSystem
    {
        private readonly IBalanceConfig _config;
        private readonly ISaveManager _saveManager;
        private readonly IEventManager _eventManager;
        private readonly CurrencySystem _currencySystem;

        /// <summary>Aktif siparis listesi.</summary>
        private readonly List<Order> _activeOrders = new();

        /// <summary>Siparis tahtasindaki bekleyen siparisler.</summary>
        private readonly List<Order> _boardOrders = new();

        /// <summary>Bugun tamamlanan siparis sayisi.</summary>
        private int _dailyCompletedCount;

        /// <summary>Siparis tahtasinin son yenilenme zamani.</summary>
        private float _boardRefreshTimer;

        // =====================================================================
        // SIPARIS TURU TANIMLARI
        // =====================================================================

        /// <summary>
        /// 5 siparis turu tanimi.
        /// Referans: docs/GDD.md Bolum 3.6 — Siparis Turleri tablosu
        /// </summary>
        public static readonly IReadOnlyDictionary<OrderCategory, OrderTypeDefinition> TypeDefinitions =
            new Dictionary<OrderCategory, OrderTypeDefinition>
            {
                // Normal Siparis: 30 dk, tek urun, dusuk miktar, x2 odul
                [OrderCategory.Normal] = new OrderTypeDefinition
                {
                    Category = OrderCategory.Normal,
                    DisplayName = "Normal Siparis",
                    TimeLimitMinutes = 30,
                    RewardMultiplier = 2f,
                    MinQuantity = 5,
                    MaxQuantity = 20,
                    MaxProductTypes = 1,
                    UnlockRequirement = "rice_field" // Baslangictan acik
                },

                // Acil Siparis: 10 dk, tek urun, orta miktar, x5 odul
                [OrderCategory.Urgent] = new OrderTypeDefinition
                {
                    Category = OrderCategory.Urgent,
                    DisplayName = "Acil Siparis",
                    TimeLimitMinutes = 10,
                    RewardMultiplier = 5f,
                    MinQuantity = 10,
                    MaxQuantity = 40,
                    MaxProductTypes = 1,
                    UnlockRequirement = "factory" // Fabrika acilinca
                },

                // VIP Siparis: 1 saat, birden fazla urun, x8 odul
                [OrderCategory.VIP] = new OrderTypeDefinition
                {
                    Category = OrderCategory.VIP,
                    DisplayName = "VIP Siparis",
                    TimeLimitMinutes = 60,
                    RewardMultiplier = 8f,
                    MinQuantity = 5,
                    MaxQuantity = 30,
                    MaxProductTypes = 3,
                    UnlockRequirement = "restaurant" // Restoran acilinca
                },

                // Toplu Siparis: 4 saat, tek urun, cok yuksek miktar, x3 odul
                [OrderCategory.Bulk] = new OrderTypeDefinition
                {
                    Category = OrderCategory.Bulk,
                    DisplayName = "Toplu Siparis",
                    TimeLimitMinutes = 240,
                    RewardMultiplier = 3f,
                    MinQuantity = 50,
                    MaxQuantity = 200,
                    MaxProductTypes = 1,
                    UnlockRequirement = "market" // Market acilinca
                },

                // Efsanevi Siparis: 24 saat, birden fazla 5 yildiz urun, x15 odul + ozel odul
                [OrderCategory.Legendary] = new OrderTypeDefinition
                {
                    Category = OrderCategory.Legendary,
                    DisplayName = "Efsanevi Siparis",
                    TimeLimitMinutes = 1440,
                    RewardMultiplier = 15f,
                    MinQuantity = 10,
                    MaxQuantity = 50,
                    MaxProductTypes = 3,
                    UnlockRequirement = "global_distribution" // Kuresel Dagitim acilinca
                }
            };

        // =====================================================================
        // URUNLER — Siparis icin kullanilabilecek urun havuzu
        // =====================================================================

        /// <summary>
        /// Oyuncu seviyesine/tesis durumuna gore siparis uretilebilecek urunler.
        /// Basit: her tesisin ana ve ikincil urunleri.
        /// </summary>
        private static readonly string[][] ProductsByFacility =
        {
            new[] { "celtik", "pirinc" },                           // rice_field
            new[] { "pirinc_unu", "pirinc_nisastasi" },             // factory
            new[] { "pirinc_ekmegi", "mochi" },                     // bakery
            new[] { "pilav_tabagi", "sushi_tabagi" },               // restaurant
            new[] { "pirinc_paketi", "gurme_kutu" },                // market
            new[] { "asya_paketi", "luks_ihracat" }                 // global_distribution
        };

        /// <summary>Tesislerin ID siralamasi (ProductsByFacility ile eslesir).</summary>
        private static readonly string[] FacilityOrder =
        {
            "rice_field", "factory", "bakery", "restaurant", "market", "global_distribution"
        };

        // =====================================================================
        // PUBLIC ERISIMCILER
        // =====================================================================

        /// <summary>Aktif (kabul edilmis) siparisler.</summary>
        public IReadOnlyList<Order> ActiveOrders => _activeOrders;

        /// <summary>Siparis tahtasindaki bekleyen siparisler.</summary>
        public IReadOnlyList<Order> BoardOrders => _boardOrders;

        /// <summary>Bugun tamamlanan siparis sayisi.</summary>
        public int DailyCompletedCount => _dailyCompletedCount;

        /// <summary>Siparis tahtasi yenilenmesine kalan sure (saniye).</summary>
        public float BoardRefreshRemainingTime
        {
            get
            {
                float refreshInterval = _config.GetFloat("orders.refreshMinutes", 15f) * 60f;
                return Mathf.Max(0f, refreshInterval - _boardRefreshTimer);
            }
        }

        // =====================================================================
        // CONSTRUCTOR
        // =====================================================================

        public OrderSystem(
            IBalanceConfig config,
            ISaveManager saveManager,
            IEventManager eventManager,
            CurrencySystem currencySystem)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _saveManager = saveManager ?? throw new ArgumentNullException(nameof(saveManager));
            _eventManager = eventManager ?? throw new ArgumentNullException(nameof(eventManager));
            _currencySystem = currencySystem ?? throw new ArgumentNullException(nameof(currencySystem));

            _eventManager.Subscribe<GameTickEvent>(OnGameTick);
        }

        // =====================================================================
        // BASLANGIC
        // =====================================================================

        /// <summary>
        /// Siparis tahtasini ilk doldurur. Boot sirasinda cagirilir.
        /// </summary>
        public void Initialize()
        {
            int boardSize = _config.GetInt("orders.boardSize", 3);

            if (_boardOrders.Count < boardSize)
            {
                RefreshBoard();
            }
        }

        // =====================================================================
        // SIPARIS TAHTASI YONETIMI
        // =====================================================================

        /// <summary>
        /// Siparis tahtasini yeniler.
        /// Mevcut tahtadaki kabul edilmemis siparisleri kaldirir ve yenilerini olusturur.
        ///
        /// Referans: GDD 3.6 — "Siparis tahtasinda 3 siparis gorunur (yenileme: 15 dk)"
        /// </summary>
        public void RefreshBoard()
        {
            _boardOrders.Clear();
            _boardRefreshTimer = 0f;

            int boardSize = _config.GetInt("orders.boardSize", 3);

            for (int i = 0; i < boardSize; i++)
            {
                var order = GenerateRandomOrder();
                if (order != null)
                {
                    _boardOrders.Add(order);
                }
            }
        }

        /// <summary>
        /// Tahtadaki bir siparisi kabul eder (aktif siparislere tasir).
        /// </summary>
        /// <param name="orderId">Kabul edilecek siparisin ID'si</param>
        /// <returns>Kabul edildiyse true</returns>
        public bool AcceptOrder(string orderId)
        {
            var order = _boardOrders.FirstOrDefault(o => o.Id == orderId);
            if (order == null)
            {
                Debug.LogWarning($"[OrderSystem] Siparis bulunamadi: {orderId}");
                return false;
            }

            _boardOrders.Remove(order);
            order.AcceptedTime = Time.time;
            order.Status = OrderStatus.Active;
            _activeOrders.Add(order);

            Debug.Log($"[OrderSystem] Siparis kabul edildi: {order.DisplayName} ({order.Category})");
            return true;
        }

        // =====================================================================
        // SIPARIS TAMAMLAMA
        // =====================================================================

        /// <summary>
        /// Aktif bir siparisi tamamlar.
        /// Kosullar: Urun stogu yeterli + sure dolmamis.
        ///
        /// Basarili tamamlama:
        ///   - Coin odulu = urun degeri x odul carpani
        ///   - Itibar puani kazanilir (balance_config.json: orders.reputationPerOrder = 10)
        ///   - OrderCompletedEvent firlatilir
        ///
        /// Referans: GDD 3.6 — Siparis Akisi
        /// </summary>
        /// <param name="orderId">Tamamlanacak siparisin ID'si</param>
        /// <param name="inventoryCheck">Stok kontrol fonksiyonu (productId, quantity) -> yeterli mi?</param>
        /// <param name="inventoryConsume">Stoktan dusme fonksiyonu (productId, quantity)</param>
        /// <returns>Tamamlandiysa odul miktari, aksi halde -1</returns>
        public double TryCompleteOrder(
            string orderId,
            Func<string, int, bool> inventoryCheck,
            Action<string, int> inventoryConsume)
        {
            var order = _activeOrders.FirstOrDefault(o => o.Id == orderId);
            if (order == null)
            {
                Debug.LogWarning($"[OrderSystem] Aktif siparis bulunamadi: {orderId}");
                return -1;
            }

            // Sure kontrolu
            if (order.IsExpired)
            {
                Debug.Log($"[OrderSystem] Siparisin suresi dolmus: {orderId}");
                ExpireOrder(order);
                return -1;
            }

            // Stok kontrolu — tum gereksinimleri kontrol et
            foreach (var req in order.Requirements)
            {
                if (inventoryCheck == null || !inventoryCheck(req.ProductId, req.Quantity))
                {
                    Debug.Log($"[OrderSystem] Yetersiz stok: {req.ProductId} x{req.Quantity}");
                    return -1;
                }
            }

            // Stoktan dus
            foreach (var req in order.Requirements)
            {
                inventoryConsume?.Invoke(req.ProductId, req.Quantity);
            }

            // Odul hesapla
            double baseReward = CalculateOrderReward(order);

            // Coin ekle
            _currencySystem.AddCoins(baseReward, $"Order:{order.Id}");

            // Itibar puani ekle
            int reputationGain = _config.GetInt("orders.reputationPerOrder", 10);
            _currencySystem.AddReputation(reputationGain, $"OrderComplete:{order.Id}");

            // Durumu guncelle
            order.Status = OrderStatus.Completed;
            _activeOrders.Remove(order);
            _dailyCompletedCount++;

            // Event firlat
            _eventManager.Publish(new OrderCompletedEvent
            {
                OrderId = order.Id,
                Type = MapCategoryToOrderType(order.Category),
                Reward = baseReward
            });

            Debug.Log($"[OrderSystem] Siparis tamamlandi: {order.DisplayName}, Odul: {baseReward:N0} coin, Itibar: +{reputationGain}");
            return baseReward;
        }

        // =====================================================================
        // SIPARIS SURESI VE TICK
        // =====================================================================

        /// <summary>
        /// Her frame cagirilir. Aktif siparislerin zamanlayicisini ve tahta yenilemesini ilerletir.
        /// </summary>
        private void OnGameTick(GameTickEvent e)
        {
            // Tahta yenileme zamanlayicisi
            _boardRefreshTimer += e.DeltaTime;
            float refreshInterval = _config.GetFloat("orders.refreshMinutes", 15f) * 60f;

            if (_boardRefreshTimer >= refreshInterval)
            {
                RefreshBoard();
            }

            // Suresi dolan aktif siparisleri kontrol et
            for (int i = _activeOrders.Count - 1; i >= 0; i--)
            {
                var order = _activeOrders[i];
                if (order.IsExpired)
                {
                    ExpireOrder(order);
                }
            }
        }

        /// <summary>
        /// Suresi dolan bir siparisi iptal eder.
        /// Kucuk itibar kaybi olur.
        ///
        /// Referans: GDD 3.6 — "Sure dolarsa siparis iptal, kucuk itibar kaybi."
        /// </summary>
        private void ExpireOrder(Order order)
        {
            order.Status = OrderStatus.Expired;
            _activeOrders.Remove(order);

            int reputationLoss = 5; // Sabit kucuk itibar kaybi

            _eventManager.Publish(new OrderExpiredEvent
            {
                OrderId = order.Id,
                ReputationLoss = reputationLoss
            });

            Debug.Log($"[OrderSystem] Siparisin suresi doldu: {order.DisplayName}, Itibar: -{reputationLoss}");
        }

        // =====================================================================
        // SIPARIS OLUSTURMA (RASTGELE)
        // =====================================================================

        /// <summary>
        /// Oyuncunun mevcut tesis durumuna uygun rastgele siparis olusturur.
        /// Siparis turu, oyuncunun acik tesislerine gore belirlenir.
        /// </summary>
        private Order GenerateRandomOrder()
        {
            // Kullanilabilir siparis turlerini belirle
            var availableCategories = GetAvailableOrderCategories();
            if (availableCategories.Count == 0)
                return null;

            // Rastgele tur sec
            var category = availableCategories[UnityEngine.Random.Range(0, availableCategories.Count)];
            var definition = TypeDefinitions[category];

            // Kullanilabilir urunleri belirle
            var availableProducts = GetAvailableProducts();
            if (availableProducts.Count == 0)
                return null;

            // Gereksinimler olustur
            var requirements = new List<OrderRequirement>();
            int productCount = Mathf.Min(
                UnityEngine.Random.Range(1, definition.MaxProductTypes + 1),
                availableProducts.Count
            );

            // Kullanilan urunleri tekrarlamamak icin karistir
            var shuffled = new List<string>(availableProducts);
            ShuffleList(shuffled);

            for (int i = 0; i < productCount; i++)
            {
                int quantity = UnityEngine.Random.Range(definition.MinQuantity, definition.MaxQuantity + 1);
                requirements.Add(new OrderRequirement
                {
                    ProductId = shuffled[i],
                    Quantity = quantity
                });
            }

            // Siparis olustur
            string orderId = $"order_{Guid.NewGuid().ToString("N").Substring(0, 8)}";

            return new Order
            {
                Id = orderId,
                Category = category,
                DisplayName = definition.DisplayName,
                Requirements = requirements,
                RewardMultiplier = definition.RewardMultiplier,
                TimeLimitSeconds = definition.TimeLimitMinutes * 60f,
                CreatedTime = Time.time,
                AcceptedTime = -1f,
                Status = OrderStatus.Available
            };
        }

        /// <summary>Oyuncunun acik tesislerine gore kullanilabilir siparis turlerini dondurur.</summary>
        private List<OrderCategory> GetAvailableOrderCategories()
        {
            var categories = new List<OrderCategory>();
            var facilities = _saveManager.Data.Facilities;
            if (facilities == null) return categories;

            var unlockedTypes = new HashSet<string>();
            foreach (var f in facilities)
            {
                if (f.IsUnlocked)
                    unlockedTypes.Add(f.FacilityType);
            }

            foreach (var kvp in TypeDefinitions)
            {
                if (unlockedTypes.Contains(kvp.Value.UnlockRequirement))
                {
                    categories.Add(kvp.Key);
                }
            }

            return categories;
        }

        /// <summary>Oyuncunun uretilebilir urun listesini dondurur.</summary>
        private List<string> GetAvailableProducts()
        {
            var products = new List<string>();
            var facilities = _saveManager.Data.Facilities;
            if (facilities == null) return products;

            var unlockedTypes = new HashSet<string>();
            foreach (var f in facilities)
            {
                if (f.IsUnlocked)
                    unlockedTypes.Add(f.FacilityType);
            }

            for (int i = 0; i < FacilityOrder.Length; i++)
            {
                if (unlockedTypes.Contains(FacilityOrder[i]))
                {
                    products.AddRange(ProductsByFacility[i]);
                }
            }

            return products;
        }

        // =====================================================================
        // ODUL HESAPLAMA
        // =====================================================================

        /// <summary>
        /// Siparis odulunu hesaplar.
        /// Formul: toplam urun degeri x odul carpani
        ///
        /// Urun fiyatlari balance_config.json'daki basePrice degerlerinden alinir.
        /// Basitlestirilmis: her urunun bir tahmini fiyati kullanilir.
        /// </summary>
        private double CalculateOrderReward(Order order)
        {
            double totalValue = 0;

            foreach (var req in order.Requirements)
            {
                float unitPrice = GetProductBasePrice(req.ProductId);
                totalValue += unitPrice * req.Quantity;
            }

            return totalValue * order.RewardMultiplier;
        }

        /// <summary>
        /// Urun temel fiyatini dondurur.
        /// Referans: docs/ECONOMY_BALANCE.md Bolum 2.3 — Temel Satis Fiyatlari
        /// </summary>
        private float GetProductBasePrice(string productId)
        {
            return productId switch
            {
                "celtik"             => 5f,
                "pirinc"             => 15f,
                "pirinc_unu"         => 40f,
                "pirinc_nisastasi"   => 55f,
                "pirinc_sirkesi"     => 120f,
                "pirinc_sutu"        => 80f,
                "sake"               => 300f,
                "pirinc_ekmegi"      => 80f,
                "pirinc_kurabiyesi"  => 110f,
                "pirinc_keki"        => 200f,
                "mochi"              => 180f,
                "pirinc_pastasi"     => 500f,
                "pilav_tabagi"       => 150f,
                "sushi_tabagi"       => 350f,
                "risotto"            => 400f,
                "onigiri_set"        => 250f,
                "paella"             => 700f,
                "gurme_omakase"      => 2_000f,
                "pirinc_paketi"      => 100f,
                "ekmek_sepeti"       => 300f,
                "gurme_kutu"         => 800f,
                "premium_set"        => 2_500f,
                "asya_paketi"        => 5_000f,
                "avrupa_paketi"      => 6_000f,
                "luks_ihracat"       => 20_000f,
                _                    => 10f
            };
        }

        // =====================================================================
        // YARDIMCI
        // =====================================================================

        /// <summary>OrderCategory -> EventManager'daki OrderType eslemesi.</summary>
        private static OrderType MapCategoryToOrderType(OrderCategory category)
        {
            return category switch
            {
                OrderCategory.Normal    => OrderType.Normal,
                OrderCategory.Urgent    => OrderType.Special,
                OrderCategory.VIP       => OrderType.Weekly,
                OrderCategory.Bulk      => OrderType.Daily,
                OrderCategory.Legendary => OrderType.Special,
                _                       => OrderType.Normal
            };
        }

        /// <summary>Fisher-Yates karistirma.</summary>
        private static void ShuffleList<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        /// <summary>Gunluk sayaci sifirlar. Her gun basinda cagirilmalidir.</summary>
        public void ResetDailyCounter()
        {
            _dailyCompletedCount = 0;
        }

        /// <summary>EventManager aboneligini temizler.</summary>
        public void Dispose()
        {
            _eventManager.Unsubscribe<GameTickEvent>(OnGameTick);
        }
    }

    // =====================================================================
    // VERI YAPILARI
    // =====================================================================

    /// <summary>
    /// GDD'deki 5 siparis kategorisi.
    /// Referans: docs/GDD.md Bolum 3.6
    /// </summary>
    public enum OrderCategory
    {
        Normal,     // 30 dk, tek urun, dusuk miktar, x2
        Urgent,     // 10 dk, tek urun, orta miktar, x5
        VIP,        // 1 saat, birden fazla urun, x8
        Bulk,       // 4 saat, tek urun, cok yuksek miktar, x3
        Legendary   // 24 saat, birden fazla 5 yildiz urun, x15
    }

    /// <summary>Siparis durumu.</summary>
    public enum OrderStatus
    {
        Available,  // Tahtada bekliyor
        Active,     // Oyuncu tarafindan kabul edildi
        Completed,  // Basariyla tamamlandi
        Expired     // Suresi doldu
    }

    /// <summary>Siparis turu tanimi (statik konfigurasyon).</summary>
    public class OrderTypeDefinition
    {
        public OrderCategory Category;
        public string DisplayName;
        public int TimeLimitMinutes;
        public float RewardMultiplier;
        public int MinQuantity;
        public int MaxQuantity;
        public int MaxProductTypes;
        public string UnlockRequirement; // Gerekli tesis ID
    }

    /// <summary>Bir siparisin gerektirdigi tek bir urun ve miktar.</summary>
    public class OrderRequirement
    {
        public string ProductId;
        public int Quantity;
    }

    /// <summary>
    /// Tek bir siparis instance'i.
    /// Olusturulma, kabul ve sure bilgilerini icerir.
    /// </summary>
    public class Order
    {
        public string Id;
        public OrderCategory Category;
        public string DisplayName;
        public List<OrderRequirement> Requirements;
        public float RewardMultiplier;
        public float TimeLimitSeconds;
        public float CreatedTime;
        public float AcceptedTime;
        public OrderStatus Status;

        /// <summary>Siparisin suresi doldu mu? (Kabul edildiyse AcceptedTime'dan itibaren).</summary>
        public bool IsExpired
        {
            get
            {
                if (Status != OrderStatus.Active) return false;
                if (AcceptedTime < 0) return false;
                return (Time.time - AcceptedTime) >= TimeLimitSeconds;
            }
        }

        /// <summary>Kalan sure (saniye).</summary>
        public float RemainingTime
        {
            get
            {
                if (AcceptedTime < 0) return TimeLimitSeconds;
                float elapsed = Time.time - AcceptedTime;
                return Mathf.Max(0f, TimeLimitSeconds - elapsed);
            }
        }

        /// <summary>Tahmini odul (UI gosterimi icin).</summary>
        public double EstimatedReward
        {
            get
            {
                double total = 0;
                foreach (var req in Requirements)
                {
                    total += req.Quantity * 10; // Basit tahmin
                }
                return total * RewardMultiplier;
            }
        }
    }
}
