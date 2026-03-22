// =============================================================================
// FeedbackManager.cs
// Merkezi feedback yoneticisi: ses, gorsel efekt, haptic feedback.
// Singleton pattern, DontDestroyOnLoad.
// Diger sistemler bu sinif uzerinden satisfying feedback tetikler.
// =============================================================================

using UnityEngine;

namespace RiceFactory.Core
{
    /// <summary>
    /// Merkezi feedback yoneticisi. Ses efektleri, UI animasyonlari,
    /// screen efektleri ve haptic feedback'i tek bir noktadan yonetir.
    /// </summary>
    public class FeedbackManager : MonoBehaviour
    {
        // =====================================================================
        // Singleton
        // =====================================================================

        public static FeedbackManager Instance { get; private set; }

        // =====================================================================
        // Referanslar
        // =====================================================================

        private IAudioManager _audio;
        private UI.ScreenEffects _screenEffects;
        private Transform _floatingTextContainer;

        // Floating text prefab — runtime'da olusturulur
        private GameObject _floatingTextPrefab;

        // Object pool
        private readonly System.Collections.Generic.Queue<UI.FloatingText> _textPool = new();
        private const int POOL_INITIAL_SIZE = 10;

        // =====================================================================
        // Unity Lifecycle
        // =====================================================================

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // AudioManager'i al
            _audio = AudioManager.Instance;

            // ScreenEffects'i bul veya olustur
            _screenEffects = UI.ScreenEffects.Instance;
            if (_screenEffects == null)
            {
                Debug.LogWarning("[FeedbackManager] ScreenEffects bulunamadi. " +
                    "Gorsel efektler devre disi kalacak.");
            }

            // FloatingText container ve pool
            InitializeFloatingTextPool();
        }

        // =====================================================================
        // Pool Yonetimi
        // =====================================================================

        private void InitializeFloatingTextPool()
        {
            // Container olustur
            var containerObj = new GameObject("FloatingTextContainer");
            containerObj.transform.SetParent(transform);
            _floatingTextContainer = containerObj.transform;

            // Canvas bul (sahnedeki ilk Canvas)
            var canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                containerObj.transform.SetParent(canvas.transform, false);
                var rect = containerObj.AddComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }

            // Pool'u doldur
            for (int i = 0; i < POOL_INITIAL_SIZE; i++)
            {
                var ft = CreateFloatingTextInstance();
                ft.gameObject.SetActive(false);
                _textPool.Enqueue(ft);
            }
        }

        private UI.FloatingText CreateFloatingTextInstance()
        {
            var obj = new GameObject("FloatingText");
            obj.transform.SetParent(_floatingTextContainer, false);

            var rect = obj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(300f, 60f);

            var tmp = obj.AddComponent<TMPro.TextMeshProUGUI>();
            tmp.fontSize = 28;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.enableAutoSizing = false;
            tmp.raycastTarget = false;

            var canvasGroup = obj.AddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            var floatingText = obj.AddComponent<UI.FloatingText>();

            return floatingText;
        }

        private UI.FloatingText GetFloatingText()
        {
            UI.FloatingText ft;
            if (_textPool.Count > 0)
            {
                ft = _textPool.Dequeue();
            }
            else
            {
                ft = CreateFloatingTextInstance();
            }

            ft.gameObject.SetActive(true);
            return ft;
        }

        /// <summary>FloatingText'i pool'a geri dondurur.</summary>
        public void ReturnFloatingText(UI.FloatingText ft)
        {
            if (ft == null) return;
            ft.gameObject.SetActive(false);
            _textPool.Enqueue(ft);
        }

        // =====================================================================
        // Feedback Metodlari
        // =====================================================================

        /// <summary>
        /// Coin kazanma feedback'i: ses + floating "+1,234" text.
        /// </summary>
        public void PlayCoinEarned(double amount)
        {
            PlaySFX("sfx_coin");

            // Floating text
            var ft = GetFloatingText();
            string formattedAmount = FormatNumber(amount);
            ft.Show($"+{formattedAmount}", new Color(0.3f, 0.69f, 0.31f, 1f)); // Yesil (#4CAF50)

            TriggerHaptic(HapticIntensity.Light);
        }

        /// <summary>
        /// Upgrade feedback'i: ses + scale bounce.
        /// </summary>
        public void PlayUpgrade()
        {
            PlaySFX("sfx_upgrade");

            // Scale bounce — FeedbackManager'in kendi transform'unda degil,
            // cagiran UI elemaninda yapilmali. Burada genel bir screen efekt kullanalim.
            if (_screenEffects != null)
                _screenEffects.FlashScreen(Color.white, 0.08f);

            TriggerHaptic(HapticIntensity.Medium);
        }

        /// <summary>
        /// Uretim feedback'i: ses + hafif screen shake.
        /// </summary>
        public void PlayProduction()
        {
            PlaySFX("sfx_production");

            if (_screenEffects != null)
                _screenEffects.ShakeScreen(2f, 0.15f);
        }

        /// <summary>
        /// Prestige feedback'i: ses + ekran flash + confetti.
        /// </summary>
        public void PlayPrestige()
        {
            PlaySFX("sfx_prestige");

            if (_screenEffects != null)
            {
                // Altin flash
                var goldColor = new Color(1f, 0.84f, 0f, 0.6f);
                _screenEffects.FlashScreen(goldColor, 0.2f);

                // Confetti
                _screenEffects.SpawnConfetti(50);
            }

            TriggerHaptic(HapticIntensity.Heavy);
        }

        /// <summary>
        /// Buton tiklama feedback'i: ses + hafif haptic.
        /// </summary>
        public void PlayButtonClick()
        {
            PlaySFX("sfx_button");
            TriggerHaptic(HapticIntensity.Light);
        }

        /// <summary>
        /// Acma/kilit acma feedback'i: ses + haptic.
        /// </summary>
        public void PlayUnlock()
        {
            PlaySFX("sfx_unlock");

            if (_screenEffects != null)
            {
                _screenEffects.FlashScreen(Color.white, 0.12f);
            }

            TriggerHaptic(HapticIntensity.Medium);
        }

        /// <summary>
        /// Hata feedback'i: ses + hafif kirmizi flash.
        /// </summary>
        public void PlayError()
        {
            PlaySFX("sfx_error");

            if (_screenEffects != null)
            {
                var redFlash = new Color(0.96f, 0.26f, 0.21f, 0.3f); // #F44336, %30 opak
                _screenEffects.FlashScreen(redFlash, 0.15f);
            }

            TriggerHaptic(HapticIntensity.Light);
        }

        /// <summary>
        /// Seviye atlama feedback'i: ses + buyuk floating text + haptic.
        /// </summary>
        public void PlayLevelUp()
        {
            PlaySFX("sfx_levelup");

            // Buyuk floating text
            var ft = GetFloatingText();
            ft.Show("LEVEL UP!", new Color(1f, 0.84f, 0f, 1f), 42f); // Altin, buyuk font

            if (_screenEffects != null)
            {
                var goldColor = new Color(1f, 0.84f, 0f, 0.4f);
                _screenEffects.FlashScreen(goldColor, 0.15f);
                _screenEffects.SpawnConfetti(30);
            }

            TriggerHaptic(HapticIntensity.Heavy);
        }

        // =====================================================================
        // Ses Yardimcisi
        // =====================================================================

        private void PlaySFX(string clipName)
        {
            if (_audio == null)
                _audio = AudioManager.Instance;

            if (_audio != null)
                _audio.PlaySFX(clipName);
        }

        // =====================================================================
        // Haptic Feedback
        // =====================================================================

        private enum HapticIntensity
        {
            Light,
            Medium,
            Heavy
        }

        private void TriggerHaptic(HapticIntensity intensity)
        {
#if UNITY_IOS && !UNITY_EDITOR
            TriggerHapticiOS(intensity);
#elif UNITY_ANDROID && !UNITY_EDITOR
            TriggerHapticAndroid(intensity);
#endif
        }

#if UNITY_IOS
        /// <summary>iOS Taptic Engine feedback.</summary>
        private void TriggerHapticiOS(HapticIntensity intensity)
        {
            try
            {
                // UnityEngine.iOS.Device.SetNoBackupFlag kullanarak
                // UIImpactFeedbackGenerator simulasyonu
                // iOS native plugin gerektirir — burada placeholder
                switch (intensity)
                {
                    case HapticIntensity.Light:
                        // UIImpactFeedbackStyle.Light
                        Handheld.Vibrate(); // Fallback
                        break;
                    case HapticIntensity.Medium:
                        Handheld.Vibrate();
                        break;
                    case HapticIntensity.Heavy:
                        Handheld.Vibrate();
                        break;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[FeedbackManager] iOS haptic hatasi: {e.Message}");
            }
        }
#endif

#if UNITY_ANDROID
        /// <summary>Android Vibration API.</summary>
        private void TriggerHapticAndroid(HapticIntensity intensity)
        {
            try
            {
                long durationMs;
                switch (intensity)
                {
                    case HapticIntensity.Light:
                        durationMs = 10;
                        break;
                    case HapticIntensity.Medium:
                        durationMs = 25;
                        break;
                    case HapticIntensity.Heavy:
                        durationMs = 50;
                        break;
                    default:
                        durationMs = 10;
                        break;
                }

                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
                {
                    // API 26+ VibrationEffect kullan
                    if (GetAndroidAPILevel() >= 26)
                    {
                        using (var vibrationEffect = new AndroidJavaClass("android.os.VibrationEffect"))
                        {
                            var effect = vibrationEffect.CallStatic<AndroidJavaObject>(
                                "createOneShot", durationMs, -1); // -1 = DEFAULT_AMPLITUDE
                            vibrator.Call("vibrate", effect);
                        }
                    }
                    else
                    {
                        vibrator.Call("vibrate", durationMs);
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[FeedbackManager] Android haptic hatasi: {e.Message}");
            }
        }

        private int GetAndroidAPILevel()
        {
            using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
            {
                return version.GetStatic<int>("SDK_INT");
            }
        }
#endif

        // =====================================================================
        // Sayi Formatlama
        // =====================================================================

        /// <summary>Buyuk sayilari okunaklı formata cevirir: 1K, 1M, 1B, 1T.</summary>
        public static string FormatNumber(double value)
        {
            if (value < 0) return "-" + FormatNumber(-value);

            if (value < 1000)
                return value.ToString("F0");
            if (value < 1_000_000)
                return (value / 1_000).ToString("F1") + "K";
            if (value < 1_000_000_000)
                return (value / 1_000_000).ToString("F2") + "M";
            if (value < 1_000_000_000_000)
                return (value / 1_000_000_000).ToString("F2") + "B";

            return (value / 1_000_000_000_000).ToString("F2") + "T";
        }
    }
}
