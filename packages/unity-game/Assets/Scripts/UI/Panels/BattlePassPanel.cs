// =============================================================================
// BattlePassPanel.cs
// Battle Pass / Sezon Karti UI paneli.
// Sezon bilgisi, 30 seviye track, XP ilerleme, odul toplama.
// Referans: docs/MONETIZATION.md Bolum 4 — Battle Pass
// =============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RiceFactory.Ads;
using RiceFactory.Core;
using RiceFactory.Economy;

namespace RiceFactory.UI
{
    /// <summary>
    /// Battle Pass paneli.
    /// Sezon bilgisi (kalan gun), 30 seviye track (ucretsiz + premium),
    /// XP ilerleme bari, odul toplama butonlari, premium upgrade butonu.
    /// </summary>
    public class BattlePassPanel : PanelBase
    {
        // ---------------------------------------------------------------
        // UI Referanslari
        // ---------------------------------------------------------------

        [Header("Sezon Bilgisi")]
        [SerializeField] private TextMeshProUGUI _seasonNameText;
        [SerializeField] private TextMeshProUGUI _seasonTimerText;
        [SerializeField] private TextMeshProUGUI _seasonNumberText;

        [Header("XP Ilerleme")]
        [SerializeField] private TextMeshProUGUI _levelText;
        [SerializeField] private TextMeshProUGUI _xpText;
        [SerializeField] private Slider _xpProgressBar;

        [Header("Track Listesi")]
        [SerializeField] private Transform _trackContainer;
        [SerializeField] private GameObject _trackLevelPrefab;
        [SerializeField] private ScrollRect _trackScrollRect;

        [Header("Premium")]
        [SerializeField] private GameObject _premiumUpgradeSection;
        [SerializeField] private Button _premiumButton;
        [SerializeField] private TextMeshProUGUI _premiumPriceText;
        [SerializeField] private GameObject _premiumActiveBadge;

        [Header("Kontrol")]
        [SerializeField] private Button _closeButton;

        // ---------------------------------------------------------------
        // Durum
        // ---------------------------------------------------------------

        private IBattlePassSystem _battlePass;
        private IEventManager _eventManager;
        private readonly List<GameObject> _spawnedLevels = new();

        // ---------------------------------------------------------------
        // PanelBase overrides
        // ---------------------------------------------------------------

        protected override void OnInitialize()
        {
            _battlePass = ServiceLocator.Get<IBattlePassSystem>();
            _eventManager = ServiceLocator.Get<IEventManager>();

            _premiumButton?.onClick.AddListener(OnPremiumUpgradeClicked);
            _closeButton?.onClick.AddListener(() => UIManager.Instance?.CloseTopPanel());

            // Seviye atlama eventini dinle
            _eventManager.Subscribe<BattlePassLevelUpEvent>(OnLevelUp);
            _eventManager.Subscribe<BattlePassRewardClaimedEvent>(OnRewardClaimed);
        }

        protected override void OnShow()
        {
            RefreshAll();
        }

        protected override void OnHide() { }

        private void OnDestroy()
        {
            _premiumButton?.onClick.RemoveAllListeners();
            _closeButton?.onClick.RemoveAllListeners();

            if (_eventManager != null)
            {
                _eventManager.Unsubscribe<BattlePassLevelUpEvent>(OnLevelUp);
                _eventManager.Unsubscribe<BattlePassRewardClaimedEvent>(OnRewardClaimed);
            }
        }

        // ---------------------------------------------------------------
        // Tum Paneli Yenile
        // ---------------------------------------------------------------

        private void RefreshAll()
        {
            UpdateSeasonInfo();
            UpdateXPProgress();
            UpdatePremiumSection();
            PopulateTrack();
        }

        // ---------------------------------------------------------------
        // Sezon Bilgisi
        // ---------------------------------------------------------------

        private void UpdateSeasonInfo()
        {
            var season = _battlePass.CurrentSeason;
            if (season == null) return;

            if (_seasonNameText != null)
                _seasonNameText.text = season.SeasonName;

            if (_seasonNumberText != null)
                _seasonNumberText.text = $"Sezon {season.SeasonNumber}";

            UpdateTimer();
        }

        private void UpdateTimer()
        {
            if (_seasonTimerText == null) return;

            var remaining = _battlePass.TimeRemaining;
            if (remaining <= TimeSpan.Zero)
            {
                _seasonTimerText.text = "Sezon sona erdi!";
                return;
            }

            if (remaining.TotalDays >= 1)
            {
                _seasonTimerText.text = $"{(int)remaining.TotalDays} gun {remaining.Hours} saat kaldi";
            }
            else
            {
                _seasonTimerText.text = $"{remaining.Hours} saat {remaining.Minutes} dk kaldi";
            }
        }

        // ---------------------------------------------------------------
        // XP Ilerleme
        // ---------------------------------------------------------------

        private void UpdateXPProgress()
        {
            if (_levelText != null)
                _levelText.text = $"Seviye {_battlePass.CurrentLevel}";

            if (_xpText != null)
            {
                int currentLevelXP = (_battlePass.CurrentLevel - 1) * 300;
                int nextLevelXP = _battlePass.CurrentLevel * 300;
                _xpText.text = $"{_battlePass.CurrentXP - currentLevelXP} / 300 XP";
            }

            if (_xpProgressBar != null)
                _xpProgressBar.value = _battlePass.LevelProgress;
        }

        // ---------------------------------------------------------------
        // Premium Bolumu
        // ---------------------------------------------------------------

        private void UpdatePremiumSection()
        {
            bool isPremium = _battlePass.IsPremium;

            if (_premiumUpgradeSection != null)
                _premiumUpgradeSection.SetActive(!isPremium);

            if (_premiumActiveBadge != null)
                _premiumActiveBadge.SetActive(isPremium);

            if (_premiumPriceText != null)
                _premiumPriceText.text = "$4.99";

            if (_premiumButton != null)
                _premiumButton.interactable = !isPremium;
        }

        private void OnPremiumUpgradeClicked()
        {
            // IAP uzerinden premium satin alma
            // Basarili olursa ActivatePremium cagirilir
            if (ServiceLocator.TryGet<IIAPManager>(out var iapManager))
            {
                _ = PurchasePremiumAsync(iapManager);
            }
        }

        private async System.Threading.Tasks.Task PurchasePremiumAsync(IIAPManager iapManager)
        {
            // Battle Pass premium: $4.99 — MONETIZATION.md 4.5
            // Ozel IAP urun ID'si kullanilabilir veya gem ile satin alinabilir
            // Su an icin direkt aktive ediyoruz (IAP entegrasyonu sonrasi guncellenir)
            var result = await iapManager.PurchaseAsync("com.ricefactory.battle_pass_premium");

            if (result == Ads.PurchaseResult.Success)
            {
                _battlePass.ActivatePremium();
                RefreshAll();
            }
            else
            {
                Debug.LogWarning($"[BattlePassPanel] Premium satin alma basarisiz: {result}");
            }
        }

        // ---------------------------------------------------------------
        // Track Doldurma (30 Seviye)
        // ---------------------------------------------------------------

        private void PopulateTrack()
        {
            // Temizle
            foreach (var obj in _spawnedLevels)
            {
                if (obj != null) Destroy(obj);
            }
            _spawnedLevels.Clear();

            if (_trackLevelPrefab == null || _trackContainer == null) return;

            var freeRewards = _battlePass.GetFreeRewards();
            var premiumRewards = _battlePass.GetPremiumRewards();

            for (int level = 1; level <= 30; level++)
            {
                var levelObj = Instantiate(_trackLevelPrefab, _trackContainer);
                _spawnedLevels.Add(levelObj);

                // Seviye numarasi
                var levelNumText = levelObj.transform.Find("LevelText")?.GetComponent<TextMeshProUGUI>();
                if (levelNumText != null) levelNumText.text = $"{level}";

                // Ucretsiz odul
                var freeReward = freeRewards.Find(r => r.Level == level);
                SetupRewardSlot(levelObj, "FreeReward", freeReward, level, false);

                // Premium odul
                var premiumReward = premiumRewards.Find(r => r.Level == level);
                SetupRewardSlot(levelObj, "PremiumReward", premiumReward, level, true);

                // Seviye durumu (aktif / kilitli)
                bool isUnlocked = level <= _battlePass.CurrentLevel;
                var lockOverlay = levelObj.transform.Find("LockOverlay");
                if (lockOverlay != null)
                {
                    lockOverlay.gameObject.SetActive(!isUnlocked);
                }

                // Mevcut seviyeyi vurgula
                var currentHighlight = levelObj.transform.Find("CurrentHighlight");
                if (currentHighlight != null)
                {
                    currentHighlight.gameObject.SetActive(level == _battlePass.CurrentLevel);
                }

                levelObj.SetActive(true);
            }

            // Mevcut seviyeye scroll
            ScrollToCurrentLevel();
        }

        private void SetupRewardSlot(GameObject levelObj, string slotName, BPReward reward, int level, bool isPremium)
        {
            var slot = levelObj.transform.Find(slotName);
            if (slot == null || reward == null) return;

            // Odul ismi
            var rewardText = slot.Find("RewardText")?.GetComponent<TextMeshProUGUI>();
            if (rewardText != null) rewardText.text = reward.DisplayName;

            // Toplama butonu
            var claimButton = slot.Find("ClaimButton")?.GetComponent<Button>();
            if (claimButton != null)
            {
                bool isClaimed = isPremium
                    ? _battlePass.IsPremiumRewardClaimed(level)
                    : _battlePass.IsFreeRewardClaimed(level);

                bool isUnlocked = level <= _battlePass.CurrentLevel;
                bool canClaim = isUnlocked && !isClaimed;

                // Premium odul: sadece premium aktifse toplanabilir
                if (isPremium && !_battlePass.IsPremium)
                    canClaim = false;

                claimButton.interactable = canClaim;

                // Toplanmis ise farkli gosterim
                var claimedBadge = slot.Find("ClaimedBadge");
                if (claimedBadge != null)
                {
                    claimedBadge.gameObject.SetActive(isClaimed);
                }

                // Premium kilit gosterimi
                var premiumLock = slot.Find("PremiumLock");
                if (premiumLock != null)
                {
                    premiumLock.gameObject.SetActive(isPremium && !_battlePass.IsPremium);
                }

                int capturedLevel = level;
                bool capturedIsPremium = isPremium;
                claimButton.onClick.AddListener(() => OnClaimReward(capturedLevel, capturedIsPremium));
            }
        }

        private void OnClaimReward(int level, bool isPremium)
        {
            bool success = isPremium
                ? _battlePass.ClaimPremiumReward(level)
                : _battlePass.ClaimFreeReward(level);

            if (success)
            {
                Debug.Log($"[BattlePassPanel] Odul toplandi: Seviye {level}, Premium: {isPremium}");
                // Sadece ilgili satiri guncelle (tam yeniden olusturma yerine)
                RefreshAll();
            }
        }

        // ---------------------------------------------------------------
        // Scroll
        // ---------------------------------------------------------------

        private void ScrollToCurrentLevel()
        {
            if (_trackScrollRect == null || _spawnedLevels.Count == 0) return;

            int currentLevel = _battlePass.CurrentLevel;
            float normalizedPosition = Mathf.Clamp01((float)(currentLevel - 1) / 29f);

            // Horizontal scroll varsayimi
            _trackScrollRect.horizontalNormalizedPosition = normalizedPosition;
        }

        // ---------------------------------------------------------------
        // Event Handler'lar
        // ---------------------------------------------------------------

        private void OnLevelUp(BattlePassLevelUpEvent e)
        {
            // Panel aciksa guncelle
            if (gameObject.activeInHierarchy)
            {
                RefreshAll();
            }
        }

        private void OnRewardClaimed(BattlePassRewardClaimedEvent e)
        {
            // Panel aciksa guncelle
            if (gameObject.activeInHierarchy)
            {
                UpdateXPProgress();
            }
        }
    }
}
