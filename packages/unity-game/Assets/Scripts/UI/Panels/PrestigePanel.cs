using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RiceFactory.Core;
using RiceFactory.Core.Events;

namespace RiceFactory.UI
{
    /// <summary>
    /// Prestige (Franchise) paneli.
    ///
    /// ART_GUIDE 4.4 Prestige Ekrani Layout'una uygun:
    /// - Mevcut Franchise Puani (FP) gosterimi
    /// - Prestige sonrasi kazanilacak FP (buyuk, parlayan, altin #FFD700)
    /// - FP bonus listesi (kalici bonuslar)
    /// - "Franchise Sat" onay butonu (gradient turuncu-kirmizi, pulse animasyonu)
    /// - Prestige kosulu karsilanmadiysa disable + aciklama
    ///
    /// Onay popup'i zorunlu: "Emin misin? Tum tesislerin sifirlanacak!"
    /// </summary>
    public class PrestigePanel : PanelBase
    {
        // ---------------------------------------------------------------
        // Mevcut Durum Bilgileri
        // ---------------------------------------------------------------

        [Header("Mevcut Imparatorluk")]
        [SerializeField] private TextMeshProUGUI _totalEarningsText;
        [SerializeField] private TextMeshProUGUI _facilitiesText;
        [SerializeField] private TextMeshProUGUI _highestStarText;

        // ---------------------------------------------------------------
        // FP Kazanc Gosterimi
        // ---------------------------------------------------------------

        [Header("FP Kazanc")]
        [Tooltip("Buyuk, parlayan FP sayisi (Fredoka One, 48pt, #FFD700)")]
        [SerializeField] private TextMeshProUGUI _earnedFPText;
        [SerializeField] private TextMeshProUGUI _currentFPText;

        // ---------------------------------------------------------------
        // FP Bonus Listesi
        // ---------------------------------------------------------------

        [Header("Kalici Bonuslar")]
        [SerializeField] private Transform _bonusListContainer;
        [SerializeField] private GameObject _bonusItemPrefab;

        // ---------------------------------------------------------------
        // Sonraki Sehir
        // ---------------------------------------------------------------

        [Header("Sonraki Sehir")]
        [SerializeField] private TextMeshProUGUI _nextCityText;
        [SerializeField] private Image _nextCityPreview;

        // ---------------------------------------------------------------
        // Aksiyon Butonlari
        // ---------------------------------------------------------------

        [Header("Butonlar")]
        [Tooltip("Ana CTA — gradient turuncu-kirmizi, pulse animasyon")]
        [SerializeField] private Button _franchiseButton;
        [SerializeField] private TextMeshProUGUI _franchiseButtonText;
        [SerializeField] private CanvasGroup _franchiseButtonCanvasGroup;

        [SerializeField] private Button _backButton;

        [Header("Kosul Karsilanmadi Uyarisi")]
        [SerializeField] private TextMeshProUGUI _requirementWarningText;

        // ---------------------------------------------------------------
        // Animasyon
        // ---------------------------------------------------------------

        private Coroutine _pulseTween;

        // ---------------------------------------------------------------
        // State
        // ---------------------------------------------------------------

        private IEventManager _eventManager;
        private IPrestigeSystem _prestigeSystem;
        private IEconomySystem _economySystem;

        // ---------------------------------------------------------------
        // PanelBase overrides
        // ---------------------------------------------------------------

        protected override void OnInitialize()
        {
            _eventManager = ServiceLocator.Get<IEventManager>();
            _prestigeSystem = ServiceLocator.Get<IPrestigeSystem>();
            _economySystem = ServiceLocator.Get<IEconomySystem>();

            _franchiseButton?.onClick.AddListener(OnFranchiseButtonClicked);
            _backButton?.onClick.AddListener(OnBackClicked);
        }

        protected override void OnShow()
        {
            RefreshAll();
            StartPulseAnimation();
        }

        protected override void OnHide()
        {
            StopPulseAnimation();
        }

        private void OnDestroy()
        {
            StopPulseAnimation();
            _franchiseButton?.onClick.RemoveAllListeners();
            _backButton?.onClick.RemoveAllListeners();
        }

        // ---------------------------------------------------------------
        // UI Guncelleme
        // ---------------------------------------------------------------

        private void RefreshAll()
        {
            var saveManager = ServiceLocator.Get<ISaveManager>();
            if (saveManager?.Data == null) return;

            var data = saveManager.Data;

            // Mevcut imparatorluk bilgileri
            if (_totalEarningsText != null)
                _totalEarningsText.text = $"Toplam Kazanc: {AnimatedCounter.FormatNumber(data.TotalEarnings)}";

            if (_facilitiesText != null)
            {
                int unlockedCount = data.GetUnlockedFacilityCount();
                int totalCount = data.GetTotalFacilityCount();
                _facilitiesText.text = $"Tesisler: {unlockedCount}/{totalCount} acik";
            }

            if (_highestStarText != null)
            {
                int maxStar = data.GetHighestStarLevel();
                string stars = new string('\u2605', maxStar);
                _highestStarText.text = $"En Yuksek Yildiz: {stars}";
            }

            // Mevcut FP
            if (_currentFPText != null)
                _currentFPText.text = $"Mevcut FP: {(int)data.FranchisePoints}";

            // Kazanilacak FP
            int earnedFP = _prestigeSystem.CalculateEarnedFP();
            if (_earnedFPText != null)
                _earnedFPText.text = $"{earnedFP} FP";

            // Sonraki sehir
            string nextCity = _prestigeSystem.GetNextCityName();
            if (_nextCityText != null)
                _nextCityText.text = $"Sonraki Sehir: {nextCity}";

            // Bonus listesini doldur
            PopulateBonusList();

            // Prestige kosulu kontrolu
            bool canPrestige = _prestigeSystem.CanPrestige();
            SetFranchiseButtonState(canPrestige, earnedFP);
        }

        /// <summary>FP bonus listesini doldurur (kalici bonuslar).</summary>
        private void PopulateBonusList()
        {
            if (_bonusListContainer == null || _bonusItemPrefab == null) return;

            // Mevcut itemleri temizle
            foreach (Transform child in _bonusListContainer)
            {
                Destroy(child.gameObject);
            }

            var bonuses = _prestigeSystem.GetAvailableBonuses();
            if (bonuses == null) return;

            foreach (var bonus in bonuses)
            {
                var item = Instantiate(_bonusItemPrefab, _bonusListContainer);
                var nameText = item.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
                var costText = item.transform.Find("CostText")?.GetComponent<TextMeshProUGUI>();
                var buyButton = item.transform.Find("BuyButton")?.GetComponent<Button>();

                if (nameText != null)
                    nameText.text = bonus.DisplayName;

                if (costText != null)
                    costText.text = $"{bonus.FPCost} FP";

                if (buyButton != null)
                {
                    var bonusId = bonus.Id; // closure icin
                    buyButton.interactable = bonus.CanAfford;
                    buyButton.onClick.AddListener(() => OnBonusPurchaseClicked(bonusId));
                }
            }
        }

        // ---------------------------------------------------------------
        // Buton Durum Kontrolu
        // ---------------------------------------------------------------

        /// <summary>
        /// Franchise butonunu prestige kosuluna gore aktif/pasif yapar.
        /// Kosul karsilanmadiysa gri + aciklayici metin gosterilir.
        /// </summary>
        private void SetFranchiseButtonState(bool canPrestige, int earnedFP)
        {
            if (_franchiseButton != null)
                _franchiseButton.interactable = canPrestige;

            if (_franchiseButtonCanvasGroup != null)
                _franchiseButtonCanvasGroup.alpha = canPrestige ? 1f : 0.5f;

            if (_franchiseButtonText != null)
                _franchiseButtonText.text = canPrestige
                    ? "FRANCHISE YAP!"
                    : "FRANCHISE YAP!";

            if (_requirementWarningText != null)
            {
                if (!canPrestige)
                {
                    _requirementWarningText.gameObject.SetActive(true);
                    _requirementWarningText.text = _prestigeSystem.GetPrestigeRequirementDescription();
                }
                else
                {
                    _requirementWarningText.gameObject.SetActive(false);
                }
            }
        }

        // ---------------------------------------------------------------
        // Pulse Animasyonu (CTA butonu)
        // ---------------------------------------------------------------

        /// <summary>ART_GUIDE: Franchise butonu pulse animasyonu (1.5s loop).</summary>
        private void StartPulseAnimation()
        {
            if (_franchiseButton == null) return;

            StopPulseAnimation();
            _pulseTween = SimpleTween.DOScale(_franchiseButton.transform, 1.05f, 0.75f,
                SimpleTween.Ease.InOutSine, onComplete: () =>
                {
                    // Yoyo: geri don ve tekrarla
                    _pulseTween = SimpleTween.DOScale(_franchiseButton.transform, 1f, 0.75f,
                        SimpleTween.Ease.InOutSine, onComplete: () =>
                        {
                            // Donguyu tekrar baslat
                            if (_franchiseButton != null)
                                StartPulseAnimation();
                        });
                });
        }

        private void StopPulseAnimation()
        {
            SimpleTween.Kill(_pulseTween);
            _pulseTween = null;

            if (_franchiseButton != null)
                _franchiseButton.transform.localScale = Vector3.one;
        }

        // ---------------------------------------------------------------
        // Buton Aksiyonlari
        // ---------------------------------------------------------------

        /// <summary>
        /// Franchise butonuna tiklandiginda onay popup'i gosterir.
        /// ART_GUIDE: Onay popup'i zorunlu — "Emin misin?"
        /// </summary>
        private void OnFranchiseButtonClicked()
        {
            if (!_prestigeSystem.CanPrestige()) return;

            // Onay popup'i goster
            var confirmData = new ConfirmPopupData
            {
                Title = "Emin misin?",
                Message = "Franchise yapildiginda tum tesislerin ve paran sifirlanacak.\nKarsiliginda Franchise Puani kazanacaksin.",
                ConfirmText = "Onayla",
                CancelText = "Vazgec",
                OnConfirm = ExecutePrestige,
                OnCancel = null
            };

            UIManager.Instance?.ShowPopup<ConfirmPopup>(confirmData);
        }

        /// <summary>Prestige islemini gerceklestirir.</summary>
        private void ExecutePrestige()
        {
            _prestigeSystem.ExecutePrestige();
            UIManager.Instance?.PopToRoot();
        }

        /// <summary>FP bonus satin alma.</summary>
        private void OnBonusPurchaseClicked(string bonusId)
        {
            bool success = _prestigeSystem.PurchaseBonus(bonusId);
            if (success)
            {
                RefreshAll();
            }
        }

        private void OnBackClicked()
        {
            UIManager.Instance?.CloseTopPanel();
        }
    }

    // ===================================================================
    // Onay Popup Data Container
    // ===================================================================

    /// <summary>
    /// ConfirmPopup'a aktarilacak veri yapisi.
    /// </summary>
    public class ConfirmPopupData
    {
        public string Title;
        public string Message;
        public string ConfirmText;
        public string CancelText;
        public System.Action OnConfirm;
        public System.Action OnCancel;
    }

    /// <summary>
    /// Genel amacli onay popup'i.
    /// ART_GUIDE 4.9 Onay Popup stiline uygun.
    /// "Vazgec" sol (ikincil/beyaz), "Onayla" sag (birincil/yesil).
    /// </summary>
    public class ConfirmPopup : PopupBase
    {
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _messageText;
        [SerializeField] private Button _confirmButton;
        [SerializeField] private TextMeshProUGUI _confirmButtonText;
        [SerializeField] private Button _cancelButton;
        [SerializeField] private TextMeshProUGUI _cancelButtonText;

        private ConfirmPopupData _data;

        protected override void OnInitialize(object data)
        {
            _data = data as ConfirmPopupData;
            if (_data == null)
            {
                Close();
                return;
            }

            if (_titleText != null)
                _titleText.text = _data.Title;

            if (_messageText != null)
                _messageText.text = _data.Message;

            if (_confirmButtonText != null)
                _confirmButtonText.text = _data.ConfirmText ?? "Onayla";

            if (_cancelButtonText != null)
                _cancelButtonText.text = _data.CancelText ?? "Vazgec";

            _confirmButton?.onClick.AddListener(OnConfirmClicked);
            _cancelButton?.onClick.AddListener(OnCancelClicked);
        }

        private void OnDestroy()
        {
            _confirmButton?.onClick.RemoveAllListeners();
            _cancelButton?.onClick.RemoveAllListeners();
        }

        private void OnConfirmClicked()
        {
            _data?.OnConfirm?.Invoke();
            Close();
        }

        private void OnCancelClicked()
        {
            _data?.OnCancel?.Invoke();
            Close();
        }
    }
}
