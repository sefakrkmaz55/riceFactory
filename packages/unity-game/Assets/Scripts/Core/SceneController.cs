// =============================================================================
// SceneController.cs
// Sahne gecis yonetimi: async yukleme, loading ekrani ve fade efektleri.
// Static sinif olarak tasarlanmistir — MonoBehaviour gerektirmez.
// SimpleTween ile fade efekti saglar.
//
// Sabit sahne isimleri: "Boot", "MainMenu", "Game"
// =============================================================================

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RiceFactory.Core
{
    /// <summary>
    /// Sahne gecislerini yoneten static sinif.
    /// Async sahne yukleme, loading ekrani ile yukleme ve fade gecis efektleri sunar.
    /// </summary>
    public static class SceneController
    {
        // =====================================================================
        // Sabit Sahne Isimleri
        // =====================================================================

        public const string SCENE_BOOT = "Boot";
        public const string SCENE_MAIN_MENU = "MainMenu";
        public const string SCENE_GAME = "Game";

        // =====================================================================
        // Dahili Durum
        // =====================================================================

        private static bool _isLoading;
        private static CanvasGroup _fadeOverlay;
        private static Canvas _fadeCanvas;

        /// <summary>Sahne yukleme devam ediyor mu?</summary>
        public static bool IsLoading => _isLoading;

        /// <summary>Aktif sahnenin adini dondurur.</summary>
        public static string CurrentSceneName => SceneManager.GetActiveScene().name;

        // =====================================================================
        // Basit Sahne Yukleme (fade ile)
        // =====================================================================

        /// <summary>
        /// Belirtilen sahneyi async olarak yukler. Kisa fade gecis efekti uygulanir.
        /// Eger zaten yukleme yapiliyorsa islem atlanir.
        /// </summary>
        /// <param name="sceneName">Yuklenecek sahne adi ("Boot", "MainMenu", "Game")</param>
        public static void LoadScene(string sceneName)
        {
            if (_isLoading)
            {
                Debug.LogWarning($"[SceneController] Zaten yukleme yapiliyor, '{sceneName}' atlanıyor.");
                return;
            }

            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("[SceneController] LoadScene: Bos sahne ismi!");
                return;
            }

            Debug.Log($"[SceneController] Sahne yukleniyor: {sceneName}");
            _isLoading = true;

            EnsureFadeOverlay();
            RunCoroutine(LoadSceneRoutine(sceneName, useFade: true, showLoadingProgress: false));
        }

        // =====================================================================
        // Loading Ekranli Sahne Yukleme
        // =====================================================================

        /// <summary>
        /// Belirtilen sahneyi loading ekrani (ilerleme gosterimi) ile yukler.
        /// Agir sahneler (Game sahnesi gibi) icin idealdir.
        /// </summary>
        /// <param name="sceneName">Yuklenecek sahne adi</param>
        public static void LoadSceneWithLoading(string sceneName)
        {
            if (_isLoading)
            {
                Debug.LogWarning($"[SceneController] Zaten yukleme yapiliyor, '{sceneName}' atlanıyor.");
                return;
            }

            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("[SceneController] LoadSceneWithLoading: Bos sahne ismi!");
                return;
            }

            Debug.Log($"[SceneController] Sahne yukleneniyor (loading ekranli): {sceneName}");
            _isLoading = true;

            EnsureFadeOverlay();
            RunCoroutine(LoadSceneRoutine(sceneName, useFade: true, showLoadingProgress: true));
        }

        // =====================================================================
        // Yukleme Coroutine'i
        // =====================================================================

        private static IEnumerator LoadSceneRoutine(string sceneName, bool useFade, bool showLoadingProgress)
        {
            float fadeDuration = 0.3f;

            // ---- Fade Out (ekrani karart) ----
            if (useFade && _fadeOverlay != null)
            {
                _fadeCanvas.gameObject.SetActive(true);
                _fadeOverlay.alpha = 0f;
                _fadeOverlay.blocksRaycasts = true;

                SimpleTween.DOFade(_fadeOverlay, 1f, fadeDuration, SimpleTween.Ease.InQuad);
                yield return new WaitForSecondsRealtime(fadeDuration);
            }

            // ---- Async sahne yukleme ----
            var asyncOp = SceneManager.LoadSceneAsync(sceneName);
            if (asyncOp == null)
            {
                Debug.LogError($"[SceneController] Sahne yuklenemedi: {sceneName}. Build Settings'te oldugundan emin olun.");
                _isLoading = false;
                if (_fadeOverlay != null)
                {
                    _fadeOverlay.alpha = 0f;
                    _fadeOverlay.blocksRaycasts = false;
                    _fadeCanvas.gameObject.SetActive(false);
                }
                yield break;
            }

            asyncOp.allowSceneActivation = false;

            // Yukleme ilerlemesini bekle
            while (asyncOp.progress < 0.9f)
            {
                if (showLoadingProgress)
                {
                    float progress = Mathf.Clamp01(asyncOp.progress / 0.9f);
                    Debug.Log($"[SceneController] Yukleme ilerlemesi: {progress:P0}");
                }
                yield return null;
            }

            // Sahneyi aktif et
            asyncOp.allowSceneActivation = true;

            // Bir frame bekle — sahne tamamen aktif olsun
            yield return null;

            // ---- Fade In (ekrani ac) ----
            if (useFade && _fadeOverlay != null)
            {
                SimpleTween.DOFade(_fadeOverlay, 0f, fadeDuration, SimpleTween.Ease.OutQuad,
                    onComplete: () =>
                    {
                        _fadeOverlay.blocksRaycasts = false;
                        _fadeCanvas.gameObject.SetActive(false);
                    });

                yield return new WaitForSecondsRealtime(fadeDuration);
            }

            _isLoading = false;
            Debug.Log($"[SceneController] Sahne yuklendi: {sceneName}");
        }

        // =====================================================================
        // Fade Overlay Yonetimi
        // =====================================================================

        /// <summary>
        /// Fade efekti icin CanvasGroup overlay'ini olusturur (yoksa).
        /// Siyah ekran overlay — DontDestroyOnLoad ile kalici.
        /// </summary>
        private static void EnsureFadeOverlay()
        {
            if (_fadeCanvas != null) return;

            // Canvas olustur
            var canvasObj = new GameObject("[SceneController.FadeOverlay]");
            canvasObj.hideFlags = HideFlags.HideAndDontSave;
            UnityEngine.Object.DontDestroyOnLoad(canvasObj);

            _fadeCanvas = canvasObj.AddComponent<Canvas>();
            _fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _fadeCanvas.sortingOrder = 9999; // Her seyin ustunde

            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            // Siyah arka plan image
            var imageObj = new GameObject("FadeImage");
            imageObj.transform.SetParent(canvasObj.transform, false);

            var rectTransform = imageObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            var image = imageObj.AddComponent<UnityEngine.UI.Image>();
            image.color = Color.black;
            image.raycastTarget = true;

            // CanvasGroup
            _fadeOverlay = canvasObj.AddComponent<CanvasGroup>();
            _fadeOverlay.alpha = 0f;
            _fadeOverlay.blocksRaycasts = false;

            canvasObj.SetActive(false);
        }

        // =====================================================================
        // Coroutine Runner (MonoBehaviour-bagimsiz)
        // =====================================================================

        private static CoroutineHost _coroutineHost;

        /// <summary>
        /// Static sinifin coroutine calistirabilmesi icin gizli MonoBehaviour kullanir.
        /// </summary>
        private static void RunCoroutine(IEnumerator routine)
        {
            if (_coroutineHost == null)
            {
                var hostObj = new GameObject("[SceneController.CoroutineHost]");
                hostObj.hideFlags = HideFlags.HideAndDontSave;
                UnityEngine.Object.DontDestroyOnLoad(hostObj);
                _coroutineHost = hostObj.AddComponent<CoroutineHost>();
            }

            _coroutineHost.StartCoroutine(routine);
        }

        /// <summary>Coroutine host MonoBehaviour — gizli ve kalici.</summary>
        private class CoroutineHost : MonoBehaviour { }
    }
}
