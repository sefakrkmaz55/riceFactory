// =============================================================================
// IBalanceConfig.cs
// Denge parametrelerine erisim saglayan interface.
// PriceCalculator, PrestigeSystem ve ProductionManager tarafindan kullanilir.
// Parametreler balance_config.json'dan yuklenir.
// =============================================================================

namespace RiceFactory.Data.Save
{
    /// <summary>
    /// Oyun denge konfigurasyonuna erisim arayuzu.
    /// Tum ekonomik parametreler (maliyet carpanlari, max seviyeler, verimlilik oranlari vb.)
    /// bu interface uzerinden okunur.
    ///
    /// Implementasyon: balance_config.json'dan veya ScriptableObject'ten yuklenebilir.
    /// Remote Config entegrasyonu ile runtime'da guncellenebilir.
    /// </summary>
    public interface IBalanceConfig
    {
        /// <summary>
        /// Float tipinde denge parametresi okur.
        /// </summary>
        /// <param name="key">Parametre anahtari (ornek: "machine.costExponent")</param>
        /// <param name="defaultValue">Anahtar bulunamazsa dondurulecek varsayilan deger</param>
        /// <returns>Parametre degeri veya varsayilan</returns>
        float GetFloat(string key, float defaultValue = 0f);

        /// <summary>
        /// Int tipinde denge parametresi okur.
        /// </summary>
        /// <param name="key">Parametre anahtari (ornek: "machine.maxLevel")</param>
        /// <param name="defaultValue">Anahtar bulunamazsa dondurulecek varsayilan deger</param>
        /// <returns>Parametre degeri veya varsayilan</returns>
        int GetInt(string key, int defaultValue = 0);

        /// <summary>
        /// String tipinde denge parametresi okur.
        /// </summary>
        /// <param name="key">Parametre anahtari</param>
        /// <param name="defaultValue">Varsayilan deger</param>
        /// <returns>Parametre degeri veya varsayilan</returns>
        string GetString(string key, string defaultValue = "");

        /// <summary>
        /// Bool tipinde denge parametresi okur.
        /// </summary>
        /// <param name="key">Parametre anahtari</param>
        /// <param name="defaultValue">Varsayilan deger</param>
        /// <returns>Parametre degeri veya varsayilan</returns>
        bool GetBool(string key, bool defaultValue = false);
    }
}
