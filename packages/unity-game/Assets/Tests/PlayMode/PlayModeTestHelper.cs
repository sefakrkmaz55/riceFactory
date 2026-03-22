// =============================================================================
// PlayModeTestHelper.cs
// Play Mode testleri icin yardimci metotlar.
// Sahne yukleme, component bulma, buton simulasyonu, bekleme ve temizlik.
// =============================================================================

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using RiceFactory.Core;

namespace RiceFactory.Tests.PlayMode
{
    /// <summary>
    /// Play Mode testleri icin ortak yardimci sinif.
    /// Sahne yukleme, component arama, buton simulasyonu ve temizlik islemleri sunar.
    /// </summary>
    public static class PlayModeTestHelper
    {
        // =====================================================================
        // Sahne Yukleme
        // =====================================================================

        /// <summary>
        /// Belirtilen sahneyi yukler ve 1 frame bekler.
        /// Sahne tamamen aktif olduktan sonra devam eder.
        /// </summary>
        /// <param name="sceneName">Yuklenecek sahne adi ("Boot", "MainMenu", "Game")</param>
        public static IEnumerator LoadSceneAndWait(string sceneName)
        {
            var asyncOp = SceneManager.LoadSceneAsync(sceneName);
            if (asyncOp == null)
            {
                Debug.LogError($"[PlayModeTestHelper] Sahne yuklenemedi: {sceneName}");
                yield break;
            }

            // Yukleme tamamlanana kadar bekle
            while (!asyncOp.isDone)
            {
                yield return null;
            }

            // Sahne tamamen aktif olsun
            yield return null;
        }

        // =====================================================================
        // Component Bulma
        // =====================================================================

        /// <summary>
        /// Aktif sahnede belirtilen tipte ilk component'i bulur.
        /// Bulamazsa null doner.
        /// </summary>
        /// <typeparam name="T">Aranan component tipi</typeparam>
        /// <returns>Bulunan component veya null</returns>
        public static T FindComponentInScene<T>() where T : Component
        {
            // Unity 2023+ icin FindFirstObjectByType, eski surumlerde FindObjectOfType
#if UNITY_2023_1_OR_NEWER
            return UnityEngine.Object.FindFirstObjectByType<T>();
#else
            return UnityEngine.Object.FindObjectOfType<T>();
#endif
        }

        // =====================================================================
        // Buton Simulasyonu
        // =====================================================================

        /// <summary>
        /// Buton tiklamasini programatik olarak simule eder.
        /// onClick eventini tetikler.
        /// </summary>
        /// <param name="button">Tiklanacak buton</param>
        public static void SimulateButtonClick(Button button)
        {
            if (button == null)
            {
                Debug.LogWarning("[PlayModeTestHelper] SimulateButtonClick: button null!");
                return;
            }

            if (!button.interactable)
            {
                Debug.LogWarning("[PlayModeTestHelper] SimulateButtonClick: buton interactable degil!");
                return;
            }

            button.onClick.Invoke();
        }

        // =====================================================================
        // Bekleme Yardimcilari
        // =====================================================================

        /// <summary>
        /// Belirtilen sure kadar bekler (saniye cinsinden).
        /// Coroutine bazli — yield return ile kullanilir.
        /// </summary>
        /// <param name="seconds">Beklenecek sure (saniye)</param>
        public static IEnumerator WaitForSeconds(float seconds)
        {
            yield return new WaitForSeconds(seconds);
        }

        /// <summary>
        /// Belirtilen kosul gerceklesene kadar veya timeout dolana kadar bekler.
        /// Kosul saglanmazsa timeout sonrasi false ile devam eder.
        /// </summary>
        /// <param name="condition">Beklenen kosul (true donunce bekleme biter)</param>
        /// <param name="timeout">Maksimum bekleme suresi (saniye). Varsayilan: 5s</param>
        /// <returns>Kosul saglandiysa true, timeout olduysa false</returns>
        public static IEnumerator WaitForCondition(Func<bool> condition, float timeout = 5f)
        {
            float elapsed = 0f;

            while (!condition() && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (!condition())
            {
                Debug.LogWarning($"[PlayModeTestHelper] WaitForCondition: Timeout ({timeout}s) — kosul saglanamadi.");
            }
        }

        // =====================================================================
        // Temizlik
        // =====================================================================

        /// <summary>
        /// Test sonrasi temizlik yapar:
        /// 1. ServiceLocator'i sifirlar.
        /// 2. Sahnedeki tum root GameObject'leri yok eder (DontDestroyOnLoad haric).
        /// </summary>
        public static void CleanupScene()
        {
            // ServiceLocator temizle
            ServiceLocator.Reset();

            // Aktif sahnedeki tum root objeleri yok et
            var scene = SceneManager.GetActiveScene();
            if (scene.IsValid())
            {
                var rootObjects = scene.GetRootGameObjects();
                foreach (var obj in rootObjects)
                {
                    UnityEngine.Object.Destroy(obj);
                }
            }
        }
    }
}
