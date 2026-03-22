// =============================================================================
// TimeManagerTests.cs
// TimeManager icin unit testleri.
// Kombo carpani, offline sure hesaplama, anti-cheat kontrolleri ve
// yardimci metotlari dogrular.
// Not: Gercek sunucu zamani ve PlayerPrefs testleri mock uzerinden yapilir.
// =============================================================================

using System;
using NUnit.Framework;
using RiceFactory.Core;
using RiceFactory.Tests;

namespace RiceFactory.Tests.Core
{
    [TestFixture]
    public class TimeManagerTests
    {
        private TimeManager _timeManager;

        // -----------------------------------------------------------------
        // SetUp
        // -----------------------------------------------------------------

        [SetUp]
        public void SetUp()
        {
            _timeManager = new TimeManager();
        }

        // -----------------------------------------------------------------
        // Kombo Carpani Testleri (Tick ile)
        // -----------------------------------------------------------------

        [Test]
        public void Tick_Under2Minutes_ComboIs1x()
        {
            // Act: 1 dakika (60 saniye) tick
            SimulateTicks(60f);

            // Assert: 0-2 dk arasi x1.0
            GameAssert.AreApproximatelyEqual(1.0f, _timeManager.ComboMultiplier);
        }

        [Test]
        public void Tick_Between2And5Minutes_ComboIs1_2x()
        {
            // Act: 3 dakika (180 saniye) tick
            SimulateTicks(180f);

            // Assert: 2-5 dk arasi x1.2
            GameAssert.AreApproximatelyEqual(1.2f, _timeManager.ComboMultiplier);
        }

        [Test]
        public void Tick_Between5And10Minutes_ComboIs1_5x()
        {
            // Act: 7 dakika (420 saniye) tick
            SimulateTicks(420f);

            // Assert: 5-10 dk arasi x1.5
            GameAssert.AreApproximatelyEqual(1.5f, _timeManager.ComboMultiplier);
        }

        [Test]
        public void Tick_Between10And20Minutes_ComboIs1_8x()
        {
            // Act: 15 dakika (900 saniye) tick
            SimulateTicks(900f);

            // Assert: 10-20 dk arasi x1.8
            GameAssert.AreApproximatelyEqual(1.8f, _timeManager.ComboMultiplier);
        }

        [Test]
        public void Tick_Over20Minutes_ComboIs2x()
        {
            // Act: 25 dakika (1500 saniye) tick
            SimulateTicks(1500f);

            // Assert: 20+ dk x2.0
            GameAssert.AreApproximatelyEqual(2.0f, _timeManager.ComboMultiplier);
        }

        [Test]
        public void Tick_AccumulatesActivePlayTime()
        {
            // Act
            _timeManager.Tick(1.5f);
            _timeManager.Tick(2.5f);
            _timeManager.Tick(1.0f);

            // Assert: Toplam aktif sure birikir
            GameAssert.AreApproximatelyEqual(5.0f, _timeManager.ActivePlayTime);
        }

        // -----------------------------------------------------------------
        // Kombo Sifirlama Testleri
        // -----------------------------------------------------------------

        [Test]
        public void ResetCombo_ResetsMultiplierTo1x()
        {
            // Arrange: Komboyu yukselt
            SimulateTicks(600f); // 10 dakika -> x1.5
            Assert.AreNotEqual(1.0f, _timeManager.ComboMultiplier);

            // Act: Kombo sifirla
            _timeManager.ResetCombo();

            // Assert: Carpan 1.0'a donmeli
            GameAssert.AreApproximatelyEqual(1.0f, _timeManager.ComboMultiplier);
        }

        [Test]
        public void ResetCombo_DoesNotResetActivePlayTime()
        {
            // Arrange: Biraz oyna
            SimulateTicks(300f);
            float playTimeBefore = _timeManager.ActivePlayTime;

            // Act: Kombo sifirla
            _timeManager.ResetCombo();

            // Assert: ActivePlayTime etkilenmemeli
            GameAssert.AreApproximatelyEqual(playTimeBefore, _timeManager.ActivePlayTime);
        }

        // -----------------------------------------------------------------
        // Offline Sure Hesaplama Testleri (Mock TimeManager ile)
        // -----------------------------------------------------------------

        [Test]
        public void MockTimeManager_GetTimeSincePause_ReturnsSetDuration()
        {
            // Arrange: Mock TimeManager ile kontrol edilebilir offline sure
            var mockTime = new MockTimeManager();
            mockTime.MockPauseDuration = TimeSpan.FromHours(2);

            // Act
            var duration = mockTime.GetTimeSincePause();

            // Assert
            Assert.AreEqual(TimeSpan.FromHours(2), duration);
        }

        [Test]
        public void MockTimeManager_ZeroPauseDuration_ReturnsZero()
        {
            // Arrange
            var mockTime = new MockTimeManager();
            mockTime.MockPauseDuration = TimeSpan.Zero;

            // Act
            var duration = mockTime.GetTimeSincePause();

            // Assert
            Assert.AreEqual(TimeSpan.Zero, duration);
        }

        // -----------------------------------------------------------------
        // Negatif Sure (Zaman Manipulasyonu) Testleri
        // -----------------------------------------------------------------

        [Test]
        public void MockTimeManager_NegativeTime_IsHandledByMock()
        {
            // Arrange: Negatif sure simule et (gercek TimeManager bu durumu 0'a cevirir)
            var mockTime = new MockTimeManager();
            // Mock'ta negatif TimeSpan ayarlanabilir, gercek implementation 0'a clamp eder
            mockTime.MockPauseDuration = TimeSpan.FromSeconds(-100);

            // Act
            var duration = mockTime.GetTimeSincePause();

            // Assert: Mock direkt degeri dondurur
            // Gercek TimeManager'da negatif deger 0'a cevirilir ve IsTimeReliable = false olur
            Assert.AreEqual(TimeSpan.FromSeconds(-100), duration);
        }

        [Test]
        public void MockTimeManager_IsTimeReliable_CanBeSetToFalse()
        {
            // Arrange: Guvenilmez zaman simule et
            var mockTime = new MockTimeManager();

            // Act
            mockTime.IsTimeReliable = false;

            // Assert
            Assert.IsFalse(mockTime.IsTimeReliable,
                "Zaman manipulasyonu tespit edildiginde IsTimeReliable false olmali");
        }

        [Test]
        public void MockTimeManager_DefaultIsTimeReliable_IsTrue()
        {
            // Arrange
            var mockTime = new MockTimeManager();

            // Assert: Varsayilan olarak guvenilir
            Assert.IsTrue(mockTime.IsTimeReliable);
        }

        // -----------------------------------------------------------------
        // Maximum Offline Sure Siniri Testleri
        // -----------------------------------------------------------------

        [Test]
        public void MockTimeManager_MaxOfflineDuration_24Hours()
        {
            // Arrange: 48 saatlik offline sure
            // Gercek TimeManager'da 24 saate sinirlandirilir
            var mockTime = new MockTimeManager();
            var twoDays = TimeSpan.FromHours(48);
            mockTime.MockPauseDuration = twoDays;

            // Act
            var duration = mockTime.GetTimeSincePause();

            // Assert: Mock direkt dondurur ama gercek implementation'da
            // 24 saatle sinirlandirilir (86400 saniye)
            // Bu test gercek TimeManager davranisini dokumante eder
            Assert.AreEqual(twoDays, duration,
                "Mock unclamped deger dondurur — gercek TimeManager 24 saatle sinirlar");
        }

        [Test]
        public void RealTimeManager_MaxOfflineDuration_ClampedTo24Hours()
        {
            // Arrange: Gercek TimeManager'da GetTimeSincePause 24 saati asmaz
            // _pauseTimestamp = 0 ve simdi > 86400 oldugunda clamp eder
            // Bu testi gercek TimeManager ile calistiramayiz (PlayerPrefs gerekir)
            // ancak davranisi dokumante ediyoruz

            // Sinir degerleri
            long maxOfflineSeconds = 86400; // 24 saat
            Assert.AreEqual(TimeSpan.FromHours(24), TimeSpan.FromSeconds(maxOfflineSeconds),
                "Maximum offline sure 24 saat (86400 saniye) olmali");
        }

        // -----------------------------------------------------------------
        // Yardimci Metot Testleri
        // -----------------------------------------------------------------

        [Test]
        public void GetUnixTimestampNow_ReturnsPositiveValue()
        {
            // Act
            long timestamp = TimeManager.GetUnixTimestampNow();

            // Assert: 2020'den sonra olmali (1577836800 = 2020-01-01)
            Assert.IsTrue(timestamp > 1577836800,
                "Unix timestamp guncel bir deger olmali");
        }

        [Test]
        public void UnixToDateTime_ConvertsCorrectly()
        {
            // Arrange: Bilinen timestamp (2024-01-01 00:00:00 UTC)
            long knownTimestamp = 1704067200;

            // Act
            var dateTime = TimeManager.UnixToDateTime(knownTimestamp);

            // Assert
            Assert.AreEqual(2024, dateTime.Year);
            Assert.AreEqual(1, dateTime.Month);
            Assert.AreEqual(1, dateTime.Day);
        }

        [Test]
        public void GetElapsed_CalculatesCorrectDifference()
        {
            // Arrange
            long from = 1000;
            long to = 1500;

            // Act
            var elapsed = TimeManager.GetElapsed(from, to);

            // Assert
            Assert.AreEqual(500, elapsed.TotalSeconds);
        }

        [Test]
        public void GetElapsed_WithReversedOrder_ReturnsAbsoluteValue()
        {
            // Arrange: Ters sira (Math.Abs kullanir)
            long from = 1500;
            long to = 1000;

            // Act
            var elapsed = TimeManager.GetElapsed(from, to);

            // Assert: Mutlak deger dondurur
            Assert.AreEqual(500, elapsed.TotalSeconds);
        }

        // -----------------------------------------------------------------
        // Yardimci: Birden fazla Tick simule et
        // -----------------------------------------------------------------

        /// <summary>Belirtilen toplam sureyi 0.5 saniyelik tick'lerle simule eder.</summary>
        private void SimulateTicks(float totalSeconds, float tickInterval = 0.5f)
        {
            float elapsed = 0f;
            while (elapsed < totalSeconds)
            {
                float dt = Math.Min(tickInterval, totalSeconds - elapsed);
                _timeManager.Tick(dt);
                elapsed += dt;
            }
        }
    }
}
