// =============================================================================
// OrderPanel.cs
// Siparis sistemi UI paneli — aktif siparisler, tahta ve tamamlama.
//
// Referans: docs/GDD.md Bolum 3.6 — Siparis Sistemi
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
    /// Siparis paneli.
    /// - Siparis tahtasi: 3 bekleyen siparis, kabul etme butonu
    /// - Aktif siparisler: urun ikonu, miktar, kalan zaman, odul
    /// - Tamamla butonu (stok yeterliyse)
    /// - Yeni siparis al butonu (tahta yenileme)
    /// </summary>
    public class OrderPanel : PanelBase
    {
        // ---------------------------------------------------------------
        // Baslik
        // ---------------------------------------------------------------

        [Header("Baslik")]
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _dailyCountText;

        // ---------------------------------------------------------------
        // Siparis Tahtasi (Bekleyen Siparisler)
        // ---------------------------------------------------------------

        [Header("Siparis Tahtasi")]
        [SerializeField] private Transform _boardContainer;
        [SerializeField] private GameObject _boardItemPrefab;
        [SerializeField] private Button _refreshBoardButton;
        [SerializeField] private TextMeshProUGUI _refreshTimerText;

        // ---------------------------------------------------------------
        // Aktif Siparisler
        // ---------------------------------------------------------------

        [Header("Aktif Siparisler")]
        [SerializeField] private Transform _activeOrderContainer;
        [SerializeField] private GameObject _activeOrderItemPrefab;
        [SerializeField] private TextMeshProUGUI _noActiveOrdersText;

        // ---------------------------------------------------------------
        // Geri Butonu
        // ---------------------------------------------------------------

        [Header("Butonlar")]
        [SerializeField] private Button _backButton;

        // ---------------------------------------------------------------
        // State
        // ---------------------------------------------------------------

        private IEventManager _eventManager;
        private OrderSystem _orderSystem;

        /// <summary>Envanter kontrol callback'i (dis sistemden atanir).</summary>
        private System.Func<string, int, bool> _inventoryCheckFunc;

        /// <summary>Envanter tuketim callback'i (dis sistemden atanir).</summary>
        private System.Action<string, int> _inventoryConsumeFunc;

        // ---------------------------------------------------------------
        // PanelBase overrides
        // ---------------------------------------------------------------

        protected override void OnInitialize()
        {
            _eventManager = ServiceLocator.Get<IEventManager>();

            if (ServiceLocator.TryGet<OrderSystem>(out var os))
            {
                _orderSystem = os;
            }

            _backButton?.onClick.AddListener(OnBackClicked);
            _refreshBoardButton?.onClick.AddListener(OnRefreshBoardClicked);

            // Siparis eventlerini dinle
            _eventManager.Subscribe<OrderCompletedEvent>(OnOrderCompleted);
            _eventManager.Subscribe<OrderExpiredEvent>(OnOrderExpired);
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
            // Kalan zamanlari guncelle
            UpdateTimers();
        }

        private void OnDestroy()
        {
            _backButton?.onClick.RemoveAllListeners();
            _refreshBoardButton?.onClick.RemoveAllListeners();
            _eventManager?.Unsubscribe<OrderCompletedEvent>(OnOrderCompleted);
            _eventManager?.Unsubscribe<OrderExpiredEvent>(OnOrderExpired);
        }

        // ---------------------------------------------------------------
        // DIS BAGIMLILIKLARI AYARLA
        // ---------------------------------------------------------------

        /// <summary>
        /// Envanter kontrol ve tuketim fonksiyonlarini ayarlar.
        /// Siparis tamamlama sirasinda stok kontrolu icin gereklidir.
        /// </summary>
        /// <param name="checkFunc">Stok kontrol fonksiyonu (productId, quantity) -> yeterli mi?</param>
        /// <param name="consumeFunc">Stoktan dusme fonksiyonu (productId, quantity)</param>
        public void SetInventoryCallbacks(
            System.Func<string, int, bool> checkFunc,
            System.Action<string, int> consumeFunc)
        {
            _inventoryCheckFunc = checkFunc;
            _inventoryConsumeFunc = consumeFunc;
        }

        // ---------------------------------------------------------------
        // UI GUNCELLEME
        // ---------------------------------------------------------------

        /// <summary>Tum paneli yeniden doldurur.</summary>
        private void RefreshAll()
        {
            if (_orderSystem == null) return;

            // Gunluk sayac
            if (_dailyCountText != null)
                _dailyCountText.text = $"Bugun: {_orderSystem.DailyCompletedCount} siparis tamamlandi";

            // Siparis tahtasi
            PopulateBoard();

            // Aktif siparisler
            PopulateActiveOrders();
        }

        // ---------------------------------------------------------------
        // SIPARIS TAHTASI
        // ---------------------------------------------------------------

        /// <summary>Siparis tahtasindaki bekleyen siparisleri doldurur.</summary>
        private void PopulateBoard()
        {
            if (_boardContainer == null || _boardItemPrefab == null) return;

            // Mevcut itemleri temizle
            foreach (Transform child in _boardContainer)
            {
                Destroy(child.gameObject);
            }

            var boardOrders = _orderSystem.BoardOrders;
            if (boardOrders == null || boardOrders.Count == 0)
            {
                return;
            }

            foreach (var order in boardOrders)
            {
                var item = Instantiate(_boardItemPrefab, _boardContainer);
                SetupBoardItem(item, order);
            }
        }

        /// <summary>Tek bir tahta siparis kartini kurar.</summary>
        private void SetupBoardItem(GameObject item, Order order)
        {
            var nameText = item.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            var detailText = item.transform.Find("DetailText")?.GetComponent<TextMeshProUGUI>();
            var rewardText = item.transform.Find("RewardText")?.GetComponent<TextMeshProUGUI>();
            var timeText = item.transform.Find("TimeText")?.GetComponent<TextMeshProUGUI>();
            var acceptButton = item.transform.Find("AcceptButton")?.GetComponent<Button>();

            // Siparis adi ve turu
            if (nameText != null)
                nameText.text = $"{order.DisplayName}";

            // Gereksinimleri listele
            if (detailText != null)
            {
                var details = new System.Text.StringBuilder();
                foreach (var req in order.Requirements)
                {
                    details.AppendLine($"  {FormatProductName(req.ProductId)} x{req.Quantity}");
                }
                detailText.text = details.ToString().TrimEnd();
            }

            // Tahmini odul
            if (rewardText != null)
                rewardText.text = $"Odul: ~{AnimatedCounter.FormatNumber(order.EstimatedReward)} coin (x{order.RewardMultiplier})";

            // Zaman limiti
            if (timeText != null)
                timeText.text = $"Sure: {FormatTime(order.TimeLimitSeconds)}";

            // Kabul butonu
            if (acceptButton != null)
            {
                var orderId = order.Id; // closure icin
                acceptButton.onClick.AddListener(() => OnAcceptOrderClicked(orderId));
            }
        }

        // ---------------------------------------------------------------
        // AKTIF SIPARISLER
        // ---------------------------------------------------------------

        /// <summary>Aktif (kabul edilmis) siparisleri doldurur.</summary>
        private void PopulateActiveOrders()
        {
            if (_activeOrderContainer == null || _activeOrderItemPrefab == null) return;

            // Mevcut itemleri temizle
            foreach (Transform child in _activeOrderContainer)
            {
                Destroy(child.gameObject);
            }

            var activeOrders = _orderSystem.ActiveOrders;

            // "Aktif siparis yok" mesaji
            if (_noActiveOrdersText != null)
                _noActiveOrdersText.gameObject.SetActive(activeOrders == null || activeOrders.Count == 0);

            if (activeOrders == null || activeOrders.Count == 0) return;

            foreach (var order in activeOrders)
            {
                var item = Instantiate(_activeOrderItemPrefab, _activeOrderContainer);
                SetupActiveOrderItem(item, order);
            }
        }

        /// <summary>Tek bir aktif siparis kartini kurar.</summary>
        private void SetupActiveOrderItem(GameObject item, Order order)
        {
            var nameText = item.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            var requirementsText = item.transform.Find("RequirementsText")?.GetComponent<TextMeshProUGUI>();
            var timeText = item.transform.Find("TimeText")?.GetComponent<TextMeshProUGUI>();
            var rewardText = item.transform.Find("RewardText")?.GetComponent<TextMeshProUGUI>();
            var progressBar = item.transform.Find("ProgressBar")?.GetComponent<Slider>();
            var completeButton = item.transform.Find("CompleteButton")?.GetComponent<Button>();

            // Siparis adi
            if (nameText != null)
                nameText.text = $"{order.DisplayName}";

            // Gereksinimler
            if (requirementsText != null)
            {
                var reqs = new System.Text.StringBuilder();
                foreach (var req in order.Requirements)
                {
                    bool hasEnough = _inventoryCheckFunc?.Invoke(req.ProductId, req.Quantity) ?? false;
                    string checkmark = hasEnough ? "[OK]" : "[X]";
                    reqs.AppendLine($"  {checkmark} {FormatProductName(req.ProductId)} x{req.Quantity}");
                }
                requirementsText.text = reqs.ToString().TrimEnd();
            }

            // Kalan zaman
            if (timeText != null)
                timeText.text = FormatTime(order.RemainingTime);

            // Odul
            if (rewardText != null)
                rewardText.text = $"~{AnimatedCounter.FormatNumber(order.EstimatedReward)} coin";

            // Progress bar (zaman bazli)
            if (progressBar != null)
            {
                float progress = 1f - (order.RemainingTime / order.TimeLimitSeconds);
                progressBar.value = Mathf.Clamp01(progress);
            }

            // Tamamla butonu — tum gereksinimler karsilanmissa aktif
            if (completeButton != null)
            {
                bool canComplete = CanCompleteOrder(order);
                completeButton.interactable = canComplete;

                var orderId = order.Id; // closure icin
                completeButton.onClick.AddListener(() => OnCompleteOrderClicked(orderId));
            }
        }

        /// <summary>Siparisin tum gereksinimlerinin karsilanip karsilanmadigini kontrol eder.</summary>
        private bool CanCompleteOrder(Order order)
        {
            if (_inventoryCheckFunc == null) return false;

            foreach (var req in order.Requirements)
            {
                if (!_inventoryCheckFunc(req.ProductId, req.Quantity))
                    return false;
            }
            return true;
        }

        // ---------------------------------------------------------------
        // ZAMAN GUNCELLEMESI
        // ---------------------------------------------------------------

        /// <summary>Her frame: kalan zamanlari ve tahta yenileme sayacini gunceller.</summary>
        private void UpdateTimers()
        {
            if (_orderSystem == null) return;

            // Tahta yenileme zamanlayicisi
            if (_refreshTimerText != null)
            {
                float remaining = _orderSystem.BoardRefreshRemainingTime;
                _refreshTimerText.text = $"Yenileme: {FormatTime(remaining)}";
            }
        }

        // ---------------------------------------------------------------
        // BUTON AKSIYONLARI
        // ---------------------------------------------------------------

        /// <summary>Tahtadaki bir siparisi kabul eder.</summary>
        private void OnAcceptOrderClicked(string orderId)
        {
            if (_orderSystem == null) return;

            bool success = _orderSystem.AcceptOrder(orderId);
            if (success)
            {
                RefreshAll();
            }
        }

        /// <summary>Aktif bir siparisi tamamlar.</summary>
        private void OnCompleteOrderClicked(string orderId)
        {
            if (_orderSystem == null) return;

            double reward = _orderSystem.TryCompleteOrder(orderId, _inventoryCheckFunc, _inventoryConsumeFunc);
            if (reward >= 0)
            {
                RefreshAll();
            }
        }

        /// <summary>Siparis tahtasini manuel yeniler.</summary>
        private void OnRefreshBoardClicked()
        {
            if (_orderSystem == null) return;

            _orderSystem.RefreshBoard();
            RefreshAll();
        }

        /// <summary>Geri butonu.</summary>
        private void OnBackClicked()
        {
            UIManager.Instance?.CloseTopPanel();
        }

        // ---------------------------------------------------------------
        // EVENT HANDLER
        // ---------------------------------------------------------------

        /// <summary>Siparis tamamlandiginda paneli yenile.</summary>
        private void OnOrderCompleted(OrderCompletedEvent e)
        {
            RefreshAll();
        }

        /// <summary>Siparis suresi dolduğunda paneli yenile.</summary>
        private void OnOrderExpired(OrderExpiredEvent e)
        {
            RefreshAll();
        }

        // ---------------------------------------------------------------
        // YARDIMCI
        // ---------------------------------------------------------------

        /// <summary>Saniyeyi okunabilir sure formatina cevirir.</summary>
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

        /// <summary>
        /// Urun ID'sini okunabilir isme cevirir.
        /// Ornek: "pirinc_unu" -> "Pirinc Unu"
        /// </summary>
        private static string FormatProductName(string productId)
        {
            if (string.IsNullOrEmpty(productId)) return "";

            // Alt cizgileri bosluklara cevir, ilk harfleri buyut
            var words = productId.Split('_');
            var sb = new System.Text.StringBuilder();

            for (int i = 0; i < words.Length; i++)
            {
                if (i > 0) sb.Append(' ');

                if (words[i].Length > 0)
                {
                    sb.Append(char.ToUpper(words[i][0]));
                    if (words[i].Length > 1)
                        sb.Append(words[i].Substring(1));
                }
            }

            return sb.ToString();
        }
    }
}
