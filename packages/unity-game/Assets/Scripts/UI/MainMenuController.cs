// =============================================================================
// MainMenuController.cs
// Ana menu sahnesi scripti. "Oyna", "Devam Et", "Ayarlar" butonlari,
// oyuncu bilgisi ve versiyon gosterimi.
// MainMenu sahnesindeki bir GameObject'e eklenir.
// =============================================================================

using UnityEngine;
using UnityEngine.UI;
using RiceFactory.Core;

namespace RiceFactory.UI
{
    /// <summary>
    /// Ana menu sahnesi controller'i.
    /// - "Oyna" butonu: yeni oyun baslatir, Game sahnesine gecer.
    /// - "Devam Et" butonu: kayitli save varsa gorunur, Game sahnesine gecer.
    /// - "Ayarlar" butonu: SettingsPanel acar.
    /// - Oyuncu adi/seviye ve versiyon numarasi gosterir.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        // =====================================================================
        // Inspector Referanslari
        // =====================================================================

        [Header("Butonlar")]
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _continueButton;
        [SerializeField] private Button _settingsButton;

        [Header("Bilgi Gosterimi")]
        [SerializeField] private TMPro.TMP_Text _versionText;
        [SerializeField] private TMPro.TMP_Text _playerNameText;
        [SerializeField] private TMPro.TMP_Text _playerLevelText;

        [Header("Continue Butonu Gorsellik")]
        [SerializeField] private GameObject _continueButtonContainer;

        // =====================================================================
        // Dahili Referanslar
        // =====================================================================

        private ISaveManager _saveManager;
        private IGameManager _gameManager;

        // =====================================================================
        // Unity Lifecycle
        // =====================================================================

        private void Start()
        {
            // ServiceLocator'dan servisleri al
            ServiceLocator.TryGet(out _saveManager);
            ServiceLocator.TryGet(out _gameManager);

            SetupUI();
            BindButtons();
        }

        private void OnDestroy()
        {
            UnbindButtons();
        }

        // =====================================================================
        // UI Kurulum
        // =====================================================================

        /// <summary>
        /// UI elemanlarini mevcut oyun verisiyle doldurur.
        /// </summary>
        private void SetupUI()
        {
            // Versiyon numarasi
            if (_versionText != null)
            {
                _versionText.text = $"v{Application.version}";
            }

            // Oyuncu bilgisi
            bool hasSave = HasExistingSave();

            if (hasSave && _saveManager != null)
            {
                var data = _saveManager.Data;

                if (_playerNameText != null)
                {
                    string name = string.IsNullOrEmpty(data.PlayerName) ? "Oyuncu" : data.PlayerName;
                    _playerNameText.text = name;
                }

                if (_playerLevelText != null)
                {
                    _playerLevelText.text = $"Seviye {data.PlayerLevel}";
                }
            }
            else
            {
                if (_playerNameText != null)
                    _playerNameText.text = "";

                if (_playerLevelText != null)
                    _playerLevelText.text = "";
            }

            // "Devam Et" butonunu sadece kayit varsa goster
            if (_continueButtonContainer != null)
            {
                _continueButtonContainer.SetActive(hasSave);
            }
            else if (_continueButton != null)
            {
                _continueButton.gameObject.SetActive(hasSave);
            }

            Debug.Log($"[MainMenuController] UI kuruldu. Save mevcut: {hasSave}");
        }

        // =====================================================================
        // Buton Baglama
        // =====================================================================

        private void BindButtons()
        {
            if (_playButton != null)
                _playButton.onClick.AddListener(OnPlayClicked);

            if (_continueButton != null)
                _continueButton.onClick.AddListener(OnContinueClicked);

            if (_settingsButton != null)
                _settingsButton.onClick.AddListener(OnSettingsClicked);
        }

        private void UnbindButtons()
        {
            if (_playButton != null)
                _playButton.onClick.RemoveListener(OnPlayClicked);

            if (_continueButton != null)
                _continueButton.onClick.RemoveListener(OnContinueClicked);

            if (_settingsButton != null)
                _settingsButton.onClick.RemoveListener(OnSettingsClicked);
        }

        // =====================================================================
        // Buton Handler'lar
        // =====================================================================

        /// <summary>
        /// "Oyna" butonu: yeni oyun. Save sifirlanmaz, Game sahnesine gecilir.
        /// </summary>
        private void OnPlayClicked()
        {
            Debug.Log("[MainMenuController] Oyna butonuna basildi.");

            if (_gameManager != null)
            {
                _gameManager.ChangeState(GameState.Playing);
            }

            SceneController.LoadSceneWithLoading(SceneController.SCENE_GAME);
        }

        /// <summary>
        /// "Devam Et" butonu: mevcut kayitta kaldigi yerden devam eder.
        /// </summary>
        private void OnContinueClicked()
        {
            Debug.Log("[MainMenuController] Devam Et butonuna basildi.");

            if (_gameManager != null)
            {
                _gameManager.ChangeState(GameState.Playing);
            }

            SceneController.LoadSceneWithLoading(SceneController.SCENE_GAME);
        }

        /// <summary>
        /// "Ayarlar" butonu: SettingsPanel acar.
        /// </summary>
        private void OnSettingsClicked()
        {
            Debug.Log("[MainMenuController] Ayarlar butonuna basildi.");

            if (_gameManager != null)
            {
                _gameManager.ChangeState(GameState.Settings);
            }

            // UIManager uzerinden SettingsPanel ac
            // Not: SettingsPanel prefab'i Resources/UI/Panels/ altinda olmali
            if (UIManager.Instance != null)
            {
                // SettingsPanel PanelBase'den turedigi icin generic OpenPanel kullanilir.
                // SettingsPanel henuz tanimlanmadiysa, loglayip devam ederiz.
                Debug.Log("[MainMenuController] SettingsPanel aciliyor...");
                // UIManager.Instance.OpenPanel<SettingsPanel>();
                // NOT: SettingsPanel sinifi henuz mevcut degil. Eklendiginde yukaridaki satir aktif edilecek.
            }
        }

        // =====================================================================
        // Yardimci
        // =====================================================================

        /// <summary>
        /// Mevcut bir kayit dosyasi olup olmadigini kontrol eder.
        /// SaveVersion > 1 ise en az bir kez kaydedilmis demektir.
        /// </summary>
        private bool HasExistingSave()
        {
            if (_saveManager == null || _saveManager.Data == null)
                return false;

            return _saveManager.Data.SaveVersion > 1;
        }
    }
}
