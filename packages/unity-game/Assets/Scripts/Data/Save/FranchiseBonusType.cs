// =============================================================================
// FranchiseBonusType.cs
// Franchise (Prestige) bonus turlerini tanimlayan enum.
// Referans: docs/ECONOMY_BALANCE.md Bolum 4.2
// =============================================================================

namespace RiceFactory.Data.Save
{
    /// <summary>
    /// Franchise Puani ile satin alinabilecek kalici bonus turleri.
    /// </summary>
    public enum FranchiseBonusType
    {
        ProductionSpeed,
        StartingCoins,
        OfflineEarnings,
        FacilityCostReduction,
        CriticalProduction,
        SpecialWorker
    }
}
