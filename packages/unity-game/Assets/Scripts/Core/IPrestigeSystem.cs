// =============================================================================
// IPrestigeSystem.cs
// Prestige (Franchise) sistemi arayuzu. PrestigePanel bu interface uzerinden erisir.
// =============================================================================

using System.Collections.Generic;

namespace RiceFactory.Core
{
    /// <summary>
    /// Prestige sistemi icin interface.
    /// PrestigeSystem bu arayuzu uygular.
    /// </summary>
    public interface IPrestigeSystem
    {
        /// <summary>Kazanilacak FP miktarini hesaplar.</summary>
        int CalculateEarnedFP();

        /// <summary>Sonraki sehir adini dondurur.</summary>
        string GetNextCityName();

        /// <summary>Prestige yapilabilir mi?</summary>
        bool CanPrestige();

        /// <summary>Prestige kosulu karsilanmadiysa aciklama dondurur.</summary>
        string GetPrestigeRequirementDescription();

        /// <summary>Satin alinabilir FP bonus listesini dondurur.</summary>
        List<PrestigeBonusItem> GetAvailableBonuses();

        /// <summary>Prestige islemini gerceklestirir.</summary>
        void ExecutePrestige();

        /// <summary>FP bonus satin alir.</summary>
        bool PurchaseBonus(string bonusId);
    }

    /// <summary>
    /// PrestigePanel tarafindan kullanilan bonus gosterim modeli.
    /// </summary>
    public class PrestigeBonusItem
    {
        public string Id;
        public string DisplayName;
        public int FPCost;
        public bool CanAfford;
    }
}
