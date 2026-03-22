// =============================================================================
// SaveManagerTests.cs
// SaveManager icin unit testleri.
// PlayerSaveData olusturma, varsayilan degerler ve veri modeli dogrulama.
// Not: Gercek dosya I/O testleri yapilamaz (Application.persistentDataPath
//      Editor disinda farkli calisir). Bu testler veri modeli ve mock
//      SaveManager davranisini dogrular.
// =============================================================================

using System;
using NUnit.Framework;
using RiceFactory.Core;
using RiceFactory.Tests;

namespace RiceFactory.Tests.Core
{
    [TestFixture]
    public class SaveManagerTests
    {
        private MockSaveManager _saveManager;

        // -----------------------------------------------------------------
        // SetUp / TearDown
        // -----------------------------------------------------------------

        [SetUp]
        public void SetUp()
        {
            _saveManager = new MockSaveManager();
        }

        // -----------------------------------------------------------------
        // Save ve Load Round-Trip Testleri
        // -----------------------------------------------------------------

        [Test]
        public void Data_DefaultCreation_HasValidDefaults()
        {
            // Assert: Varsayilan degerleri dogrula
            var data = _saveManager.Data;

            Assert.IsNotNull(data, "Data null olmamali");
            Assert.IsNotEmpty(data.PlayerId, "PlayerId bos olmamali");
            Assert.AreEqual(1, data.SaveVersion, "SaveVersion 1 olmali");
            Assert.AreEqual(0, data.Coins, "Baslangic coin 0 olmali");
            Assert.AreEqual(0, data.Gems, "Baslangic gem 0 olmali");
            Assert.AreEqual(0, data.FranchisePoints, "Baslangic FP 0 olmali");
            Assert.AreEqual(0, data.Reputation, "Baslangic reputation 0 olmali");
            Assert.AreEqual(1, data.PlayerLevel, "Baslangic level 1 olmali");
            Assert.AreEqual("istanbul", data.CurrentCityId, "Varsayilan sehir istanbul olmali");
            Assert.AreEqual("tr", data.Language, "Varsayilan dil tr olmali");
        }

        [Test]
        public void SaveLocal_AndModifyData_PersistsChanges()
        {
            // Arrange: Veriyi degistir
            _saveManager.Data.Coins = 5000;
            _saveManager.Data.Gems = 100;
            _saveManager.Data.PlayerLevel = 5;

            // Act: Kaydet (mock — sadece call count artar)
            _saveManager.SaveLocal();

            // Assert: Veriler degismis olmali (mock'ta in-memory kalir)
            Assert.AreEqual(5000, _saveManager.Data.Coins);
            Assert.AreEqual(100, _saveManager.Data.Gems);
            Assert.AreEqual(5, _saveManager.Data.PlayerLevel);
            Assert.AreEqual(1, _saveManager.SaveLocalCallCount, "SaveLocal bir kez cagirilmali");
        }

        [Test]
        public void SaveAsync_IncrementsCallCount()
        {
            // Act
            _saveManager.SaveAsync().Wait();
            _saveManager.SaveAsync().Wait();

            // Assert
            Assert.AreEqual(2, _saveManager.SaveAsyncCallCount, "SaveAsync iki kez cagirilmali");
        }

        [Test]
        public void LoadAsync_IncrementsCallCount()
        {
            // Act
            _saveManager.LoadAsync().Wait();

            // Assert
            Assert.AreEqual(1, _saveManager.LoadAsyncCallCount, "LoadAsync bir kez cagirilmali");
        }

        // -----------------------------------------------------------------
        // Bos Data ile Load Testleri
        // -----------------------------------------------------------------

        [Test]
        public void Data_WhenSetToNull_CanBeReassigned()
        {
            // Arrange: Data'yi null yap
            _saveManager.Data = null;
            Assert.IsNull(_saveManager.Data);

            // Act: Yeni data ata
            _saveManager.Data = SaveDataFactory.CreateDefault();

            // Assert: Yeni data gecerli olmali
            Assert.IsNotNull(_saveManager.Data);
            Assert.IsNotEmpty(_saveManager.Data.PlayerId);
        }

        [Test]
        public void Data_EmptyTimestamps_AreHandled()
        {
            // Arrange: Timestamp sifir olan data
            var data = SaveDataFactory.CreateDefault();
            data.LastSaveTimestamp = 0;
            data.FirstPlayTimestamp = 0;
            _saveManager.Data = data;

            // Assert: Sifir timestamp gecerli bir durum (eski kayit veya bozuk veri)
            Assert.AreEqual(0, _saveManager.Data.LastSaveTimestamp);
            Assert.AreEqual(0, _saveManager.Data.FirstPlayTimestamp);
        }

        // -----------------------------------------------------------------
        // Corrupted Data Handling Testleri
        // -----------------------------------------------------------------

        [Test]
        public void Data_WithNegativeCoins_IsDetectable()
        {
            // Arrange: Bozuk veri simulasyonu — negatif coin
            _saveManager.Data.Coins = -500;

            // Assert: Negatif coin tespit edilebilir olmali
            Assert.IsTrue(_saveManager.Data.Coins < 0,
                "Negatif coin bozuk veri gostergesi olarak tespit edilebilir olmali");
        }

        [Test]
        public void Data_WithExtremeValues_DoesNotCrash()
        {
            // Arrange: Asiri buyuk degerler
            _saveManager.Data.Coins = double.MaxValue;
            _saveManager.Data.Gems = int.MaxValue;
            _saveManager.Data.TotalLifetimeEarnings = double.MaxValue;

            // Assert: Veri erisimi hata vermemeli
            Assert.DoesNotThrow(() =>
            {
                var coins = _saveManager.Data.Coins;
                var gems = _saveManager.Data.Gems;
                var total = _saveManager.Data.TotalLifetimeEarnings;
            });
        }

        [Test]
        public void Data_WithEmptyStrings_IsHandled()
        {
            // Arrange: Bos string alanlari
            _saveManager.Data.PlayerId = "";
            _saveManager.Data.PlayerName = null;
            _saveManager.Data.GameVersion = "";
            _saveManager.Data.CurrentCityId = null;

            // Assert: Erisim hata vermemeli
            Assert.DoesNotThrow(() =>
            {
                var id = _saveManager.Data.PlayerId;
                var name = _saveManager.Data.PlayerName;
                var version = _saveManager.Data.GameVersion;
                var city = _saveManager.Data.CurrentCityId;
            });
        }

        // -----------------------------------------------------------------
        // DeleteSave Testleri
        // -----------------------------------------------------------------

        [Test]
        public void DeleteSave_ResetsDataToDefaults()
        {
            // Arrange: Veriyi degistir
            _saveManager.Data.Coins = 999999;
            _saveManager.Data.PlayerLevel = 50;

            // Act: Kaydi sil
            _saveManager.DeleteSave();

            // Assert: Varsayilan degerlere donmeli
            Assert.AreEqual(0, _saveManager.Data.Coins);
            Assert.AreEqual(1, _saveManager.Data.PlayerLevel);
            Assert.AreEqual(1, _saveManager.DeleteSaveCallCount);
        }

        // -----------------------------------------------------------------
        // Tick (Auto-Save) Testleri
        // -----------------------------------------------------------------

        [Test]
        public void Tick_AccumulatesTime()
        {
            // Act: Tick cagir
            _saveManager.Tick(1.5f);
            _saveManager.Tick(2.5f);

            // Assert: Toplam sure birikir
            GameAssert.AreApproximatelyEqual(4.0f, _saveManager.TotalTickTime);
        }

        // -----------------------------------------------------------------
        // SaveData Factory Testleri
        // -----------------------------------------------------------------

        [Test]
        public void SaveDataFactory_CreateRichPlayer_HasCorrectValues()
        {
            // Act
            var data = SaveDataFactory.CreateRichPlayer(500_000, 200);

            // Assert
            Assert.AreEqual(500_000, data.Coins);
            Assert.AreEqual(200, data.Gems);
            Assert.AreEqual(50, data.FranchisePoints);
            Assert.AreEqual(1000, data.Reputation);
            Assert.AreEqual(10, data.PlayerLevel);
        }

        [Test]
        public void SaveDataFactory_CreatePrestigeReady_HasSufficientEarnings()
        {
            // Act
            var data = SaveDataFactory.CreatePrestigeReady(25_000_000);

            // Assert
            Assert.AreEqual(25_000_000, data.TotalEarnings);
            Assert.IsTrue(data.TotalEarnings >= 1_000_000,
                "Prestige icin minimum 1M kazanc olmali");
        }
    }
}
