// =============================================================================
// ResearchPanel.cs
// Arastirma agaci UI paneli — 4 dal gosterimi ve arastirma baslatma.
//
// Referans: docs/GDD.md Bolum 3.4 — Arastirma Agaci
// Referans: PrestigePanel.cs — UI pattern ornegi
// =============================================================================

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RiceFactory.Core;
using RiceFactory.Core.Events;
using RiceFactory.Economy;

namespace RiceFactory.UI
{
    /// <summary>
    /// Arastirma agaci paneli.
    /// 4 arastirma dali gosterimi: Otomasyon, Kalite, Hiz, Kapasite.
    /// Her dal icin: mevcut seviye, sonraki seviye maliyeti, etki aciklamasi.
    /// Arastirma baslatma butonu ve CanAfford kontrolu.
    /// </summary>
    public class ResearchPanel : PanelBase
    {
        // ---------------------------------------------------------------
        // Baslik
        // ---------------------------------------------------------------

        [Header("Baslik")]
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _coinsText;

        // ---------------------------------------------------------------
        // Aktif Arastirma Gosterimi
        // ---------------------------------------------------------------

        [Header("Aktif Arastirma")]
        [SerializeField] private GameObject _activeResearchContainer;
        [SerializeField] private TextMeshProUGUI _activeResearchNameText;
        [SerializeField] private Slider _activeResearchProgressBar;
        [SerializeField] private TextMeshProUGUI _activeResearchTimeText;

        // ---------------------------------------------------------------
        // Dal Listesi
        // ---------------------------------------------------------------

        [Header("Arastirma Dallari")]
        [SerializeField] private Transform _branchListContainer;
        [SerializeField] private GameObject _branchItemPrefab;

        // ---------------------------------------------------------------
        // Geri Butonu
        // ---------------------------------------------------------------

        [Header("Butonlar")]
        [SerializeField] private Button _backButton;

        // ---------------------------------------------------------------
        // State
        // ---------------------------------------------------------------

        private IEventManager _eventManager;
        private IEconomySystem _economySystem;
        private ResearchSystem _researchSystem;

        /// <summary>Olusturulan dal item'larinin referanslari (guncelleme icin).</summary>
        private readonly List<ResearchBranchItemRefs> _branchItems = new();

        // ---------------------------------------------------------------
        // PanelBase overrides
        // ---------------------------------------------------------------

        protected override void OnInitialize()
        {
            _eventManager = ServiceLocator.Get<IEventManager>();
            _economySystem = ServiceLocator.Get<IEconomySystem>();

            // ResearchSystem'i ServiceLocator'dan al
            if (ServiceLocator.TryGet<ResearchSystem>(out var rs))
            {
                _researchSystem = rs;
            }

            _backButton?.onClick.AddListener(OnBackClicked);

            // Arastirma tamamlanma eventini dinle
            _eventManager.Subscribe<UpgradeCompletedEvent>(OnUpgradeCompleted);
        }

        protected override void OnShow()
        {
            RefreshAll();
        }

        protected override void OnHide()
        {
            // Opsiyonel: temizlik
        }

        private void Update()
        {
            // Aktif arastirma varsa progress bar'i guncelle
            if (_researchSystem != null && _researchSystem.IsResearching)
            {
                UpdateActiveResearchDisplay();
            }
        }

        private void OnDestroy()
        {
            _backButton?.onClick.RemoveAllListeners();
            _eventManager?.Unsubscribe<UpgradeCompletedEvent>(OnUpgradeCompleted);
        }

        // ---------------------------------------------------------------
        // UI GUNCELLEME
        // ---------------------------------------------------------------

        /// <summary>Tum paneli yeniden doldurur.</summary>
        private void RefreshAll()
        {
            if (_researchSystem == null) return;

            // Coin gosterimi
            UpdateCoinsDisplay();

            // Aktif arastirma gosterimi
            UpdateActiveResearchSection();

            // Dal listesini doldur
            PopulateBranchList();
        }

        /// <summary>Mevcut coin miktarini gosterir.</summary>
        private void UpdateCoinsDisplay()
        {
            if (_coinsText == null) return;
            var saveManager = ServiceLocator.Get<ISaveManager>();
            if (saveManager?.Data != null)
            {
                _coinsText.text = $"Para: {AnimatedCounter.FormatNumber(saveManager.Data.Coins)}";
            }
        }

        /// <summary>Aktif arastirma bolumunu gunceller.</summary>
        private void UpdateActiveResearchSection()
        {
            if (_activeResearchContainer == null) return;

            bool isResearching = _researchSystem.IsResearching;
            _activeResearchContainer.SetActive(isResearching);

            if (isResearching)
            {
                UpdateActiveResearchDisplay();
            }
        }

        /// <summary>Aktif arastirmanin progress bar ve zaman gosterimini gunceller.</summary>
        private void UpdateActiveResearchDisplay()
        {
            if (_researchSystem == null || !_researchSystem.IsResearching) return;

            string branchId = _researchSystem.ActiveResearchBranch;
            if (ResearchSystem.BranchDefinitions.TryGetValue(branchId, out var def))
            {
                if (_activeResearchNameText != null)
                    _activeResearchNameText.text = $"{def.DisplayName} araştırılıyor...";
            }

            if (_activeResearchProgressBar != null)
                _activeResearchProgressBar.value = _researchSystem.ActiveResearchProgress;

            if (_activeResearchTimeText != null)
            {
                float remaining = _researchSystem.ActiveResearchRemainingTime;
                _activeResearchTimeText.text = FormatTime(remaining);
            }
        }

        /// <summary>4 arastirma dalini listeye doldurur.</summary>
        private void PopulateBranchList()
        {
            if (_branchListContainer == null || _branchItemPrefab == null) return;

            // Mevcut itemleri temizle
            foreach (Transform child in _branchListContainer)
            {
                Destroy(child.gameObject);
            }
            _branchItems.Clear();

            var branches = _researchSystem.GetAllBranchInfos();
            foreach (var info in branches)
            {
                var item = Instantiate(_branchItemPrefab, _branchListContainer);
                var refs = SetupBranchItem(item, info);
                _branchItems.Add(refs);
            }
        }

        /// <summary>Tek bir dal item'ini kurar ve referanslarini dondurur.</summary>
        private ResearchBranchItemRefs SetupBranchItem(GameObject item, ResearchBranchInfo info)
        {
            var refs = new ResearchBranchItemRefs();

            // UI elementlerini bul
            refs.NameText = item.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            refs.LevelText = item.transform.Find("LevelText")?.GetComponent<TextMeshProUGUI>();
            refs.EffectText = item.transform.Find("EffectText")?.GetComponent<TextMeshProUGUI>();
            refs.CostText = item.transform.Find("CostText")?.GetComponent<TextMeshProUGUI>();
            refs.ResearchButton = item.transform.Find("ResearchButton")?.GetComponent<Button>();
            refs.BranchId = info.BranchId;

            // Degerleri doldur
            if (refs.NameText != null)
                refs.NameText.text = info.DisplayName;

            if (refs.LevelText != null)
                refs.LevelText.text = info.IsMaxed
                    ? $"Lv.{info.CurrentLevel} (MAX)"
                    : $"Lv.{info.CurrentLevel}/{info.MaxLevel}";

            if (refs.EffectText != null)
            {
                if (info.IsMaxed)
                {
                    refs.EffectText.text = $"Mevcut: {info.CurrentLevelEffect}";
                }
                else
                {
                    refs.EffectText.text = $"Sonraki: {info.NextLevelEffect}";
                }
            }

            if (refs.CostText != null)
            {
                if (info.IsMaxed)
                {
                    refs.CostText.text = "Tamamlandi";
                }
                else
                {
                    refs.CostText.text = $"{AnimatedCounter.FormatNumber(info.NextLevelCost)} coin | {FormatTime(info.NextLevelTime)}";
                }
            }

            if (refs.ResearchButton != null)
            {
                bool canStart = !info.IsMaxed && info.CanAfford && !_researchSystem.IsResearching;
                refs.ResearchButton.interactable = canStart;

                var branchId = info.BranchId; // closure icin
                refs.ResearchButton.onClick.AddListener(() => OnResearchButtonClicked(branchId));
            }

            return refs;
        }

        // ---------------------------------------------------------------
        // BUTON AKSIYONLARI
        // ---------------------------------------------------------------

        /// <summary>Arastirma baslatma butonuna tiklandiginda.</summary>
        private void OnResearchButtonClicked(string branchId)
        {
            if (_researchSystem == null) return;

            bool success = _researchSystem.TryStartResearch(branchId);
            if (success)
            {
                RefreshAll();
            }
        }

        /// <summary>Geri butonu.</summary>
        private void OnBackClicked()
        {
            UIManager.Instance?.CloseTopPanel();
        }

        // ---------------------------------------------------------------
        // EVENT HANDLER
        // ---------------------------------------------------------------

        /// <summary>Arastirma tamamlandiginda paneli yenile.</summary>
        private void OnUpgradeCompleted(UpgradeCompletedEvent e)
        {
            if (e.Type == UpgradeType.Research)
            {
                RefreshAll();
            }
        }

        // ---------------------------------------------------------------
        // YARDIMCI
        // ---------------------------------------------------------------

        /// <summary>Saniyeyi okunabilir sure formatina cevirir (5dk 30sn, 2sa 15dk).</summary>
        private static string FormatTime(float totalSeconds)
        {
            if (totalSeconds <= 0) return "0sn";

            int hours = (int)(totalSeconds / 3600f);
            int minutes = (int)((totalSeconds % 3600f) / 60f);
            int seconds = (int)(totalSeconds % 60f);

            if (hours > 0)
                return $"{hours}sa {minutes}dk";
            if (minutes > 0)
                return $"{minutes}dk {seconds}sn";
            return $"{seconds}sn";
        }
    }

    /// <summary>Bir dal item'inin UI element referanslari (guncelleme icin).</summary>
    internal class ResearchBranchItemRefs
    {
        public string BranchId;
        public TextMeshProUGUI NameText;
        public TextMeshProUGUI LevelText;
        public TextMeshProUGUI EffectText;
        public TextMeshProUGUI CostText;
        public Button ResearchButton;
    }
}
