// =============================================================================
// FactoryConfigs.cs
// Tum fabrika turlerinin statik veri tanimlari.
// ScriptableObject yerine kod icerisinde tanimlanir — Unity Editor gerektirmez.
//
// Referans: docs/GDD.md Bolum 3.1 — Uretim Sistemi
// Referans: docs/ECONOMY_BALANCE.md Bolum 3 — Tesis Ekonomisi
// Parametreler: packages/economy-simulator/balance_config.json facilities[]
// =============================================================================

using System.Collections.Generic;

namespace RiceFactory.Production
{
    /// <summary>
    /// 6 tesis turunu statik olarak tanimlayan yardimci sinif.
    /// Her tesis icin: id, ad, aciklama, temel maliyet, uretim parametreleri,
    /// acma maliyeti, acma sirasi ve uretim zinciri.
    ///
    /// Uretim Zinciri:
    ///   celtik -> pirinc -> pirinc_unu/pirinc_ekmegi -> pilav_tabagi -> pirinc_paketi -> asya_paketi
    /// </summary>
    public static class FactoryConfigs
    {
        // =====================================================================
        // FABRIKA TANIMLARI
        // =====================================================================

        /// <summary>
        /// Tum fabrika tanimlarini icerir. Key = factoryId.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, FactoryConfigData> All = new Dictionary<string, FactoryConfigData>
        {
            ["rice_field"] = RiceField,
            ["factory"] = RiceFactory,
            ["bakery"] = Bakery,
            ["restaurant"] = Restaurant,
            ["market"] = MarketChain,
            ["global_distribution"] = GlobalDistribution
        };

        /// <summary>Acilma sirasina gore siralanmis fabrika ID listesi.</summary>
        public static readonly string[] OrderedIds =
        {
            "rice_field", "factory", "bakery", "restaurant", "market", "global_distribution"
        };

        // =====================================================================
        // 1. PIRINC TARLASI
        // balance_config.json: facilities[0]
        // =====================================================================

        /// <summary>
        /// Pirinc Tarlasi — ilk tesis, ucretsiz.
        /// Ana urun: celtik (5 coin), Ikincil: pirinc (15 coin).
        /// Temel uretim: 12 birim/dk, 5s dongu.
        /// </summary>
        public static readonly FactoryConfigData RiceField = new()
        {
            Id = "rice_field",
            Name = "Pirinc Tarlasi",
            Description = "Pirinc uretim zincirinin baslangic noktasi. Celtik hasat eder ve pirince isler.",
            BaseCost = 100f,              // machineBaseCost
            BaseProduction = 12f,         // baseProductionPerMinute
            BasePrice = 5f,               // basePrice (celtik)
            UnlockCost = 0f,              // ucretsiz
            UnlockOrder = 1,
            BaseProductionTime = 5f,      // baseProductionTime_s
            SecondaryProductionTime = 8f, // secondaryProductionTime_s
            SecondaryPrice = 15f,         // pirinc satis fiyati
            MainProduct = "celtik",
            SecondaryProduct = "pirinc",
            ProductChain = new List<ProductChainData>
            {
                new()
                {
                    InputProductId = "",    // Girdi yok — tarla dogal uretir
                    InputQuantity = 0,
                    OutputProductId = "celtik",
                    OutputQuantity = 1,
                    RequiredStarLevel = 1
                },
                new()
                {
                    InputProductId = "celtik",
                    InputQuantity = 2,
                    OutputProductId = "pirinc",
                    OutputQuantity = 1,
                    RequiredStarLevel = 2
                }
            }
        };

        // =====================================================================
        // 2. PIRINC FABRIKASI
        // balance_config.json: facilities[1]
        // =====================================================================

        /// <summary>
        /// Pirinc Fabrikasi — pirinci una ve nisastaya isler.
        /// Ana urun: pirinc_unu (40 coin), Ikincil: pirinc_nisastasi (55 coin).
        /// Temel uretim: 5 birim/dk, 12s dongu.
        /// Acma maliyeti: 1,000 coin.
        /// </summary>
        public static readonly FactoryConfigData RiceFactory = new()
        {
            Id = "factory",
            Name = "Pirinc Fabrikasi",
            Description = "Pirinci un, nisasta ve ileri urunlere isler. Uretim zincirinin isleme halkasi.",
            BaseCost = 500f,
            BaseProduction = 5f,
            BasePrice = 40f,
            UnlockCost = 1_000f,
            UnlockOrder = 2,
            BaseProductionTime = 12f,
            SecondaryProductionTime = 15f,
            SecondaryPrice = 55f,
            MainProduct = "pirinc_unu",
            SecondaryProduct = "pirinc_nisastasi",
            ProductChain = new List<ProductChainData>
            {
                new()
                {
                    InputProductId = "pirinc",
                    InputQuantity = 3,
                    OutputProductId = "pirinc_unu",
                    OutputQuantity = 2,
                    RequiredStarLevel = 1
                },
                new()
                {
                    InputProductId = "pirinc",
                    InputQuantity = 5,
                    OutputProductId = "pirinc_nisastasi",
                    OutputQuantity = 1,
                    RequiredStarLevel = 2
                },
                new()
                {
                    InputProductId = "pirinc",
                    InputQuantity = 8,
                    OutputProductId = "sake",
                    OutputQuantity = 1,
                    RequiredStarLevel = 4
                },
                new()
                {
                    InputProductId = "pirinc",
                    InputQuantity = 4,
                    OutputProductId = "pirinc_sutu",
                    OutputQuantity = 1,
                    RequiredStarLevel = 3
                }
            }
        };

        // =====================================================================
        // 3. FIRIN
        // balance_config.json: facilities[2]
        // =====================================================================

        /// <summary>
        /// Firin — pirinc unundan ekmek, kek ve mochi uretir.
        /// Ana urun: pirinc_ekmegi (80 coin), Ikincil: mochi (180 coin).
        /// Temel uretim: 4 birim/dk, 15s dongu.
        /// Acma maliyeti: 10,000 coin.
        /// </summary>
        public static readonly FactoryConfigData Bakery = new()
        {
            Id = "bakery",
            Name = "Firin",
            Description = "Pirinc unundan ekmek, kurabiye, kek ve mochi uretir. Lezzetli pisinme halkasi.",
            BaseCost = 2_500f,
            BaseProduction = 4f,
            BasePrice = 80f,
            UnlockCost = 10_000f,
            UnlockOrder = 3,
            BaseProductionTime = 15f,
            SecondaryProductionTime = 25f,
            SecondaryPrice = 180f,
            MainProduct = "pirinc_ekmegi",
            SecondaryProduct = "mochi",
            ProductChain = new List<ProductChainData>
            {
                new()
                {
                    InputProductId = "pirinc_unu",
                    InputQuantity = 3,
                    OutputProductId = "pirinc_ekmegi",
                    OutputQuantity = 2,
                    RequiredStarLevel = 1
                },
                new()
                {
                    InputProductId = "pirinc_unu",
                    InputQuantity = 4,
                    OutputProductId = "mochi",
                    OutputQuantity = 1,
                    RequiredStarLevel = 2
                },
                new()
                {
                    InputProductId = "pirinc_unu",
                    InputQuantity = 5,
                    OutputProductId = "pirinc_keki",
                    OutputQuantity = 1,
                    RequiredStarLevel = 3
                },
                new()
                {
                    InputProductId = "pirinc_unu",
                    InputQuantity = 8,
                    OutputProductId = "pirinc_pastasi",
                    OutputQuantity = 1,
                    RequiredStarLevel = 5
                }
            }
        };

        // =====================================================================
        // 4. RESTORAN
        // balance_config.json: facilities[3]
        // =====================================================================

        /// <summary>
        /// Restoran — pirinc ve islenmis urunlerden yemek tabagi uretir.
        /// Ana urun: pilav_tabagi (150 coin), Ikincil: sushi_tabagi (350 coin).
        /// Temel uretim: 3 birim/dk, 20s dongu.
        /// Acma maliyeti: 100,000 coin.
        /// </summary>
        public static readonly FactoryConfigData Restaurant = new()
        {
            Id = "restaurant",
            Name = "Restoran",
            Description = "Pirinc ve islenmis urunlerden yemek tablagi hazirlar. Zincirin servis halkasi.",
            BaseCost = 15_000f,
            BaseProduction = 3f,
            BasePrice = 150f,
            UnlockCost = 100_000f,
            UnlockOrder = 4,
            BaseProductionTime = 20f,
            SecondaryProductionTime = 30f,
            SecondaryPrice = 350f,
            MainProduct = "pilav_tabagi",
            SecondaryProduct = "sushi_tabagi",
            ProductChain = new List<ProductChainData>
            {
                new()
                {
                    InputProductId = "pirinc",
                    InputQuantity = 5,
                    OutputProductId = "pilav_tabagi",
                    OutputQuantity = 1,
                    RequiredStarLevel = 1
                },
                new()
                {
                    InputProductId = "pirinc",
                    InputQuantity = 8,
                    OutputProductId = "sushi_tabagi",
                    OutputQuantity = 1,
                    RequiredStarLevel = 2
                },
                new()
                {
                    InputProductId = "pirinc",
                    InputQuantity = 6,
                    OutputProductId = "onigiri_set",
                    OutputQuantity = 2,
                    RequiredStarLevel = 3
                },
                new()
                {
                    InputProductId = "pirinc",
                    InputQuantity = 12,
                    OutputProductId = "gurme_omakase",
                    OutputQuantity = 1,
                    RequiredStarLevel = 5
                }
            }
        };

        // =====================================================================
        // 5. MARKET ZINCIRI
        // balance_config.json: facilities[4]
        // =====================================================================

        /// <summary>
        /// Market Zinciri — urunleri paketler ve toptan satar.
        /// Ana urun: pirinc_paketi (100 coin), Ikincil: gurme_kutu (800 coin).
        /// Temel uretim: 6 birim/dk, 10s dongu.
        /// Acma maliyeti: 1,000,000 coin.
        /// </summary>
        public static readonly FactoryConfigData MarketChain = new()
        {
            Id = "market",
            Name = "Market Zinciri",
            Description = "Urunleri paketler ve toptan dagitir. Yuksek hacim, genis erisim.",
            BaseCost = 100_000f,
            BaseProduction = 6f,
            BasePrice = 100f,
            UnlockCost = 1_000_000f,
            UnlockOrder = 5,
            BaseProductionTime = 10f,
            SecondaryProductionTime = 30f,
            SecondaryPrice = 800f,
            MainProduct = "pirinc_paketi",
            SecondaryProduct = "gurme_kutu",
            ProductChain = new List<ProductChainData>
            {
                new()
                {
                    InputProductId = "pirinc",
                    InputQuantity = 10,
                    OutputProductId = "pirinc_paketi",
                    OutputQuantity = 5,
                    RequiredStarLevel = 1
                },
                new()
                {
                    InputProductId = "pirinc_ekmegi",
                    InputQuantity = 5,
                    OutputProductId = "ekmek_sepeti",
                    OutputQuantity = 1,
                    RequiredStarLevel = 2
                },
                new()
                {
                    InputProductId = "pilav_tabagi",
                    InputQuantity = 3,
                    OutputProductId = "gurme_kutu",
                    OutputQuantity = 1,
                    RequiredStarLevel = 3
                },
                new()
                {
                    InputProductId = "sushi_tabagi",
                    InputQuantity = 5,
                    OutputProductId = "premium_set",
                    OutputQuantity = 1,
                    RequiredStarLevel = 5
                }
            }
        };

        // =====================================================================
        // 6. KURESEL DAGITIM
        // balance_config.json: facilities[5]
        // =====================================================================

        /// <summary>
        /// Kuresel Dagitim — urunleri dunya capinda ihrac eder.
        /// Ana urun: asya_paketi (5,000 coin), Ikincil: luks_ihracat (20,000 coin).
        /// Temel uretim: 0.5 birim/dk, 120s dongu.
        /// Acma maliyeti: 25,000,000 coin.
        /// </summary>
        public static readonly FactoryConfigData GlobalDistribution = new()
        {
            Id = "global_distribution",
            Name = "Kuresel Dagitim",
            Description = "Urunleri dunya capinda ihrac eder. En yuksek gelir, en yavas uretim.",
            BaseCost = 2_500_000f,
            BaseProduction = 0.5f,
            BasePrice = 5_000f,
            UnlockCost = 25_000_000f,
            UnlockOrder = 6,
            BaseProductionTime = 120f,
            SecondaryProductionTime = 300f,
            SecondaryPrice = 20_000f,
            MainProduct = "asya_paketi",
            SecondaryProduct = "luks_ihracat",
            ProductChain = new List<ProductChainData>
            {
                new()
                {
                    InputProductId = "pirinc_paketi",
                    InputQuantity = 10,
                    OutputProductId = "asya_paketi",
                    OutputQuantity = 1,
                    RequiredStarLevel = 1
                },
                new()
                {
                    InputProductId = "gurme_kutu",
                    InputQuantity = 5,
                    OutputProductId = "avrupa_paketi",
                    OutputQuantity = 1,
                    RequiredStarLevel = 3
                },
                new()
                {
                    InputProductId = "premium_set",
                    InputQuantity = 3,
                    OutputProductId = "luks_ihracat",
                    OutputQuantity = 1,
                    RequiredStarLevel = 5
                }
            }
        };

        // =====================================================================
        // YARDIMCI METOTLAR
        // =====================================================================

        /// <summary>
        /// Fabrika ID'sine gore konfigurasyon dondurur. Bulamazsa null.
        /// </summary>
        public static FactoryConfigData GetById(string factoryId)
        {
            return All.TryGetValue(factoryId, out var config) ? config : null;
        }

        /// <summary>
        /// Acilma sirasina gore siralanmis tum konfigurasyon listesini dondurur.
        /// </summary>
        public static List<FactoryConfigData> GetAllOrdered()
        {
            var list = new List<FactoryConfigData>();
            foreach (var id in OrderedIds)
            {
                if (All.TryGetValue(id, out var config))
                    list.Add(config);
            }
            return list;
        }
    }

    // =====================================================================
    // VERI YAPILARI
    // =====================================================================

    /// <summary>
    /// Tek bir fabrika turunu tanimlayan veri sinifi.
    /// ScriptableObject'e alternatif — runtime'da kullanilir.
    /// </summary>
    public class FactoryConfigData
    {
        // --- Kimlik ---
        public string Id;
        public string Name;
        public string Description;

        // --- Ekonomi (balance_config.json degerlerinden) ---
        public float BaseCost;              // machineBaseCost
        public float BaseProduction;        // baseProductionPerMinute
        public float BasePrice;             // temel urun fiyati
        public float UnlockCost;            // tesis acma maliyeti
        public int UnlockOrder;             // acilma sirasi (1 = ilk)

        // --- Uretim Sureleri ---
        public float BaseProductionTime;    // ana urun dongu suresi (saniye)
        public float SecondaryProductionTime; // ikincil urun dongu suresi (saniye)
        public float SecondaryPrice;        // ikincil urun fiyati

        // --- Urun Bilgileri ---
        public string MainProduct;          // ana urun ID
        public string SecondaryProduct;     // ikincil urun ID

        // --- Uretim Zinciri ---
        public List<ProductChainData> ProductChain;

        /// <summary>
        /// Temel gelir/dakika = basePrice x baseProductionPerMinute
        /// </summary>
        public float BaseRevenuePerMinute => BasePrice * BaseProduction;
    }

    /// <summary>
    /// Uretim zinciri giris verisi (statik tanim).
    /// ProductChainEntry'nin ScriptableObject-bagimsiz karsiligi.
    /// </summary>
    public class ProductChainData
    {
        /// <summary>Girdi urun ID (bos = girdi gerekmez).</summary>
        public string InputProductId;

        /// <summary>Gereken girdi miktari.</summary>
        public int InputQuantity;

        /// <summary>Cikti urun ID.</summary>
        public string OutputProductId;

        /// <summary>Uretilen cikti miktari.</summary>
        public int OutputQuantity;

        /// <summary>Bu zincirin acilmasi icin gereken yildiz seviyesi.</summary>
        public int RequiredStarLevel;
    }
}
