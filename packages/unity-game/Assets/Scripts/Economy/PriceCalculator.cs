using System;
using RiceFactory.Core;
using RiceFactory.Data.Save;
using UnityEngine;

namespace RiceFactory.Economy
{
    /// <summary>
    /// Tum ekonomik hesaplamalari icerir: upgrade maliyetleri, uretim hizi, satis fiyati, ROI.
    /// Tum formuller ECONOMY_BALANCE.md'den birebir uygulanmistir.
    ///
    /// Referans: docs/ECONOMY_BALANCE.md
    /// Parametreler: packages/economy-simulator/balance_config.json
    /// </summary>
    public class PriceCalculator
    {
        private readonly IBalanceConfig _config;

        public PriceCalculator(IBalanceConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        // =====================================================================
        // UPGRADE MALIYET FORMULERI
        // Referans: docs/ECONOMY_BALANCE.md Bolum 2.2
        // =====================================================================

        /// <summary>
        /// Makine upgrade maliyeti.
        /// Formul: BaseCost x 5^(level - 1)
        ///
        /// Ornek (Pirinc Tarlasi, BaseCost=100):
        ///   Lv.1->2: 100 x 5^0 = 100
        ///   Lv.2->3: 100 x 5^1 = 500
        ///   Lv.3->4: 100 x 5^2 = 2,500
        ///   Lv.4->5: 100 x 5^3 = 12,500
        ///
        /// Referans: docs/ECONOMY_BALANCE.md Bolum 2.2 "Makine Upgrade Formulu"
        /// balance_config.json: machine.costExponent = 5.0
        /// </summary>
        /// <param name="machineBaseCost">Tesisin makine temel maliyeti (balance_config.json facilities[].machineBaseCost)</param>
        /// <param name="targetLevel">Hedeflenen seviye (2-5 arasi)</param>
        /// <returns>Upgrade maliyeti (coin)</returns>
        public double CalculateUpgradeCost(float machineBaseCost, int targetLevel)
        {
            if (targetLevel < 2 || targetLevel > _config.GetInt("machine.maxLevel", 5))
            {
                Debug.LogWarning($"[PriceCalculator] Gecersiz makine seviyesi: {targetLevel}");
                return 0;
            }

            // BaseCost x 5^(level - 1)
            float costExponent = _config.GetFloat("machine.costExponent", 5f);
            double globalMultiplier = _config.GetFloat("general.globalUpgradeCostMultiplier", 1f);

            return machineBaseCost * Math.Pow(costExponent, targetLevel - 1) * globalMultiplier;
        }

        /// <summary>
        /// Calisan seviye atlama maliyeti.
        /// Formul: 50 x level^2.2
        ///
        /// Ornek:
        ///   Lv.1->2:  50 x 2^2.2  = ~230
        ///   Lv.10->11: 50 x 11^2.2 = ~7,940
        ///   Lv.49->50: 50 x 50^2.2 = ~280,000
        ///
        /// Referans: docs/ECONOMY_BALANCE.md Bolum 2.2 "Calisan Seviye Atlama Formulu"
        /// balance_config.json: worker.costBase = 50, worker.costExponent = 2.2
        /// </summary>
        /// <param name="targetLevel">Hedeflenen seviye (2-50 arasi)</param>
        /// <returns>Seviye atlama maliyeti (coin)</returns>
        public double CalculateWorkerUpgradeCost(int targetLevel)
        {
            if (targetLevel < 2 || targetLevel > _config.GetInt("worker.maxLevel", 50))
            {
                Debug.LogWarning($"[PriceCalculator] Gecersiz calisan seviyesi: {targetLevel}");
                return 0;
            }

            // 50 x level^2.2
            float baseCost = _config.GetFloat("worker.costBase", 50f);
            float costExponent = _config.GetFloat("worker.costExponent", 2.2f);
            double globalMultiplier = _config.GetFloat("general.globalUpgradeCostMultiplier", 1f);

            return baseCost * Math.Pow(targetLevel, costExponent) * globalMultiplier;
        }

        /// <summary>
        /// Tesis yildiz upgrade maliyeti.
        /// Formul: TesisAcmaMaliyeti x 3^(yildiz - 1)
        ///
        /// Ornek (Pirinc Tarlasi, UnlockCost icin ozel hesap: 1000 baz):
        ///   Yildiz 2: 1000 x 3^1 = 3,000
        ///   Yildiz 3: 1000 x 3^2 = 9,000
        ///   Yildiz 4: 1000 x 3^3 = 27,000
        ///   Yildiz 5: 1000 x 3^4 = 81,000
        ///
        /// Referans: docs/ECONOMY_BALANCE.md Bolum 3.3 "Yildiz Upgrade Maliyetleri"
        /// balance_config.json: facilityStar.costExponent = 3.0
        /// </summary>
        /// <param name="facilityUnlockCost">Tesisin acilma maliyeti</param>
        /// <param name="targetStar">Hedeflenen yildiz seviyesi (2-5 arasi)</param>
        /// <returns>Yildiz atlama maliyeti (coin)</returns>
        public double CalculateStarUpgradeCost(float facilityUnlockCost, int targetStar)
        {
            if (targetStar < 2 || targetStar > _config.GetInt("facilityStar.maxStars", 5))
            {
                Debug.LogWarning($"[PriceCalculator] Gecersiz yildiz seviyesi: {targetStar}");
                return 0;
            }

            // TesisAcmaMaliyeti x 3^(yildiz - 1)
            float costExponent = _config.GetFloat("facilityStar.costExponent", 3f);
            double globalMultiplier = _config.GetFloat("general.globalUpgradeCostMultiplier", 1f);

            return facilityUnlockCost * Math.Pow(costExponent, targetStar - 1) * globalMultiplier;
        }

        /// <summary>
        /// Arastirma maliyeti.
        /// Formul: BaseCost x 3^(level - 1)
        ///
        /// Ornek (BaseCost=500):
        ///   Lv.1->2: 500 x 3^0 = 500
        ///   Lv.2->3: 500 x 3^1 = 1,500
        ///   Lv.3->4: 500 x 3^2 = 4,500
        ///   Lv.8:    500 x 3^7 = 1,093,500
        ///
        /// Referans: docs/ECONOMY_BALANCE.md Bolum 2.2 "Arastirma Maliyet Formulu"
        /// balance_config.json: research.costExponent = 3.0
        /// </summary>
        /// <param name="researchBaseCost">Arastirma dalinin temel maliyeti</param>
        /// <param name="targetLevel">Hedeflenen seviye (1-8 arasi)</param>
        /// <returns>Arastirma maliyeti (coin)</returns>
        public double CalculateResearchCost(float researchBaseCost, int targetLevel)
        {
            if (targetLevel < 1 || targetLevel > _config.GetInt("research.maxLevel", 8))
            {
                Debug.LogWarning($"[PriceCalculator] Gecersiz arastirma seviyesi: {targetLevel}");
                return 0;
            }

            // BaseCost x 3^(level - 1)
            float costExponent = _config.GetFloat("research.costExponent", 3f);
            double globalMultiplier = _config.GetFloat("general.globalUpgradeCostMultiplier", 1f);

            return researchBaseCost * Math.Pow(costExponent, targetLevel - 1) * globalMultiplier;
        }

        // =====================================================================
        // URETIM HIZI HESAPLAMA
        // Referans: docs/ECONOMY_BALANCE.md Bolum 2.1
        // =====================================================================

        /// <summary>
        /// Tesis uretim hizini hesaplar (birim/saniye).
        ///
        /// Formul:
        /// UretimHizi = TemelHiz x MakineCarpani x CalisanBonus x YildizBonus x FP_Bonus x ArastirmaBonus
        ///
        /// MakineCarpani = [1.0, 1.5, 2.2, 3.5, 5.0]     (Makine Lv.1-5)
        /// CalisanBonus  = 1 + (calisanSeviyesi x 0.02)   (Lv.50'de x2.0)
        /// YildizBonus   = 1 + [0, 0.25, 0.50, 1.00, 2.00] (Yildiz 1-5)
        /// FP_Bonus      = 1 + (UretimHiziSeviyes x 0.10) (Prestige bonusu)
        ///
        /// Referans: docs/ECONOMY_BALANCE.md Bolum 2.1 "Uretim Hizi Egrisi"
        /// balance_config.json: machine.speedMultipliers, worker.efficiencyPerLevel, facilityStar.productionBonuses
        /// </summary>
        /// <param name="baseProductionTime">Temel uretim suresi (saniye)</param>
        /// <param name="machineLevel">Makine seviyesi (1-5)</param>
        /// <param name="workerLevel">Calisan seviyesi (1-50)</param>
        /// <param name="starLevel">Yildiz seviyesi (1-5)</param>
        /// <param name="franchiseProductionBonus">Franchise uretim hizi bonusu (0.0 - 2.0)</param>
        /// <param name="researchSpeedBonus">Arastirma hiz bonusu (0.0+)</param>
        /// <returns>Uretim hizi (birim/saniye)</returns>
        public float CalculateProductionRate(
            float baseProductionTime,
            int machineLevel,
            int workerLevel,
            int starLevel,
            float franchiseProductionBonus = 0f,
            float researchSpeedBonus = 0f)
        {
            if (baseProductionTime <= 0f)
            {
                Debug.LogWarning("[PriceCalculator] CalculateProductionRate: baseProductionTime <= 0.");
                return 0f;
            }

            // Temel hiz: 1 urun / baseProductionTime saniye
            float baseRate = 1f / baseProductionTime;

            // Makine carpani: [1.0, 1.5, 2.2, 3.5, 5.0]
            float[] machineMultipliers = { 1.0f, 1.5f, 2.2f, 3.5f, 5.0f };
            int machineIndex = Mathf.Clamp(machineLevel - 1, 0, machineMultipliers.Length - 1);
            float machineMultiplier = machineMultipliers[machineIndex];

            // Calisan bonusu: 1 + (level x 0.02)
            float workerEfficiency = _config.GetFloat("worker.efficiencyPerLevel", 0.02f);
            float workerBonus = 1f + (workerLevel * workerEfficiency);

            // Yildiz bonusu: [0.0, 0.25, 0.50, 1.00, 2.00]
            float[] starBonuses = { 0.0f, 0.25f, 0.50f, 1.00f, 2.00f };
            int starIndex = Mathf.Clamp(starLevel - 1, 0, starBonuses.Length - 1);
            float starBonus = 1f + starBonuses[starIndex];

            // Franchise Puani bonusu: 1 + (seviye x 0.10)
            float fpBonus = 1f + franchiseProductionBonus;

            // Arastirma bonusu
            float researchBonus = 1f + researchSpeedBonus;

            // Global carpan
            float globalMultiplier = _config.GetFloat("general.globalProductionMultiplier", 1f);

            return baseRate * machineMultiplier * workerBonus * starBonus
                   * fpBonus * researchBonus * globalMultiplier;
        }

        // =====================================================================
        // SATIS FIYATI HESAPLAMA
        // Referans: docs/ECONOMY_BALANCE.md Bolum 2.3
        // =====================================================================

        /// <summary>
        /// Urun satis fiyatini hesaplar.
        ///
        /// Formul:
        /// SatisFiyati = TemelFiyat x KaliteCarpani x TalepCarpani x ItibarBonusu
        ///
        /// KaliteCarpani: [1.0, 1.3, 1.7, 2.2, 3.0]  (1-5 Yildiz kalite)
        /// TalepCarpani:  0.8 - 1.5 (dinamik pazar)
        /// ItibarBonusu:  1 + (ItibarPuani / 10000)
        ///
        /// Referans: docs/ECONOMY_BALANCE.md Bolum 2.3 "Gelir Egrisi"
        /// balance_config.json: quality.priceMultipliers
        /// </summary>
        /// <param name="basePrice">Urunun temel satis fiyati</param>
        /// <param name="qualityLevel">Urun kalitesi (1-5)</param>
        /// <param name="demandMultiplier">Mevcut talep carpani (0.8 - 1.5)</param>
        /// <param name="reputationPoints">Oyuncunun itibar puani</param>
        /// <returns>Hesaplanan satis fiyati (coin)</returns>
        public double CalculateSellPrice(
            float basePrice,
            int qualityLevel,
            float demandMultiplier,
            int reputationPoints)
        {
            // Kalite carpanlari: [1.0, 1.3, 1.7, 2.2, 3.0]
            float[] qualityMultipliers = { 1.0f, 1.3f, 1.7f, 2.2f, 3.0f };
            int qualityIndex = Mathf.Clamp(qualityLevel - 1, 0, qualityMultipliers.Length - 1);
            float qualityMultiplier = qualityMultipliers[qualityIndex];

            // Talep carpanini 0.8 - 1.5 araliginda sinirla
            float clampedDemand = Mathf.Clamp(demandMultiplier, 0.8f, 1.5f);

            // Itibar bonusu: 1 + (ItibarPuani / 10000)
            // Her 100 itibar = +%1
            float reputationBonus = 1f + (reputationPoints / 10000f);

            // Global satis fiyati carpani
            float globalMultiplier = _config.GetFloat("general.globalSellPriceMultiplier", 1f);

            return basePrice * qualityMultiplier * clampedDemand * reputationBonus * globalMultiplier;
        }

        /// <summary>
        /// Toplu satis fiyatini hesaplar (miktar ile carpilmis).
        /// </summary>
        public double CalculateBulkSellPrice(
            float basePrice,
            int qualityLevel,
            float demandMultiplier,
            int reputationPoints,
            int quantity)
        {
            return CalculateSellPrice(basePrice, qualityLevel, demandMultiplier, reputationPoints) * quantity;
        }

        // =====================================================================
        // ROI (YATIRIM GERI DONUS SURESI) HESAPLAMA
        // Referans: docs/ECONOMY_BALANCE.md Bolum 3.2
        // =====================================================================

        /// <summary>
        /// Yatirim geri donus suresini hesaplar.
        ///
        /// Formul:
        /// ROI (dakika) = UpgradeMaliyeti / (YeniGelir/dk - MevcutGelir/dk)
        ///
        /// Referans: docs/ECONOMY_BALANCE.md Bolum 3.2 "ROI Hesabi"
        /// </summary>
        /// <param name="upgradeCost">Upgrade maliyeti (coin)</param>
        /// <param name="currentRevenuePerMinute">Mevcut gelir/dakika</param>
        /// <param name="newRevenuePerMinute">Upgrade sonrasi gelir/dakika</param>
        /// <returns>ROI suresi dakika cinsinden. Negatif veya sonsuz ise -1 doner.</returns>
        public float CalculateROI(double upgradeCost, double currentRevenuePerMinute, double newRevenuePerMinute)
        {
            double revenueIncrease = newRevenuePerMinute - currentRevenuePerMinute;

            if (revenueIncrease <= 0)
            {
                // Gelir artisi yok veya azalma var — ROI hesaplanamaz
                return -1f;
            }

            float roi = (float)(upgradeCost / revenueIncrease);
            return roi;
        }

        /// <summary>
        /// Belirli bir tesisin gelir/dakika hesaplar.
        /// basePrice x UretimHizi(birim/sn) x 60 = gelir/dakika
        /// </summary>
        public double CalculateRevenuePerMinute(
            float basePrice,
            float productionRatePerSecond,
            int qualityLevel,
            float demandMultiplier,
            int reputationPoints)
        {
            double sellPrice = CalculateSellPrice(basePrice, qualityLevel, demandMultiplier, reputationPoints);
            return sellPrice * productionRatePerSecond * 60.0;
        }

        // =====================================================================
        // TESIS ACMA MALIYETI (FRANCHISE INDIRIMLI)
        // Referans: docs/ECONOMY_BALANCE.md Bolum 3.1
        // =====================================================================

        /// <summary>
        /// Tesis acma maliyetini franchise indirimi ile hesaplar.
        ///
        /// Formul:
        /// FinalMaliyet = TemelMaliyet x (1 - FranchiseIndirimi)
        /// FranchiseIndirimi = FacilityCostReductionLevel x 0.10  (max %80)
        ///
        /// Referans: docs/ECONOMY_BALANCE.md Bolum 4.2 "Tesis Acma Maliyeti -%10"
        /// </summary>
        /// <param name="baseUnlockCost">Tesisin temel acilma maliyeti</param>
        /// <param name="facilityCostReduction">Franchise indirim orani (0.0 - 0.8)</param>
        /// <returns>Indirimli acma maliyeti</returns>
        public double CalculateFacilityUnlockCost(float baseUnlockCost, float facilityCostReduction)
        {
            float clampedReduction = Mathf.Clamp01(facilityCostReduction);
            return baseUnlockCost * (1.0 - clampedReduction);
        }
    }
}
