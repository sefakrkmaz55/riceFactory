// =============================================================================
// FriendsPanel.cs
// Arkadas listesi UI paneli.
// Arkadas ekleme (ID girisi), ziyaret butonu, gunluk limit gosterimi.
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
    /// Arkadas paneli.
    /// Arkadas listesi, ID ile ekleme, fabrika ziyareti.
    /// Gunluk ziyaret limiti (3) gosterimi.
    /// </summary>
    public class FriendsPanel : PanelBase
    {
        // ---------------------------------------------------------------
        // UI Referanslari
        // ---------------------------------------------------------------

        [Header("Arkadas Ekleme")]
        [SerializeField] private TMP_InputField _friendIdInput;
        [SerializeField] private Button _addFriendButton;
        [SerializeField] private TextMeshProUGUI _addResultText;

        [Header("Arkadas Listesi")]
        [SerializeField] private Transform _listContainer;
        [SerializeField] private GameObject _friendEntryPrefab;
        [SerializeField] private TextMeshProUGUI _emptyListText;

        [Header("Ziyaret Bilgisi")]
        [SerializeField] private TextMeshProUGUI _visitCountText;

        [Header("Kontrol")]
        [SerializeField] private Button _closeButton;
        [SerializeField] private GameObject _loadingIndicator;

        // ---------------------------------------------------------------
        // Durum
        // ---------------------------------------------------------------

        private IFriendSystem _friendSystem;
        private bool _isLoading;

        // Olusturulan entry objeleri
        private readonly List<GameObject> _spawnedEntries = new();

        // ---------------------------------------------------------------
        // PanelBase overrides
        // ---------------------------------------------------------------

        protected override void OnInitialize()
        {
            _friendSystem = ServiceLocator.Get<IFriendSystem>();

            _addFriendButton?.onClick.AddListener(OnAddFriendClicked);
            _closeButton?.onClick.AddListener(() => UIManager.Instance?.CloseTopPanel());

            // Bos metin gizle
            if (_addResultText != null) _addResultText.text = "";
        }

        protected override void OnShow()
        {
            UpdateVisitCount();
            LoadFriendList();

            // Input alanini temizle
            if (_friendIdInput != null) _friendIdInput.text = "";
            if (_addResultText != null) _addResultText.text = "";
        }

        protected override void OnHide() { }

        private void OnDestroy()
        {
            _addFriendButton?.onClick.RemoveAllListeners();
            _closeButton?.onClick.RemoveAllListeners();
        }

        // ---------------------------------------------------------------
        // Arkadas Ekleme
        // ---------------------------------------------------------------

        private async void OnAddFriendClicked()
        {
            if (_friendIdInput == null) return;

            string friendId = _friendIdInput.text.Trim();
            if (string.IsNullOrEmpty(friendId))
            {
                ShowAddResult("Lutfen bir oyuncu ID'si girin.", false);
                return;
            }

            _addFriendButton.interactable = false;

            var result = await _friendSystem.AddFriendAsync(friendId);

            string message = result switch
            {
                FriendRequestResult.Success => "Arkadas eklendi!",
                FriendRequestResult.AlreadyFriends => "Zaten arkadassiniz.",
                FriendRequestResult.PlayerNotFound => "Oyuncu bulunamadi.",
                FriendRequestResult.RequestPending => "Istek zaten gonderildi.",
                _ => "Bir hata olustu."
            };

            bool success = result == FriendRequestResult.Success;
            ShowAddResult(message, success);

            if (success)
            {
                _friendIdInput.text = "";
                LoadFriendList(); // Listeyi yenile
            }

            _addFriendButton.interactable = true;
        }

        private void ShowAddResult(string message, bool isSuccess)
        {
            if (_addResultText == null) return;
            _addResultText.text = message;
            _addResultText.color = isSuccess ? Color.green : Color.red;
        }

        // ---------------------------------------------------------------
        // Arkadas Listesi
        // ---------------------------------------------------------------

        private async void LoadFriendList()
        {
            if (_isLoading) return;
            _isLoading = true;

            SetLoadingState(true);

            try
            {
                var friends = await _friendSystem.GetFriendsAsync();
                PopulateList(friends);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[FriendsPanel] Liste yukleme hatasi: {ex.Message}");
            }
            finally
            {
                _isLoading = false;
                SetLoadingState(false);
            }
        }

        private void PopulateList(List<FriendInfo> friends)
        {
            // Mevcut satirlari temizle
            foreach (var obj in _spawnedEntries)
            {
                if (obj != null) Destroy(obj);
            }
            _spawnedEntries.Clear();

            // Bos liste mesaji
            if (_emptyListText != null)
            {
                _emptyListText.gameObject.SetActive(friends == null || friends.Count == 0);
            }

            if (friends == null || _friendEntryPrefab == null || _listContainer == null) return;

            foreach (var friend in friends)
            {
                var entryObj = Instantiate(_friendEntryPrefab, _listContainer);
                _spawnedEntries.Add(entryObj);

                // Isim
                var nameText = entryObj.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
                if (nameText != null) nameText.text = friend.PlayerName;

                // Seviye
                var levelText = entryObj.transform.Find("LevelText")?.GetComponent<TextMeshProUGUI>();
                if (levelText != null) levelText.text = $"Lv.{friend.Level}";

                // Franchise sayisi
                var franchiseText = entryObj.transform.Find("FranchiseText")?.GetComponent<TextMeshProUGUI>();
                if (franchiseText != null) franchiseText.text = $"Franchise: {friend.FranchiseCount}";

                // Online durumu
                var onlineIndicator = entryObj.transform.Find("OnlineIndicator");
                if (onlineIndicator != null)
                {
                    onlineIndicator.gameObject.SetActive(friend.IsOnline);
                }

                // Ziyaret butonu
                var visitButton = entryObj.transform.Find("VisitButton")?.GetComponent<Button>();
                if (visitButton != null)
                {
                    bool canVisit = _friendSystem.GetRemainingVisitsToday() > 0;
                    visitButton.interactable = canVisit;

                    string capturedFriendId = friend.PlayerId;
                    visitButton.onClick.AddListener(() => OnVisitClicked(capturedFriendId));
                }

                entryObj.SetActive(true);
            }
        }

        // ---------------------------------------------------------------
        // Fabrika Ziyareti
        // ---------------------------------------------------------------

        private async void OnVisitClicked(string friendId)
        {
            var result = await _friendSystem.VisitFriendFactoryAsync(friendId);

            if (result.Success)
            {
                ShowAddResult($"{result.FriendName} ziyaret edildi! +{result.CoinReward:F0} coin", true);
            }
            else
            {
                ShowAddResult(result.Message, false);
            }

            UpdateVisitCount();
            UpdateVisitButtons();
        }

        // ---------------------------------------------------------------
        // Ziyaret Sayaci
        // ---------------------------------------------------------------

        private void UpdateVisitCount()
        {
            if (_visitCountText == null) return;

            int remaining = _friendSystem.GetRemainingVisitsToday();
            _visitCountText.text = $"Gunluk Ziyaret: {3 - remaining}/3";
        }

        private void UpdateVisitButtons()
        {
            bool canVisit = _friendSystem.GetRemainingVisitsToday() > 0;

            foreach (var entryObj in _spawnedEntries)
            {
                if (entryObj == null) continue;
                var visitButton = entryObj.transform.Find("VisitButton")?.GetComponent<Button>();
                if (visitButton != null)
                {
                    visitButton.interactable = canVisit;
                }
            }
        }

        // ---------------------------------------------------------------
        // Yardimci
        // ---------------------------------------------------------------

        private void SetLoadingState(bool loading)
        {
            if (_loadingIndicator != null)
                _loadingIndicator.SetActive(loading);
        }
    }
}
