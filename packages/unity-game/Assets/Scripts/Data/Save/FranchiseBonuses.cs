// =============================================================================
// FranchiseBonuses.cs
// Oyuncunun Franchise Puani ile satin aldigi kalici bonuslarin durumu.
// Her bonus turunun mevcut seviyesini ve hesaplanan etkisini tutar.
// Referans: docs/ECONOMY_BALANCE.md Bolum 4.2
// =============================================================================

using System;
using System.Collections.Generic;

namespace RiceFactory.Data.Save
{
    /// <summary>
    /// Franchise bonuslarinin kayit verisi.
    /// Her bonus turunun seviyesini tutar ve hesaplanmis etki degerlerini sunar.
    /// </summary>
    [Serializable]
    public class FranchiseBonuses
    {
        // ---- Bonus Seviyeleri ----
        public int ProductionSpeedLevel;
        public int StartingCoinsLevel;
        public int OfflineEarningsLevel;
        public int FacilityCostReductionLevel;
        public int CriticalProductionLevel;
        public int SpecialWorkerLevel;

        // =====================================================================
        // Hesaplanmis Bonus Degerleri (readonly erisimciler)
        // =====================================================================

        /// <summary>Uretim hizi bonusu: seviye x 0.10 (max %200).</summary>
        public float ProductionSpeedBonus => ProductionSpeedLevel * 0.10f;

        /// <summary>Baslangic parasi bonusu: seviye x 0.50 (max %500).</summary>
        public float StartingCoinBonus => StartingCoinsLevel * 0.50f;

        /// <summary>Offline kazanc bonusu: seviye x 0.05 (max %100).</summary>
        public float OfflineEarningBonus => OfflineEarningsLevel * 0.05f;

        /// <summary>Tesis acma maliyeti indirimi: seviye x 0.10 (max %80).</summary>
        public float FacilityCostReduction => FacilityCostReductionLevel * 0.10f;

        /// <summary>Kritik uretim sansi: seviye x 0.02 (max %20).</summary>
        public float CriticalProductionChance => CriticalProductionLevel * 0.02f;

        /// <summary>Ozel calisan acildi mi (tek seferlik).</summary>
        public bool HasSpecialWorker => SpecialWorkerLevel > 0;

        // =====================================================================
        // Seviye Erisim Metotlari
        // =====================================================================

        /// <summary>Belirtilen bonus turunun mevcut seviyesini dondurur.</summary>
        public int GetLevel(FranchiseBonusType bonusType)
        {
            return bonusType switch
            {
                FranchiseBonusType.ProductionSpeed => ProductionSpeedLevel,
                FranchiseBonusType.StartingCoins => StartingCoinsLevel,
                FranchiseBonusType.OfflineEarnings => OfflineEarningsLevel,
                FranchiseBonusType.FacilityCostReduction => FacilityCostReductionLevel,
                FranchiseBonusType.CriticalProduction => CriticalProductionLevel,
                FranchiseBonusType.SpecialWorker => SpecialWorkerLevel,
                _ => 0
            };
        }

        /// <summary>Belirtilen bonus turunun seviyesini ayarlar.</summary>
        public void SetLevel(FranchiseBonusType bonusType, int level)
        {
            switch (bonusType)
            {
                case FranchiseBonusType.ProductionSpeed:
                    ProductionSpeedLevel = level;
                    break;
                case FranchiseBonusType.StartingCoins:
                    StartingCoinsLevel = level;
                    break;
                case FranchiseBonusType.OfflineEarnings:
                    OfflineEarningsLevel = level;
                    break;
                case FranchiseBonusType.FacilityCostReduction:
                    FacilityCostReductionLevel = level;
                    break;
                case FranchiseBonusType.CriticalProduction:
                    CriticalProductionLevel = level;
                    break;
                case FranchiseBonusType.SpecialWorker:
                    SpecialWorkerLevel = level;
                    break;
            }
        }
    }
}
