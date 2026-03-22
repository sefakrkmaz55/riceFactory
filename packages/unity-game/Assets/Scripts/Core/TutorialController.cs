// =============================================================================
// TutorialController.cs
// Ilk oyuncu deneyimi (Onboarding). GDD Bolum 8'deki ilk 10 dakika akisi.
// Adim adim tutorial: hos geldin -> uretim -> upgrade -> serbest oyun.
// Vurgulama efekti (spotlight overlay + mask) ile hedef UI elemanini gosterir.
// Tutorial durumu PlayerPrefs'e kaydedilir.
// =============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RiceFactory.Core.Events;

namespace RiceFactory.Core
{
    /// <summary>
    /// Tutorial sistemi. Ilk oyuncu deneyimini adim adim yonlendirir.
    ///
    /// Adimlar:
    /// 1. "Hos geldin! Pirinc tarlana goz at" -> Fabrika kartina vurgula
    /// 2. "Uretim basladi! Bekle..." -> Uretim tamamlaninca devam
    /// 3. "Para kazandin! Simdi makineyi yukselt" -> Upgrade butonuna yonlendir
    /// 4. "Harika! Uretim hizlandi" -> Serbest oyun
    ///
    /// Tutorial durumu PlayerPrefs'te saklanir ("tutorial_completed", "tutorial_step").
    /// Skip butonu ile atlanabilir.
    /// </summary>
    public class TutorialController : MonoBehaviour
    {
        // =====================================================================
        // Inspector Referanslari
        // =====================================================================

        [Header("Overlay")]
        [Tooltip("Tutorial overlay CanvasGroup (ekrani karartir)")]
        [SerializeField] private CanvasGroup _overlayCanvasGroup;

        [Tooltip("Tutorial arka plan overlay image (yari seffaf siyah)")]
        [SerializeField] private Image _overlayBackground;

        [Header("Mesaj Paneli")]
        [SerializeField] private GameObject _messagePanel;
        [SerializeField] private TMPro.TMP_Text _messageText;
        [SerializeField] private Button _nextButton;
        [SerializeField] private TMPro.TMP_Text _nextButtonText;

        [Header("Skip")]
        [SerializeField] private Button _skipButton;

        [Header("Spotlight")]
        [Tooltip("Vurgulanan UI elemani etrafindaki spotlight mask")]
        [SerializeField] private RectTransform _spotlightMask;

        // =====================================================================
        // Tutorial Adimlari
        // =====================================================================

        /// <summary>Tutorial adim tanimlari.</summary>
        private static readonly TutorialStep[] Steps =
        {
            new TutorialStep
            {
                StepId = 0,
                Message = "Hos geldin! Bu senin pirinc tarlan.\nUretim otomatik olarak basliyor.",
                WaitForEvent = false,
                HighlightTarget = "FactoryCard_rice_field",
                NextButtonLabel = "Tamam"
            },
            new TutorialStep
            {
                StepId = 1,
                Message = "Uretim basladi! Ilk mahsulun yetisiyor...\nProgress bar'in dolmasini bekle.",
                WaitForEvent = true,
                EventType = typeof(ProductionCompletedEvent),
                HighlightTarget = "FactoryCard_rice_field",
                NextButtonLabel = null // Event tetikleyecek
            },
            new TutorialStep
            {
                StepId = 2,
                Message = "Harika! Para kazandin!\nSimdi makineyi yukseltmek icin fabrika kartina dokun.",
                WaitForEvent = false,
                HighlightTarget = "FactoryCard_rice_field",
                NextButtonLabel = "Anladim"
            },
            new TutorialStep
            {
                StepId = 3,
                Message = "Tebrikler! Artik kendi pirinc imparatorlugunu kurabilirsin.\nIyi oyunlar!",
                WaitForEvent = false,
                HighlightTarget = null,
                NextButtonLabel = "Basla"
            }
        };

        // =====================================================================
        // Dahili Durum
        // =====================================================================

        private int _currentStepIndex;
        private bool _isActive;
        private IEventManager _eventManager;

        // Aktif event dinleyici referansi (temizleme icin)
        private Action<ProductionCompletedEvent> _productionListener;

        // =====================================================================
        // Public API
        // =====================================================================

        /// <summary>Tutorial tamamlandi mi?</summary>
        public bool IsCompleted => PlayerPrefs.GetInt("tutorial_completed", 0) == 1;

        /// <summary>Tutorial aktif mi?</summary>
        public bool IsActive => _isActive;

        // =====================================================================
        // Tutorial Baslatma
        // =====================================================================

        /// <summary>
        /// Tutorial'i bastan baslatir. Eger zaten tamamlanmissa hicbir sey yapmaz.
        /// </summary>
        public void StartTutorial()
        {
            if (IsCompleted)
            {
                Debug.Log("[TutorialController] Tutorial zaten tamamlanmis, atlanıyor.");
                return;
            }

            ServiceLocator.TryGet(out _eventManager);

            _isActive = true;
            _currentStepIndex = PlayerPrefs.GetInt("tutorial_step", 0);

            // UI kur
            SetupUI();

            // Ilk adimi goster
            ShowStep(_currentStepIndex);

            Debug.Log("[TutorialController] Tutorial baslatildi.");
        }

        // =====================================================================
        // UI Kurulum
        // =====================================================================

        private void SetupUI()
        {
            // Overlay'i ac
            if (_overlayCanvasGroup != null)
            {
                _overlayCanvasGroup.gameObject.SetActive(true);
                _overlayCanvasGroup.alpha = 0f;
                SimpleTween.DOFade(_overlayCanvasGroup, 1f, 0.3f);
            }

            // Skip butonu
            if (_skipButton != null)
            {
                _skipButton.onClick.RemoveAllListeners();
                _skipButton.onClick.AddListener(SkipTutorial);
            }

            // Next butonu
            if (_nextButton != null)
            {
                _nextButton.onClick.RemoveAllListeners();
                _nextButton.onClick.AddListener(OnNextClicked);
            }
        }

        // =====================================================================
        // Adim Gosterme
        // =====================================================================

        /// <summary>
        /// Belirtilen adimi gosterir: mesaj, spotlight, buton durumu.
        /// </summary>
        private void ShowStep(int stepIndex)
        {
            if (stepIndex < 0 || stepIndex >= Steps.Length)
            {
                CompleteTutorial();
                return;
            }

            var step = Steps[stepIndex];
            _currentStepIndex = stepIndex;

            // Adim ilerlemesini kaydet
            PlayerPrefs.SetInt("tutorial_step", stepIndex);
            PlayerPrefs.Save();

            // Mesaj goster
            if (_messagePanel != null)
            {
                _messagePanel.SetActive(true);
            }

            if (_messageText != null)
            {
                _messageText.text = step.Message;
            }

            // "Devam" butonu: event bekliyorsa gizle, degilse goster
            if (_nextButton != null)
            {
                _nextButton.gameObject.SetActive(!step.WaitForEvent);
            }

            if (_nextButtonText != null && step.NextButtonLabel != null)
            {
                _nextButtonText.text = step.NextButtonLabel;
            }

            // Spotlight (vurgulama)
            UpdateSpotlight(step.HighlightTarget);

            // Event dinleyici kur (gerekiyorsa)
            ClearEventListeners();
            if (step.WaitForEvent && step.EventType == typeof(ProductionCompletedEvent))
            {
                _productionListener = OnProductionCompleted;
                _eventManager?.Subscribe(_productionListener);
            }

            // Analytics
            LogTutorialStep(stepIndex);

            Debug.Log($"[TutorialController] Adim {stepIndex}: {step.Message.Substring(0, Mathf.Min(40, step.Message.Length))}...");
        }

        // =====================================================================
        // Adim Gecisleri
        // =====================================================================

        /// <summary>"Devam" butonuna basildiginda bir sonraki adima gecer.</summary>
        private void OnNextClicked()
        {
            ShowStep(_currentStepIndex + 1);
        }

        /// <summary>ProductionCompletedEvent tetiklendiginde otomatik ilerleme.</summary>
        private void OnProductionCompleted(ProductionCompletedEvent e)
        {
            // Sadece ilgili adimda ilerle
            if (_currentStepIndex == 1 && _isActive)
            {
                ClearEventListeners();
                ShowStep(_currentStepIndex + 1);
            }
        }

        // =====================================================================
        // Spotlight (Vurgulama)
        // =====================================================================

        /// <summary>
        /// Hedef UI elemaninin etrafina spotlight efekti uygular.
        /// Overlay uzerinde bir delik acarak hedefi vurgular.
        /// </summary>
        /// <param name="targetName">Hedef GameObject'in adi (null = spotlight kapat)</param>
        private void UpdateSpotlight(string targetName)
        {
            if (_spotlightMask == null) return;

            if (string.IsNullOrEmpty(targetName))
            {
                _spotlightMask.gameObject.SetActive(false);
                return;
            }

            // Hedef nesneyi bul
            var targetObj = GameObject.Find(targetName);
            if (targetObj == null)
            {
                Debug.LogWarning($"[TutorialController] Spotlight hedefi bulunamadi: {targetName}");
                _spotlightMask.gameObject.SetActive(false);
                return;
            }

            var targetRect = targetObj.GetComponent<RectTransform>();
            if (targetRect == null)
            {
                _spotlightMask.gameObject.SetActive(false);
                return;
            }

            // Spotlight'i hedef pozisyonuna tasi
            _spotlightMask.gameObject.SetActive(true);
            _spotlightMask.position = targetRect.position;

            // Boyutu hedefin boyutuna gore ayarla (biraz fazlalik ile)
            Vector2 targetSize = targetRect.rect.size;
            float padding = 20f;
            _spotlightMask.sizeDelta = new Vector2(
                targetSize.x + padding,
                targetSize.y + padding
            );
        }

        // =====================================================================
        // Tutorial Tamamlama / Atlama
        // =====================================================================

        /// <summary>
        /// Tutorial'i basariyla tamamlar.
        /// </summary>
        private void CompleteTutorial()
        {
            _isActive = false;

            PlayerPrefs.SetInt("tutorial_completed", 1);
            PlayerPrefs.Save();

            ClearEventListeners();
            HideOverlay();

            Debug.Log("[TutorialController] Tutorial tamamlandi!");

            // Analytics event
            if (ServiceLocator.TryGet<IAnalyticsManager>(out var analytics))
            {
                analytics.LogEvent("tutorial_completed");
            }
        }

        /// <summary>
        /// Tutorial'i atlar (skip butonu).
        /// </summary>
        public void SkipTutorial()
        {
            Debug.Log("[TutorialController] Tutorial atlanıyor.");

            _isActive = false;

            PlayerPrefs.SetInt("tutorial_completed", 1);
            PlayerPrefs.Save();

            ClearEventListeners();
            HideOverlay();

            // Analytics event
            if (ServiceLocator.TryGet<IAnalyticsManager>(out var analytics))
            {
                analytics.LogEvent("tutorial_skipped", new System.Collections.Generic.Dictionary<string, object>
                {
                    { "skipped_at_step", _currentStepIndex }
                });
            }
        }

        /// <summary>Tutorial overlay'ini gizler (fade out).</summary>
        private void HideOverlay()
        {
            if (_overlayCanvasGroup != null)
            {
                SimpleTween.DOFade(_overlayCanvasGroup, 0f, 0.3f, SimpleTween.Ease.OutQuad,
                    onComplete: () =>
                    {
                        _overlayCanvasGroup.gameObject.SetActive(false);
                    });
            }

            if (_messagePanel != null)
            {
                _messagePanel.SetActive(false);
            }

            if (_spotlightMask != null)
            {
                _spotlightMask.gameObject.SetActive(false);
            }
        }

        // =====================================================================
        // Temizlik
        // =====================================================================

        private void ClearEventListeners()
        {
            if (_productionListener != null && _eventManager != null)
            {
                _eventManager.Unsubscribe(_productionListener);
                _productionListener = null;
            }
        }

        private void OnDestroy()
        {
            ClearEventListeners();
        }

        // =====================================================================
        // Analytics
        // =====================================================================

        private void LogTutorialStep(int stepIndex)
        {
            if (ServiceLocator.TryGet<IAnalyticsManager>(out var analytics))
            {
                analytics.LogEvent("tutorial_step", new System.Collections.Generic.Dictionary<string, object>
                {
                    { "step_index", stepIndex },
                    { "step_message", Steps[stepIndex].Message.Substring(0, Mathf.Min(50, Steps[stepIndex].Message.Length)) }
                });
            }
        }
    }

    // =========================================================================
    // Tutorial Adim Veri Modeli
    // =========================================================================

    /// <summary>
    /// Tek bir tutorial adimini tanimlayan veri yapisi.
    /// </summary>
    [Serializable]
    public class TutorialStep
    {
        /// <summary>Adim numarasi (0'dan baslar).</summary>
        public int StepId;

        /// <summary>Oyuncuya gosterilecek mesaj.</summary>
        public string Message;

        /// <summary>Event tetiklenmesini mi bekleyecek (true) yoksa buton mu (false)?</summary>
        public bool WaitForEvent;

        /// <summary>Beklenen event tipi (WaitForEvent = true ise).</summary>
        public Type EventType;

        /// <summary>Vurgulanacak UI elemaninin adi (GameObject.Find ile bulunur). Null = spotlight yok.</summary>
        public string HighlightTarget;

        /// <summary>"Devam" butonu etiketi. Null = buton gizli (event bekliyor).</summary>
        public string NextButtonLabel;
    }
}
