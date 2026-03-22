// =============================================================================
// BootstrapTests.cs
// Boot sahnesinin entegrasyon testleri.
// GameBootstrapper'in tum servisleri dogru sirada kaydedip kaydetmedigini,
// GameManager baslangic durumunu ve event sisteminin calistigini dogrular.
// =============================================================================

using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using RiceFactory.Core;
using RiceFactory.Core.Events;
using RiceFactory.Data.Save;
using RiceFactory.Economy;
using RiceFactory.Production;

namespace RiceFactory.Tests.PlayMode
{
    /// <summary>
    /// GameBootstrapper entegrasyon testleri.
    /// Boot sahnesini yukleyerek tum servislerin dogru kayit edildigini dogrular.
    /// </summary>
    [TestFixture]
    public class BootstrapTests
    {
        // =====================================================================
        // Kurulum ve Temizlik
        // =====================================================================

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // Her testten once temiz bir durumda basla
            PlayModeTestHelper.CleanupScene();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            // Test sonrasi temizlik
            PlayModeTestHelper.CleanupScene();
            yield return null;
        }

        // =====================================================================
        // Testler
        // =====================================================================

        /// <summary>
        /// Boot sahnesini yukle, GameBootstrapper calissin.
        /// ServiceLocator'da tum kritik servislerin kayitli oldugunu dogrula:
        /// IEventManager, ISaveManager, IGameManager, IEconomySystem,
        /// PriceCalculator, ProductionManager, IPrestigeSystem vb.
        /// </summary>
        [UnityTest]
        public IEnumerator Bootstrap_AllServicesRegistered()
        {
            // Boot sahnesini yukle
            yield return PlayModeTestHelper.LoadSceneAndWait(SceneController.SCENE_BOOT);

            // GameBootstrapper async boot yaptiginden kisa sure bekle
            yield return PlayModeTestHelper.WaitForCondition(
                () => ServiceLocator.IsRegistered<IGameManager>(),
                timeout: 10f
            );

            // Kritik servislerin kayitli oldugunu dogrula
            Assert.IsTrue(ServiceLocator.IsRegistered<IEventManager>(),
                "IEventManager ServiceLocator'da kayitli olmali.");

            Assert.IsTrue(ServiceLocator.IsRegistered<ISaveManager>(),
                "ISaveManager ServiceLocator'da kayitli olmali.");

            Assert.IsTrue(ServiceLocator.IsRegistered<IGameManager>(),
                "IGameManager ServiceLocator'da kayitli olmali.");

            Assert.IsTrue(ServiceLocator.IsRegistered<IEconomySystem>(),
                "IEconomySystem (CurrencySystem) ServiceLocator'da kayitli olmali.");

            Assert.IsTrue(ServiceLocator.IsRegistered<PriceCalculator>(),
                "PriceCalculator ServiceLocator'da kayitli olmali.");

            Assert.IsTrue(ServiceLocator.IsRegistered<ProductionManager>(),
                "ProductionManager ServiceLocator'da kayitli olmali.");

            Assert.IsTrue(ServiceLocator.IsRegistered<IPrestigeSystem>(),
                "IPrestigeSystem ServiceLocator'da kayitli olmali.");

            Assert.IsTrue(ServiceLocator.IsRegistered<IUpgradeSystem>(),
                "IUpgradeSystem ServiceLocator'da kayitli olmali.");

            Assert.IsTrue(ServiceLocator.IsRegistered<ITimeManager>(),
                "ITimeManager ServiceLocator'da kayitli olmali.");

            Assert.IsTrue(ServiceLocator.IsRegistered<IBalanceConfig>(),
                "IBalanceConfig ServiceLocator'da kayitli olmali.");
        }

        /// <summary>
        /// GameManager baslangiçta Loading veya Playing state'inde olmali.
        /// Boot tamamlandiktan sonra MainMenu veya Playing durumuna gecilmis olmali.
        /// </summary>
        [UnityTest]
        public IEnumerator Bootstrap_GameManagerInitialState()
        {
            // Boot sahnesini yukle
            yield return PlayModeTestHelper.LoadSceneAndWait(SceneController.SCENE_BOOT);

            // Boot tamamlanmasini bekle
            yield return PlayModeTestHelper.WaitForCondition(
                () => ServiceLocator.IsRegistered<IGameManager>(),
                timeout: 10f
            );

            // GameManager'i al
            Assert.IsTrue(ServiceLocator.TryGet<IGameManager>(out var gameManager),
                "IGameManager ServiceLocator'dan alinabilmeli.");

            // Boot tamamlandiktan sonra state, Loading, Playing veya MainMenu olmali
            var currentState = gameManager.CurrentState;
            bool validState = currentState == GameState.Loading
                           || currentState == GameState.Playing
                           || currentState == GameState.MainMenu;

            Assert.IsTrue(validState,
                $"GameManager boot sonrasi Loading, Playing veya MainMenu durumunda olmali. Mevcut: {currentState}");
        }

        /// <summary>
        /// SaveManager.Data null olmamali — boot sirasinda yuklenmis olmali.
        /// </summary>
        [UnityTest]
        public IEnumerator Bootstrap_SaveManagerHasData()
        {
            // Boot sahnesini yukle
            yield return PlayModeTestHelper.LoadSceneAndWait(SceneController.SCENE_BOOT);

            // Boot tamamlanmasini bekle
            yield return PlayModeTestHelper.WaitForCondition(
                () => ServiceLocator.IsRegistered<ISaveManager>(),
                timeout: 10f
            );

            // SaveManager'i al ve Data'nin null olmadigini kontrol et
            Assert.IsTrue(ServiceLocator.TryGet<ISaveManager>(out var saveManager),
                "ISaveManager ServiceLocator'dan alinabilmeli.");

            Assert.IsNotNull(saveManager.Data,
                "SaveManager.Data boot sonrasi null olmamali — LoadAsync cagrilmis olmali.");
        }

        /// <summary>
        /// EventManager publish/subscribe mekanizmasinin calistigini dogrula.
        /// Bir event subscribe et, publish et, callback'in cagirildigini kontrol et.
        /// </summary>
        [UnityTest]
        public IEnumerator Bootstrap_EventManagerFunctional()
        {
            // Boot sahnesini yukle
            yield return PlayModeTestHelper.LoadSceneAndWait(SceneController.SCENE_BOOT);

            // Boot tamamlanmasini bekle
            yield return PlayModeTestHelper.WaitForCondition(
                () => ServiceLocator.IsRegistered<IEventManager>(),
                timeout: 10f
            );

            // EventManager'i al
            Assert.IsTrue(ServiceLocator.TryGet<IEventManager>(out var eventManager),
                "IEventManager ServiceLocator'dan alinabilmeli.");

            // Test: Subscribe -> Publish -> callback cagirilmali
            bool eventReceived = false;
            GameState receivedNewState = GameState.Loading;

            eventManager.Subscribe<GameStateChangedEvent>(e =>
            {
                eventReceived = true;
                receivedNewState = e.NewState;
            });

            // Test eventi yayinla
            eventManager.Publish(new GameStateChangedEvent(GameState.Loading, GameState.Playing));

            // 1 frame bekle
            yield return null;

            // Sonuclari dogrula
            Assert.IsTrue(eventReceived,
                "Subscribe edilen event, Publish sonrasi callback'i tetiklemeli.");

            Assert.AreEqual(GameState.Playing, receivedNewState,
                "Publish edilen event'teki NewState dogru olmali.");
        }

        /// <summary>
        /// Boot sonrasi MainMenu veya Game sahnesine gecis yapilmali.
        /// GameBootstrapper, save durumuna gore sahne gecisi yapar.
        /// </summary>
        [UnityTest]
        public IEnumerator Bootstrap_TransitionsToCorrectScene()
        {
            // Boot sahnesini yukle
            yield return PlayModeTestHelper.LoadSceneAndWait(SceneController.SCENE_BOOT);

            // Boot + sahne gecisi icin bekle
            // GameBootstrapper async calistigindan ve sahne gecisi de async oldugundan
            // yeterli sure beklememiz gerekiyor
            yield return PlayModeTestHelper.WaitForCondition(
                () =>
                {
                    string currentScene = SceneManager.GetActiveScene().name;
                    return currentScene == SceneController.SCENE_MAIN_MENU
                        || currentScene == SceneController.SCENE_GAME;
                },
                timeout: 15f
            );

            // Aktif sahnenin MainMenu veya Game oldugunu dogrula
            string activeScene = SceneManager.GetActiveScene().name;
            bool validTransition = activeScene == SceneController.SCENE_MAIN_MENU
                                || activeScene == SceneController.SCENE_GAME;

            Assert.IsTrue(validTransition,
                $"Boot sonrasi MainMenu veya Game sahnesine gecilmeli. Aktif sahne: {activeScene}");
        }
    }
}
