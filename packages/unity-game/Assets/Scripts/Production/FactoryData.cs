using System;
using System.Collections.Generic;
using UnityEngine;

namespace RiceFactory.Production
{
    /// <summary>
    /// Tesis verilerini tutan ScriptableObject.
    /// Unity Editor'de her tesis icin bir asset olusturulur.
    ///
    /// Referans: docs/ECONOMY_BALANCE.md Bolum 3 — Tesis Ekonomisi
    /// Parametreler: balance_config.json facilities[]
    /// </summary>
    [CreateAssetMenu(fileName = "NewFactoryData", menuName = "RiceFactory/Factory Data", order = 1)]
    public class FactoryData : ScriptableObject
    {
        [Header("Kimlik")]
        [Tooltip("Tesisin benzersiz kimlik kodu (ornek: rice_field, factory, bakery)")]
        [SerializeField] private string _factoryId;

        [Tooltip("Tesisin goruntulenen adi")]
        [SerializeField] private string _factoryName;

        [Tooltip("Tesis aciklamasi")]
        [TextArea(2, 4)]
        [SerializeField] private string _description;

        [Header("Ekonomi")]
        [Tooltip("Makine temel maliyeti (balance_config: facilities[].machineBaseCost)")]
        [SerializeField] private float _baseCost;

        [Tooltip("Temel uretim miktari / dakika (balance_config: facilities[].baseProductionPerMinute)")]
        [SerializeField] private float _baseProduction;

        [Tooltip("Temel urun fiyati (balance_config: facilities[].basePrice)")]
        [SerializeField] private float _basePrice;

        [Tooltip("Tesis acma maliyeti (balance_config: facilities[].unlockCost)")]
        [SerializeField] private float _unlockCost;

        [Tooltip("Tesisin acilma sirasi (1 = ilk)")]
        [SerializeField] private int _unlockOrder;

        [Header("Uretim")]
        [Tooltip("Ana urun uretim suresi (saniye)")]
        [SerializeField] private float _baseProductionTime;

        [Tooltip("Ikincil urun uretim suresi (saniye)")]
        [SerializeField] private float _secondaryProductionTime;

        [Tooltip("Ikincil urun satis fiyati")]
        [SerializeField] private float _secondaryPrice;

        [Header("Urun Zinciri")]
        [Tooltip("Bu tesisin uretim zinciri (girdi -> cikti)")]
        [SerializeField] private List<ProductChainEntry> _productChain = new();

        [Header("Gorseller")]
        [Tooltip("Tesis ikonu / sprite")]
        [SerializeField] private Sprite _facilityIcon;

        [Tooltip("Tesis arka plan sprite")]
        [SerializeField] private Sprite _facilityBackground;

        [Tooltip("Ana urun sprite")]
        [SerializeField] private Sprite _mainProductSprite;

        [Tooltip("Ikincil urun sprite")]
        [SerializeField] private Sprite _secondaryProductSprite;

        // --- Public Erisimciler ---
        public string FactoryId => _factoryId;
        public string FactoryName => _factoryName;
        public string Description => _description;
        public float BaseCost => _baseCost;
        public float BaseProduction => _baseProduction;
        public float BasePrice => _basePrice;
        public float UnlockCost => _unlockCost;
        public int UnlockOrder => _unlockOrder;
        public float BaseProductionTime => _baseProductionTime;
        public float SecondaryProductionTime => _secondaryProductionTime;
        public float SecondaryPrice => _secondaryPrice;
        public IReadOnlyList<ProductChainEntry> ProductChain => _productChain;
        public Sprite FacilityIcon => _facilityIcon;
        public Sprite FacilityBackground => _facilityBackground;
        public Sprite MainProductSprite => _mainProductSprite;
        public Sprite SecondaryProductSprite => _secondaryProductSprite;

        /// <summary>
        /// Temel uretim hizi (birim/dakika).
        /// balance_config: facilities[].baseProductionPerMinute
        /// </summary>
        public float BaseProductionPerMinute => _baseProduction;

        /// <summary>
        /// Temel gelir/dakika = basePrice x baseProductionPerMinute
        /// </summary>
        public float BaseRevenuePerMinute => _basePrice * _baseProduction;

        /// <summary>
        /// Belirli bir urun zinciri girdisini dondurur.
        /// </summary>
        public ProductChainEntry GetChainEntry(int index)
        {
            if (index < 0 || index >= _productChain.Count) return null;
            return _productChain[index];
        }

        /// <summary>
        /// FactoryConfigData'dan runtime'da deger yuklemek icin yardimci metot.
        /// Editor'de balance_config.json import, runtime'da FactoryConfigs'ten doldurma icin kullanilir.
        /// </summary>
        public void SetFromConfig(
            string id, string name, string desc,
            float baseCost, float baseProduction, float basePrice,
            float unlockCost, int order,
            float productionTime, float secondaryTime, float secondaryPrice)
        {
            _factoryId = id;
            _factoryName = name;
            _description = desc;
            _baseCost = baseCost;
            _baseProduction = baseProduction;
            _basePrice = basePrice;
            _unlockCost = unlockCost;
            _unlockOrder = order;
            _baseProductionTime = productionTime;
            _secondaryProductionTime = secondaryTime;
            _secondaryPrice = secondaryPrice;
        }

        /// <summary>
        /// Uretim zinciri girdilerini runtime'da ayarlar.
        /// </summary>
        public void SetProductChain(List<ProductChainEntry> chain)
        {
            _productChain = chain ?? new List<ProductChainEntry>();
        }
    }

    /// <summary>
    /// Uretim zinciri girisi: hangi girdiden hangi cikti uretilir.
    /// Ornek: celtik -> pirinc, pirinc_unu -> pirinc_ekmegi
    /// </summary>
    [Serializable]
    public class ProductChainEntry
    {
        [Tooltip("Girdi urun ID (bos ise girdi gerekmez, ornek: tarla)")]
        public string inputProductId;

        [Tooltip("Gereken girdi miktari")]
        public int inputQuantity;

        [Tooltip("Cikti urun ID")]
        public string outputProductId;

        [Tooltip("Uretilen cikti miktari")]
        public int outputQuantity;

        [Tooltip("Bu urun zinciri hangi yildiz seviyesinde acilir")]
        public int requiredStarLevel;
    }
}
