using System;
using System.Collections.Generic;
using UnityEngine;
using RiceFactory.Core;

namespace RiceFactory.UI
{
    /// <summary>
    /// Singleton UIManager — panel stack sistemi ile panel acma/kapama/geri islemlerini yonetir.
    /// Popup'lar stack'ten bagimsiz calısır ve modal olarak gosterilir.
    /// Mimari: TECH_ARCHITECTURE.md 3.6 UIManager bolumune uygun.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Containers")]
        [SerializeField] private Transform _panelContainer;
        [SerializeField] private Transform _popupContainer;

        [Header("Transition Settings")]
        [SerializeField] private float _fadeDuration = 0.2f;
        [SerializeField] private float _fadeOutDuration = 0.15f;

        // Panel stack — en ustteki aktif panel
        private readonly Stack<PanelBase> _panelStack = new();

        // Onceden olusturulmus panellerin cache'i (tip -> instance)
        private readonly Dictionary<Type, PanelBase> _panelRegistry = new();

        // Aktif popup referansi (ayni anda tek popup desteklenir)
        private PopupBase _activePopup;

        // ---------------------------------------------------------------
        // Lifecycle
        // ---------------------------------------------------------------

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

        private void Update()
        {
            // Android geri butonu veya ESC
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                OnBackPressed();
            }
        }

        // ---------------------------------------------------------------
        // Panel Yonetimi
        // ---------------------------------------------------------------

        /// <summary>
        /// Belirtilen tipteki paneli acar.
        /// Mevcut ust panel gizlenir ve yeni panel stack'e eklenir.
        /// </summary>
        public T OpenPanel<T>() where T : PanelBase
        {
            var panel = GetOrCreatePanel<T>();

            // Mevcut ust paneli gizle
            if (_panelStack.Count > 0)
            {
                var current = _panelStack.Peek();
                current.Hide();
            }

            _panelStack.Push(panel);
            panel.Show();
            return panel;
        }

        /// <summary>
        /// Ust paneli kapatir ve altindaki paneli tekrar gosterir.
        /// Ana panel (stack'teki ilk panel) kapatılamaz.
        /// </summary>
        public void CloseTopPanel()
        {
            if (_panelStack.Count <= 1) return; // Ana panel kapanamaz

            var top = _panelStack.Pop();
            top.Hide();

            if (_panelStack.Count > 0)
            {
                _panelStack.Peek().Show();
            }
        }

        /// <summary>
        /// Stack'teki tum panelleri kapatir ve sadece ana panele doner.
        /// </summary>
        public void PopToRoot()
        {
            while (_panelStack.Count > 1)
            {
                var top = _panelStack.Pop();
                top.Hide();
            }

            if (_panelStack.Count > 0)
            {
                _panelStack.Peek().Show();
            }
        }

        /// <summary>
        /// Belirtilen tipteki panelin halihazirda acik olup olmadigini kontrol eder.
        /// </summary>
        public bool IsPanelOpen<T>() where T : PanelBase
        {
            if (_panelStack.Count == 0) return false;
            return _panelStack.Peek() is T;
        }

        // ---------------------------------------------------------------
        // Popup Yonetimi (modal, stack'ten bagimsiz)
        // ---------------------------------------------------------------

        /// <summary>
        /// Popup gosterir. Stack'i etkilemez, mevcut panelin uzerine biner.
        /// Varolan popup otomatik kapatilir (ayni anda tek popup).
        /// </summary>
        public T ShowPopup<T>(object data = null) where T : PopupBase
        {
            // Mevcut popup varsa kapat
            if (_activePopup != null)
            {
                _activePopup.Close();
                _activePopup = null;
            }

            var popup = GetOrCreatePopup<T>();
            popup.Initialize(data);
            popup.Show();
            _activePopup = popup;

            // Popup kapandiginda referansi temizle
            popup.OnClosed += () => {
                if (_activePopup == popup) _activePopup = null;
            };

            return popup;
        }

        /// <summary>
        /// Aktif popup'i kapatir.
        /// </summary>
        public void ClosePopup()
        {
            if (_activePopup != null)
            {
                _activePopup.Close();
                _activePopup = null;
            }
        }

        /// <summary>
        /// Aktif bir popup olup olmadigini kontrol eder.
        /// </summary>
        public bool HasActivePopup => _activePopup != null;

        // ---------------------------------------------------------------
        // Geri Butonu (Android ESC / fiziksel geri)
        // ---------------------------------------------------------------

        /// <summary>
        /// Geri butonu mantigi: once popup kapatilir, sonra ust panel.
        /// </summary>
        public void OnBackPressed()
        {
            // Oncelikle aktif popup var mi?
            if (_activePopup != null)
            {
                ClosePopup();
                return;
            }

            CloseTopPanel();
        }

        // ---------------------------------------------------------------
        // Factory — Panel / Popup olusturma
        // ---------------------------------------------------------------

        private T GetOrCreatePanel<T>() where T : PanelBase
        {
            var type = typeof(T);

            if (_panelRegistry.TryGetValue(type, out var existing))
                return (T)existing;

            // Resources/UI/Panels altindan prefab yukle
            // Uretim ortaminda Addressables ile degistirilebilir
            var prefabName = $"UI/Panels/{type.Name}";
            var prefab = Resources.Load<T>(prefabName);

            if (prefab == null)
            {
                Debug.LogError($"[UIManager] Panel prefab bulunamadi: {prefabName}");
                return null;
            }

            var instance = Instantiate(prefab, _panelContainer);
            _panelRegistry[type] = instance;
            instance.Initialize();
            return instance;
        }

        private T GetOrCreatePopup<T>() where T : PopupBase
        {
            var type = typeof(T);
            var prefabName = $"UI/Popups/{type.Name}";
            var prefab = Resources.Load<T>(prefabName);

            if (prefab == null)
            {
                Debug.LogError($"[UIManager] Popup prefab bulunamadi: {prefabName}");
                return null;
            }

            // Popup'lar her seferinde yeniden olusturulur (kapaninca destroy)
            var instance = Instantiate(prefab, _popupContainer);
            return instance;
        }
    }

    // ===================================================================
    // PanelBase — Tum panellerin temel sinifi
    // ===================================================================

    /// <summary>
    /// Panel base class. CanvasGroup uzerinden fade in/out animasyonu saglar.
    /// Her panel bu siniftan turetilir.
    /// </summary>
    public abstract class PanelBase : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;

        private bool _isInitialized;

        /// <summary>Panel ilk olusturuldugunda bir kez cagirilir.</summary>
        public void Initialize()
        {
            if (_isInitialized) return;
            _isInitialized = true;
            OnInitialize();
        }

        /// <summary>Paneli gorunur yapar (fade in).</summary>
        public void Show()
        {
            gameObject.SetActive(true);

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                SimpleTween.DOFade(_canvasGroup, 1f, 0.2f, SimpleTween.Ease.OutQuad);
                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;
            }

            OnShow();
        }

        /// <summary>Paneli gizler (fade out, sonra deaktif).</summary>
        public void Hide()
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
                SimpleTween.DOFade(_canvasGroup, 0f, 0.15f, SimpleTween.Ease.InQuad,
                    onComplete: () => gameObject.SetActive(false));
            }
            else
            {
                gameObject.SetActive(false);
            }

            OnHide();
        }

        /// <summary>Panel ilk olusturuldugunda — referanslari bagla.</summary>
        protected abstract void OnInitialize();

        /// <summary>Panel her gosterildiginde.</summary>
        protected virtual void OnShow() { }

        /// <summary>Panel her gizlendiginde.</summary>
        protected virtual void OnHide() { }
    }

    // ===================================================================
    // PopupBase — Tum popup'larin temel sinifi
    // ===================================================================

    /// <summary>
    /// Popup base class. Modal olarak gosterilir, scale+fade animasyonuyla acilir.
    /// Kapandiginda destroy edilir.
    /// </summary>
    public abstract class PopupBase : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _popupBody;

        /// <summary>Popup kapandiginda tetiklenir (UIManager referans temizligi icin).</summary>
        public event Action OnClosed;

        /// <summary>Popup verilerini hazirlar.</summary>
        public void Initialize(object data) => OnInitialize(data);

        /// <summary>Popup'i gosterir — scale+fade animasyonu.</summary>
        public void Show()
        {
            gameObject.SetActive(true);

            if (_canvasGroup != null)
                _canvasGroup.alpha = 0f;

            if (_popupBody != null)
                _popupBody.localScale = Vector3.one * 0.8f;

            // Paralel: fade + scale ayni anda
            if (_canvasGroup != null)
                SimpleTween.DOFade(_canvasGroup, 1f, 0.2f);

            if (_popupBody != null)
                SimpleTween.DOScale(_popupBody, 1f, 0.25f, SimpleTween.Ease.OutBack);
        }

        /// <summary>Popup'i kapatir — animasyon sonrasi destroy.</summary>
        public void Close()
        {
            // Paralel: fade + scale ayni anda, en uzun sure sonunda destroy
            if (_canvasGroup != null)
                SimpleTween.DOFade(_canvasGroup, 0f, 0.15f);

            if (_popupBody != null)
                SimpleTween.DOScale(_popupBody, 0.8f, 0.15f);

            // 0.15s sonra destroy (en uzun animasyon suresi)
            SimpleTween.DelayedCall(0.15f, () =>
            {
                OnClosed?.Invoke();
                Destroy(gameObject);
            });
        }

        /// <summary>Popup verileri ile baslatma — alt siniflar override eder.</summary>
        protected abstract void OnInitialize(object data);
    }
}
