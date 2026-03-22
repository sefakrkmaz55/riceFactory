// =============================================================================
// ResearchData.cs
// Oyuncunun arastirma ilerlemesini tutan kayit verisi.
// Factory.cs ve ProductionManager.cs tarafindan Research.GetBranchLevel() ile
// kullanilir.
// =============================================================================

using System;
using System.Collections.Generic;

namespace RiceFactory.Data.Save
{
    /// <summary>
    /// Arastirma dallarinin seviye bilgisini tutan serialize edilebilir sinif.
    /// Her dal (hiz, otomasyon, kalite vb.) bir string key ile tanimlanir.
    /// </summary>
    [Serializable]
    public class ResearchData
    {
        /// <summary>Arastirma dali seviyeleri (dal_id -> seviye).</summary>
        public List<ResearchBranchEntry> Branches = new();

        /// <summary>
        /// Belirtilen arastirma dalinin mevcut seviyesini dondurur.
        /// Dal bulunamazsa 0 dondurur.
        /// </summary>
        /// <param name="branchId">Arastirma dali kimlik kodu (ornek: "hiz", "otomasyon")</param>
        /// <returns>Mevcut seviye (0 = arastirma yapilmamis)</returns>
        public int GetBranchLevel(string branchId)
        {
            if (Branches == null) return 0;

            for (int i = 0; i < Branches.Count; i++)
            {
                if (Branches[i].BranchId == branchId)
                    return Branches[i].Level;
            }
            return 0;
        }

        /// <summary>
        /// Belirtilen arastirma dalinin seviyesini ayarlar.
        /// Dal yoksa yeni ekler.
        /// </summary>
        public void SetBranchLevel(string branchId, int level)
        {
            if (Branches == null)
                Branches = new List<ResearchBranchEntry>();

            for (int i = 0; i < Branches.Count; i++)
            {
                if (Branches[i].BranchId == branchId)
                {
                    Branches[i].Level = level;
                    return;
                }
            }

            Branches.Add(new ResearchBranchEntry { BranchId = branchId, Level = level });
        }
    }

    /// <summary>Tek bir arastirma dalinin kayit girdisi.</summary>
    [Serializable]
    public class ResearchBranchEntry
    {
        public string BranchId;
        public int Level;
    }
}
