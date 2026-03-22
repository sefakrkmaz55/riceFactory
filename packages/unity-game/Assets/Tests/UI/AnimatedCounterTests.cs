using NUnit.Framework;
using RiceFactory.UI;

namespace RiceFactory.Tests
{
    /// <summary>
    /// AnimatedCounter.FormatNumber static metodunun unit testleri.
    /// MonoBehaviour'dan bagimsiz — saf matematik formatlama testleri.
    /// </summary>
    [TestFixture]
    public class AnimatedCounterTests
    {
        // =====================================================================
        // KUCUK SAYILAR (1000'den kucuk — tam sayi gosterimi)
        // =====================================================================

        [Test]
        public void FormatNumber_Zero_ReturnsZero()
        {
            // Sifir degeri icin kontrol
            string result = AnimatedCounter.FormatNumber(0);
            Assert.AreEqual("0", result, "Sifir '0' olarak formatlanmali.");
        }

        [TestCase(1, "1")]
        [TestCase(42, "42")]
        [TestCase(100, "100")]
        [TestCase(999, "999")]
        public void FormatNumber_SmallNumbers_ReturnsWholeNumber(int value, string expected)
        {
            // 999 ve alti tam sayi olarak gosterilmeli
            string result = AnimatedCounter.FormatNumber(value);
            Assert.AreEqual(expected, result,
                $"{value} degeri '{expected}' olarak formatlanmali.");
        }

        // =====================================================================
        // KILO (K) — 1,000 - 999,999
        // =====================================================================

        [Test]
        public void FormatNumber_1000_Returns1K()
        {
            // 1000 = 1.00K
            string result = AnimatedCounter.FormatNumber(1000);
            Assert.AreEqual("1.00K", result, "1000 degeri '1.00K' olmali.");
        }

        [Test]
        public void FormatNumber_1500_Returns1_50K()
        {
            string result = AnimatedCounter.FormatNumber(1500);
            Assert.AreEqual("1.50K", result, "1500 degeri '1.50K' olmali.");
        }

        [Test]
        public void FormatNumber_15000_Returns15_0K()
        {
            // 15K — 10-99 araliginda 1 ondalik
            string result = AnimatedCounter.FormatNumber(15000);
            Assert.AreEqual("15.0K", result, "15000 degeri '15.0K' olmali.");
        }

        [Test]
        public void FormatNumber_999999_Returns1000K_Or1M()
        {
            // 999,999 => 1000K tier'da kalir veya 1M'a yuvarlanir
            // Implementasyona gore: 999.999 -> scaled=999.999, tier=1 -> 100+ oldugu icin tam sayi
            string result = AnimatedCounter.FormatNumber(999999);
            Assert.AreEqual("999K", result, "999999 degeri '999K' olmali.");
        }

        // =====================================================================
        // MEGA (M) — 1,000,000 - 999,999,999
        // =====================================================================

        [Test]
        public void FormatNumber_1500000_Returns1_50M()
        {
            string result = AnimatedCounter.FormatNumber(1_500_000);
            Assert.AreEqual("1.50M", result, "1.5 milyon '1.50M' olmali.");
        }

        [Test]
        public void FormatNumber_25000000_Returns25_0M()
        {
            string result = AnimatedCounter.FormatNumber(25_000_000);
            Assert.AreEqual("25.0M", result, "25 milyon '25.0M' olmali.");
        }

        // =====================================================================
        // BILLION (B) — 1,000,000,000+
        // =====================================================================

        [Test]
        public void FormatNumber_1Billion_Returns1_00B()
        {
            string result = AnimatedCounter.FormatNumber(1_000_000_000);
            Assert.AreEqual("1.00B", result, "1 milyar '1.00B' olmali.");
        }

        // =====================================================================
        // TRILLION (T) — 1,000,000,000,000+
        // =====================================================================

        [Test]
        public void FormatNumber_1Trillion_Returns1_00T()
        {
            string result = AnimatedCounter.FormatNumber(1e12);
            Assert.AreEqual("1.00T", result, "1 trilyon '1.00T' olmali.");
        }

        // =====================================================================
        // COK BUYUK SAYILAR (1e15+)
        // =====================================================================

        [Test]
        public void FormatNumber_1Quadrillion_Returns1_00Qa()
        {
            // 1e15 = Quadrillion = Qa
            string result = AnimatedCounter.FormatNumber(1e15);
            Assert.AreEqual("1.00Qa", result, "1 katrilyon '1.00Qa' olmali.");
        }

        [Test]
        public void FormatNumber_1Quintillion_Returns1_00Qi()
        {
            // 1e18 = Quintillion = Qi
            string result = AnimatedCounter.FormatNumber(1e18);
            Assert.AreEqual("1.00Qi", result, "1 kentilyon '1.00Qi' olmali.");
        }

        [Test]
        public void FormatNumber_VeryLargeNumber_DoesNotThrow()
        {
            // 1e30+ — suffix listesi disina cikabilir, crash olmamali
            Assert.DoesNotThrow(() => AnimatedCounter.FormatNumber(1e30),
                "Cok buyuk sayilarda exception firlatmamali.");
        }

        [Test]
        public void FormatNumber_MaxDoubleTier_HandledGracefully()
        {
            // En buyuk suffix Dc (1e33) olduktan sonra tasmamali
            string result = AnimatedCounter.FormatNumber(1e36);
            Assert.IsNotNull(result, "Cok buyuk sayi formatlama null donmemeli.");
            Assert.IsNotEmpty(result, "Cok buyuk sayi formatlama bos donmemeli.");
        }

        // =====================================================================
        // NEGATIF SAYILAR
        // =====================================================================

        [Test]
        public void FormatNumber_NegativeSmallNumber_ReturnsMinusPrefix()
        {
            string result = AnimatedCounter.FormatNumber(-500);
            Assert.AreEqual("-500", result, "Negatif kucuk sayi '-500' olmali.");
        }

        [Test]
        public void FormatNumber_NegativeLargeNumber_ReturnsMinusWithSuffix()
        {
            string result = AnimatedCounter.FormatNumber(-1_500_000);
            Assert.AreEqual("-1.50M", result, "Negatif buyuk sayi '-1.50M' olmali.");
        }

        // =====================================================================
        // ONDALIK GOSTERIM PARAMETRESI
        // =====================================================================

        [Test]
        public void FormatNumber_ShowDecimalFalse_NoDecimalPoint()
        {
            // showDecimal=false durumunda 100+ scaled degerler icin zaten yok,
            // kucuk scaled'da da olmamali
            string result = AnimatedCounter.FormatNumber(150_000_000, showDecimal: false);
            Assert.AreEqual("150M", result,
                "showDecimal=false ile ondalik gosterilmemeli.");
        }

        // =====================================================================
        // SINIR DEGERLERI
        // =====================================================================

        [Test]
        public void FormatNumber_ExactlyOneThousand_ShowsK()
        {
            string result = AnimatedCounter.FormatNumber(1000);
            Assert.IsTrue(result.Contains("K"),
                "Tam 1000 degeri K suffix'i icermeli.");
        }

        [TestCase(0.5)]
        [TestCase(0.99)]
        public void FormatNumber_FractionalBelowOne_ReturnsZero(double value)
        {
            // 1'den kucuk pozitif sayilar: (long)value = 0
            string result = AnimatedCounter.FormatNumber(value);
            Assert.AreEqual("0", result,
                $"{value} degeri '0' olarak formatlanmali (tam sayi kesme).");
        }
    }
}
