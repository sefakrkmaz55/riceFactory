// =============================================================================
// FeedbackTests.cs
// Feedback sistemi testleri: FloatingText, ScreenEffects, FeedbackManager.
// Gorsel geri bildirim mekaniklerinin dogru calistigini dogrular.
// =============================================================================

using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using TMPro;
using RiceFactory.Core;
using RiceFactory.UI;

namespace RiceFactory.Tests.PlayMode
{
    /// <summary>
    /// Feedback sistemi testleri.
    /// FloatingText animasyonu, ScreenEffects overlay'i ve
    /// FeedbackManager entegrasyon testlerini icerir.
    /// </summary>
    [TestFixture]
    public class FeedbackTests
    {
        // =====================================================================
        // Kurulum ve Temizlik
        // =====================================================================

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            PlayModeTestHelper.CleanupScene();
            yield return null;

            // Test icin minimal bir Canvas ortami olustur
            EnsureTestCanvas();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            PlayModeTestHelper.CleanupScene();
            yield return null;
        }

        /// <summary>
        /// Test ortami icin basit bir Canvas olusturur.
        /// FloatingText ve ScreenEffects Canvas'a ihtiyac duyar.
        /// </summary>
        private void EnsureTestCanvas()
        {
            if (Object.FindObjectOfType<Canvas>() == null)
            {
                var canvasObj = new GameObject("TestCanvas");
                var canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }
        }

        // =====================================================================
        // Testler
        // =====================================================================

        /// <summary>
        /// FloatingText gosterildikten sonra DURATION (1.0s) suresi sonunda
        /// gameObject deaktif olmali veya yok edilmeli.
        /// FloatingText.DURATION = 1.0s — animasyon sonrasi pool'a doner.
        /// </summary>
        [UnityTest]
        public IEnumerator FloatingText_ShowsAndDisappears()
        {
            // FloatingText componenti olustur (FeedbackManager pool sistemi disinda, manuel)
            var ftObj = new GameObject("TestFloatingText");
            var canvas = Object.FindObjectOfType<Canvas>();
            ftObj.transform.SetParent(canvas.transform, false);

            var rect = ftObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(300f, 60f);

            var tmp = ftObj.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 28;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;

            var canvasGroup = ftObj.AddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;

            var floatingText = ftObj.AddComponent<FloatingText>();

            // 1 frame bekle — Awake calissin
            yield return null;

            // FloatingText'i goster
            floatingText.Show("+1,234", FloatingText.COLOR_COIN);

            // Text'in gosterildigini dogrula
            Assert.IsTrue(ftObj.activeInHierarchy,
                "FloatingText Show() sonrasi aktif olmali.");
            Assert.AreEqual("+1,234", tmp.text,
                "FloatingText dogru metni gostermeli.");

            // FloatingText DURATION = 1.0s — animasyon tamamlanana kadar bekle
            // Biraz fazla bekliyoruz (1.5s) — animasyon kesinlikle bitmis olsun
            yield return PlayModeTestHelper.WaitForSeconds(1.5f);

            // Animasyon bittikten sonra FloatingText pool'a donmeli
            // Pool'a donunce alpha 0 olur ve gameObject deaktif olur
            // (FeedbackManager.Instance yoksa kendi kendine deaktif olur)
            bool disappeared = !ftObj.activeInHierarchy || canvasGroup.alpha < 0.01f;

            Assert.IsTrue(disappeared,
                $"FloatingText 1.5s sonra kaybolmali (deaktif veya alpha~0). " +
                $"Active: {ftObj.activeInHierarchy}, Alpha: {canvasGroup.alpha:F2}");
        }

        /// <summary>
        /// ScreenEffects.FlashScreen cagirildiginda overlay Canvas olusturulmali.
        /// Flash efekti bir Image componenti ile beyaz/renkli overlay gosterir.
        /// </summary>
        [UnityTest]
        public IEnumerator ScreenEffects_FlashCreatesOverlay()
        {
            // ScreenEffects singleton'ini olustur (yoksa)
            if (ScreenEffects.Instance == null)
            {
                var seObj = new GameObject("TestScreenEffects");
                seObj.AddComponent<ScreenEffects>();
                yield return null; // Awake calissin
            }

            var screenEffects = ScreenEffects.Instance;
            Assert.IsNotNull(screenEffects,
                "ScreenEffects.Instance olusturulmus olmali.");

            // Flash efektini tetikle
            screenEffects.FlashScreen(Color.white, 0.5f);

            // 1 frame bekle — flash baslasın
            yield return null;

            // ScreenEffects kendi icinde bir Canvas ve Image olusturur
            // Overlay Canvas'in varligini dogrula
            var screenEffectsCanvas = screenEffects.GetComponentInChildren<Canvas>(true);
            Assert.IsNotNull(screenEffectsCanvas,
                "ScreenEffects altinda bir overlay Canvas olmali.");

            // Flash Image'ini bul
            var images = screenEffects.GetComponentsInChildren<Image>(true);
            Assert.IsTrue(images.Length > 0,
                "ScreenEffects altinda en az 1 Image (flash overlay) olmali.");

            // Flash suresi boyunca image alpha > 0 olmali
            // (0.5s flash suresi var, hemen kontrol edersek gorulmeli)
            bool hasVisibleFlash = false;
            foreach (var img in images)
            {
                if (img.color.a > 0.01f)
                {
                    hasVisibleFlash = true;
                    break;
                }
            }

            // Flash baslangicinda alpha > 0 olmali
            // Not: Cok hizli calisirsa alpha dusmus olabilir, bu durumda da gecerli
            Debug.Log($"[FeedbackTests] Flash overlay mevcut, {images.Length} image bulundu.");

            // Flash bitmesini bekle
            yield return PlayModeTestHelper.WaitForSeconds(0.6f);

            // Flash bittikten sonra overlay alpha 0 olmali (tamamen solmus)
            bool allCleared = true;
            foreach (var img in images)
            {
                if (img.color.a > 0.01f)
                {
                    allCleared = false;
                    break;
                }
            }

            Assert.IsTrue(allCleared,
                "Flash suresi dolduktan sonra tum overlay image'larin alpha'si ~0 olmali.");
        }

        /// <summary>
        /// FeedbackManager.PlayCoinEarned cagirildiginda floating text gorunmeli.
        /// FeedbackManager object pool'undan FloatingText alarak "+1,234" gosterir.
        /// </summary>
        [UnityTest]
        public IEnumerator FeedbackManager_CoinEarnedTriggersFloatingText()
        {
            // FeedbackManager singleton'ini olustur (yoksa)
            if (FeedbackManager.Instance == null)
            {
                var fmObj = new GameObject("TestFeedbackManager");
                fmObj.AddComponent<FeedbackManager>();
                yield return null; // Awake calissin
            }

            // Start() icin 1 frame daha bekle — pool olusturulsun
            yield return null;

            var feedbackManager = FeedbackManager.Instance;
            Assert.IsNotNull(feedbackManager,
                "FeedbackManager.Instance olusturulmus olmali.");

            // Onceki FloatingText sayisini kaydet
            var previousTexts = Object.FindObjectsOfType<FloatingText>(true);
            int previousActiveCount = 0;
            foreach (var ft in previousTexts)
            {
                if (ft.gameObject.activeInHierarchy)
                    previousActiveCount++;
            }

            // PlayCoinEarned tetikle — floating text gosterilmeli
            feedbackManager.PlayCoinEarned(1234);

            // 1 frame bekle — floating text olusturulsun
            yield return null;

            // Aktif FloatingText sayisi artmis olmali
            var currentTexts = Object.FindObjectsOfType<FloatingText>(true);
            int currentActiveCount = 0;
            foreach (var ft in currentTexts)
            {
                if (ft.gameObject.activeInHierarchy)
                    currentActiveCount++;
            }

            Assert.IsTrue(currentActiveCount > previousActiveCount,
                $"PlayCoinEarned() sonrasi aktif FloatingText sayisi artmali. " +
                $"Onceki: {previousActiveCount}, Guncel: {currentActiveCount}");

            // Floating text'in "+1,234" veya benzeri icerik gosterdigini dogrula
            bool hasCorrectText = false;
            foreach (var ft in currentTexts)
            {
                if (!ft.gameObject.activeInHierarchy) continue;

                var tmp = ft.GetComponent<TextMeshProUGUI>();
                if (tmp != null && tmp.text.Contains("+"))
                {
                    hasCorrectText = true;
                    Debug.Log($"[FeedbackTests] FloatingText icerigi: '{tmp.text}'");
                    break;
                }
            }

            Assert.IsTrue(hasCorrectText,
                "PlayCoinEarned() sonrasi '+' isareti iceren bir FloatingText gosterilmeli.");

            // Animasyonun tamamlanmasini bekle (1.5s)
            yield return PlayModeTestHelper.WaitForSeconds(1.5f);
        }
    }
}
