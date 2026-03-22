// =============================================================================
// SceneTransitionTests.cs
// Sahne gecis testleri.
// Boot -> MainMenu, MainMenu -> Game gecislerinin basarili oldugunu,
// Build Settings'teki sahne sirasinin dogru oldugunu ve servis surekliligini dogrular.
// =============================================================================

using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using RiceFactory.Core;

namespace RiceFactory.Tests.PlayMode
{
    /// <summary>
    /// Sahne gecis testleri.
    /// Sahneler arasi navigasyonun dogru calistigini ve
    /// ServiceLocator servislerinin gecis sirasinda korunup korunmadigini dogrular.
    /// </summary>
    [TestFixture]
    public class SceneTransitionTests
    {
        // =====================================================================
        // Kurulum ve Temizlik
        // =====================================================================

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            PlayModeTestHelper.CleanupScene();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            PlayModeTestHelper.CleanupScene();
            yield return null;
        }

        // =====================================================================
        // Testler
        // =====================================================================

        /// <summary>
        /// Boot sahnesinden MainMenu sahnesine gecis testi.
        /// Boot tamamlandiktan sonra (save yoksa) MainMenu sahnesine gecilmeli.
        /// </summary>
        [UnityTest]
        public IEnumerator SceneTransition_BootToMainMenu()
        {
            // Boot sahnesini yukle
            yield return PlayModeTestHelper.LoadSceneAndWait(SceneController.SCENE_BOOT);

            // Boot + sahne gecisi icin yeterli sure bekle
            // Boot async ve sahne gecisi de async — en fazla 15s timeout
            yield return PlayModeTestHelper.WaitForCondition(
                () =>
                {
                    string current = SceneManager.GetActiveScene().name;
                    return current == SceneController.SCENE_MAIN_MENU
                        || current == SceneController.SCENE_GAME;
                },
                timeout: 15f
            );

            string activeScene = SceneManager.GetActiveScene().name;

            // Boot sahnesinden ayrilmis olmali
            Assert.AreNotEqual(SceneController.SCENE_BOOT, activeScene,
                "Boot sonrasi hala Boot sahnesinde kalmamali.");

            // MainMenu veya Game sahnesinde olmali (save durumuna bagli)
            bool validScene = activeScene == SceneController.SCENE_MAIN_MENU
                           || activeScene == SceneController.SCENE_GAME;

            Assert.IsTrue(validScene,
                $"Boot sonrasi MainMenu veya Game sahnesine gecilmeli. Aktif: {activeScene}");
        }

        /// <summary>
        /// MainMenu'den Game sahnesine gecis testi.
        /// Oyna butonuna basildiginda Game sahnesine gecilmeli.
        /// Not: Bu test servislerin onceden kayitli oldugunu varsayar (Boot yapilmis).
        /// </summary>
        [UnityTest]
        public IEnumerator SceneTransition_MainMenuToGame()
        {
            // Once Boot sahnesini yukle — servislerin kayit olmasi icin
            yield return PlayModeTestHelper.LoadSceneAndWait(SceneController.SCENE_BOOT);

            // Boot tamamlanmasini ve MainMenu/Game sahnesine gecisi bekle
            yield return PlayModeTestHelper.WaitForCondition(
                () => SceneManager.GetActiveScene().name != SceneController.SCENE_BOOT,
                timeout: 15f
            );

            // Eger zaten Game sahnesindeyse (save varsa), test basarili
            if (SceneManager.GetActiveScene().name == SceneController.SCENE_GAME)
            {
                Assert.Pass("Boot dogrudan Game sahnesine gecti (mevcut save).");
                yield break;
            }

            // MainMenu sahnesinde olmali
            Assert.AreEqual(SceneController.SCENE_MAIN_MENU, SceneManager.GetActiveScene().name,
                "Bu noktada MainMenu sahnesinde olmaliyiz.");

            // MainMenuController'daki Oyna butonunu bul
            // MainMenuController'in Start()'inda butonlar baglanir — 1 frame bekle
            yield return null;

            var mainMenuController = PlayModeTestHelper.FindComponentInScene<RiceFactory.UI.MainMenuController>();
            Assert.IsNotNull(mainMenuController,
                "MainMenu sahnesinde MainMenuController bulunmali.");

            // SceneController uzerinden Game sahnesine gec
            // (Buton tiklamasi yerine direkt API kullanarak test ediyoruz —
            //  buton referanslari private oldugundan)
            SceneController.LoadSceneWithLoading(SceneController.SCENE_GAME);

            // Game sahnesine gecisi bekle
            yield return PlayModeTestHelper.WaitForCondition(
                () => SceneManager.GetActiveScene().name == SceneController.SCENE_GAME,
                timeout: 10f
            );

            Assert.AreEqual(SceneController.SCENE_GAME, SceneManager.GetActiveScene().name,
                "MainMenu'den Game sahnesine gecis basarili olmali.");
        }

        /// <summary>
        /// Build Settings'teki 3 sahnenin dogru sirada oldugunu dogrula.
        /// Sira: Boot (0), MainMenu (1), Game (2).
        /// </summary>
        [UnityTest]
        public IEnumerator SceneTransition_SceneNamesValid()
        {
            yield return null; // En az 1 frame bekle (UnityTest zorunlulugu)

            // Build Settings'teki sahne sayisini kontrol et
            int sceneCount = SceneManager.sceneCountInBuildSettings;
            Assert.GreaterOrEqual(sceneCount, 3,
                $"Build Settings'te en az 3 sahne olmali. Mevcut: {sceneCount}");

            // Sahne isimlerini kontrol et (Build Index sirasi)
            string scene0 = System.IO.Path.GetFileNameWithoutExtension(
                SceneUtility.GetScenePathByBuildIndex(0));
            string scene1 = System.IO.Path.GetFileNameWithoutExtension(
                SceneUtility.GetScenePathByBuildIndex(1));
            string scene2 = System.IO.Path.GetFileNameWithoutExtension(
                SceneUtility.GetScenePathByBuildIndex(2));

            Assert.AreEqual(SceneController.SCENE_BOOT, scene0,
                $"Build index 0: Boot olmali. Gelen: {scene0}");

            Assert.AreEqual(SceneController.SCENE_MAIN_MENU, scene1,
                $"Build index 1: MainMenu olmali. Gelen: {scene1}");

            Assert.AreEqual(SceneController.SCENE_GAME, scene2,
                $"Build index 2: Game olmali. Gelen: {scene2}");
        }

        /// <summary>
        /// Sahne gecisinde ServiceLocator servisleri korunuyor mu?
        /// Boot -> MainMenu gecisinden sonra IGameManager hala eriselebilir olmali.
        /// DontDestroyOnLoad ile servisler sahne gecisinde kalici kalir.
        /// </summary>
        [UnityTest]
        public IEnumerator SceneTransition_ServicesPreservedAfterTransition()
        {
            // Boot sahnesini yukle
            yield return PlayModeTestHelper.LoadSceneAndWait(SceneController.SCENE_BOOT);

            // Boot tamamlanmasini bekle
            yield return PlayModeTestHelper.WaitForCondition(
                () => ServiceLocator.IsRegistered<IGameManager>(),
                timeout: 10f
            );

            // Servislerin kayitli oldugunu dogrula
            Assert.IsTrue(ServiceLocator.IsRegistered<IGameManager>(),
                "Boot sonrasi IGameManager kayitli olmali.");
            Assert.IsTrue(ServiceLocator.IsRegistered<IEventManager>(),
                "Boot sonrasi IEventManager kayitli olmali.");

            // Sahne gecisini bekle
            yield return PlayModeTestHelper.WaitForCondition(
                () => SceneManager.GetActiveScene().name != SceneController.SCENE_BOOT,
                timeout: 15f
            );

            // 2 frame daha bekle — sahne tamamen yuklenmis olsun
            yield return null;
            yield return null;

            // Sahne gecisi sonrasi servisler hala kayitli mi?
            Assert.IsTrue(ServiceLocator.IsRegistered<IGameManager>(),
                "Sahne gecisi sonrasi IGameManager hala kayitli olmali (DontDestroyOnLoad).");

            Assert.IsTrue(ServiceLocator.IsRegistered<IEventManager>(),
                "Sahne gecisi sonrasi IEventManager hala kayitli olmali.");

            Assert.IsTrue(ServiceLocator.IsRegistered<ISaveManager>(),
                "Sahne gecisi sonrasi ISaveManager hala kayitli olmali.");

            Assert.IsTrue(ServiceLocator.IsRegistered<IEconomySystem>(),
                "Sahne gecisi sonrasi IEconomySystem hala kayitli olmali.");

            // Servis instance'larinin null olmadigini da kontrol et
            Assert.IsTrue(ServiceLocator.TryGet<IGameManager>(out var gm) && gm != null,
                "IGameManager instance null olmamali.");

            Assert.IsTrue(ServiceLocator.TryGet<IEventManager>(out var em) && em != null,
                "IEventManager instance null olmamali.");
        }
    }
}
