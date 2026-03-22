// =============================================================================
// IUpgradeSystem.cs
// Upgrade sistemi arayuzu. UpgradePanel bu interface uzerinden erisir.
// =============================================================================

using RiceFactory.Core.Events;

namespace RiceFactory.Core
{
    /// <summary>
    /// Upgrade sistemi icin interface.
    /// Makine, calisan ve yildiz yukseltme islemlerini yonetir.
    /// </summary>
    public interface IUpgradeSystem
    {
        /// <summary>Belirtilen upgrade turinin maliyetini dondurur.</summary>
        double GetUpgradeCost(UpgradeType type, string facilityId, int level);

        /// <summary>Makine hizini dondurur.</summary>
        float GetMachineSpeed(string facilityId, int level);

        /// <summary>Calisan verimliligini dondurur.</summary>
        float GetWorkerEfficiency(string facilityId, int level);

        /// <summary>Yildiz upgrade gereksinimleri karsilaniyor mu?</summary>
        bool MeetsStarRequirements(string facilityId, int starLevel);

        /// <summary>Yildiz upgrade gereksinim aciklamasi.</summary>
        string GetStarRequirementDescription(string facilityId, int starLevel);

        /// <summary>Upgrade islemi dener. Basarili ise true dondurur.</summary>
        bool TryUpgrade(UpgradeType type, string facilityId);
    }
}
