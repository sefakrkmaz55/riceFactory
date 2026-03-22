using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RiceFactory.Core;

namespace RiceFactory.UI
{
    /// <summary>
    /// Ayarlar paneli.
    ///
    /// ART_GUIDE 4.6 Ayarlar Ekrani Layout'una uygun:
    /// - SES: Muzik (BGM) slider/toggle, Efektler (SFX) slider/toggle
    /// - Titresim (Haptic) toggle
    /// - Dil secimi (placeholder — dropdown)
    /// - Hesap baglama (Google/Apple — placeholder)
    /// - Versiyon numarasi
    /// - Destek/geri bildirim linki
    ///
    /// Tek elle kullanim: tum kontroller orta-alt bolgede.
    /// Toggle/slider minimum 44x44pt dokunma alani.
    /// </summary>
    public class SettingsPanel : PanelBase
    {
        // ---------------------------------------------------------------
        // SES Ayarlari
        // ---------------------------------------------------------------

        [Header("Ses — BGM (Muzik)")]
        [SerializeField] private Toggle _bgmToggle;
        [SerializeField] private Slider _bgmSlider;
        [SerializeField] private TextMeshProUGUI _bgmLabel;

        [Header("Ses — SFX (Efektler)")]
        [SerializeField] private Toggle _sfxToggle;
        [SerializeField] private Slider _sfxSlider;
        [SerializeField] private TextMeshProUGUI _sfxLabel;

        // ---------------------------------------------------------------
        // Haptic (Titresim)
        // ---------------------------------------------------------------

        [Header("Haptic")]
        [SerializeField] private Toggle _hapticToggle;
        [SerializeField] private TextMeshProUGUI _hapticLabel;

        // ---------------------------------------------------------------
        // Dil Secimi
        // ---------------------------------------------------------------

        [Header("Dil (Placeholder)")]
        [SerializeField] private TMP_Dropdown _languageDropdown;

        // ---------------------------------------------------------------
        // Hesap Baglama
        // ---------------------------------------------------------------

        [Header("Hesap Baglama (Placeholder)")]
        [SerializeField] private Button _googleLinkButton;
        [SerializeField] private Button _appleLinkButton;
        [SerializeField] private TextMeshProUGUI _accountStatusText;

        // ---------------------------------------------------------------
        // Bilgi
        // ---------------------------------------------------------------

        [Header("Bilgi")]
        [SerializeField] private TextMeshProUGUI _versionText;
        [SerializeField] private Button _supportButton;
        [SerializeField] private Button _privacyButton;
        [SerializeField] private Button _termsButton;

        // ---------------------------------------------------------------
        // Panel Kontrol
        // ---------------------------------------------------------------

        [Header("Panel Kontrol")]
        [SerializeField] private Button _closeButton;

        // ---------------------------------------------------------------
        // PanelBase overrides
        // ---------------------------------------------------------------

        protected override void OnInitialize()
        {
            // Listener'lar
            _bgmToggle?.onValueChanged.AddListener(OnBGMToggleChanged);
            _bgmSlider?.onValueChanged.AddListener(OnBGMVolumeChanged);

            _sfxToggle?.onValueChanged.AddListener(OnSFXToggleChanged);
            _sfxSlider?.onValueChanged.AddListener(OnSFXVolumeChanged);

            _hapticToggle?.onValueChanged.AddListener(OnHapticToggleChanged);

            _languageDropdown?.onValueChanged.AddListener(OnLanguageChanged);

            _googleLinkButton?.onClick.AddListener(OnGoogleLinkClicked);
            _appleLinkButton?.onClick.AddListener(OnAppleLinkClicked);

            _supportButton?.onClick.AddListener(OnSupportClicked);
            _privacyButton?.onClick.AddListener(OnPrivacyClicked);
            _termsButton?.onClick.AddListener(OnTermsClicked);
            _closeButton?.onClick.AddListener(OnCloseClicked);

            // Versiyon numarasi
            if (_versionText != null)
                _versionText.text = $"Versiyon: {Application.version} (Build {GetBuildNumber()})";

            // Dil dropdown'u doldur (placeholder)
            SetupLanguageDropdown();
        }

        protected override void OnShow()
        {
            LoadCurrentSettings();
        }

        private void OnDestroy()
        {
            _bgmToggle?.onValueChanged.RemoveAllListeners();
            _bgmSlider?.onValueChanged.RemoveAllListeners();
            _sfxToggle?.onValueChanged.RemoveAllListeners();
            _sfxSlider?.onValueChanged.RemoveAllListeners();
            _hapticToggle?.onValueChanged.RemoveAllListeners();
            _languageDropdown?.onValueChanged.RemoveAllListeners();
            _googleLinkButton?.onClick.RemoveAllListeners();
            _appleLinkButton?.onClick.RemoveAllListeners();
            _supportButton?.onClick.RemoveAllListeners();
            _privacyButton?.onClick.RemoveAllListeners();
            _termsButton?.onClick.RemoveAllListeners();
            _closeButton?.onClick.RemoveAllListeners();
        }

        // ---------------------------------------------------------------
        // Mevcut Ayarlari Yukle
        // ---------------------------------------------------------------

        /// <summary>
        /// Kayitli ayarlar PlayerPrefs veya SaveManager uzerinden yuklenip UI'ya yansitilir.
        /// AudioManager'dan mevcut ses durumunu al.
        /// </summary>
        private void LoadCurrentSettings()
        {
            var audioManager = ServiceLocator.Get<IAudioManager>();

            // BGM
            if (_bgmToggle != null && audioManager != null)
                _bgmToggle.isOn = audioManager.IsBGMEnabled;

            if (_bgmSlider != null && audioManager != null)
                _bgmSlider.value = audioManager.BGMVolume;

            // SFX
            if (_sfxToggle != null && audioManager != null)
                _sfxToggle.isOn = audioManager.IsSFXEnabled;

            if (_sfxSlider != null && audioManager != null)
                _sfxSlider.value = audioManager.SFXVolume;

            // Haptic
            if (_hapticToggle != null)
                _hapticToggle.isOn = PlayerPrefs.GetInt("haptic_enabled", 1) == 1;

            // Hesap durumu
            RefreshAccountStatus();
        }

        // ---------------------------------------------------------------
        // SES — BGM
        // ---------------------------------------------------------------

        private void OnBGMToggleChanged(bool isOn)
        {
            var audioManager = ServiceLocator.Get<IAudioManager>();
            if (audioManager != null)
            {
                audioManager.SetBGMEnabled(isOn);
            }

            // Slider'i toggle durumuna gore aktif/pasif yap
            if (_bgmSlider != null)
                _bgmSlider.interactable = isOn;
        }

        private void OnBGMVolumeChanged(float volume)
        {
            var audioManager = ServiceLocator.Get<IAudioManager>();
            audioManager?.SetBGMVolume(volume);
        }

        // ---------------------------------------------------------------
        // SES — SFX
        // ---------------------------------------------------------------

        private void OnSFXToggleChanged(bool isOn)
        {
            var audioManager = ServiceLocator.Get<IAudioManager>();
            if (audioManager != null)
            {
                audioManager.SetSFXEnabled(isOn);
                // Acildiginda ornek ses cal
                if (isOn)
                    audioManager.PlaySFX("ui_toggle");
            }

            if (_sfxSlider != null)
                _sfxSlider.interactable = isOn;
        }

        private void OnSFXVolumeChanged(float volume)
        {
            var audioManager = ServiceLocator.Get<IAudioManager>();
            audioManager?.SetSFXVolume(volume);
        }

        // ---------------------------------------------------------------
        // HAPTIC
        // ---------------------------------------------------------------

        private void OnHapticToggleChanged(bool isOn)
        {
            PlayerPrefs.SetInt("haptic_enabled", isOn ? 1 : 0);
            PlayerPrefs.Save();

            // Haptic Manager varsa bildir
            // HapticManager.SetEnabled(isOn);
        }

        // ---------------------------------------------------------------
        // DIL (Placeholder)
        // ---------------------------------------------------------------

        private void SetupLanguageDropdown()
        {
            if (_languageDropdown == null) return;

            _languageDropdown.ClearOptions();
            _languageDropdown.AddOptions(new System.Collections.Generic.List<string>
            {
                "Turkce",
                "English",
                "Deutsch",
                "Espanol",
                "Francais",
                "Japonca"
            });

            // Mevcut dili sec
            int savedLang = PlayerPrefs.GetInt("language_index", 0);
            _languageDropdown.value = savedLang;
        }

        private void OnLanguageChanged(int index)
        {
            PlayerPrefs.SetInt("language_index", index);
            PlayerPrefs.Save();

            // TODO: Localization sistemi entegrasyonu
            Debug.Log($"[SettingsPanel] Dil degistirildi: index={index}");
        }

        // ---------------------------------------------------------------
        // HESAP BAGLAMA (Placeholder)
        // ---------------------------------------------------------------

        private void OnGoogleLinkClicked()
        {
            // TODO: Google Play Games / Firebase Auth entegrasyonu
            Debug.Log("[SettingsPanel] Google hesap baglama — placeholder");
            RefreshAccountStatus();
        }

        private void OnAppleLinkClicked()
        {
            // TODO: Apple Sign-In entegrasyonu
            Debug.Log("[SettingsPanel] Apple hesap baglama — placeholder");
            RefreshAccountStatus();
        }

        private void RefreshAccountStatus()
        {
            if (_accountStatusText == null) return;

            // TODO: Gercek hesap durumunu kontrol et
            bool isLinked = PlayerPrefs.GetInt("account_linked", 0) == 1;
            _accountStatusText.text = isLinked
                ? "Hesap bagli"
                : "Hesap baglanmadi";
        }

        // ---------------------------------------------------------------
        // DESTEK / LINKLER
        // ---------------------------------------------------------------

        private void OnSupportClicked()
        {
            Application.OpenURL("https://ricefactory.game/support");
        }

        private void OnPrivacyClicked()
        {
            Application.OpenURL("https://ricefactory.game/privacy");
        }

        private void OnTermsClicked()
        {
            Application.OpenURL("https://ricefactory.game/terms");
        }

        // ---------------------------------------------------------------
        // PANEL KONTROL
        // ---------------------------------------------------------------

        private void OnCloseClicked()
        {
            UIManager.Instance?.CloseTopPanel();
        }

        // ---------------------------------------------------------------
        // Yardimci
        // ---------------------------------------------------------------

        /// <summary>Build numarasini dondurur (placeholder).</summary>
        private static string GetBuildNumber()
        {
            // TODO: CI/CD pipeline'dan enjekte edilen build numarasini oku
            return "1";
        }
    }
}
