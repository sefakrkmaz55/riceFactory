// =============================================================================
// GameSceneController.cs
// Game sahnesi scripti. Sahneye girildiginde HUD'u acar, ProductionManager'i
// baslatir, tutorial kontrolu yapar ve fabrika kartlarini olusturur.
// =============================================================================

using System.Collections.Generic;
using UnityEngine;
using RiceFactory.Core;
using RiceFactory.Core.Events;
using RiceFactory.Economy;
using RiceFactory.Production;

namespace RiceFactory.UI
{
    /// <summary>
    /// Game sahnesinin ana controller'i.
    /// - Sahneye girildiginde HUDPanel'i acar.
    /// - ProductionManager'i baslatir (fabrika verileriyle).
    /// - Ilk fabrika yoksa TutorialController'i baslatir.
    /// - Fabrika kartlarini (FactoryCardUI) olusturur ve yonetir.
    /// </summary>
    public class GameSceneController : MonoBehaviour
    {
        // =====================================================================
        // Inspector Referanslari
        // =====================================================================

        [Header("Fabrika Kartlari")]
        [Tooltip("Fabrika kartlarinin olusturulacagi parent Transform")]
        [SerializeField] private Transform _factoryCardContainer;

        [Tooltip("FactoryCardUI prefab'i")]
        [SerializeField] private FactoryCardUI _factoryCardPrefab;

        [Header("Tutorial")]
        [Tooltip("TutorialController referansi (sahnede hazir olmali)")]
        [SerializeField] private TutorialController _tutorialController;

        // =====================================================================
        // Dahili Referanslar
        // =====================================================================

        private IGameManager _gameManager;
        private IEventManager _eventManager;
        private ProductionManager _productionManager;
        private ResearchSystem _researchSystem;
        private OrderSystem _orderSystem;

        // Aktif fabrika kartlari (factoryInstanceId -> FactoryCardUI)
        private readonly Dictionary<string, FactoryCardUI> _factoryCards = new();

        // =====================================================================
        // Unity Lifecycle
        // =====================================================================

        private void Start()
        {
            // Servisleri al
            ServiceLocator.TryGet(out _gameManager);
            ServiceLocator.TryGet(out _eventManager);
            ServiceLocator.TryGet(out _productionManager);
            ServiceLocator.TryGet(out _researchSystem);
            ServiceLocator.TryGet(out _orderSystem);

            InitializeGameScene();
        }

        private void OnDestroy()
        {
            // Eventlerden cikarilma
            if (_eventManager != null)
            {
                _eventManager.Unsubscribe<GameTickEvent>(OnGameTick);
            }
        }

        // =====================================================================
        // Sahne Baslangici
        // =====================================================================

        /// <summary>
        /// Game sahnesine girildiginde yapilacak islemler.
        /// </summary>
        private void InitializeGameScene()
        {
            Debug.Log("[GameSceneController] Game sahnesi baslatiliyor...");

            // 1. Oyun durumunu Playing'e gecir
            if (_gameManager != null && _gameManager.CurrentState != GameState.Playing)
            {
                _gameManager.ChangeState(GameState.Playing);
            }

            // 2. HUDPanel'i ac
            // Not: HUDPanel PanelBase'den turediginde OpenPanel ile acilir
            // UIManager.Instance?.OpenPanel<HUDPanel>();
            Debug.Log("[GameSceneController] HUDPanel acildi.");

            // 3. ProductionManager'i fabrika verileriyle baslat
            InitializeProduction();

            // 4. OrderSystem'i baslat
            _orderSystem?.Initialize();

            // 5. Fabrika kartlarini olustur
            RefreshFactoryCards();

            // 6. Tutorial kontrolu
            CheckTutorial();

            // 7. GameTick eventine abone ol (tick dongusu)
            if (_eventManager != null)
            {
                _eventManager.Subscribe<GameTickEvent>(OnGameTick);
            }

            Debug.Log("[GameSceneController] Game sahnesi hazir.");
        }

        // =====================================================================
        // Uretim Baslatma
        // =====================================================================

        /// <summary>
        /// ProductionManager'i FactoryConfigs'ten gelen verilerle baslatir.
        /// Eger hic acik tesis yoksa ilk tesisi (Pirinc Tarlasi) otomatik acar.
        /// </summary>
        private void InitializeProduction()
        {
            if (_productionManager == null)
            {
                Debug.LogWarning("[GameSceneController] ProductionManager bulunamadi!");
                return;
            }

            // FactoryConfigs'ten tum fabrika tanimlarini al ve FactoryData'ya donustur
            var factoryDataList = BuildFactoryDataList();
            _productionManager.Initialize(factoryDataList);

            // Hic acik fabrika yoksa ilk tesisi (rice_field) otomatik ac
            if (_productionManager.UnlockedFactoryCount == 0)
            {
                Debug.Log("[GameSceneController] Hic acik tesis yok, Pirinc Tarlasi otomatik aciliyor.");
                _productionManager.UnlockFactory("rice_field");
            }

            Debug.Log($"[GameSceneController] ProductionManager baslatildi. " +
                      $"Acik fabrika: {_productionManager.UnlockedFactoryCount}");
        }

        /// <summary>
        /// FactoryConfigs'teki statik verilerden FactoryData nesneleri olusturur.
        /// ProductionManager.Initialize bu listeyi bekler.
        /// </summary>
        private List<FactoryData> BuildFactoryDataList()
        {
            var list = new List<FactoryData>();

            foreach (var configData in FactoryConfigs.GetAllOrdered())
            {
                var factoryData = ScriptableObject.CreateInstance<FactoryData>();

                // SetFromConfig ile tum temel alanlari doldur
                factoryData.SetFromConfig(
                    id: configData.Id,
                    name: configData.Name,
                    desc: configData.Description,
                    baseCost: configData.BaseCost,
                    baseProduction: configData.BaseProduction,
                    basePrice: configData.BasePrice,
                    unlockCost: configData.UnlockCost,
                    order: configData.UnlockOrder,
                    productionTime: configData.BaseProductionTime,
                    secondaryTime: configData.SecondaryProductionTime,
                    secondaryPrice: configData.SecondaryPrice
                );

                // Uretim zincirini kopyala
                var chainEntries = new List<ProductChainEntry>();
                foreach (var chain in configData.ProductChain)
                {
                    chainEntries.Add(new ProductChainEntry
                    {
                        inputProductId = chain.InputProductId,
                        inputQuantity = chain.InputQuantity,
                        outputProductId = chain.OutputProductId,
                        outputQuantity = chain.OutputQuantity,
                        requiredStarLevel = chain.RequiredStarLevel
                    });
                }
                factoryData.SetProductChain(chainEntries);

                list.Add(factoryData);
            }

            return list;
        }

        // =====================================================================
        // Fabrika Kartlari
        // =====================================================================

        /// <summary>
        /// Tum acik fabrikalar icin FactoryCardUI bilesenlerini olusturur/gunceller.
        /// </summary>
        public void RefreshFactoryCards()
        {
            if (_productionManager == null || _factoryCardContainer == null) return;

            var allFactories = _productionManager.GetAllFactoriesSorted();

            // Tum konfigurasyonlari dolasirak kartlari olustur
            foreach (var configId in FactoryConfigs.OrderedIds)
            {
                var factory = _productionManager.GetFactoryByType(configId);
                bool isUnlocked = factory != null && factory.IsUnlocked;
                string instanceId = factory?.State?.Id ?? configId;

                // Kart zaten varsa guncelle
                if (_factoryCards.TryGetValue(instanceId, out var existingCard))
                {
                    if (isUnlocked)
                    {
                        existingCard.UpdateDisplay(factory);
                    }
                    continue;
                }

                // Yeni kart olustur
                if (_factoryCardPrefab == null)
                {
                    Debug.LogWarning("[GameSceneController] FactoryCardUI prefab atanmamis!");
                    break;
                }

                var card = Instantiate(_factoryCardPrefab, _factoryCardContainer);

                if (isUnlocked)
                {
                    card.Setup(factory, FactoryConfigs.GetById(configId));
                }
                else
                {
                    card.SetupLocked(FactoryConfigs.GetById(configId));
                }

                _factoryCards[instanceId] = card;
            }
        }

        // =====================================================================
        // Tutorial Kontrolu
        // =====================================================================

        /// <summary>
        /// Ilk fabrika yoksa veya tutorial tamamlanmamissa tutorial akisini baslatir.
        /// </summary>
        private void CheckTutorial()
        {
            if (_tutorialController == null) return;

            if (ServiceLocator.TryGet<ISaveManager>(out var saveManager))
            {
                // Tutorial tamamlanmis mi kontrol et (PlayerPrefs'ten)
                bool tutorialCompleted = PlayerPrefs.GetInt("tutorial_completed", 0) == 1;

                if (!tutorialCompleted)
                {
                    Debug.Log("[GameSceneController] Tutorial baslatiliyor...");
                    _tutorialController.StartTutorial();
                }
            }
        }

        // =====================================================================
        // Tick (GameTickEvent abone)
        // =====================================================================

        /// <summary>
        /// GameTickEvent uzerinden her frame cagirilir.
        /// Fabrika kartlarinin gorsel guncellemesini yapar.
        /// </summary>
        private void OnGameTick(GameTickEvent e)
        {
            // Fabrika kartlarini guncelle (progress bar, gelir gosterimi)
            if (_productionManager != null)
            {
                foreach (var kvp in _factoryCards)
                {
                    var factory = _productionManager.GetFactory(kvp.Key);
                    if (factory != null && factory.IsActive)
                    {
                        kvp.Value.UpdateDisplay(factory);
                    }
                }
            }
        }
    }
}
