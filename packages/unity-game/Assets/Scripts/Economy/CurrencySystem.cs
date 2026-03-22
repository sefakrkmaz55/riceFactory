using System;
using RiceFactory.Core;
using RiceFactory.Core.Events;
using RiceFactory.Data.Save;
using UnityEngine;

namespace RiceFactory.Economy
{
    /// <summary>
    /// Coin ve Diamond (Elmas) para birimlerini yoneten sistem.
    /// EventManager uzerinden para degisiklik eventleri firlatir.
    /// Double-spend onleme icin islem kilidi kullanir.
    ///
    /// Referans: docs/ECONOMY_BALANCE.md Bolum 1.1 — Para Birimleri
    /// </summary>
    public class CurrencySystem : IEconomySystem
    {
        private readonly ISaveManager _saveManager;
        private readonly IEventManager _eventManager;

        // Double-spend onleme: ayni anda birden fazla harcama islemini engeller
        private bool _isProcessingTransaction;

        // --- Public Erisimciler ---
        public double Coins => _saveManager.Data.Coins;
        public int Gems => _saveManager.Data.Gems;
        public int FranchisePoints => _saveManager.Data.FranchisePoints;
        public int Reputation => _saveManager.Data.Reputation;
        public double TotalEarnings => _saveManager.Data.TotalEarnings;
        public double TotalLifetimeEarnings => _saveManager.Data.TotalLifetimeEarnings;

        public CurrencySystem(ISaveManager saveManager, IEventManager eventManager)
        {
            _saveManager = saveManager ?? throw new ArgumentNullException(nameof(saveManager));
            _eventManager = eventManager ?? throw new ArgumentNullException(nameof(eventManager));
        }

        // =====================================================================
        // COIN ISLEMLERI
        // =====================================================================

        /// <summary>
        /// Belirtilen miktarda coin ekler ve TotalEarnings'i gunceller.
        /// </summary>
        /// <param name="amount">Eklenecek miktar (pozitif olmali)</param>
        /// <param name="reason">Islem nedeni (loglama ve analytics icin)</param>
        public void AddCoins(double amount, string reason)
        {
            if (amount <= 0)
            {
                Debug.LogWarning($"[CurrencySystem] AddCoins: Gecersiz miktar ({amount}). Islem iptal.");
                return;
            }

            double oldAmount = _saveManager.Data.Coins;
            _saveManager.Data.Coins += amount;
            _saveManager.Data.TotalEarnings += amount;

            PublishCurrencyChanged(CurrencyType.Coin, oldAmount, _saveManager.Data.Coins, reason);
        }

        /// <summary>
        /// Belirtilen miktarda coin harcar.
        /// Double-spend korumasina sahiptir.
        /// </summary>
        /// <param name="amount">Harcanacak miktar (pozitif olmali)</param>
        /// <param name="reason">Islem nedeni</param>
        /// <returns>Basarili ise true, yetersiz bakiye veya kilit varsa false</returns>
        public bool SpendCoins(double amount, string reason)
        {
            if (amount <= 0)
            {
                Debug.LogWarning($"[CurrencySystem] SpendCoins: Gecersiz miktar ({amount}). Islem iptal.");
                return false;
            }

            // Double-spend onleme
            if (_isProcessingTransaction)
            {
                Debug.LogWarning($"[CurrencySystem] SpendCoins: Baska bir islem devam ediyor. '{reason}' reddedildi.");
                return false;
            }

            if (!CanAffordCoins(amount))
            {
                return false;
            }

            _isProcessingTransaction = true;
            try
            {
                double oldAmount = _saveManager.Data.Coins;
                _saveManager.Data.Coins -= amount;

                PublishCurrencyChanged(CurrencyType.Coin, oldAmount, _saveManager.Data.Coins, reason);
                return true;
            }
            finally
            {
                _isProcessingTransaction = false;
            }
        }

        /// <summary>
        /// Belirtilen miktarda coin karsilanabilir mi kontrol eder.
        /// </summary>
        public bool CanAffordCoins(double amount)
        {
            return _saveManager.Data.Coins >= amount && amount > 0;
        }

        // =====================================================================
        // DIAMOND (ELMAS) ISLEMLERI
        // Referans: docs/ECONOMY_BALANCE.md Bolum 6 — Elmas Ekonomisi
        // =====================================================================

        /// <summary>
        /// Belirtilen miktarda elmas ekler.
        /// </summary>
        /// <param name="amount">Eklenecek miktar (pozitif olmali)</param>
        /// <param name="reason">Islem nedeni (daily_login, milestone, ad_wheel vb.)</param>
        public void AddGems(int amount, string reason)
        {
            if (amount <= 0)
            {
                Debug.LogWarning($"[CurrencySystem] AddGems: Gecersiz miktar ({amount}). Islem iptal.");
                return;
            }

            double oldAmount = _saveManager.Data.Gems;
            _saveManager.Data.Gems += amount;

            PublishCurrencyChanged(CurrencyType.Gem, oldAmount, _saveManager.Data.Gems, reason);
        }

        /// <summary>
        /// Belirtilen miktarda elmas harcar.
        /// </summary>
        /// <param name="amount">Harcanacak miktar (pozitif olmali)</param>
        /// <param name="reason">Islem nedeni (production_boost, research_speedup vb.)</param>
        /// <returns>Basarili ise true</returns>
        public bool SpendGems(int amount, string reason)
        {
            if (amount <= 0)
            {
                Debug.LogWarning($"[CurrencySystem] SpendGems: Gecersiz miktar ({amount}). Islem iptal.");
                return false;
            }

            if (_isProcessingTransaction)
            {
                Debug.LogWarning($"[CurrencySystem] SpendGems: Baska bir islem devam ediyor. '{reason}' reddedildi.");
                return false;
            }

            if (!CanAffordGems(amount))
            {
                return false;
            }

            _isProcessingTransaction = true;
            try
            {
                double oldAmount = _saveManager.Data.Gems;
                _saveManager.Data.Gems -= amount;

                PublishCurrencyChanged(CurrencyType.Gem, oldAmount, _saveManager.Data.Gems, reason);
                return true;
            }
            finally
            {
                _isProcessingTransaction = false;
            }
        }

        /// <summary>
        /// Belirtilen miktarda elmas karsilanabilir mi kontrol eder.
        /// </summary>
        public bool CanAffordGems(int amount)
        {
            return _saveManager.Data.Gems >= amount && amount > 0;
        }

        // =====================================================================
        // FRANCHISE PUANI (FP) ISLEMLERI
        // Referans: docs/ECONOMY_BALANCE.md Bolum 4 — Prestige Dengesi
        // =====================================================================

        /// <summary>
        /// Franchise puani ekler (prestige sonrasi).
        /// </summary>
        public void AddFranchisePoints(int amount, string reason)
        {
            if (amount <= 0) return;

            double oldAmount = _saveManager.Data.FranchisePoints;
            _saveManager.Data.FranchisePoints += amount;

            PublishCurrencyChanged(CurrencyType.FP, oldAmount, _saveManager.Data.FranchisePoints, reason);
        }

        /// <summary>
        /// Franchise puani harcar (bonus satin alma).
        /// </summary>
        public bool SpendFranchisePoints(int amount, string reason)
        {
            if (amount <= 0 || _saveManager.Data.FranchisePoints < amount)
                return false;

            if (_isProcessingTransaction)
            {
                Debug.LogWarning($"[CurrencySystem] SpendFP: Baska bir islem devam ediyor. '{reason}' reddedildi.");
                return false;
            }

            _isProcessingTransaction = true;
            try
            {
                double oldAmount = _saveManager.Data.FranchisePoints;
                _saveManager.Data.FranchisePoints -= amount;

                PublishCurrencyChanged(CurrencyType.FP, oldAmount, _saveManager.Data.FranchisePoints, reason);
                return true;
            }
            finally
            {
                _isProcessingTransaction = false;
            }
        }

        /// <summary>
        /// Belirtilen miktarda FP karsilanabilir mi kontrol eder.
        /// </summary>
        public bool CanAffordFranchisePoints(int amount)
        {
            return _saveManager.Data.FranchisePoints >= amount && amount > 0;
        }

        // =====================================================================
        // ITIBAR PUANI ISLEMLERI
        // Referans: docs/ECONOMY_BALANCE.md Bolum 2.3 — ItibarBonusu = 1 + (ItibarPuani / 10000)
        // =====================================================================

        /// <summary>
        /// Itibar puani ekler (siparis tamamlama sonrasi).
        /// </summary>
        public void AddReputation(int amount, string reason)
        {
            if (amount <= 0) return;

            double oldAmount = _saveManager.Data.Reputation;
            _saveManager.Data.Reputation += amount;

            PublishCurrencyChanged(CurrencyType.Reputation, oldAmount, _saveManager.Data.Reputation, reason);
        }

        /// <summary>
        /// Itibar puani dusurur (suresi gecen siparisler icin).
        /// Sifirin altina dusmez.
        /// </summary>
        public void RemoveReputation(int amount, string reason)
        {
            if (amount <= 0) return;

            double oldAmount = _saveManager.Data.Reputation;
            _saveManager.Data.Reputation = Math.Max(0, _saveManager.Data.Reputation - amount);

            PublishCurrencyChanged(CurrencyType.Reputation, oldAmount, _saveManager.Data.Reputation, reason);
        }

        // =====================================================================
        // YARDIMCI METOTLAR
        // =====================================================================

        /// <summary>
        /// Oyuncu seviyesini toplam kazanca gore hesaplar.
        /// Formul: floor(log10(ToplamKazanc + 1) x 5)
        /// Referans: docs/ECONOMY_BALANCE.md Bolum 2.4
        /// </summary>
        public int CalculatePlayerLevel()
        {
            double totalEarnings = _saveManager.Data.TotalLifetimeEarnings + _saveManager.Data.TotalEarnings;
            return (int)Math.Floor(Math.Log10(totalEarnings + 1) * 5);
        }

        /// <summary>
        /// Itibar bonusu carpanini hesaplar.
        /// Formul: 1 + (ItibarPuani / 10000)
        /// Her 100 itibar = +%1
        /// Referans: docs/ECONOMY_BALANCE.md Bolum 2.3
        /// </summary>
        public float GetReputationMultiplier()
        {
            return 1f + (_saveManager.Data.Reputation / 10000f);
        }

        // =====================================================================
        // IEconomySystem IMPLEMENTASYONU
        // =====================================================================

        /// <summary>
        /// Belirtilen para biriminden belirtilen miktari karsilayabilir mi?
        /// IEconomySystem interface implementasyonu.
        /// </summary>
        public bool CanAfford(CurrencyType type, double amount)
        {
            return type switch
            {
                CurrencyType.Coin => CanAffordCoins(amount),
                CurrencyType.Gem => CanAffordGems((int)amount),
                CurrencyType.FP => CanAffordFranchisePoints((int)amount),
                _ => false
            };
        }

        /// <summary>
        /// Belirtilen para birimine belirtilen miktari ekler.
        /// IEconomySystem interface implementasyonu.
        /// </summary>
        public void AddCurrency(CurrencyType type, double amount, string reason)
        {
            switch (type)
            {
                case CurrencyType.Coin:
                    AddCoins(amount, reason);
                    break;
                case CurrencyType.Gem:
                    AddGems((int)amount, reason);
                    break;
                case CurrencyType.FP:
                    AddFranchisePoints((int)amount, reason);
                    break;
                case CurrencyType.Reputation:
                    AddReputation((int)amount, reason);
                    break;
                default:
                    Debug.LogWarning($"[CurrencySystem] AddCurrency: Desteklenmeyen para birimi: {type}");
                    break;
            }
        }

        // =====================================================================
        // YARDIMCI METOTLAR
        // =====================================================================

        /// <summary>
        /// Para degisiklik eventini firlatir.
        /// </summary>
        private void PublishCurrencyChanged(CurrencyType type, double oldAmount, double newAmount, string reason)
        {
            _eventManager.Publish(new CurrencyChangedEvent
            {
                Type = type,
                OldAmount = oldAmount,
                NewAmount = newAmount,
                Reason = reason
            });
        }
    }
}
