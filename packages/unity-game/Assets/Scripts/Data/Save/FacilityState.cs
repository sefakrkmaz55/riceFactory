// =============================================================================
// FacilityState.cs
// Tek bir tesisin kayit durumu. Factory.cs ve ProductionManager.cs tarafindan
// kullanilir. PlayerSaveData.Facilities listesinde saklanir.
// =============================================================================

using System;
using System.Collections.Generic;

namespace RiceFactory.Data.Save
{
    /// <summary>
    /// Bir tesisin (fabrika) mevcut durumunu tutan serialize edilebilir sinif.
    /// Her tesis instance'i icin ayri bir FacilityState tutulur.
    /// </summary>
    [Serializable]
    public class FacilityState
    {
        // ---- Kimlik ----

        /// <summary>Tesis instance ID (benzersiz, ornek: "factory_a3b2c1d4").</summary>
        public string Id;

        /// <summary>Tesis tipi (FactoryData.FactoryId ile eslesir, ornek: "rice_field").</summary>
        public string FacilityType;

        /// <summary>Tesisin gorunen adi (ornek: "Pirinc Tarlasi").</summary>
        public string DisplayName;

        // ---- Durum ----

        /// <summary>Tesis acilmis mi.</summary>
        public bool IsUnlocked;

        // ---- Seviyeler ----

        /// <summary>Yildiz seviyesi (1-5).</summary>
        public int StarLevel;

        /// <summary>Makine seviyesi (1-5).</summary>
        public int MachineLevel;

        /// <summary>Calisan seviyesi (1-50).</summary>
        public int WorkerLevel;

        /// <summary>Calisan hiz becerisi seviyesi.</summary>
        public int WorkerSpeedLevel;

        /// <summary>Calisan kalite becerisi seviyesi.</summary>
        public int WorkerQualityLevel;

        /// <summary>Calisan kapasite becerisi seviyesi.</summary>
        public int WorkerCapacityLevel;

        /// <summary>Calisan otomasyon becerisi seviyesi.</summary>
        public int WorkerAutomationLevel;

        // ---- Uretim ----

        /// <summary>Aktif urun zinciri index'i.</summary>
        public int ActiveProductIndex;

        /// <summary>Aktif urun ID'si.</summary>
        public string ActiveProductId;

        /// <summary>Otomatik satis acik mi.</summary>
        public bool AutoSellEnabled;

        /// <summary>Temel cikti miktari.</summary>
        public int BaseOutputAmount;

        // ---- Istatistik ----

        /// <summary>Toplam satilan urun sayisi (yildiz upgrade kosulu).</summary>
        public int TotalProductsSold;

        /// <summary>Toplam uretim sayisi.</summary>
        public long TotalProductionCount;

        /// <summary>Ortalama urun kalitesi.</summary>
        public float AverageQuality;

        // ---- Acilmis Tarifler ----

        /// <summary>Acilmis uretim tarifi ID listesi.</summary>
        public List<string> UnlockedRecipes;
    }
}
