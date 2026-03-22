// =============================================================================
// PlayerSaveDataExtensions.cs
// PlayerSaveData partial sinifinin ek alanlari ve metotlari.
// FacilityState listesi, FranchiseBonuses, Research verisi ve
// Prestige sirasinda kullanilan ResetForFranchise metodu burada tanimlanir.
// =============================================================================

using System.Collections.Generic;
using RiceFactory.Data.Save;

namespace RiceFactory.Core
{
    /// <summary>
    /// PlayerSaveData'nin Data katmani alanlari ve Franchise reset metodu.
    /// Ana PlayerSaveData partial sinifini genisletir.
    /// </summary>
    public partial class PlayerSaveData
    {
        // ---- Seviye (Property) ----

        /// <summary>Oyuncu seviyesi. PlayerLevel alaninin property erisimcisi.</summary>
        public int Level
        {
            get => PlayerLevel;
            set => PlayerLevel = value;
        }

        // ---- Tesis Verileri ----

        /// <summary>Acilmis tesislerin durum listesi.</summary>
        public List<FacilityState> Facilities;

        // ---- Franchise Bonuslari ----

        /// <summary>Franchise Puani ile satin alinan kalici bonuslar.</summary>
        public FranchiseBonuses FranchiseBonuses = new();

        // ---- Arastirma ----

        /// <summary>Arastirma dallarinin ilerleme verisi.</summary>
        public ResearchData Research = new();

        // ---- Koleksiyonlar ----

        /// <summary>Acilmis sehir temalari.</summary>
        public List<string> UnlockedCities = new();

        /// <summary>Kazanilmis basarim ID listesi.</summary>
        public List<string> Achievements = new();

        /// <summary>Sahip olunan kozmetik ID listesi.</summary>
        public List<string> CosmeticInventory = new();

        // ---- Monetizasyon ----

        /// <summary>Oyunun ilk acilma zamani (UTC). Starter Pack suresi icin.</summary>
        public System.DateTime FirstOpenTime;

        /// <summary>Reklamsiz paket satin alinmis mi?</summary>
        public bool IsAdFree;

        /// <summary>Starter Pack satin alinmis mi?</summary>
        public bool IsStarterPackPurchased;

        // =====================================================================
        // Tesis Sorgulama Metotlari
        // =====================================================================

        /// <summary>ID ile tesis durumunu bulur. Bulunamazsa null dondurur.</summary>
        public FacilityState GetFacility(string facilityId)
        {
            if (Facilities == null) return null;
            for (int i = 0; i < Facilities.Count; i++)
            {
                if (Facilities[i].Id == facilityId)
                    return Facilities[i];
            }
            return null;
        }

        /// <summary>Acilmis (IsUnlocked == true) tesis sayisini dondurur.</summary>
        public int GetUnlockedFacilityCount()
        {
            if (Facilities == null) return 0;
            int count = 0;
            for (int i = 0; i < Facilities.Count; i++)
            {
                if (Facilities[i].IsUnlocked)
                    count++;
            }
            return count;
        }

        /// <summary>Toplam tesis sayisini dondurur (acik + kapali).</summary>
        public int GetTotalFacilityCount()
        {
            return Facilities?.Count ?? 0;
        }

        /// <summary>Tum tesisler arasindaki en yuksek yildiz seviyesini dondurur.</summary>
        public int GetHighestStarLevel()
        {
            if (Facilities == null || Facilities.Count == 0) return 0;
            int max = 0;
            for (int i = 0; i < Facilities.Count; i++)
            {
                if (Facilities[i].StarLevel > max)
                    max = Facilities[i].StarLevel;
            }
            return max;
        }

        // =====================================================================
        // Franchise (Prestige) Sifirlama
        // =====================================================================

        /// <summary>
        /// Franchise sirasinda ilerlemeyi sifirlar, kalici verileri korur.
        /// PrestigeSystem.ExecuteFranchise tarafindan cagirilir.
        /// </summary>
        /// <param name="persistent">Korunacak kalici veriler</param>
        public void ResetForFranchise(RiceFactory.Economy.PersistentFranchiseData persistent)
        {
            // Kalici verileri geri yukle
            FranchisePoints = persistent.FranchisePoints;
            FranchiseCount = persistent.FranchiseCount;
            FranchiseBonuses = persistent.FranchiseBonuses ?? new FranchiseBonuses();
            TotalLifetimeEarnings = persistent.TotalLifetimeEarnings;
            UnlockedCities = persistent.UnlockedCities ?? new List<string>();
            Achievements = persistent.Achievements ?? new List<string>();
            CosmeticInventory = persistent.CosmeticInventory ?? new List<string>();

            // Sifirlanan veriler
            Coins = 0;
            Gems = 0;
            TotalEarnings = 0;
            Reputation = 0;
            PlayerLevel = 1;
            HasBattlePass = false;
            BattlePassTier = 0;

            // Tesis verilerini sifirla
            Facilities = new List<FacilityState>();

            // Arastirma verilerini sifirla
            Research = new ResearchData();
        }
    }
}
