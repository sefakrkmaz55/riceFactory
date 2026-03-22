// =============================================================================
// GameManagerTests.cs
// GameManager icin unit testleri.
// State gecisleri, tick dongusu, event firlama ve yasam dongusu dogrulama.
// =============================================================================

using System;
using NUnit.Framework;
using RiceFactory.Core;
using RiceFactory.Core.Events;
using RiceFactory.Tests;

namespace RiceFactory.Tests.Core
{
    [TestFixture]
    public class GameManagerTests
    {
        private GameManager _gameManager;
        private MockSaveManager _saveManager;
        private EventManager _eventManager;
        private MockTimeManager _timeManager;

        // -----------------------------------------------------------------
        // SetUp / TearDown
        // -----------------------------------------------------------------

        [SetUp]
        public void SetUp()
        {
            ServiceLocator.Reset();

            _saveManager = new MockSaveManager();
            _eventManager = new EventManager();
            _timeManager = new MockTimeManager();

            _gameManager = new GameManager(_saveManager, _eventManager, _timeManager);
        }

        [TearDown]
        public void TearDown()
        {
            _eventManager.Clear();
            ServiceLocator.Reset();
        }

        // -----------------------------------------------------------------
        // State Degisim Testleri
        // -----------------------------------------------------------------

        [Test]
        public void InitialState_IsLoading()
        {
            // Assert: Baslangic durumu Loading olmali
            Assert.AreEqual(GameState.Loading, _gameManager.CurrentState);
        }

        [Test]
        public void ChangeState_UpdatesCurrentState()
        {
            // Act
            _gameManager.ChangeState(GameState.Playing);

            // Assert
            Assert.AreEqual(GameState.Playing, _gameManager.CurrentState);
        }

        [Test]
        public void ChangeState_MultipleTransitions_AllSucceed()
        {
            // Act: Birden fazla gecis
            _gameManager.ChangeState(GameState.MainMenu);
            Assert.AreEqual(GameState.MainMenu, _gameManager.CurrentState);

            _gameManager.ChangeState(GameState.Playing);
            Assert.AreEqual(GameState.Playing, _gameManager.CurrentState);

            _gameManager.ChangeState(GameState.Paused);
            Assert.AreEqual(GameState.Paused, _gameManager.CurrentState);

            _gameManager.ChangeState(GameState.MiniGame);
            Assert.AreEqual(GameState.MiniGame, _gameManager.CurrentState);

            _gameManager.ChangeState(GameState.Playing);
            Assert.AreEqual(GameState.Playing, _gameManager.CurrentState);
        }

        // -----------------------------------------------------------------
        // Ayni State'e Gecis Testi
        // -----------------------------------------------------------------

        [Test]
        public void ChangeState_ToSameState_DoesNotFireEvent()
        {
            // Arrange
            _gameManager.ChangeState(GameState.Playing);

            var recorder = new EventRecorder<GameStateChangedEvent>();
            recorder.SubscribeTo(_eventManager);

            // Act: Ayni state'e gecis
            _gameManager.ChangeState(GameState.Playing);

            // Assert: Event firlanmamali
            Assert.AreEqual(0, recorder.Count,
                "Ayni state'e geciste event firlanmamali");
        }

        // -----------------------------------------------------------------
        // Event Firlama Dogrulama
        // -----------------------------------------------------------------

        [Test]
        public void ChangeState_FiresGameStateChangedEvent()
        {
            // Arrange
            var recorder = new EventRecorder<GameStateChangedEvent>();
            recorder.SubscribeTo(_eventManager);

            // Act: State degistir
            _gameManager.ChangeState(GameState.Playing);

            // Assert: Event firlanmali
            Assert.AreEqual(1, recorder.Count, "Bir adet state change eventi firlanmali");
            Assert.AreEqual(GameState.Loading, recorder.Last.OldState);
            Assert.AreEqual(GameState.Playing, recorder.Last.NewState);
        }

        [Test]
        public void ChangeState_MultipleTimes_FiresCorrectEvents()
        {
            // Arrange
            var recorder = new EventRecorder<GameStateChangedEvent>();
            recorder.SubscribeTo(_eventManager);

            // Act: Iki gecis
            _gameManager.ChangeState(GameState.MainMenu);
            _gameManager.ChangeState(GameState.Playing);

            // Assert: Iki event firlanmali
            Assert.AreEqual(2, recorder.Count);

            // Ilk event: Loading -> MainMenu
            Assert.AreEqual(GameState.Loading, recorder.ReceivedEvents[0].OldState);
            Assert.AreEqual(GameState.MainMenu, recorder.ReceivedEvents[0].NewState);

            // Ikinci event: MainMenu -> Playing
            Assert.AreEqual(GameState.MainMenu, recorder.ReceivedEvents[1].OldState);
            Assert.AreEqual(GameState.Playing, recorder.ReceivedEvents[1].NewState);
        }

        // -----------------------------------------------------------------
        // Tick Dongusu Testleri
        // -----------------------------------------------------------------

        [Test]
        public void Tick_InPlayingState_UpdatesPlayTime()
        {
            // Arrange
            _gameManager.ChangeState(GameState.Playing);

            // Act
            _gameManager.Tick(0.016f); // ~60fps
            _gameManager.Tick(0.016f);

            // Assert: PlayTime artmali
            GameAssert.AreApproximatelyEqual(0.032f, _gameManager.PlayTimeThisSession);
        }

        [Test]
        public void Tick_InPlayingState_TicksTimeManager()
        {
            // Arrange
            _gameManager.ChangeState(GameState.Playing);

            // Act
            _gameManager.Tick(1.0f);

            // Assert: TimeManager tick almali
            Assert.AreEqual(1, _timeManager.TickCallCount);
        }

        [Test]
        public void Tick_InPlayingState_PublishesGameTickEvent()
        {
            // Arrange
            _gameManager.ChangeState(GameState.Playing);
            var recorder = new EventRecorder<GameTickEvent>();
            recorder.SubscribeTo(_eventManager);

            // Act
            _gameManager.Tick(0.5f);

            // Assert
            Assert.AreEqual(1, recorder.Count);
            GameAssert.AreApproximatelyEqual(0.5f, recorder.Last.DeltaTime);
        }

        [Test]
        public void Tick_NotInPlayingState_DoesNotUpdatePlayTime()
        {
            // Arrange: Paused state
            _gameManager.ChangeState(GameState.Paused);

            // Act
            _gameManager.Tick(1.0f);

            // Assert: PlayTime degismemeli
            GameAssert.AreApproximatelyEqual(0f, _gameManager.PlayTimeThisSession);
        }

        [Test]
        public void Tick_NotInPlayingState_DoesNotTickTimeManager()
        {
            // Arrange: MainMenu state
            _gameManager.ChangeState(GameState.MainMenu);

            // Act
            _gameManager.Tick(1.0f);

            // Assert: TimeManager tick almamali
            Assert.AreEqual(0, _timeManager.TickCallCount);
        }

        [Test]
        public void Tick_NotInPlayingState_DoesNotPublishGameTickEvent()
        {
            // Arrange
            _gameManager.ChangeState(GameState.Paused);
            var recorder = new EventRecorder<GameTickEvent>();
            recorder.SubscribeTo(_eventManager);

            // Act
            _gameManager.Tick(1.0f);

            // Assert: GameTickEvent firlanmamali
            Assert.AreEqual(0, recorder.Count);
        }

        [Test]
        public void Tick_AlwaysTicksSaveManager()
        {
            // Arrange: Paused state bile olsa SaveManager tick almali
            _gameManager.ChangeState(GameState.Paused);

            // Act
            _gameManager.Tick(1.0f);
            _gameManager.Tick(1.0f);

            // Assert: SaveManager her durumda tick alir (auto-save icin)
            GameAssert.AreApproximatelyEqual(2.0f, _saveManager.TotalTickTime);
        }

        // -----------------------------------------------------------------
        // Uygulama Yasam Dongusu Testleri
        // -----------------------------------------------------------------

        [Test]
        public void OnApplicationPause_True_RecordsPauseTimeAndSaves()
        {
            // Act: Arka plana al
            _gameManager.OnApplicationPause(true);

            // Assert: Pause zamani kaydedilmeli ve kayit yapilmali
            Assert.AreEqual(1, _timeManager.RecordPauseCallCount,
                "RecordPauseTime cagirilmali");
            Assert.AreEqual(1, _saveManager.SaveAsyncCallCount,
                "SaveAsync cagirilmali");
        }

        [Test]
        public void OnApplicationPause_False_PublishesAppResumedEvent()
        {
            // Arrange
            _timeManager.MockPauseDuration = TimeSpan.FromMinutes(30);
            var recorder = new EventRecorder<AppResumedEvent>();
            recorder.SubscribeTo(_eventManager);

            // Act: On plana gel
            _gameManager.OnApplicationPause(false);

            // Assert: AppResumedEvent firlanmali
            Assert.AreEqual(1, recorder.Count);
            Assert.AreEqual(TimeSpan.FromMinutes(30), recorder.Last.OfflineDuration);
        }

        [Test]
        public void OnApplicationQuit_RecordsPauseTimeAndSavesLocal()
        {
            // Act
            _gameManager.OnApplicationQuit();

            // Assert
            Assert.AreEqual(1, _timeManager.RecordPauseCallCount,
                "RecordPauseTime cagirilmali");
            Assert.AreEqual(1, _saveManager.SaveLocalCallCount,
                "SaveLocal cagirilmali (senkron kayit)");
        }

        // -----------------------------------------------------------------
        // Constructor Dogrulama
        // -----------------------------------------------------------------

        [Test]
        public void Constructor_NullSaveManager_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                new GameManager(null, _eventManager, _timeManager);
            });
        }

        [Test]
        public void Constructor_NullEventManager_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                new GameManager(_saveManager, null, _timeManager);
            });
        }

        [Test]
        public void Constructor_NullTimeManager_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                new GameManager(_saveManager, _eventManager, null);
            });
        }

        // -----------------------------------------------------------------
        // PlayerData Erisim Testi
        // -----------------------------------------------------------------

        [Test]
        public void PlayerData_ReturnsSaveManagerData()
        {
            // Arrange
            _saveManager.Data.Coins = 12345;

            // Assert: PlayerData, SaveManager.Data'yi dondurur
            Assert.AreEqual(12345, _gameManager.PlayerData.Coins);
        }
    }
}
