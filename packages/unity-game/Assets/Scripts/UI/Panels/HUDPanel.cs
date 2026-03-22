using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RiceFactory.Core;
using RiceFactory.Core.Events;

namespace RiceFactory.UI
{
    /// <summary>
    /// Ana ekran HUD paneli.
    ///
    /// Ust bar: Coin, Elmas, Seviye gostergesi (goruntulenme bolgesi — ust %40)
    /// Alt bar: Fabrika, Upgrade, Prestige, Ayarlar butonlari (kritik aksiyon bolgesi — alt %40)
    ///
    /// EventManager uzerinden CurrencyChangedEvent dinler ve animasyonlu sayac gunceller.
    /// ART_GUIDE 4.2 Ana Ekran Layout'una uygun.
    /// Tek elle kullanim: kritik butonlar alt kisimda, bilgi ust kisimda.
    /// </summary>
    public class HUDPanel : PanelBase
    {
        // ---------------------------------------------------------------
        // Ust Bar — Bilgi Gostergesi
        // ---------------------------------------------------------------

        [Header("Ust Bar — Para Gostergesi")]
        [SerializeField] private AnimatedCounter _coinCounter;
        [SerializeField] private Image _coinIcon;

        [Header("Ust Bar — Elmas Gostergesi")]
        [SerializeField] private AnimatedCounter _diamondCounter;
        [SerializeField] private Image _diamondIcon;

        [Header("Ust Bar — Seviye / Yildiz")]
        [SerializeField] private TextMeshProUGUI _levelText;
        [SerializeField] private Image _levelIcon;

        // ---------------------------------------------------------------
        // Alt Bar — Navigasyon Butonlari
        // ---------------------------------------------------------------

        [Header("Alt Bar — Navigasyon")]
        [Tooltip("Ana sayfa / fabrika listesi butonu")]
        [SerializeField] private Button _factoryButton;

        [Tooltip("Upgrade panelini acar")]
        [SerializeField] private Button _upgradeButton;

        [Tooltip("Prestige (Franchise) panelini acar")]
        [SerializeField] private Button _prestigeButton;

        [Tooltip("Ayarlar panelini acar")]
        [SerializeField] private Button _settingsButton;

        // ---------------------------------------------------------------
        // EventManager referansi
        // ---------------------------------------------------------------

        private IEventManager _eventManager;

        // ---------------------------------------------------------------
        // PanelBase overrides
        // ---------------------------------------------------------------

        protected override void OnInitialize()
        {
            // EventManager'i ServiceLocator'dan al
            _eventManager = ServiceLocator.Get<IEventManager>();

            // Event abonelikleri
            _eventManager.Subscribe<CurrencyChangedEvent>(OnCurrencyChanged);

            // Buton click listener'lari
            _factoryButton?.onClick.AddListener(OnFactoryButtonClicked);
            _upgradeButton?.onClick.AddListener(OnUpgradeButtonClicked);
            _prestigeButton?.onClick.AddListener(OnPrestigeButtonClicked);
            _settingsButton?.onClick.AddListener(OnSettingsButtonClicked);

            // Ilk degerleri aninda goster (animasyonsuz)
            RefreshDisplayImmediate();
        }

        protected override void OnShow()
        {
            // Panel her gosterildiginde guncel degerleri tazele
            RefreshDisplayImmediate();
        }

        protected override void OnHide()
        {
            // Gizlenirken ek islem gerekmez
        }

        private void OnDestroy()
        {
            // Event aboneliklerini kaldir (bellek sizintisi onleme)
            if (_eventManager != null)
            {
                _eventManager.Unsubscribe<CurrencyChangedEvent>(OnCurrencyChanged);
            }

            // Buton listener temizligi
            _factoryButton?.onClick.RemoveListener(OnFactoryButtonClicked);
            _upgradeButton?.onClick.RemoveListener(OnUpgradeButtonClicked);
            _prestigeButton?.onClick.RemoveListener(OnPrestigeButtonClicked);
            _settingsButton?.onClick.RemoveListener(OnSettingsButtonClicked);
        }

        // ---------------------------------------------------------------
        // Event Handler'lar
        // ---------------------------------------------------------------

        /// <summary>
        /// Para veya elmas degistiginde animasyonlu sayaci gunceller.
        /// CurrencyType'a gore dogru counter secilir.
        /// </summary>
        private void OnCurrencyChanged(CurrencyChangedEvent e)
        {
            switch (e.Type)
            {
                case CurrencyType.Coin:
                    _coinCounter?.SetValue(e.NewAmount);
                    break;

                case CurrencyType.Gem:
                    _diamondCounter?.SetValue(e.NewAmount);
                    break;

                // FP ve Reputation bu panelde gosterilmez
            }
        }

        // ---------------------------------------------------------------
        // Buton Aksiyonlari
        // ---------------------------------------------------------------

        private void OnFactoryButtonClicked()
        {
            // Ana ekrana don (zaten HUD aciksa bir sey yapma)
            UIManager.Instance?.PopToRoot();
        }

        private void OnUpgradeButtonClicked()
        {
            UIManager.Instance?.OpenPanel<UpgradePanel>();
        }

        private void OnPrestigeButtonClicked()
        {
            UIManager.Instance?.OpenPanel<PrestigePanel>();
        }

        private void OnSettingsButtonClicked()
        {
            UIManager.Instance?.OpenPanel<SettingsPanel>();
        }

        // ---------------------------------------------------------------
        // Yardimci
        // ---------------------------------------------------------------

        /// <summary>
        /// Mevcut oyuncu verisinden tum gostergeleri aninda gunceller.
        /// Animasyonsuz — panel ilk acildiginda veya geri donuldugunde kullanilir.
        /// </summary>
        private void RefreshDisplayImmediate()
        {
            var saveManager = ServiceLocator.Get<ISaveManager>();
            if (saveManager == null) return;

            var data = saveManager.Data;
            if (data == null) return;

            _coinCounter?.SetValueImmediate(data.Coins);
            _diamondCounter?.SetValueImmediate(data.Gems);

            if (_levelText != null)
            {
                _levelText.text = $"Lv.{data.Level}";
            }
        }
    }
}
