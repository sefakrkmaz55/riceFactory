using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RiceFactory.Core;
using RiceFactory.Core.Events;

namespace RiceFactory.UI
{
    /// <summary>
    /// Upgrade paneli — bottom sheet stili, asagidan yukari kayarak acilir.
    /// Secili tesisin makinelerini, calisanlarini ve yildizlarini upgrade etmeyi saglar.
    ///
    /// ART_GUIDE 4.3 Upgrade Panel Layout'una uygun:
    /// - Tesis adi + yildiz seviyesi
    /// - Makine upgrade (maliyet + mevcut/sonraki seviye)
    /// - Calisan upgrade
    /// - Yildiz upgrade
    /// - CanAfford kontrolu ile buton aktif/pasif durumu
    ///
    /// Upgrade butonlari: yesil (#4CAF50) aktif, gri (#E0E0E0) pasif.
    /// Minimum dokunma alani: 44x44pt (Apple HIG).
    /// </summary>
    public class UpgradePanel : PanelBase
    {
        // ---------------------------------------------------------------
        // Tesis Bilgisi
        // ---------------------------------------------------------------

        [Header("Tesis Bilgisi")]
        [SerializeField] private TextMeshProUGUI _facilityNameText;
        [SerializeField] private TextMeshProUGUI _starRatingText;
        [SerializeField] private Image _facilityIcon;

        // ---------------------------------------------------------------
        // Makine Upgrade
        // ---------------------------------------------------------------

        [Header("Makine Upgrade")]
        [SerializeField] private TextMeshProUGUI _machineLevelText;
        [SerializeField] private TextMeshProUGUI _machineStatsText;
        [SerializeField] private TextMeshProUGUI _machineCostText;
        [SerializeField] private Button _machineUpgradeButton;
        [SerializeField] private Image _machineUpgradeButtonBg;

        // ---------------------------------------------------------------
        // Calisan Upgrade
        // ---------------------------------------------------------------

        [Header("Calisan Upgrade")]
        [SerializeField] private TextMeshProUGUI _workerLevelText;
        [SerializeField] private TextMeshProUGUI _workerStatsText;
        [SerializeField] private TextMeshProUGUI _workerCostText;
        [SerializeField] private Button _workerUpgradeButton;
        [SerializeField] private Image _workerUpgradeButtonBg;

        // ---------------------------------------------------------------
        // Yildiz Upgrade
        // ---------------------------------------------------------------

        [Header("Yildiz Upgrade")]
        [SerializeField] private TextMeshProUGUI _starLevelText;
        [SerializeField] private TextMeshProUGUI _starRequirementText;
        [SerializeField] private TextMeshProUGUI _starCostText;
        [SerializeField] private Button _starUpgradeButton;
        [SerializeField] private Image _starUpgradeButtonBg;

        // ---------------------------------------------------------------
        // Kapat Butonu
        // ---------------------------------------------------------------

        [Header("Panel Kontrol")]
        [SerializeField] private Button _closeButton;

        // ---------------------------------------------------------------
        // Renk sabitleri (ART_GUIDE'dan)
        // ---------------------------------------------------------------

        private static readonly Color COLOR_AFFORDABLE = new Color32(0x4C, 0xAF, 0x50, 0xFF);   // #4CAF50 yesil
        private static readonly Color COLOR_UNAFFORDABLE = new Color32(0xE0, 0xE0, 0xE0, 0xFF); // #E0E0E0 gri
        private static readonly Color COLOR_COST_WARNING = new Color32(0xF4, 0x43, 0x36, 0xFF); // #F44336 kirmizi

        // ---------------------------------------------------------------
        // State
        // ---------------------------------------------------------------

        private string _selectedFacilityId;
        private IEventManager _eventManager;
        private IUpgradeSystem _upgradeSystem;
        private IEconomySystem _economySystem;

        // ---------------------------------------------------------------
        // PanelBase overrides
        // ---------------------------------------------------------------

        protected override void OnInitialize()
        {
            _eventManager = ServiceLocator.Get<IEventManager>();
            _upgradeSystem = ServiceLocator.Get<IUpgradeSystem>();
            _economySystem = ServiceLocator.Get<IEconomySystem>();

            // Event abonelikleri
            _eventManager.Subscribe<CurrencyChangedEvent>(OnCurrencyChanged);
            _eventManager.Subscribe<UpgradeCompletedEvent>(OnUpgradeCompleted);

            // Buton listener'lari
            _machineUpgradeButton?.onClick.AddListener(OnMachineUpgradeClicked);
            _workerUpgradeButton?.onClick.AddListener(OnWorkerUpgradeClicked);
            _starUpgradeButton?.onClick.AddListener(OnStarUpgradeClicked);
            _closeButton?.onClick.AddListener(OnCloseClicked);
        }

        protected override void OnShow()
        {
            RefreshAll();
        }

        private void OnDestroy()
        {
            if (_eventManager != null)
            {
                _eventManager.Unsubscribe<CurrencyChangedEvent>(OnCurrencyChanged);
                _eventManager.Unsubscribe<UpgradeCompletedEvent>(OnUpgradeCompleted);
            }

            _machineUpgradeButton?.onClick.RemoveAllListeners();
            _workerUpgradeButton?.onClick.RemoveAllListeners();
            _starUpgradeButton?.onClick.RemoveAllListeners();
            _closeButton?.onClick.RemoveAllListeners();
        }

        // ---------------------------------------------------------------
        // Public — Secili tesis ayarlama
        // ---------------------------------------------------------------

        /// <summary>
        /// Paneli acmadan once hangi tesisin upgrade edilecegini belirler.
        /// </summary>
        public void SetFacility(string facilityId)
        {
            _selectedFacilityId = facilityId;
            RefreshAll();
        }

        // ---------------------------------------------------------------
        // UI Guncelleme
        // ---------------------------------------------------------------

        /// <summary>Tum upgrade bilgilerini guncel veriden tazeler.</summary>
        private void RefreshAll()
        {
            if (string.IsNullOrEmpty(_selectedFacilityId)) return;

            var saveManager = ServiceLocator.Get<ISaveManager>();
            if (saveManager?.Data == null) return;

            RefreshFacilityInfo(saveManager);
            RefreshMachineUpgrade(saveManager);
            RefreshWorkerUpgrade(saveManager);
            RefreshStarUpgrade(saveManager);
        }

        private void RefreshFacilityInfo(ISaveManager saveManager)
        {
            var facility = saveManager.Data.GetFacility(_selectedFacilityId);
            if (facility == null) return;

            if (_facilityNameText != null)
                _facilityNameText.text = facility.DisplayName;

            if (_starRatingText != null)
            {
                // Yildiz gosterimi: dolu yildizlar + bos yildizlar (max 5)
                string stars = new string('\u2605', facility.StarLevel)  // ★ dolu
                             + new string('\u2606', 5 - facility.StarLevel); // ☆ bos
                _starRatingText.text = $"{stars} ({facility.StarLevel}/5)";
            }
        }

        private void RefreshMachineUpgrade(ISaveManager saveManager)
        {
            var facility = saveManager.Data.GetFacility(_selectedFacilityId);
            if (facility == null) return;

            int currentLevel = facility.MachineLevel;
            int nextLevel = currentLevel + 1;
            double cost = _upgradeSystem.GetUpgradeCost(UpgradeType.Machine, _selectedFacilityId, nextLevel);
            bool canAfford = _economySystem.CanAfford(CurrencyType.Coin, cost);

            if (_machineLevelText != null)
                _machineLevelText.text = $"Lv.{currentLevel} \u25b6 Lv.{nextLevel}"; // ▶

            if (_machineStatsText != null)
            {
                float currentSpeed = _upgradeSystem.GetMachineSpeed(_selectedFacilityId, currentLevel);
                float nextSpeed = _upgradeSystem.GetMachineSpeed(_selectedFacilityId, nextLevel);
                _machineStatsText.text = $"Hiz: x{currentSpeed:F1} \u25b6 x{nextSpeed:F1}";
            }

            if (_machineCostText != null)
                _machineCostText.text = AnimatedCounter.FormatNumber(cost);

            UpdateUpgradeButton(_machineUpgradeButton, _machineUpgradeButtonBg, canAfford);
        }

        private void RefreshWorkerUpgrade(ISaveManager saveManager)
        {
            var facility = saveManager.Data.GetFacility(_selectedFacilityId);
            if (facility == null) return;

            int currentLevel = facility.WorkerLevel;
            int nextLevel = currentLevel + 1;
            double cost = _upgradeSystem.GetUpgradeCost(UpgradeType.Worker, _selectedFacilityId, nextLevel);
            bool canAfford = _economySystem.CanAfford(CurrencyType.Coin, cost);

            if (_workerLevelText != null)
                _workerLevelText.text = $"Lv.{currentLevel} \u25b6 Lv.{nextLevel}";

            if (_workerStatsText != null)
            {
                float currentEff = _upgradeSystem.GetWorkerEfficiency(_selectedFacilityId, currentLevel);
                float nextEff = _upgradeSystem.GetWorkerEfficiency(_selectedFacilityId, nextLevel);
                _workerStatsText.text = $"Verim: %{currentEff * 100:F0} \u25b6 %{nextEff * 100:F0}";
            }

            if (_workerCostText != null)
                _workerCostText.text = AnimatedCounter.FormatNumber(cost);

            UpdateUpgradeButton(_workerUpgradeButton, _workerUpgradeButtonBg, canAfford);
        }

        private void RefreshStarUpgrade(ISaveManager saveManager)
        {
            var facility = saveManager.Data.GetFacility(_selectedFacilityId);
            if (facility == null) return;

            int currentStar = facility.StarLevel;

            if (currentStar >= 5)
            {
                // Maksimum yildiza ulasildi
                if (_starLevelText != null)
                    _starLevelText.text = "\u2605\u2605\u2605\u2605\u2605 MAX";
                if (_starRequirementText != null)
                    _starRequirementText.text = "Maksimum seviye!";

                SetButtonEnabled(_starUpgradeButton, _starUpgradeButtonBg, false);
                return;
            }

            int nextStar = currentStar + 1;
            double cost = _upgradeSystem.GetUpgradeCost(UpgradeType.Star, _selectedFacilityId, nextStar);
            bool canAfford = _economySystem.CanAfford(CurrencyType.Coin, cost);
            bool meetsRequirements = _upgradeSystem.MeetsStarRequirements(_selectedFacilityId, nextStar);

            if (_starLevelText != null)
                _starLevelText.text = $"\u2605{currentStar} \u25b6 \u2605{nextStar}";

            if (_starRequirementText != null)
            {
                if (!meetsRequirements)
                    _starRequirementText.text = _upgradeSystem.GetStarRequirementDescription(_selectedFacilityId, nextStar);
                else
                    _starRequirementText.text = "Gereksinimler karsilandi!";
            }

            if (_starCostText != null)
                _starCostText.text = AnimatedCounter.FormatNumber(cost);

            bool canUpgrade = canAfford && meetsRequirements;
            UpdateUpgradeButton(_starUpgradeButton, _starUpgradeButtonBg, canUpgrade);
        }

        // ---------------------------------------------------------------
        // Buton Durum Guncelleme
        // ---------------------------------------------------------------

        /// <summary>
        /// Upgrade butonunun rengini ve etkilesim durumunu CanAfford'a gore ayarlar.
        /// Yesil = satin alinabilir, gri = yetersiz kaynak.
        /// </summary>
        private void UpdateUpgradeButton(Button button, Image background, bool canAfford)
        {
            SetButtonEnabled(button, background, canAfford);
        }

        private void SetButtonEnabled(Button button, Image background, bool enabled)
        {
            if (button != null)
                button.interactable = enabled;

            if (background != null)
                background.color = enabled ? COLOR_AFFORDABLE : COLOR_UNAFFORDABLE;
        }

        // ---------------------------------------------------------------
        // Buton Aksiyonlari
        // ---------------------------------------------------------------

        private void OnMachineUpgradeClicked()
        {
            if (string.IsNullOrEmpty(_selectedFacilityId)) return;

            bool success = _upgradeSystem.TryUpgrade(UpgradeType.Machine, _selectedFacilityId);
            if (success)
            {
                PlayUpgradeAnimation(_machineUpgradeButton?.transform);
            }

            RefreshAll();
        }

        private void OnWorkerUpgradeClicked()
        {
            if (string.IsNullOrEmpty(_selectedFacilityId)) return;

            bool success = _upgradeSystem.TryUpgrade(UpgradeType.Worker, _selectedFacilityId);
            if (success)
            {
                PlayUpgradeAnimation(_workerUpgradeButton?.transform);
            }

            RefreshAll();
        }

        private void OnStarUpgradeClicked()
        {
            if (string.IsNullOrEmpty(_selectedFacilityId)) return;

            bool success = _upgradeSystem.TryUpgrade(UpgradeType.Star, _selectedFacilityId);
            if (success)
            {
                PlayUpgradeAnimation(_starUpgradeButton?.transform);
            }

            RefreshAll();
        }

        private void OnCloseClicked()
        {
            UIManager.Instance?.CloseTopPanel();
        }

        // ---------------------------------------------------------------
        // Upgrade Animasyonu
        // ---------------------------------------------------------------

        /// <summary>
        /// Upgrade basarili oldugunda buton uzerinde bounce animasyonu tetikler.
        /// ART_GUIDE 5.2: Scale 1.0 -> 1.2 -> 1.0 (ease-out-back, 0.4s)
        /// </summary>
        private void PlayUpgradeAnimation(Transform target)
        {
            if (target == null) return;

            SimpleTween.DOScale(target, 1.2f, 0.2f, SimpleTween.Ease.OutBack,
                onComplete: () =>
                {
                    SimpleTween.DOScale(target, 1f, 0.2f, SimpleTween.Ease.InOutQuad);
                });
        }

        // ---------------------------------------------------------------
        // Event Handler'lar
        // ---------------------------------------------------------------

        /// <summary>Para degistiginde buton durumlarini guncelle.</summary>
        private void OnCurrencyChanged(CurrencyChangedEvent e)
        {
            if (e.Type == CurrencyType.Coin)
            {
                RefreshAll();
            }
        }

        /// <summary>Herhangi bir upgrade tamamlandiginda paneli tazele.</summary>
        private void OnUpgradeCompleted(UpgradeCompletedEvent e)
        {
            if (e.TargetId == _selectedFacilityId)
            {
                RefreshAll();
            }
        }
    }
}
