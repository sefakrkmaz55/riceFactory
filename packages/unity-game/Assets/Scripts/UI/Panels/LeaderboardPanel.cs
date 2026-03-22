// =============================================================================
// LeaderboardPanel.cs
// Liderboard UI paneli.
// Haftalik/Aylik tab, siralama listesi, oyuncunun kendi sirasi vurgulu.
// =============================================================================

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RiceFactory.Core;
using RiceFactory.Social;

namespace RiceFactory.UI
{
    /// <summary>
    /// Liderboard paneli.
    /// Haftalik ve aylik tab ile siralama gosterimi.
    /// Oyuncunun kendi sirasi vurgulanir.
    /// Yenile butonu ile cache'i gecersiz kilarak taze veri ceker.
    /// </summary>
    public class LeaderboardPanel : PanelBase
    {
        // ---------------------------------------------------------------
        // UI Referanslari
        // ---------------------------------------------------------------

        [Header("Tab Butonlari")]
        [SerializeField] private Button _weeklyTabButton;
        [SerializeField] private Button _monthlyTabButton;
        [SerializeField] private Image _weeklyTabHighlight;
        [SerializeField] private Image _monthlyTabHighlight;

        [Header("Kategori Secimi")]
        [SerializeField] private TMP_Dropdown _categoryDropdown;

        [Header("Siralama Listesi")]
        [SerializeField] private Transform _listContainer;
        [SerializeField] private GameObject _entryPrefab;

        [Header("Oyuncu Sirasi")]
        [SerializeField] private TextMeshProUGUI _playerRankText;
        [SerializeField] private TextMeshProUGUI _playerScoreText;
        [SerializeField] private GameObject _playerRankPanel;

        [Header("Kontrol")]
        [SerializeField] private Button _refreshButton;
        [SerializeField] private Button _closeButton;
        [SerializeField] private GameObject _loadingIndicator;

        // ---------------------------------------------------------------
        // Durum
        // ---------------------------------------------------------------

        private ILeaderboardSystem _leaderboard;
        private LeaderboardPeriod _currentPeriod = LeaderboardPeriod.Weekly;
        private LeaderboardCategory _currentCategory = LeaderboardCategory.TopEarner;
        private bool _isLoading;

        // Olusturulan entry objeleri
        private readonly List<GameObject> _spawnedEntries = new();

        // ---------------------------------------------------------------
        // PanelBase overrides
        // ---------------------------------------------------------------

        protected override void OnInitialize()
        {
            _leaderboard = ServiceLocator.Get<ILeaderboardSystem>();

            // Tab butonlari
            _weeklyTabButton?.onClick.AddListener(() => SwitchPeriod(LeaderboardPeriod.Weekly));
            _monthlyTabButton?.onClick.AddListener(() => SwitchPeriod(LeaderboardPeriod.Monthly));

            // Kategori dropdown
            if (_categoryDropdown != null)
            {
                _categoryDropdown.ClearOptions();
                _categoryDropdown.AddOptions(new List<string>
                {
                    "En Cok Kazanan",
                    "En Cok Ureten",
                    "Siparis Krali",
                    "Kalite Sampiyonu"
                });
                _categoryDropdown.onValueChanged.AddListener(OnCategoryChanged);
            }

            // Kontrol butonlari
            _refreshButton?.onClick.AddListener(OnRefreshClicked);
            _closeButton?.onClick.AddListener(() => UIManager.Instance?.CloseTopPanel());
        }

        protected override void OnShow()
        {
            UpdateTabVisuals();
            LoadLeaderboard();
        }

        protected override void OnHide()
        {
            // Temizlik gerekmez — cache'li veri kalir
        }

        private void OnDestroy()
        {
            _weeklyTabButton?.onClick.RemoveAllListeners();
            _monthlyTabButton?.onClick.RemoveAllListeners();
            _categoryDropdown?.onValueChanged.RemoveAllListeners();
            _refreshButton?.onClick.RemoveAllListeners();
            _closeButton?.onClick.RemoveAllListeners();
        }

        // ---------------------------------------------------------------
        // Tab Gecisi
        // ---------------------------------------------------------------

        private void SwitchPeriod(LeaderboardPeriod period)
        {
            if (_currentPeriod == period) return;
            _currentPeriod = period;

            UpdateTabVisuals();

            // Aylik liderboard icin kategori seceneklerini guncelle
            UpdateCategoryOptions();

            LoadLeaderboard();
        }

        private void UpdateTabVisuals()
        {
            bool isWeekly = _currentPeriod == LeaderboardPeriod.Weekly;

            if (_weeklyTabHighlight != null)
                _weeklyTabHighlight.enabled = isWeekly;
            if (_monthlyTabHighlight != null)
                _monthlyTabHighlight.enabled = !isWeekly;
        }

        private void UpdateCategoryOptions()
        {
            if (_categoryDropdown == null) return;

            _categoryDropdown.ClearOptions();

            if (_currentPeriod == LeaderboardPeriod.Weekly)
            {
                _categoryDropdown.AddOptions(new List<string>
                {
                    "En Cok Kazanan",
                    "En Cok Ureten",
                    "Siparis Krali",
                    "Kalite Sampiyonu"
                });
            }
            else
            {
                _categoryDropdown.AddOptions(new List<string>
                {
                    "Imparator",
                    "Franchise Ustasi"
                });
            }

            _categoryDropdown.value = 0;
            OnCategoryChanged(0);
        }

        private void OnCategoryChanged(int index)
        {
            if (_currentPeriod == LeaderboardPeriod.Weekly)
            {
                _currentCategory = index switch
                {
                    0 => LeaderboardCategory.TopEarner,
                    1 => LeaderboardCategory.TopProducer,
                    2 => LeaderboardCategory.OrderKing,
                    3 => LeaderboardCategory.QualityChampion,
                    _ => LeaderboardCategory.TopEarner
                };
            }
            else
            {
                _currentCategory = index switch
                {
                    0 => LeaderboardCategory.Emperor,
                    1 => LeaderboardCategory.FranchiseMaster,
                    _ => LeaderboardCategory.Emperor
                };
            }

            LoadLeaderboard();
        }

        // ---------------------------------------------------------------
        // Veri Yukleme
        // ---------------------------------------------------------------

        private async void LoadLeaderboard()
        {
            if (_isLoading) return;
            _isLoading = true;

            SetLoadingState(true);

            try
            {
                // Paralel: siralama + oyuncu sirasi
                var entriesTask = _leaderboard.GetLeaderboardAsync(_currentPeriod, _currentCategory);
                var playerRankTask = _leaderboard.GetPlayerRankAsync(_currentPeriod, _currentCategory);

                var entries = await entriesTask;
                var playerRank = await playerRankTask;

                PopulateList(entries);
                UpdatePlayerRank(playerRank);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[LeaderboardPanel] Veri yukleme hatasi: {ex.Message}");
            }
            finally
            {
                _isLoading = false;
                SetLoadingState(false);
            }
        }

        private void OnRefreshClicked()
        {
            _leaderboard.InvalidateCache();
            LoadLeaderboard();
        }

        // ---------------------------------------------------------------
        // Liste Doldurma
        // ---------------------------------------------------------------

        private void PopulateList(List<LeaderboardEntry> entries)
        {
            // Mevcut satirlari temizle
            foreach (var obj in _spawnedEntries)
            {
                if (obj != null) Destroy(obj);
            }
            _spawnedEntries.Clear();

            if (entries == null || _entryPrefab == null || _listContainer == null) return;

            foreach (var entry in entries)
            {
                var entryObj = Instantiate(_entryPrefab, _listContainer);
                _spawnedEntries.Add(entryObj);

                // Rank
                var rankText = entryObj.transform.Find("RankText")?.GetComponent<TextMeshProUGUI>();
                if (rankText != null) rankText.text = $"#{entry.Rank}";

                // Isim
                var nameText = entryObj.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
                if (nameText != null) nameText.text = entry.PlayerName;

                // Skor
                var scoreText = entryObj.transform.Find("ScoreText")?.GetComponent<TextMeshProUGUI>();
                if (scoreText != null) scoreText.text = FormatScore(entry.Score);

                // Oyuncunun kendi satirini vurgula
                var highlight = entryObj.transform.Find("Highlight");
                if (highlight != null)
                {
                    highlight.gameObject.SetActive(entry.IsCurrentPlayer);
                }

                entryObj.SetActive(true);
            }
        }

        private void UpdatePlayerRank(LeaderboardEntry playerEntry)
        {
            if (playerEntry == null)
            {
                if (_playerRankPanel != null) _playerRankPanel.SetActive(false);
                return;
            }

            if (_playerRankPanel != null) _playerRankPanel.SetActive(true);

            if (_playerRankText != null)
                _playerRankText.text = playerEntry.Rank > 0 ? $"#{playerEntry.Rank}" : "-";

            if (_playerScoreText != null)
                _playerScoreText.text = FormatScore(playerEntry.Score);
        }

        // ---------------------------------------------------------------
        // Yardimci
        // ---------------------------------------------------------------

        private void SetLoadingState(bool loading)
        {
            if (_loadingIndicator != null)
                _loadingIndicator.SetActive(loading);
            if (_refreshButton != null)
                _refreshButton.interactable = !loading;
        }

        private static string FormatScore(double score)
        {
            if (score >= 1_000_000) return $"{score / 1_000_000:F1}M";
            if (score >= 1_000) return $"{score / 1_000:F1}K";
            return $"{score:F0}";
        }
    }
}
