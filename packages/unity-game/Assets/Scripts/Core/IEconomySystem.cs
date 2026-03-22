// =============================================================================
// IEconomySystem.cs
// Ekonomi sistemi arayuzu. UI panelleri bu interface uzerinden para islemlerine erisir.
// =============================================================================

using RiceFactory.Core.Events;

namespace RiceFactory.Core
{
    /// <summary>
    /// Ekonomi sistemi icin interface.
    /// CurrencySystem bu arayuzu uygular.
    /// </summary>
    public interface IEconomySystem
    {
        /// <summary>Belirtilen para biriminden belirtilen miktari karsilayabilir mi?</summary>
        bool CanAfford(CurrencyType type, double amount);

        /// <summary>Belirtilen para biriminden belirtilen miktari ekler.</summary>
        void AddCurrency(CurrencyType type, double amount, string reason);
    }
}
