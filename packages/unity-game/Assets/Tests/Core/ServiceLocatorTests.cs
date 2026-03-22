// =============================================================================
// ServiceLocatorTests.cs
// ServiceLocator icin unit testleri.
// Register, Get, TryGet, Unregister ve Reset islemlerini dogrular.
// =============================================================================

using System;
using NUnit.Framework;
using RiceFactory.Core;
using RiceFactory.Tests;

namespace RiceFactory.Tests.Core
{
    [TestFixture]
    public class ServiceLocatorTests
    {
        // Test icin kullanilacak interface ve sinif
        private interface ITestService
        {
            string Name { get; }
        }

        private class TestServiceA : ITestService
        {
            public string Name => "ServiceA";
        }

        private class TestServiceB : ITestService
        {
            public string Name => "ServiceB";
        }

        private interface IAnotherService
        {
            int Value { get; }
        }

        private class AnotherService : IAnotherService
        {
            public int Value => 42;
        }

        // -----------------------------------------------------------------
        // SetUp / TearDown
        // -----------------------------------------------------------------

        [SetUp]
        public void SetUp()
        {
            // Her testten once ServiceLocator temizlenir
            ServiceLocator.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            ServiceLocator.Reset();
        }

        // -----------------------------------------------------------------
        // Register ve Get Testleri
        // -----------------------------------------------------------------

        [Test]
        public void Register_AndGet_ReturnsRegisteredService()
        {
            // Arrange: Servis olustur
            var service = new TestServiceA();

            // Act: Kaydet ve al
            ServiceLocator.Register<ITestService>(service);
            var result = ServiceLocator.Get<ITestService>();

            // Assert: Ayni instance donmeli
            Assert.AreSame(service, result);
            Assert.AreEqual("ServiceA", result.Name);
        }

        [Test]
        public void Register_OverwritesExistingService()
        {
            // Arrange: Iki farkli servis olustur
            var serviceA = new TestServiceA();
            var serviceB = new TestServiceB();

            // Act: Ayni tip ile iki kez kaydet
            ServiceLocator.Register<ITestService>(serviceA);
            ServiceLocator.Register<ITestService>(serviceB);
            var result = ServiceLocator.Get<ITestService>();

            // Assert: Son kaydedilen donmeli
            Assert.AreSame(serviceB, result);
            Assert.AreEqual("ServiceB", result.Name);
        }

        [Test]
        public void Register_MultipleDifferentServices_AllAccessible()
        {
            // Arrange: Farkli tipte servisler
            var testService = new TestServiceA();
            var anotherService = new AnotherService();

            // Act: Farkli tipler ile kaydet
            ServiceLocator.Register<ITestService>(testService);
            ServiceLocator.Register<IAnotherService>(anotherService);

            // Assert: Her iki servis de erisilebiilr olmali
            Assert.AreSame(testService, ServiceLocator.Get<ITestService>());
            Assert.AreSame(anotherService, ServiceLocator.Get<IAnotherService>());
        }

        // -----------------------------------------------------------------
        // Kayitli Olmayan Servis Testleri
        // -----------------------------------------------------------------

        [Test]
        public void Get_UnregisteredService_ThrowsInvalidOperationException()
        {
            // Act & Assert: Kayitli olmayan servis icin exception beklenir
            Assert.Throws<InvalidOperationException>(() =>
            {
                ServiceLocator.Get<ITestService>();
            });
        }

        [Test]
        public void TryGet_UnregisteredService_ReturnsFalse()
        {
            // Act: Kayitli olmayan servisi guvenli al
            bool found = ServiceLocator.TryGet<ITestService>(out var service);

            // Assert: Bulunamadi, null donmeli
            Assert.IsFalse(found);
            Assert.IsNull(service);
        }

        [Test]
        public void TryGet_RegisteredService_ReturnsTrueAndService()
        {
            // Arrange
            var testService = new TestServiceA();
            ServiceLocator.Register<ITestService>(testService);

            // Act
            bool found = ServiceLocator.TryGet<ITestService>(out var service);

            // Assert
            Assert.IsTrue(found);
            Assert.AreSame(testService, service);
        }

        [Test]
        public void IsRegistered_UnregisteredService_ReturnsFalse()
        {
            // Act & Assert
            Assert.IsFalse(ServiceLocator.IsRegistered<ITestService>());
        }

        [Test]
        public void IsRegistered_RegisteredService_ReturnsTrue()
        {
            // Arrange
            ServiceLocator.Register<ITestService>(new TestServiceA());

            // Act & Assert
            Assert.IsTrue(ServiceLocator.IsRegistered<ITestService>());
        }

        // -----------------------------------------------------------------
        // Unregister Testleri
        // -----------------------------------------------------------------

        [Test]
        public void Unregister_RemovesService()
        {
            // Arrange: Servisi kaydet
            ServiceLocator.Register<ITestService>(new TestServiceA());
            Assert.IsTrue(ServiceLocator.IsRegistered<ITestService>());

            // Act: Servisi kaldir
            ServiceLocator.Unregister<ITestService>();

            // Assert: Artik kayitli olmamali
            Assert.IsFalse(ServiceLocator.IsRegistered<ITestService>());
            Assert.Throws<InvalidOperationException>(() => ServiceLocator.Get<ITestService>());
        }

        [Test]
        public void Unregister_NonExistentService_DoesNotThrow()
        {
            // Act & Assert: Kayitli olmayan servisi kaldirmak hata vermemeli
            Assert.DoesNotThrow(() =>
            {
                ServiceLocator.Unregister<ITestService>();
            });
        }

        [Test]
        public void Unregister_DoesNotAffectOtherServices()
        {
            // Arrange: Iki farkli servis kaydet
            ServiceLocator.Register<ITestService>(new TestServiceA());
            ServiceLocator.Register<IAnotherService>(new AnotherService());

            // Act: Birini kaldir
            ServiceLocator.Unregister<ITestService>();

            // Assert: Digeri hala erisilebiilr olmali
            Assert.IsFalse(ServiceLocator.IsRegistered<ITestService>());
            Assert.IsTrue(ServiceLocator.IsRegistered<IAnotherService>());
        }

        // -----------------------------------------------------------------
        // Reset Testleri
        // -----------------------------------------------------------------

        [Test]
        public void Reset_ClearsAllServices()
        {
            // Arrange: Birden fazla servis kaydet
            ServiceLocator.Register<ITestService>(new TestServiceA());
            ServiceLocator.Register<IAnotherService>(new AnotherService());

            // Act: Tum servisleri temizle
            ServiceLocator.Reset();

            // Assert: Hicbir servis kayitli olmamali
            Assert.IsFalse(ServiceLocator.IsRegistered<ITestService>());
            Assert.IsFalse(ServiceLocator.IsRegistered<IAnotherService>());
        }

        [Test]
        public void Reset_CanRegisterAfterReset()
        {
            // Arrange: Kaydet ve temizle
            ServiceLocator.Register<ITestService>(new TestServiceA());
            ServiceLocator.Reset();

            // Act: Tekrar kaydet
            var newService = new TestServiceB();
            ServiceLocator.Register<ITestService>(newService);

            // Assert: Yeni servis erisilebiilr olmali
            var result = ServiceLocator.Get<ITestService>();
            Assert.AreSame(newService, result);
            Assert.AreEqual("ServiceB", result.Name);
        }
    }
}
