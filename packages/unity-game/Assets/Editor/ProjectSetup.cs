// =============================================================================
// ProjectSetup.cs
// Unity Editor batch mode ile sahneleri, prefab'lari ve Build Settings'i
// programatik olarak olusturan editor scripti.
//
// Kullanim:
//   MenuItem: RiceFactory > Setup Project
//   Batch mode: -executeMethod RiceFactory.Editor.ProjectSetup.SetupFromCommandLine
// =============================================================================

#if UNITY_EDITOR

using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

// Runtime script namespace'leri
using RiceFactory.Core;
using RiceFactory.UI;

namespace RiceFactory.Editor
{
    public static class ProjectSetup
    {
        // =====================================================================
        // Sabit Degerler
        // =====================================================================

        private const string TAG = "[ProjectSetup]";

        // Sahne yollari
        private const string SCENE_BOOT = "Assets/Scenes/Boot.unity";
        private const string SCENE_MAIN_MENU = "Assets/Scenes/MainMenu.unity";
        private const string SCENE_GAME = "Assets/Scenes/Game.unity";

        // Prefab yollari
        private const string PREFAB_FACTORY_CARD = "Assets/Prefabs/FactoryCard.prefab";

        // Sprite yollari
        private const string SPRITE_DIR = "Assets/Sprites";

        // Canvas referans cozunurlugu (portrait mobile)
        private const float REF_WIDTH = 1080f;
        private const float REF_HEIGHT = 1920f;

        // =====================================================================
        // Giris Noktalari
        // =====================================================================

        [MenuItem("RiceFactory/Setup Project")]
        public static void SetupProject()
        {
            Debug.Log($"{TAG} Proje kurulumu basliyor...");

            // F. Placeholder sprite'lari olustur (once, sahnelerde kullanilabilir)
            CreatePlaceholderSprites();

            // A. Boot sahnesi
            CreateBootScene();

            // B. MainMenu sahnesi
            CreateMainMenuScene();

            // D. FactoryCard prefab (Game sahnesinden once, referans icin)
            var factoryCardPrefab = CreateFactoryCardPrefab();

            // C. Game sahnesi
            CreateGameScene(factoryCardPrefab);

            // E. Build Settings
            ConfigureBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"{TAG} Proje kurulumu tamamlandi!");
        }

        /// <summary>
        /// Batch mode ile calistirilabilecek static metot.
        /// Unity -batchmode -executeMethod RiceFactory.Editor.ProjectSetup.SetupFromCommandLine -quit
        /// </summary>
        public static void SetupFromCommandLine()
        {
            Debug.Log($"{TAG} Batch mode ile proje kurulumu basliyor...");
            SetupProject();
            Debug.Log($"{TAG} Batch mode kurulum tamamlandi.");
        }

        // =====================================================================
        // A. Boot Sahnesi
        // =====================================================================

        private static void CreateBootScene()
        {
            Debug.Log($"{TAG} Boot sahnesi olusturuluyor...");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // --- Main Camera ---
            var cameraObj = new GameObject("Main Camera");
            cameraObj.tag = "MainCamera";
            var camera = cameraObj.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            ColorUtility.TryParseHtmlString("#1A1A2E", out Color bgColor);
            camera.backgroundColor = bgColor;
            camera.orthographic = true;
            cameraObj.AddComponent<AudioListener>();

            // --- EventSystem ---
            CreateEventSystem();

            // --- Canvas ---
            var canvas = CreateCanvas("Canvas");

            // --- Managers ---
            var managersObj = new GameObject("--- Managers ---");
            var bootstrapper = managersObj.AddComponent<GameBootstrapper>();

            // --- Audio ---
            var audioObj = new GameObject("--- Audio ---");
            var audioManager = audioObj.AddComponent<AudioManager>();

            // --- UI ---
            var uiObj = new GameObject("--- UI ---");
            var uiManager = uiObj.AddComponent<UIManager>();

            // UIManager container'larini olustur (panelContainer, popupContainer)
            var panelContainer = new GameObject("PanelContainer");
            panelContainer.transform.SetParent(canvas.transform, false);
            SetFullStretch(panelContainer.AddComponent<RectTransform>());

            var popupContainer = new GameObject("PopupContainer");
            popupContainer.transform.SetParent(canvas.transform, false);
            SetFullStretch(popupContainer.AddComponent<RectTransform>());

            // UIManager SerializeField baglantilari
            SetSerializedField(uiManager, "_panelContainer", panelContainer.transform);
            SetSerializedField(uiManager, "_popupContainer", popupContainer.transform);

            // --- Analytics ---
            var analyticsObj = new GameObject("--- Analytics ---");
            var analyticsManager = analyticsObj.AddComponent<AnalyticsManager>();
            var analyticsBridge = analyticsObj.AddComponent<AnalyticsBridge>();

            // --- GameBootstrapper SerializeField baglantilari ---
            SetSerializedField(bootstrapper, "_audioManager", audioManager);
            SetSerializedField(bootstrapper, "_uiManager", uiManager);
            SetSerializedField(bootstrapper, "_analyticsManager", analyticsManager);
            SetSerializedField(bootstrapper, "_analyticsBridge", analyticsBridge);

            // --- Splash/Logo Image ---
            var splashObj = CreateUIElement<Image>("SplashLogo", canvas.transform);
            var splashRect = splashObj.GetComponent<RectTransform>();
            splashRect.anchorMin = new Vector2(0.5f, 0.5f);
            splashRect.anchorMax = new Vector2(0.5f, 0.5f);
            splashRect.pivot = new Vector2(0.5f, 0.5f);
            splashRect.sizeDelta = new Vector2(600f, 200f);
            splashRect.anchoredPosition = Vector2.zero;
            var splashImage = splashObj.GetComponent<Image>();
            ColorUtility.TryParseHtmlString("#2D5016", out Color splashBg);
            splashImage.color = splashBg;

            // "riceFactory" splash text
            var splashText = CreateTMPText("SplashText", splashObj.transform, "riceFactory",
                64, TextAlignmentOptions.Center, Color.white);
            SetFullStretch(splashText.GetComponent<RectTransform>());

            // Sahneyi kaydet
            EnsureDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, SCENE_BOOT);
            Debug.Log($"{TAG} Boot sahnesi kaydedildi: {SCENE_BOOT}");
        }

        // =====================================================================
        // B. MainMenu Sahnesi
        // =====================================================================

        private static void CreateMainMenuScene()
        {
            Debug.Log($"{TAG} MainMenu sahnesi olusturuluyor...");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // --- Main Camera ---
            var cameraObj = new GameObject("Main Camera");
            cameraObj.tag = "MainCamera";
            var camera = cameraObj.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            ColorUtility.TryParseHtmlString("#2D5016", out Color bgColor);
            camera.backgroundColor = bgColor;
            camera.orthographic = true;
            cameraObj.AddComponent<AudioListener>();

            // --- EventSystem ---
            CreateEventSystem();

            // --- Canvas ---
            var canvas = CreateCanvas("Canvas");

            // --- Controller ---
            var controllerObj = new GameObject("--- Controller ---");
            var menuController = controllerObj.AddComponent<MainMenuController>();

            // ---- UI Elemanlari ----

            // Title Text: "riceFactory" (ust orta, buyuk font)
            var titleText = CreateTMPText("TitleText", canvas.transform, "riceFactory",
                72, TextAlignmentOptions.Center, Color.white);
            var titleRect = titleText.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.sizeDelta = new Vector2(800f, 120f);
            titleRect.anchoredPosition = new Vector2(0f, -150f);

            // Subtitle Text: "Gida Imparatorlugu" (title'in altinda)
            var subtitleText = CreateTMPText("SubtitleText", canvas.transform,
                "G\u0131da \u0130mparatorlu\u011fu",
                36, TextAlignmentOptions.Center, Color.white);
            var subtitleRect = subtitleText.GetComponent<RectTransform>();
            subtitleRect.anchorMin = new Vector2(0.5f, 1f);
            subtitleRect.anchorMax = new Vector2(0.5f, 1f);
            subtitleRect.pivot = new Vector2(0.5f, 1f);
            subtitleRect.sizeDelta = new Vector2(800f, 60f);
            subtitleRect.anchoredPosition = new Vector2(0f, -280f);

            // "Oyna" Button (orta, buyuk, yesil arka plan)
            var playButton = CreateButton("PlayButton", canvas.transform, "Oyna",
                48, new Vector2(400f, 100f), new Vector2(0f, 50f));
            ColorUtility.TryParseHtmlString("#4CAF50", out Color greenColor);
            playButton.GetComponent<Image>().color = greenColor;

            // "Devam Et" Button (Oyna'nin altinda, baslangicta gizli)
            var continueButton = CreateButton("ContinueButton", canvas.transform, "Devam Et",
                36, new Vector2(400f, 80f), new Vector2(0f, -70f));
            ColorUtility.TryParseHtmlString("#388E3C", out Color darkGreen);
            continueButton.GetComponent<Image>().color = darkGreen;
            // ContinueButtonContainer olarak kullanalim — baslangicta gizli
            var continueContainer = continueButton;
            continueContainer.SetActive(false);

            // "Ayarlar" Button (sag ust kose, kucuk)
            var settingsButton = CreateButton("SettingsButton", canvas.transform, "\u2699",
                32, new Vector2(80f, 80f), Vector2.zero);
            var settingsRect = settingsButton.GetComponent<RectTransform>();
            settingsRect.anchorMin = new Vector2(1f, 1f);
            settingsRect.anchorMax = new Vector2(1f, 1f);
            settingsRect.pivot = new Vector2(1f, 1f);
            settingsRect.anchoredPosition = new Vector2(-30f, -50f);
            ColorUtility.TryParseHtmlString("#757575", out Color grayColor);
            settingsButton.GetComponent<Image>().color = grayColor;

            // Version Text (sol alt: "v0.1.0")
            var versionText = CreateTMPText("VersionText", canvas.transform, "v0.1.0",
                20, TextAlignmentOptions.BottomLeft, Color.white);
            var versionRect = versionText.GetComponent<RectTransform>();
            versionRect.anchorMin = new Vector2(0f, 0f);
            versionRect.anchorMax = new Vector2(0f, 0f);
            versionRect.pivot = new Vector2(0f, 0f);
            versionRect.sizeDelta = new Vector2(200f, 40f);
            versionRect.anchoredPosition = new Vector2(20f, 20f);

            // Player Name Text (sag alt)
            var playerNameText = CreateTMPText("PlayerNameText", canvas.transform, "",
                20, TextAlignmentOptions.BottomRight, Color.white);
            var playerNameRect = playerNameText.GetComponent<RectTransform>();
            playerNameRect.anchorMin = new Vector2(1f, 0f);
            playerNameRect.anchorMax = new Vector2(1f, 0f);
            playerNameRect.pivot = new Vector2(1f, 0f);
            playerNameRect.sizeDelta = new Vector2(300f, 40f);
            playerNameRect.anchoredPosition = new Vector2(-20f, 50f);

            // Player Level Text (sag alt, name'in altinda)
            var playerLevelText = CreateTMPText("PlayerLevelText", canvas.transform, "",
                18, TextAlignmentOptions.BottomRight, Color.white);
            var playerLevelRect = playerLevelText.GetComponent<RectTransform>();
            playerLevelRect.anchorMin = new Vector2(1f, 0f);
            playerLevelRect.anchorMax = new Vector2(1f, 0f);
            playerLevelRect.pivot = new Vector2(1f, 0f);
            playerLevelRect.sizeDelta = new Vector2(300f, 40f);
            playerLevelRect.anchoredPosition = new Vector2(-20f, 20f);

            // --- MainMenuController SerializeField baglantilari ---
            SetSerializedField(menuController, "_playButton", playButton.GetComponent<Button>());
            SetSerializedField(menuController, "_continueButton", continueButton.GetComponent<Button>());
            SetSerializedField(menuController, "_settingsButton", settingsButton.GetComponent<Button>());
            SetSerializedField(menuController, "_versionText", versionText.GetComponent<TMP_Text>());
            SetSerializedField(menuController, "_playerNameText", playerNameText.GetComponent<TMP_Text>());
            SetSerializedField(menuController, "_playerLevelText", playerLevelText.GetComponent<TMP_Text>());
            SetSerializedField(menuController, "_continueButtonContainer", continueContainer);

            // Sahneyi kaydet
            EditorSceneManager.SaveScene(scene, SCENE_MAIN_MENU);
            Debug.Log($"{TAG} MainMenu sahnesi kaydedildi: {SCENE_MAIN_MENU}");
        }

        // =====================================================================
        // C. Game Sahnesi
        // =====================================================================

        private static void CreateGameScene(GameObject factoryCardPrefab)
        {
            Debug.Log($"{TAG} Game sahnesi olusturuluyor...");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // --- Main Camera ---
            var cameraObj = new GameObject("Main Camera");
            cameraObj.tag = "MainCamera";
            var camera = cameraObj.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            ColorUtility.TryParseHtmlString("#87CEEB", out Color bgColor);
            camera.backgroundColor = bgColor;
            camera.orthographic = true;
            cameraObj.AddComponent<AudioListener>();

            // --- EventSystem ---
            CreateEventSystem();

            // --- Controller ---
            var controllerObj = new GameObject("--- Controller ---");
            var gameSceneController = controllerObj.AddComponent<GameSceneController>();

            // --- HUD Canvas ---
            var canvas = CreateCanvas("HUDCanvas");

            // ---- Ust Bar Paneli ----
            var topBar = CreatePanel("TopBar", canvas.transform);
            var topBarRect = topBar.GetComponent<RectTransform>();
            topBarRect.anchorMin = new Vector2(0f, 1f);
            topBarRect.anchorMax = new Vector2(1f, 1f);
            topBarRect.pivot = new Vector2(0.5f, 1f);
            topBarRect.sizeDelta = new Vector2(0f, 120f);
            topBarRect.anchoredPosition = Vector2.zero;
            ColorUtility.TryParseHtmlString("#1A1A2E99", out Color topBarColor);
            topBar.GetComponent<Image>().color = topBarColor;

            // Coin Text
            var coinText = CreateTMPText("CoinText", topBar.transform, "\u26AA 0",
                28, TextAlignmentOptions.MidlineLeft, Color.yellow);
            var coinRect = coinText.GetComponent<RectTransform>();
            coinRect.anchorMin = new Vector2(0f, 0.5f);
            coinRect.anchorMax = new Vector2(0.33f, 0.5f);
            coinRect.pivot = new Vector2(0f, 0.5f);
            coinRect.sizeDelta = new Vector2(0f, 50f);
            coinRect.anchoredPosition = new Vector2(20f, 0f);
            coinRect.offsetMin = new Vector2(20f, -25f);
            coinRect.offsetMax = new Vector2(0f, 25f);

            // Gem Text
            var gemText = CreateTMPText("GemText", topBar.transform, "\u25C6 0",
                28, TextAlignmentOptions.Center, Color.cyan);
            var gemRect = gemText.GetComponent<RectTransform>();
            gemRect.anchorMin = new Vector2(0.33f, 0.5f);
            gemRect.anchorMax = new Vector2(0.66f, 0.5f);
            gemRect.pivot = new Vector2(0.5f, 0.5f);
            gemRect.sizeDelta = new Vector2(0f, 50f);
            gemRect.anchoredPosition = Vector2.zero;
            gemRect.offsetMin = new Vector2(0f, -25f);
            gemRect.offsetMax = new Vector2(0f, 25f);

            // Level Text
            var levelText = CreateTMPText("LevelText", topBar.transform, "Lv. 1",
                28, TextAlignmentOptions.MidlineRight, Color.white);
            var levelRect = levelText.GetComponent<RectTransform>();
            levelRect.anchorMin = new Vector2(0.66f, 0.5f);
            levelRect.anchorMax = new Vector2(1f, 0.5f);
            levelRect.pivot = new Vector2(1f, 0.5f);
            levelRect.sizeDelta = new Vector2(0f, 50f);
            levelRect.anchoredPosition = new Vector2(-20f, 0f);
            levelRect.offsetMin = new Vector2(0f, -25f);
            levelRect.offsetMax = new Vector2(-20f, 25f);

            // ---- Orta Alan: ScrollView (fabrika kartlari) ----
            var scrollView = CreateScrollView("FactoryScrollView", canvas.transform);
            var scrollRect = scrollView.GetComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0f, 0f);
            scrollRect.anchorMax = new Vector2(1f, 1f);
            scrollRect.offsetMin = new Vector2(10f, 160f);  // alt bar yuksekligi + padding
            scrollRect.offsetMax = new Vector2(-10f, -130f); // ust bar yuksekligi + padding

            // Content Transform (fabrika kartlari buraya eklenir)
            var contentTransform = scrollView.transform.Find("Viewport/Content");

            // VerticalLayoutGroup ekle
            var vlg = contentTransform.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 15f;
            vlg.padding = new RectOffset(10, 10, 10, 10);
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // ContentSizeFitter ekle (dikey icerigin boyutunu otomatik ayarlamak icin)
            var csf = contentTransform.gameObject.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // ---- Alt Bar Paneli ----
            var bottomBar = CreatePanel("BottomBar", canvas.transform);
            var bottomBarRect = bottomBar.GetComponent<RectTransform>();
            bottomBarRect.anchorMin = new Vector2(0f, 0f);
            bottomBarRect.anchorMax = new Vector2(1f, 0f);
            bottomBarRect.pivot = new Vector2(0.5f, 0f);
            bottomBarRect.sizeDelta = new Vector2(0f, 150f);
            bottomBarRect.anchoredPosition = Vector2.zero;
            ColorUtility.TryParseHtmlString("#1A1A2ECC", out Color bottomBarColor);
            bottomBar.GetComponent<Image>().color = bottomBarColor;

            // Alt bar butonlari: HorizontalLayoutGroup
            var hlg = bottomBar.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8f;
            hlg.padding = new RectOffset(10, 10, 10, 10);
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            // Alt bar buton isimleri
            string[] bottomButtons = { "Fabrika", "Upgrade", "Prestige", "Ara\u015ft\u0131rma", "Sipari\u015f", "Ma\u011faza" };
            ColorUtility.TryParseHtmlString("#4CAF50", out Color btnColor);

            foreach (var btnName in bottomButtons)
            {
                var btnObj = new GameObject(btnName + "Btn");
                btnObj.transform.SetParent(bottomBar.transform, false);

                var btnImage = btnObj.AddComponent<Image>();
                btnImage.color = btnColor;

                var btn = btnObj.AddComponent<Button>();
                btn.targetGraphic = btnImage;

                var btnText = CreateTMPText(btnName + "Text", btnObj.transform, btnName,
                    18, TextAlignmentOptions.Center, Color.white);
                SetFullStretch(btnText.GetComponent<RectTransform>());

                // Layout Element ile minimum boyut
                var le = btnObj.AddComponent<LayoutElement>();
                le.minHeight = 80f;
                le.minWidth = 60f;
            }

            // --- GameSceneController SerializeField baglantilari ---
            SetSerializedField(gameSceneController, "_factoryCardContainer", contentTransform);

            // FactoryCardPrefab baglantisi
            if (factoryCardPrefab != null)
            {
                var prefabCardUI = factoryCardPrefab.GetComponent<FactoryCardUI>();
                if (prefabCardUI != null)
                {
                    SetSerializedField(gameSceneController, "_factoryCardPrefab", prefabCardUI);
                }
            }

            // Sahneyi kaydet
            EditorSceneManager.SaveScene(scene, SCENE_GAME);
            Debug.Log($"{TAG} Game sahnesi kaydedildi: {SCENE_GAME}");
        }

        // =====================================================================
        // D. FactoryCard Prefab
        // =====================================================================

        private static GameObject CreateFactoryCardPrefab()
        {
            Debug.Log($"{TAG} FactoryCard prefab'i olusturuluyor...");

            EnsureDirectory("Assets/Prefabs");

            // Ana Panel
            var cardObj = new GameObject("FactoryCard");
            var cardRect = cardObj.AddComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(1000f, 200f);

            // Arka plan Image
            var cardImage = cardObj.AddComponent<Image>();
            ColorUtility.TryParseHtmlString("#FFFFFF33", out Color panelBg);
            cardImage.color = panelBg;

            // Layout Element (ScrollView icerisinde)
            var cardLayout = cardObj.AddComponent<LayoutElement>();
            cardLayout.preferredHeight = 200f;
            cardLayout.minHeight = 180f;

            // === Unlocked Content ===
            var unlockedContent = new GameObject("UnlockedContent");
            unlockedContent.transform.SetParent(cardObj.transform, false);
            var unlockedRect = unlockedContent.AddComponent<RectTransform>();
            SetFullStretch(unlockedRect);

            // Factory Name
            var nameText = CreateTMPText("FactoryNameText", unlockedContent.transform, "Fabrika Adi",
                32, TextAlignmentOptions.TopLeft, Color.white);
            var nameRect = nameText.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0f, 0.6f);
            nameRect.anchorMax = new Vector2(0.65f, 1f);
            nameRect.offsetMin = new Vector2(20f, 0f);
            nameRect.offsetMax = new Vector2(0f, -10f);

            // Star Rating
            var starText = CreateTMPText("StarText", unlockedContent.transform,
                "\u2605\u2605\u2605\u2606\u2606",
                24, TextAlignmentOptions.TopRight, Color.yellow);
            var starRect = starText.GetComponent<RectTransform>();
            starRect.anchorMin = new Vector2(0.65f, 0.6f);
            starRect.anchorMax = new Vector2(1f, 1f);
            starRect.offsetMin = new Vector2(0f, 0f);
            starRect.offsetMax = new Vector2(-20f, -10f);

            // Production Rate
            var prodRateText = CreateTMPText("ProductionRateText", unlockedContent.transform, "12.5/dk",
                22, TextAlignmentOptions.MidlineLeft, Color.white);
            var prodRect = prodRateText.GetComponent<RectTransform>();
            prodRect.anchorMin = new Vector2(0f, 0.3f);
            prodRect.anchorMax = new Vector2(0.5f, 0.6f);
            prodRect.offsetMin = new Vector2(20f, 0f);
            prodRect.offsetMax = Vector2.zero;

            // Revenue
            var revenueText = CreateTMPText("RevenueText", unlockedContent.transform, "450 coin/dk",
                22, TextAlignmentOptions.MidlineRight, Color.yellow);
            var revRect = revenueText.GetComponent<RectTransform>();
            revRect.anchorMin = new Vector2(0.5f, 0.3f);
            revRect.anchorMax = new Vector2(1f, 0.6f);
            revRect.offsetMin = Vector2.zero;
            revRect.offsetMax = new Vector2(-20f, 0f);

            // Progress Bar (Slider)
            var sliderObj = CreateSlider("ProgressBar", unlockedContent.transform);
            var sliderRect = sliderObj.GetComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0f, 0f);
            sliderRect.anchorMax = new Vector2(1f, 0.3f);
            sliderRect.offsetMin = new Vector2(20f, 10f);
            sliderRect.offsetMax = new Vector2(-20f, -5f);

            var progressFill = sliderObj.transform.Find("Fill Area/Fill")?.GetComponent<Image>();

            // === Locked Content ===
            var lockedContent = new GameObject("LockedContent");
            lockedContent.transform.SetParent(cardObj.transform, false);
            var lockedRect = lockedContent.AddComponent<RectTransform>();
            SetFullStretch(lockedRect);
            lockedContent.SetActive(false); // baslangicta gizli

            // Lock overlay arka plan
            var lockOverlay = new GameObject("LockOverlay");
            lockOverlay.transform.SetParent(lockedContent.transform, false);
            var lockOverlayRect = lockOverlay.AddComponent<RectTransform>();
            SetFullStretch(lockOverlayRect);
            var lockOverlayImage = lockOverlay.AddComponent<Image>();
            ColorUtility.TryParseHtmlString("#00000099", out Color lockBg);
            lockOverlayImage.color = lockBg;

            // Lock Icon
            var lockIconObj = new GameObject("LockIcon");
            lockIconObj.transform.SetParent(lockedContent.transform, false);
            var lockIconRect = lockIconObj.AddComponent<RectTransform>();
            lockIconRect.anchorMin = new Vector2(0.5f, 0.5f);
            lockIconRect.anchorMax = new Vector2(0.5f, 0.5f);
            lockIconRect.pivot = new Vector2(0.5f, 0.5f);
            lockIconRect.sizeDelta = new Vector2(80f, 80f);
            lockIconRect.anchoredPosition = new Vector2(0f, 20f);
            var lockIcon = lockIconObj.AddComponent<Image>();
            lockIcon.color = Color.white;

            // Lock icon yerine text
            var lockSymbol = CreateTMPText("LockSymbol", lockIconObj.transform, "\uD83D\uDD12",
                48, TextAlignmentOptions.Center, Color.white);
            SetFullStretch(lockSymbol.GetComponent<RectTransform>());

            // Unlock Cost Text
            var unlockCostText = CreateTMPText("UnlockCostText", lockedContent.transform,
                "A\u00e7: 1,000",
                24, TextAlignmentOptions.Center, Color.white);
            var unlockCostRect = unlockCostText.GetComponent<RectTransform>();
            unlockCostRect.anchorMin = new Vector2(0.5f, 0f);
            unlockCostRect.anchorMax = new Vector2(0.5f, 0.5f);
            unlockCostRect.pivot = new Vector2(0.5f, 0.5f);
            unlockCostRect.sizeDelta = new Vector2(300f, 50f);
            unlockCostRect.anchoredPosition = new Vector2(0f, -10f);

            // Unlock Button (kilitli icerik uzerinde)
            var unlockBtnObj = new GameObject("UnlockButton");
            unlockBtnObj.transform.SetParent(lockedContent.transform, false);
            var unlockBtnRect = unlockBtnObj.AddComponent<RectTransform>();
            SetFullStretch(unlockBtnRect);
            var unlockBtnImage = unlockBtnObj.AddComponent<Image>();
            unlockBtnImage.color = new Color(0f, 0f, 0f, 0.01f); // neredeyse seffaf, tiklanabilir
            var unlockButton = unlockBtnObj.AddComponent<Button>();
            unlockButton.targetGraphic = unlockBtnImage;

            // Card Button (tum kart uzerine tiklanabilirlik)
            var cardButton = cardObj.AddComponent<Button>();
            cardButton.targetGraphic = cardImage;

            // === FactoryCardUI component ekle ve SerializeField'lari bagla ===
            var factoryCardUI = cardObj.AddComponent<FactoryCardUI>();

            SetSerializedField(factoryCardUI, "_factoryNameText", nameText.GetComponent<TMP_Text>());
            SetSerializedField(factoryCardUI, "_starText", starText.GetComponent<TMP_Text>());
            SetSerializedField(factoryCardUI, "_productionRateText", prodRateText.GetComponent<TMP_Text>());
            SetSerializedField(factoryCardUI, "_revenueText", revenueText.GetComponent<TMP_Text>());
            SetSerializedField(factoryCardUI, "_progressBar", sliderObj.GetComponent<Slider>());
            if (progressFill != null)
                SetSerializedField(factoryCardUI, "_progressFill", progressFill);
            SetSerializedField(factoryCardUI, "_unlockedContent", unlockedContent);
            SetSerializedField(factoryCardUI, "_lockedContent", lockedContent);
            SetSerializedField(factoryCardUI, "_unlockCostText", unlockCostText.GetComponent<TMP_Text>());
            SetSerializedField(factoryCardUI, "_lockIcon", lockIcon);
            SetSerializedField(factoryCardUI, "_cardButton", cardButton);
            SetSerializedField(factoryCardUI, "_unlockButton", unlockButton);

            // Prefab olarak kaydet
            var prefab = PrefabUtility.SaveAsPrefabAsset(cardObj, PREFAB_FACTORY_CARD);
            Object.DestroyImmediate(cardObj);

            Debug.Log($"{TAG} FactoryCard prefab kaydedildi: {PREFAB_FACTORY_CARD}");
            return prefab;
        }

        // =====================================================================
        // E. Build Settings
        // =====================================================================

        private static void ConfigureBuildSettings()
        {
            Debug.Log($"{TAG} Build Settings ayarlaniyor...");

            // Sahne listesi
            var scenes = new[]
            {
                new EditorBuildSettingsScene(SCENE_BOOT, true),
                new EditorBuildSettingsScene(SCENE_MAIN_MENU, true),
                new EditorBuildSettingsScene(SCENE_GAME, true)
            };
            EditorBuildSettings.scenes = scenes;

            // Player Settings
            PlayerSettings.companyName = "RiceFactory";
            PlayerSettings.productName = "riceFactory";

#if UNITY_ANDROID
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.ricefactory.game");
#endif
#if UNITY_IOS
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, "com.ricefactory.game");
#endif
            // Tum platformlar icin fallback
            PlayerSettings.SetApplicationIdentifier(
                EditorUserBuildSettings.selectedBuildTargetGroup, "com.ricefactory.game");

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;

            Debug.Log($"{TAG} Build Settings tamamlandi. " +
                      $"Sahneler: {scenes.Length}, Orientation: Portrait");
        }

        // =====================================================================
        // F. Placeholder Sprite'lari
        // =====================================================================

        private static void CreatePlaceholderSprites()
        {
            Debug.Log($"{TAG} Placeholder sprite'lari olusturuluyor...");

            EnsureDirectory(SPRITE_DIR);

            // placeholder_icon.png (64x64 beyaz kare)
            CreateAndSaveTexture("placeholder_icon.png", 64, 64, Color.white);

            // placeholder_button.png (256x64 yuvarlak koseli dikdortgen)
            CreateRoundedRectTexture("placeholder_button.png", 256, 64, Color.white, 12);

            // placeholder_panel.png (512x512 yari-seffaf beyaz)
            CreateAndSaveTexture("placeholder_panel.png", 512, 512,
                new Color(1f, 1f, 1f, 0.5f));

            // placeholder_progress_bg.png (256x32 gri)
            ColorUtility.TryParseHtmlString("#808080", out Color grayColor);
            CreateAndSaveTexture("placeholder_progress_bg.png", 256, 32, grayColor);

            // placeholder_progress_fill.png (256x32 yesil)
            ColorUtility.TryParseHtmlString("#4CAF50", out Color progressGreen);
            CreateAndSaveTexture("placeholder_progress_fill.png", 256, 32, progressGreen);

            AssetDatabase.Refresh();
            Debug.Log($"{TAG} Placeholder sprite'lari olusturuldu.");
        }

        // =====================================================================
        // Yardimci Metotlar
        // =====================================================================

        /// <summary>
        /// EventSystem + StandaloneInputModule olusturur.
        /// </summary>
        private static GameObject CreateEventSystem()
        {
            var esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<StandaloneInputModule>();
            return esObj;
        }

        /// <summary>
        /// Standart Canvas olusturur: ScreenSpaceOverlay, CanvasScaler (ScaleWithScreenSize),
        /// GraphicRaycaster.
        /// </summary>
        private static GameObject CreateCanvas(string name)
        {
            var canvasObj = new GameObject(name);

            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(REF_WIDTH, REF_HEIGHT);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            return canvasObj;
        }

        /// <summary>
        /// TextMeshProUGUI ile metin olusturur.
        /// </summary>
        private static GameObject CreateTMPText(string name, Transform parent, string text,
            float fontSize, TextAlignmentOptions alignment, Color color)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rect = obj.AddComponent<RectTransform>();

            var tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = color;
            tmp.enableAutoSizing = false;
            tmp.overflowMode = TextOverflowModes.Ellipsis;

            return obj;
        }

        /// <summary>
        /// Image component'li UI elemani olusturur.
        /// </summary>
        private static GameObject CreateUIElement<T>(string name, Transform parent) where T : Component
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.AddComponent<RectTransform>();
            obj.AddComponent<T>();
            return obj;
        }

        /// <summary>
        /// Buton olusturur (Image + Button + TMP_Text).
        /// Ortaya hizali, verilen boyut ve offset ile konumlandirilir.
        /// </summary>
        private static GameObject CreateButton(string name, Transform parent, string label,
            float fontSize, Vector2 size, Vector2 offset)
        {
            var btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);

            var btnRect = btnObj.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.5f, 0.5f);
            btnRect.anchorMax = new Vector2(0.5f, 0.5f);
            btnRect.pivot = new Vector2(0.5f, 0.5f);
            btnRect.sizeDelta = size;
            btnRect.anchoredPosition = offset;

            var btnImage = btnObj.AddComponent<Image>();
            btnImage.color = Color.white;

            var btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = btnImage;

            // Buton label'i
            var textObj = CreateTMPText(name + "Label", btnObj.transform, label,
                fontSize, TextAlignmentOptions.Center, Color.white);
            SetFullStretch(textObj.GetComponent<RectTransform>());

            return btnObj;
        }

        /// <summary>
        /// Image arka planli panel olusturur.
        /// </summary>
        private static GameObject CreatePanel(string name, Transform parent)
        {
            var panelObj = new GameObject(name);
            panelObj.transform.SetParent(parent, false);
            panelObj.AddComponent<RectTransform>();
            panelObj.AddComponent<Image>();
            return panelObj;
        }

        /// <summary>
        /// Scroll View (ScrollRect + Viewport + Content) olusturur.
        /// </summary>
        private static GameObject CreateScrollView(string name, Transform parent)
        {
            var scrollObj = new GameObject(name);
            scrollObj.transform.SetParent(parent, false);
            var scrollRect = scrollObj.AddComponent<RectTransform>();

            var scroll = scrollObj.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.elasticity = 0.1f;

            // Viewport (mask alanı)
            var viewportObj = new GameObject("Viewport");
            viewportObj.transform.SetParent(scrollObj.transform, false);
            var viewportRect = viewportObj.AddComponent<RectTransform>();
            SetFullStretch(viewportRect);

            var viewportImage = viewportObj.AddComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0f); // seffaf
            var mask = viewportObj.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            // Content
            var contentObj = new GameObject("Content");
            contentObj.transform.SetParent(viewportObj.transform, false);
            var contentRect = contentObj.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.sizeDelta = new Vector2(0f, 0f);
            contentRect.anchoredPosition = Vector2.zero;

            scroll.viewport = viewportRect;
            scroll.content = contentRect;

            return scrollObj;
        }

        /// <summary>
        /// Slider (progress bar) olusturur.
        /// </summary>
        private static GameObject CreateSlider(string name, Transform parent)
        {
            var sliderObj = new GameObject(name);
            sliderObj.transform.SetParent(parent, false);
            sliderObj.AddComponent<RectTransform>();

            // Background
            var bgObj = new GameObject("Background");
            bgObj.transform.SetParent(sliderObj.transform, false);
            var bgRect = bgObj.AddComponent<RectTransform>();
            SetFullStretch(bgRect);
            var bgImage = bgObj.AddComponent<Image>();
            ColorUtility.TryParseHtmlString("#555555", out Color bgColor);
            bgImage.color = bgColor;

            // Fill Area
            var fillAreaObj = new GameObject("Fill Area");
            fillAreaObj.transform.SetParent(sliderObj.transform, false);
            var fillAreaRect = fillAreaObj.AddComponent<RectTransform>();
            SetFullStretch(fillAreaRect);
            fillAreaRect.offsetMin = new Vector2(5f, 5f);
            fillAreaRect.offsetMax = new Vector2(-5f, -5f);

            // Fill
            var fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(fillAreaObj.transform, false);
            var fillRect = fillObj.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0f, 1f);  // Slider kontrol eder
            fillRect.sizeDelta = new Vector2(0f, 0f);
            fillRect.anchoredPosition = Vector2.zero;
            var fillImage = fillObj.AddComponent<Image>();
            ColorUtility.TryParseHtmlString("#4CAF50", out Color fillColor);
            fillImage.color = fillColor;

            // Slider component
            var slider = sliderObj.AddComponent<Slider>();
            slider.fillRect = fillRect;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.35f;
            slider.interactable = false; // progress bar olarak kullanilacak
            slider.transition = Selectable.Transition.None;

            return sliderObj;
        }

        /// <summary>
        /// RectTransform'u tam streche ayarlar (parent'in tamamini kaplar).
        /// </summary>
        private static void SetFullStretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// SerializedObject uzerinden private SerializeField'a deger atar.
        /// </summary>
        private static void SetSerializedField(Object target, string fieldName, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                prop.objectReferenceValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                Debug.LogWarning($"{TAG} SerializeField bulunamadi: {target.GetType().Name}.{fieldName}");
            }
        }

        /// <summary>
        /// Dizin yoksa olusturur.
        /// </summary>
        private static void EnsureDirectory(string path)
        {
            var fullPath = Path.Combine(Application.dataPath, "..", path);
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }
        }

        /// <summary>
        /// Belirtilen boyut ve renkte duz bir texture olusturur ve PNG olarak kaydeder.
        /// </summary>
        private static void CreateAndSaveTexture(string fileName, int width, int height, Color color)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = color;
            tex.SetPixels(pixels);
            tex.Apply();

            SaveTexturePNG(tex, fileName);
            Object.DestroyImmediate(tex);
        }

        /// <summary>
        /// Yuvarlak koseli dikdortgen texture olusturur ve PNG olarak kaydeder.
        /// </summary>
        private static void CreateRoundedRectTexture(string fileName, int width, int height,
            Color color, int radius)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var transparent = new Color(0f, 0f, 0f, 0f);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    // Koselerden uzaklik hesapla
                    bool inside = true;

                    // Sol alt kose
                    if (x < radius && y < radius)
                        inside = (Vector2.Distance(new Vector2(x, y), new Vector2(radius, radius)) <= radius);
                    // Sag alt kose
                    else if (x > width - radius - 1 && y < radius)
                        inside = (Vector2.Distance(new Vector2(x, y), new Vector2(width - radius - 1, radius)) <= radius);
                    // Sol ust kose
                    else if (x < radius && y > height - radius - 1)
                        inside = (Vector2.Distance(new Vector2(x, y), new Vector2(radius, height - radius - 1)) <= radius);
                    // Sag ust kose
                    else if (x > width - radius - 1 && y > height - radius - 1)
                        inside = (Vector2.Distance(new Vector2(x, y), new Vector2(width - radius - 1, height - radius - 1)) <= radius);

                    tex.SetPixel(x, y, inside ? color : transparent);
                }
            }

            tex.Apply();
            SaveTexturePNG(tex, fileName);
            Object.DestroyImmediate(tex);
        }

        /// <summary>
        /// Texture2D'yi PNG olarak Sprites dizinine kaydeder.
        /// </summary>
        private static void SaveTexturePNG(Texture2D tex, string fileName)
        {
            var bytes = tex.EncodeToPNG();
            var fullPath = Path.Combine(Application.dataPath, "Sprites", fileName);
            File.WriteAllBytes(fullPath, bytes);
            Debug.Log($"{TAG} Sprite kaydedildi: Assets/Sprites/{fileName}");
        }
    }
}

#endif
