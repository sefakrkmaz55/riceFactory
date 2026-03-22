using System;
using System.Linq;
using RiceFactory.Core;
using RiceFactory.Core.Events;
using RiceFactory.Data.Save;
using RiceFactory.Production;
using UnityEngine;

namespace RiceFactory.Economy
{
    /// <summary>
    /// Upgrade sistemi. IUpgradeSystem interface'ini uygular.
    /// UpgradePanel bu sinifi ServiceLocator uzerinden IUpgradeSystem olarak kullanir.
    ///
    /// PriceCalculator ile maliyet hesabi, Factory ile upgrade islemleri yonetilir.
    /// </summary>
    public class UpgradeSystem : IUpgradeSystem
    {
        private readonly PriceCalculator _priceCalculator;
        private readonly ProductionManager _productionManager;
        private readonly CurrencySystem _currencySystem;
        private readonly IBalanceConfig _config;

        /// <summary>
        /// Makine hiz carpanlari: [1.0, 1.5, 2.2, 3.5, 5.0] (Makine Lv.1-5)
        /// Referans: docs/ECONOMY_BALANCE.md Bolum 2.1
        /// </summary>
        private static readonly float[] MachineSpeedMultipliers = { 1.0f, 1.5f, 2.2f, 3.5f, 5.0f };

        public UpgradeSystem(
            PriceCalculator priceCalculator,
            ProductionManager productionManager,
            CurrencySystem currencySystem,
            IBalanceConfig config)
        {
            _priceCalculator = priceCalculator ?? throw new ArgumentNullException(nameof(priceCalculator));
            _productionManager = productionManager ?? throw new ArgumentNullException(nameof(productionManager));
            _currencySystem = currencySystem ?? throw new ArgumentNullException(nameof(currencySystem));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        // =====================================================================
        // IUpgradeSystem IMPLEMENTASYONU
        // =====================================================================

        /// <summary>
        /// Belirtilen upgrade turinin maliyetini dondurur.
        /// </summary>
        public double GetUpgradeCost(UpgradeType type, string facilityId, int level)
        {
            var factory = _productionManager.GetFactory(facilityId);
            if (factory == null)
            {
                Debug.LogWarning($"[UpgradeSystem] GetUpgradeCost: '{facilityId}' fabrikasi bulunamadi.");
                return 0;
            }

            return type switch
            {
                UpgradeType.Machine => _priceCalculator.CalculateUpgradeCost(factory.Data.BaseCost, level),
                UpgradeType.Worker => _priceCalculator.CalculateWorkerUpgradeCost(level),
                UpgradeType.Star => _priceCalculator.CalculateStarUpgradeCost(factory.Data.UnlockCost, level),
                _ => 0
            };
        }

        /// <summary>
        /// Makine hiz carpanini dondurur (seviyeye gore).
        /// Referans: docs/ECONOMY_BALANCE.md Bolum 2.1
        /// </summary>
        public float GetMachineSpeed(string facilityId, int level)
        {
            int index = Mathf.Clamp(level - 1, 0, MachineSpeedMultipliers.Length - 1);
            return MachineSpeedMultipliers[index];
        }

        /// <summary>
        /// Calisan verimliligini dondurur (seviyeye gore).
        /// Formul: 1 + (level x 0.02)
        /// Referans: docs/ECONOMY_BALANCE.md Bolum 2.1
        /// </summary>
        public float GetWorkerEfficiency(string facilityId, int level)
        {
            float efficiencyPerLevel = _config.GetFloat("worker.efficiencyPerLevel", 0.02f);
            return 1f + (level * efficiencyPerLevel);
        }

        /// <summary>
        /// Yildiz upgrade gereksinimleri karsilaniyor mu kontrol eder.
        /// Gereksinimler:
        /// - Makine seviyesi >= hedef yildiz seviyesi
        /// - Yeterli urun satisi
        /// Referans: docs/ECONOMY_BALANCE.md Bolum 3.3
        /// </summary>
        public bool MeetsStarRequirements(string facilityId, int starLevel)
        {
            var factory = _productionManager.GetFactory(facilityId);
            if (factory == null) return false;

            // Makine seviyesi kontrolu
            if (factory.MachineLevel < starLevel)
                return false;

            // Urun satisi kontrolu
            int requiredSales = GetRequiredSalesForStar(starLevel);
            if (factory.State.TotalProductsSold < requiredSales)
                return false;

            return true;
        }

        /// <summary>
        /// Yildiz upgrade gereksinim aciklamasini dondurur.
        /// </summary>
        public string GetStarRequirementDescription(string facilityId, int starLevel)
        {
            var factory = _productionManager.GetFactory(facilityId);
            if (factory == null)
                return "Tesis bulunamadi.";

            var requirements = new System.Text.StringBuilder();

            // Makine seviyesi kontrolu
            if (factory.MachineLevel < starLevel)
            {
                requirements.AppendLine($"Makine Lv.{starLevel} gerekli (Mevcut: Lv.{factory.MachineLevel})");
            }

            // Urun satisi kontrolu
            int requiredSales = GetRequiredSalesForStar(starLevel);
            if (factory.State.TotalProductsSold < requiredSales)
            {
                requirements.AppendLine($"{requiredSales:N0} urun satisi gerekli (Mevcut: {factory.State.TotalProductsSold:N0})");
            }

            return requirements.Length > 0
                ? requirements.ToString().TrimEnd()
                : "Gereksinimler karsilandi!";
        }

        /// <summary>
        /// Upgrade islemi dener. Basarili ise true dondurur.
        /// Factory sinifinin mevcut TryUpgrade metotlarini kullanir.
        /// </summary>
        public bool TryUpgrade(UpgradeType type, string facilityId)
        {
            var factory = _productionManager.GetFactory(facilityId);
            if (factory == null)
            {
                Debug.LogWarning($"[UpgradeSystem] TryUpgrade: '{facilityId}' fabrikasi bulunamadi.");
                return false;
            }

            return type switch
            {
                UpgradeType.Machine => factory.TryUpgradeMachine(_currencySystem),
                UpgradeType.Worker => factory.TryUpgradeWorker(_currencySystem),
                UpgradeType.Star => factory.TryUpgradeStar(_currencySystem),
                _ => false
            };
        }

        // =====================================================================
        // YARDIMCI METOTLAR
        // =====================================================================

        /// <summary>
        /// Yildiz icin gerekli minimum urun satisi sayisi.
        /// Factory.GetRequiredSalesForStar ile ayni degerler.
        /// </summary>
        private int GetRequiredSalesForStar(int targetStar)
        {
            return targetStar switch
            {
                2 => 500,
                3 => 5_000,
                4 => 50_000,
                5 => 500_000,
                _ => int.MaxValue
            };
        }
    }
}
