// =============================================================================
// EventManagerTests.cs
// EventManager icin unit testleri.
// Subscribe, Unsubscribe, Publish ve guvenlik senaryolarini dogrular.
// =============================================================================

using System;
using System.Collections.Generic;
using NUnit.Framework;
using RiceFactory.Core;
using RiceFactory.Core.Events;
using RiceFactory.Tests;

namespace RiceFactory.Tests.Core
{
    [TestFixture]
    public class EventManagerTests
    {
        private EventManager _eventManager;

        // Test icin basit event tanimlari
        private struct TestEvent : IGameEvent
        {
            public int Value;
            public TestEvent(int value) { Value = value; }
        }

        private struct AnotherTestEvent : IGameEvent
        {
            public string Message;
            public AnotherTestEvent(string message) { Message = message; }
        }

        // -----------------------------------------------------------------
        // SetUp / TearDown
        // -----------------------------------------------------------------

        [SetUp]
        public void SetUp()
        {
            _eventManager = new EventManager();
        }

        [TearDown]
        public void TearDown()
        {
            _eventManager.Clear();
        }

        // -----------------------------------------------------------------
        // Subscribe ve Publish Testleri
        // -----------------------------------------------------------------

        [Test]
        public void Subscribe_AndPublish_ListenerReceivesEvent()
        {
            // Arrange: Listener kaydet
            TestEvent receivedEvent = default;
            bool wasReceived = false;
            _eventManager.Subscribe<TestEvent>(e =>
            {
                receivedEvent = e;
                wasReceived = true;
            });

            // Act: Event yayinla
            _eventManager.Publish(new TestEvent(42));

            // Assert: Listener eventi almali
            Assert.IsTrue(wasReceived, "Listener event almali");
            Assert.AreEqual(42, receivedEvent.Value);
        }

        [Test]
        public void Publish_WithNoListeners_DoesNotThrow()
        {
            // Act & Assert: Listener olmadan yayinlamak hata vermemeli
            Assert.DoesNotThrow(() =>
            {
                _eventManager.Publish(new TestEvent(1));
            });
        }

        [Test]
        public void Publish_EventDataIsCorrectlyPassed()
        {
            // Arrange
            AnotherTestEvent receivedEvent = default;
            _eventManager.Subscribe<AnotherTestEvent>(e => receivedEvent = e);

            // Act
            _eventManager.Publish(new AnotherTestEvent("merhaba"));

            // Assert
            Assert.AreEqual("merhaba", receivedEvent.Message);
        }

        // -----------------------------------------------------------------
        // Unsubscribe Testleri
        // -----------------------------------------------------------------

        [Test]
        public void Unsubscribe_ListenerNoLongerReceivesEvents()
        {
            // Arrange
            int receiveCount = 0;
            Action<TestEvent> listener = e => receiveCount++;

            _eventManager.Subscribe(listener);
            _eventManager.Publish(new TestEvent(1));
            Assert.AreEqual(1, receiveCount, "Ilk publish'te 1 almali");

            // Act: Unsubscribe yap
            _eventManager.Unsubscribe(listener);
            _eventManager.Publish(new TestEvent(2));

            // Assert: Unsubscribe sonrasi event almamali
            Assert.AreEqual(1, receiveCount, "Unsubscribe sonrasi ek event almamali");
        }

        [Test]
        public void Unsubscribe_NonExistentListener_DoesNotThrow()
        {
            // Act & Assert: Kayitli olmayan listener'i kaldirmak hata vermemeli
            Action<TestEvent> listener = e => { };
            Assert.DoesNotThrow(() =>
            {
                _eventManager.Unsubscribe(listener);
            });
        }

        // -----------------------------------------------------------------
        // Birden Fazla Listener Testleri
        // -----------------------------------------------------------------

        [Test]
        public void MultipleListeners_AllReceiveEvent()
        {
            // Arrange: 3 farkli listener kaydet
            int count1 = 0, count2 = 0, count3 = 0;
            _eventManager.Subscribe<TestEvent>(e => count1++);
            _eventManager.Subscribe<TestEvent>(e => count2++);
            _eventManager.Subscribe<TestEvent>(e => count3++);

            // Act: Tek event yayinla
            _eventManager.Publish(new TestEvent(1));

            // Assert: Tum listener'lar almali
            Assert.AreEqual(1, count1, "Listener 1 almali");
            Assert.AreEqual(1, count2, "Listener 2 almali");
            Assert.AreEqual(1, count3, "Listener 3 almali");
        }

        [Test]
        public void MultipleListeners_UnsubscribeOne_OthersStillReceive()
        {
            // Arrange
            int count1 = 0, count2 = 0;
            Action<TestEvent> listener1 = e => count1++;
            Action<TestEvent> listener2 = e => count2++;

            _eventManager.Subscribe(listener1);
            _eventManager.Subscribe(listener2);

            // Act: Birini kaldir, event yayinla
            _eventManager.Unsubscribe(listener1);
            _eventManager.Publish(new TestEvent(1));

            // Assert: Sadece kalan listener almali
            Assert.AreEqual(0, count1, "Unsubscribe edilen listener almamali");
            Assert.AreEqual(1, count2, "Diger listener almali");
        }

        [Test]
        public void DifferentEventTypes_ListenersAreIsolated()
        {
            // Arrange: Farkli event tiplerine farkli listener'lar
            int testEventCount = 0;
            int anotherEventCount = 0;
            _eventManager.Subscribe<TestEvent>(e => testEventCount++);
            _eventManager.Subscribe<AnotherTestEvent>(e => anotherEventCount++);

            // Act: Sadece TestEvent yayinla
            _eventManager.Publish(new TestEvent(1));

            // Assert: Sadece TestEvent listener'i almali
            Assert.AreEqual(1, testEventCount);
            Assert.AreEqual(0, anotherEventCount);
        }

        // -----------------------------------------------------------------
        // Listener Icerisinden Unsubscribe Guvenligi
        // -----------------------------------------------------------------

        [Test]
        public void UnsubscribeInsideListener_IsSafe()
        {
            // Arrange: Listener kendi kendini unsubscribe eder
            int callCount = 0;
            Action<TestEvent> selfRemovingListener = null;
            selfRemovingListener = e =>
            {
                callCount++;
                _eventManager.Unsubscribe(selfRemovingListener);
            };

            _eventManager.Subscribe(selfRemovingListener);

            // Act: Ilk publish — listener kendini kaldirir
            Assert.DoesNotThrow(() =>
            {
                _eventManager.Publish(new TestEvent(1));
            });

            // Assert: Bir kez cagirilmali
            Assert.AreEqual(1, callCount, "Listener bir kez cagirilmali");

            // Ikinci publish — artik listener yok
            _eventManager.Publish(new TestEvent(2));
            Assert.AreEqual(1, callCount, "Ikinci publish'te cagirilmamali");
        }

        [Test]
        public void SubscribeInsideListener_IsSafe()
        {
            // Arrange: Listener icerisinden yeni listener eklenir
            int originalCount = 0;
            int newListenerCount = 0;

            Action<TestEvent> originalListener = e =>
            {
                originalCount++;
                if (originalCount == 1) // Sadece ilk sefer ekle
                {
                    _eventManager.Subscribe<TestEvent>(e2 => newListenerCount++);
                }
            };

            _eventManager.Subscribe(originalListener);

            // Act: Ilk publish — yeni listener eklenir (ama bu publish'te cagirilmaz cunku snapshot kullanilir)
            Assert.DoesNotThrow(() =>
            {
                _eventManager.Publish(new TestEvent(1));
            });

            Assert.AreEqual(1, originalCount);
            // Yeni listener ilk publish sirasinda eklendigi icin snapshot'ta yoktu
            Assert.AreEqual(0, newListenerCount, "Yeni listener ilk publish'te cagirilmamali (snapshot)");

            // Ikinci publish — artik yeni listener da aktif
            _eventManager.Publish(new TestEvent(2));
            Assert.AreEqual(2, originalCount);
            Assert.AreEqual(1, newListenerCount, "Yeni listener ikinci publish'te cagirilmali");
        }

        // -----------------------------------------------------------------
        // Listener Exception Guvenligi
        // -----------------------------------------------------------------

        [Test]
        public void ListenerException_DoesNotBlockOtherListeners()
        {
            // Arrange: Biri exception firlatir, digeri normal calisir
            int count = 0;
            _eventManager.Subscribe<TestEvent>(e => throw new Exception("Test exception"));
            _eventManager.Subscribe<TestEvent>(e => count++);

            // Unity'nin Debug.LogError cagrisini bekledigimizi belirt
            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Error,
                new System.Text.RegularExpressions.Regex(".*Test exception.*"));

            // Act: Event yayinla
            Assert.DoesNotThrow(() =>
            {
                _eventManager.Publish(new TestEvent(1));
            });

            // Assert: Ikinci listener hala cagirilmali
            Assert.AreEqual(1, count, "Exception firlatilan listener'dan sonra digeri cagirilmali");
        }

        // -----------------------------------------------------------------
        // Clear Testleri
        // -----------------------------------------------------------------

        [Test]
        public void Clear_RemovesAllListeners()
        {
            // Arrange
            int count = 0;
            _eventManager.Subscribe<TestEvent>(e => count++);
            _eventManager.Subscribe<AnotherTestEvent>(e => count++);

            // Act
            _eventManager.Clear();
            _eventManager.Publish(new TestEvent(1));
            _eventManager.Publish(new AnotherTestEvent("test"));

            // Assert: Hicbir listener cagirilmamali
            Assert.AreEqual(0, count, "Clear sonrasi listener cagirilmamali");
        }

        // -----------------------------------------------------------------
        // GetListenerCount Testleri
        // -----------------------------------------------------------------

        [Test]
        public void GetListenerCount_ReturnsCorrectCount()
        {
            // Arrange
            Assert.AreEqual(0, _eventManager.GetListenerCount<TestEvent>());

            // Act
            Action<TestEvent> l1 = e => { };
            Action<TestEvent> l2 = e => { };
            _eventManager.Subscribe(l1);
            _eventManager.Subscribe(l2);

            // Assert
            Assert.AreEqual(2, _eventManager.GetListenerCount<TestEvent>());

            // Birini kaldir
            _eventManager.Unsubscribe(l1);
            Assert.AreEqual(1, _eventManager.GetListenerCount<TestEvent>());
        }

        // -----------------------------------------------------------------
        // Ayni Listener Tekrar Eklenmemeli
        // -----------------------------------------------------------------

        [Test]
        public void Subscribe_SameListenerTwice_OnlyCalledOnce()
        {
            // Arrange
            int callCount = 0;
            Action<TestEvent> listener = e => callCount++;

            // Act: Ayni listener'i iki kez ekle
            _eventManager.Subscribe(listener);
            _eventManager.Subscribe(listener);
            _eventManager.Publish(new TestEvent(1));

            // Assert: Sadece bir kez cagirilmali (duplicate engellenir)
            Assert.AreEqual(1, callCount, "Ayni listener tekrar eklenmemeli");
            Assert.AreEqual(1, _eventManager.GetListenerCount<TestEvent>());
        }
    }
}
