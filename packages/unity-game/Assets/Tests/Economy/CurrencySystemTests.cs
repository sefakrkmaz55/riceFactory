// =============================================================================
// CurrencySystemTests.cs
// CurrencySystem icin unit testleri.
// Coin/Gem islemleri, CanAfford kontrolleri, event firlama ve
// negatif harcama korumasi dogrulanir.
// =============================================================================

using System;
using NUnit.Framework;
using RiceFactory.Core;
using RiceFactory.Core.Events;
using RiceFactory.Economy;
using RiceFactory.Tests;

namespace RiceFactory.Tests.Economy
{
    [TestFixture]
    public class CurrencySystemTests
    {
        private CurrencySystem _currencySystem;
        private MockSaveManager _saveManager;
        private EventManager _eventManager;

        // -----------------------------------------------------------------
        // SetUp / TearDown
        // -----------------------------------------------------------------

        [SetUp]
        public void SetUp()
        {
            _saveManager = new MockSaveManager();
            _eventManager = new EventManager();
            _currencySystem = new CurrencySystem(_saveManager, _eventManager);
        }

        [TearDown]
        public void TearDown()
        {
            _eventManager.Clear();
        }

        // -----------------------------------------------------------------
        // AddCoins Testleri
        // -----------------------------------------------------------------

        [Test]
        public void AddCoins_IncreasesBalance()
        {
            // Arrange: Baslangic 0 coin
            Assert.AreEqual(0, _currencySystem.Coins);

            // Act
            _currencySystem.AddCoins(1000, "test_reward");

            // Assert
            GameAssert.AreApproximatelyEqual(1000, _currencySystem.Coins);
        }

        [Test]
        public void AddCoins_MultipleTimes_Accumulates()
        {
            // Act
            _currencySystem.AddCoins(500, "reward_1");
            _currencySystem.AddCoins(300, "reward_2");
            _currencySystem.AddCoins(200, "reward_3");

            // Assert
            GameAssert.AreApproximatelyEqual(1000, _currencySystem.Coins);
        }

        [Test]
        public void AddCoins_UpdatesTotalEarnings()
        {
            // Act
            _currencySystem.AddCoins(5000, "production");

            // Assert: TotalEarnings da artmali
            GameAssert.AreApproximatelyEqual(5000, _currencySystem.TotalEarnings);
        }

        [Test]
        public void AddCoins_ZeroAmount_IsIgnored()
        {
            // Act: Sifir miktar ekleme
            _currencySystem.AddCoins(0, "zero_test");

            // Assert: Degisiklik olmamali
            GameAssert.AreApproximatelyEqual(0, _currencySystem.Coins);
        }

        [Test]
        public void AddCoins_NegativeAmount_IsIgnored()
        {
            // Arrange: Once biraz coin ekle
            _currencySystem.AddCoins(1000, "initial");

            // Act: Negatif miktar ekleme
            _currencySystem.AddCoins(-500, "negative_test");

            // Assert: Coin miktari degismemeli
            GameAssert.AreApproximatelyEqual(1000, _currencySystem.Coins);
        }

        // -----------------------------------------------------------------
        // SpendCoins Testleri
        // -----------------------------------------------------------------

        [Test]
        public void SpendCoins_DecreasesBalance()
        {
            // Arrange
            _currencySystem.AddCoins(1000, "setup");

            // Act
            bool result = _currencySystem.SpendCoins(400, "purchase");

            // Assert
            Assert.IsTrue(result);
            GameAssert.AreApproximatelyEqual(600, _currencySystem.Coins);
        }

        [Test]
        public void SpendCoins_ExactAmount_LeavesZero()
        {
            // Arrange
            _currencySystem.AddCoins(500, "setup");

            // Act
            bool result = _currencySystem.SpendCoins(500, "all_in");

            // Assert
            Assert.IsTrue(result);
            GameAssert.AreApproximatelyEqual(0, _currencySystem.Coins);
        }

        [Test]
        public void SpendCoins_InsufficientBalance_ReturnsFalse()
        {
            // Arrange
            _currencySystem.AddCoins(100, "setup");

            // Act
            bool result = _currencySystem.SpendCoins(500, "too_expensive");

            // Assert: Islem basarisiz, bakiye degismemeli
            Assert.IsFalse(result);
            GameAssert.AreApproximatelyEqual(100, _currencySystem.Coins);
        }

        // -----------------------------------------------------------------
        // CanAfford Testleri
        // -----------------------------------------------------------------

        [Test]
        public void CanAffordCoins_SufficientBalance_ReturnsTrue()
        {
            // Arrange
            _currencySystem.AddCoins(1000, "setup");

            // Assert
            Assert.IsTrue(_currencySystem.CanAffordCoins(500));
            Assert.IsTrue(_currencySystem.CanAffordCoins(1000)); // Tam miktar
        }

        [Test]
        public void CanAffordCoins_InsufficientBalance_ReturnsFalse()
        {
            // Arrange
            _currencySystem.AddCoins(100, "setup");

            // Assert
            Assert.IsFalse(_currencySystem.CanAffordCoins(500));
        }

        [Test]
        public void CanAffordCoins_ZeroBalance_ReturnsFalse()
        {
            // Assert: 0 bakiye ile hicbir sey karsilanamaz
            Assert.IsFalse(_currencySystem.CanAffordCoins(1));
        }

        [Test]
        public void CanAffordCoins_ZeroAmount_ReturnsFalse()
        {
            // Arrange
            _currencySystem.AddCoins(1000, "setup");

            // Assert: Sifir miktar sorgulamak false doner (amount > 0 kontrolu)
            Assert.IsFalse(_currencySystem.CanAffordCoins(0));
        }

        [Test]
        public void CanAffordCoins_NegativeAmount_ReturnsFalse()
        {
            // Arrange
            _currencySystem.AddCoins(1000, "setup");

            // Assert: Negatif miktar false doner
            Assert.IsFalse(_currencySystem.CanAffordCoins(-100));
        }

        // -----------------------------------------------------------------
        // Negatif Harcama Korumasi
        // -----------------------------------------------------------------

        [Test]
        public void SpendCoins_ZeroAmount_ReturnsFalse()
        {
            // Arrange
            _currencySystem.AddCoins(1000, "setup");

            // Act
            bool result = _currencySystem.SpendCoins(0, "zero_spend");

            // Assert: Sifir harcama reddedilir
            Assert.IsFalse(result);
            GameAssert.AreApproximatelyEqual(1000, _currencySystem.Coins);
        }

        [Test]
        public void SpendCoins_NegativeAmount_ReturnsFalse()
        {
            // Arrange
            _currencySystem.AddCoins(1000, "setup");

            // Act: Negatif harcama (exploit denemesi)
            bool result = _currencySystem.SpendCoins(-500, "exploit_attempt");

            // Assert: Reddedilir, bakiye degismez
            Assert.IsFalse(result);
            GameAssert.AreApproximatelyEqual(1000, _currencySystem.Coins);
        }

        // -----------------------------------------------------------------
        // Gem (Elmas) Testleri
        // -----------------------------------------------------------------

        [Test]
        public void AddGems_IncreasesBalance()
        {
            // Act
            _currencySystem.AddGems(50, "daily_login");

            // Assert
            Assert.AreEqual(50, _currencySystem.Gems);
        }

        [Test]
        public void SpendGems_DecreasesBalance()
        {
            // Arrange
            _currencySystem.AddGems(100, "setup");

            // Act
            bool result = _currencySystem.SpendGems(30, "production_boost");

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(70, _currencySystem.Gems);
        }

        [Test]
        public void SpendGems_InsufficientBalance_ReturnsFalse()
        {
            // Arrange
            _currencySystem.AddGems(10, "setup");

            // Act
            bool result = _currencySystem.SpendGems(50, "too_expensive");

            // Assert
            Assert.IsFalse(result);
            Assert.AreEqual(10, _currencySystem.Gems);
        }

        [Test]
        public void CanAffordGems_SufficientBalance_ReturnsTrue()
        {
            _currencySystem.AddGems(100, "setup");
            Assert.IsTrue(_currencySystem.CanAffordGems(50));
        }

        [Test]
        public void CanAffordGems_InsufficientBalance_ReturnsFalse()
        {
            _currencySystem.AddGems(10, "setup");
            Assert.IsFalse(_currencySystem.CanAffordGems(50));
        }

        // -----------------------------------------------------------------
        // Event Firlama Dogrulama
        // -----------------------------------------------------------------

        [Test]
        public void AddCoins_FiresCurrencyChangedEvent()
        {
            // Arrange
            var recorder = new EventRecorder<CurrencyChangedEvent>();
            recorder.SubscribeTo(_eventManager);

            // Act
            _currencySystem.AddCoins(1000, "test_event");

            // Assert
            Assert.AreEqual(1, recorder.Count, "CurrencyChangedEvent firlanmali");
            Assert.AreEqual(CurrencyType.Coin, recorder.Last.Type);
            GameAssert.AreApproximatelyEqual(0, recorder.Last.OldAmount);
            GameAssert.AreApproximatelyEqual(1000, recorder.Last.NewAmount);
            Assert.AreEqual("test_event", recorder.Last.Reason);
        }

        [Test]
        public void SpendCoins_FiresCurrencyChangedEvent()
        {
            // Arrange
            _currencySystem.AddCoins(1000, "setup");
            var recorder = new EventRecorder<CurrencyChangedEvent>();
            recorder.SubscribeTo(_eventManager);

            // Act
            _currencySystem.SpendCoins(400, "upgrade");

            // Assert
            Assert.AreEqual(1, recorder.Count);
            Assert.AreEqual(CurrencyType.Coin, recorder.Last.Type);
            GameAssert.AreApproximatelyEqual(1000, recorder.Last.OldAmount);
            GameAssert.AreApproximatelyEqual(600, recorder.Last.NewAmount);
            Assert.AreEqual("upgrade", recorder.Last.Reason);
        }

        [Test]
        public void SpendCoins_Failure_DoesNotFireEvent()
        {
            // Arrange: Yetersiz bakiye
            _currencySystem.AddCoins(100, "setup");
            var recorder = new EventRecorder<CurrencyChangedEvent>();
            recorder.SubscribeTo(_eventManager);

            // Act: Basarisiz harcama
            _currencySystem.SpendCoins(500, "fail");

            // Assert: Event firlanmamali
            Assert.AreEqual(0, recorder.Count, "Basarisiz islemde event firlanmamali");
        }

        [Test]
        public void AddGems_FiresCurrencyChangedEvent()
        {
            // Arrange
            var recorder = new EventRecorder<CurrencyChangedEvent>();
            recorder.SubscribeTo(_eventManager);

            // Act
            _currencySystem.AddGems(25, "milestone");

            // Assert
            Assert.AreEqual(1, recorder.Count);
            Assert.AreEqual(CurrencyType.Gem, recorder.Last.Type);
        }

        // -----------------------------------------------------------------
        // Reputation Testleri
        // -----------------------------------------------------------------

        [Test]
        public void AddReputation_IncreasesValue()
        {
            // Act
            _currencySystem.AddReputation(100, "order_complete");

            // Assert
            Assert.AreEqual(100, _currencySystem.Reputation);
        }

        [Test]
        public void RemoveReputation_DecreasesValue()
        {
            // Arrange
            _currencySystem.AddReputation(500, "setup");

            // Act
            _currencySystem.RemoveReputation(200, "order_expired");

            // Assert
            Assert.AreEqual(300, _currencySystem.Reputation);
        }

        [Test]
        public void RemoveReputation_DoesNotGoBelowZero()
        {
            // Arrange
            _currencySystem.AddReputation(50, "setup");

            // Act: Mevcut degerin ustunde cikarma
            _currencySystem.RemoveReputation(200, "penalty");

            // Assert: Sifirin altina dusmemeli
            Assert.AreEqual(0, _currencySystem.Reputation);
        }

        // -----------------------------------------------------------------
        // Yardimci Metot Testleri
        // -----------------------------------------------------------------

        [Test]
        public void GetReputationMultiplier_CalculatesCorrectly()
        {
            // Arrange: 1000 itibar puani
            _currencySystem.AddReputation(1000, "setup");

            // Act
            float multiplier = _currencySystem.GetReputationMultiplier();

            // Assert: 1 + (1000 / 10000) = 1.1
            GameAssert.AreApproximatelyEqual(1.1f, multiplier);
        }

        [Test]
        public void GetReputationMultiplier_ZeroReputation_Returns1()
        {
            // Act
            float multiplier = _currencySystem.GetReputationMultiplier();

            // Assert
            GameAssert.AreApproximatelyEqual(1.0f, multiplier);
        }

        // -----------------------------------------------------------------
        // Constructor Dogrulama
        // -----------------------------------------------------------------

        [Test]
        public void Constructor_NullSaveManager_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                new CurrencySystem(null, _eventManager);
            });
        }

        [Test]
        public void Constructor_NullEventManager_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                new CurrencySystem(_saveManager, null);
            });
        }
    }
}
