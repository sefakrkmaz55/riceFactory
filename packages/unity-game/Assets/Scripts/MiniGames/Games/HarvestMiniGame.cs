// =============================================================================
// HarvestMiniGame.cs
// Hasat Mini-Game — Pirinc Tarlasi
// Sure: 15 saniye. Ekranda rastgele beliren pirinc basaklarina dokunarak hasat et.
// Basaklar 2 saniye sonra kaybolur. Altin basak +5 puan. Combo carpani destegi.
// Grade: S>=50, A>=35, B>=20, C>=0
// =============================================================================

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RiceFactory.Core;
using RiceFactory.Core.Events;

namespace RiceFactory.MiniGames
{
    /// <summary>
    /// Hasat mini-game. Pirinc tarlasinda rastgele beliren basaklara dokunarak puan topla.
    /// - Normal basak: +1 puan
    /// - Altin basak: +5 puan
    /// - Combo: art arda hizli toplamalarda 1.5x, 2x carpan
    /// </summary>
    public class HarvestMiniGame : MiniGameBase
    {
        // =====================================================================
        // Sabitler
        // =====================================================================

        private const float STALK_LIFETIME = 2f;            // Basak ekranda kalma suresi (saniye)
        private const float SPAWN_INTERVAL_MIN = 0.3f;      // Minimum spawn aralik
        private const float SPAWN_INTERVAL_MAX = 0.8f;      // Maksimum spawn aralik
        private const float GOLDEN_CHANCE = 0.15f;           // Altin basak olasiligi (%15)
        private const int NORMAL_POINTS = 1;                 // Normal basak puani
        private const int GOLDEN_POINTS = 5;                 // Altin basak puani
        private const float COMBO_TIMEOUT = 1.0f;            // Combo zaman asimi (saniye)
        private const float COMBO_TIER1_MULTIPLIER = 1.5f;   // 3+ combo carpani
        private const float COMBO_TIER2_MULTIPLIER = 2.0f;   // 6+ combo carpani
        private const int COMBO_TIER1_THRESHOLD = 3;
        private const int COMBO_TIER2_THRESHOLD = 6;
        private const float STALK_TAP_RADIUS = 50f;          // Dokunma algilama yaricapi (piksel)

        // =====================================================================
        // UI Referanslari
        // =====================================================================

        [Header("Hasat Mini-Game UI")]
        [SerializeField] private RectTransform _gameArea;     // Basaklarin belirdigi alan
        [SerializeField] private TextMeshProUGUI _scoreText;
        [SerializeField] private TextMeshProUGUI _timerText;
        [SerializeField] private TextMeshProUGUI _comboText;

        // =====================================================================
        // Oyun Durumu
        // =====================================================================

        // Aktif basaklar
        private readonly List<StalkData> _activeStalks = new();

        // Spawn zamanlayici
        private float _nextSpawnTime;

        // Combo sistemi
        private int _comboCount;
        private float _lastHarvestTime;

        // Geri donusum — nesne havuzu basitlestirilmis
        private readonly List<GameObject> _stalkPool = new();

        // =====================================================================
        // Basak Verisi
        // =====================================================================

        private class StalkData
        {
            public GameObject GameObject;
            public RectTransform RectTransform;
            public Image Image;
            public bool IsGolden;
            public float SpawnTime;
            public bool IsActive;
        }

        // =====================================================================
        // MiniGameBase Overrides
        // =====================================================================

        protected override void OnInitialize(MiniGameConfig config)
        {
            // UI sifirlama
            if (_scoreText != null) _scoreText.text = "0";
            if (_timerText != null) _timerText.text = config.Duration.ToString("F0");
            if (_comboText != null) _comboText.gameObject.SetActive(false);

            _activeStalks.Clear();
            _comboCount = 0;
            _lastHarvestTime = 0f;
        }

        protected override void OnGameStart()
        {
            _nextSpawnTime = 0f;

            if (FeedbackManager.Instance != null)
                FeedbackManager.Instance.PlayButtonClick();
        }

        protected override void OnGameUpdate(float deltaTime)
        {
            // Zamanlayici guncelle
            if (_timerText != null)
                _timerText.text = Mathf.CeilToInt(_timeRemaining).ToString();

            // Basak spawn
            _nextSpawnTime -= deltaTime;
            if (_nextSpawnTime <= 0f)
            {
                SpawnStalk();
                _nextSpawnTime = Random.Range(SPAWN_INTERVAL_MIN, SPAWN_INTERVAL_MAX);
            }

            // Suresi dolan basaklari kaldir
            RemoveExpiredStalks();

            // Input kontrolu
            HandleInput();
        }

        protected override void OnGameEnd(MiniGameGrade grade, int score)
        {
            // Tum basaklari temizle
            foreach (var stalk in _activeStalks)
            {
                if (stalk.GameObject != null)
                    stalk.GameObject.SetActive(false);
            }
            _activeStalks.Clear();
        }

        protected override void OnScoreChanged(int newScore)
        {
            if (_scoreText != null)
                _scoreText.text = newScore.ToString();
        }

        // =====================================================================
        // Basak Spawn
        // =====================================================================

        /// <summary>
        /// Rastgele pozisyonda yeni basak olusturur.
        /// </summary>
        private void SpawnStalk()
        {
            if (_gameArea == null) return;

            var stalkObj = GetOrCreateStalkObject();
            var rectTransform = stalkObj.GetComponent<RectTransform>();
            var image = stalkObj.GetComponent<Image>();

            // Rastgele pozisyon — oyun alani icinde
            var areaRect = _gameArea.rect;
            float x = Random.Range(areaRect.xMin + STALK_TAP_RADIUS, areaRect.xMax - STALK_TAP_RADIUS);
            float y = Random.Range(areaRect.yMin + STALK_TAP_RADIUS, areaRect.yMax - STALK_TAP_RADIUS);
            rectTransform.anchoredPosition = new Vector2(x, y);

            // Altin basak mi?
            bool isGolden = Random.value < GOLDEN_CHANCE;

            // Gorsel — altin basak farkli renk
            if (image != null)
            {
                image.color = isGolden
                    ? new Color(1f, 0.84f, 0f, 1f)    // Altin renk
                    : new Color(0.13f, 0.55f, 0.13f, 1f); // Yesil
            }

            // Boyut — altin biraz daha buyuk
            rectTransform.sizeDelta = isGolden
                ? new Vector2(80f, 80f)
                : new Vector2(60f, 60f);

            stalkObj.SetActive(true);

            var stalkData = new StalkData
            {
                GameObject = stalkObj,
                RectTransform = rectTransform,
                Image = image,
                IsGolden = isGolden,
                SpawnTime = Time.time,
                IsActive = true
            };

            _activeStalks.Add(stalkData);
        }

        // =====================================================================
        // Nesne Havuzu
        // =====================================================================

        private GameObject GetOrCreateStalkObject()
        {
            // Havuzdan pasif nesne bul
            foreach (var obj in _stalkPool)
            {
                if (obj != null && !obj.activeSelf)
                    return obj;
            }

            // Yeni olustur
            var newObj = new GameObject("Stalk");
            newObj.transform.SetParent(_gameArea, false);

            var rect = newObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(60f, 60f);

            var img = newObj.AddComponent<Image>();
            img.raycastTarget = false;

            // Basit daire sprite — runtime placeholder
            img.sprite = null; // Prefab'da atanacak
            img.color = Color.green;

            _stalkPool.Add(newObj);
            return newObj;
        }

        // =====================================================================
        // Basak Kaldirilmasi
        // =====================================================================

        private void RemoveExpiredStalks()
        {
            for (int i = _activeStalks.Count - 1; i >= 0; i--)
            {
                var stalk = _activeStalks[i];
                if (!stalk.IsActive) continue;

                if (Time.time - stalk.SpawnTime >= STALK_LIFETIME)
                {
                    stalk.IsActive = false;
                    stalk.GameObject.SetActive(false);
                    _activeStalks.RemoveAt(i);
                }
            }
        }

        // =====================================================================
        // Input — Touch / Mouse
        // =====================================================================

        private void HandleInput()
        {
            Vector2 inputPos;
            bool hasInput = false;

            // Touch kontrolu
            if (Input.touchCount > 0)
            {
                for (int t = 0; t < Input.touchCount; t++)
                {
                    var touch = Input.GetTouch(t);
                    if (touch.phase == TouchPhase.Began)
                    {
                        inputPos = touch.position;
                        hasInput = true;
                        TryHarvestAtPosition(inputPos);
                    }
                }
            }
            // Mouse kontrolu (editor ve PC)
            else if (Input.GetMouseButtonDown(0))
            {
                inputPos = Input.mousePosition;
                TryHarvestAtPosition(inputPos);
            }
        }

        /// <summary>
        /// Belirtilen ekran pozisyonundaki basagi hasat etmeye calis.
        /// </summary>
        private void TryHarvestAtPosition(Vector2 screenPos)
        {
            // Ekran pozisyonunu local pozisyona cevir
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _gameArea, screenPos, null, out Vector2 localPos);

            // En yakin basagi bul
            StalkData closest = null;
            float closestDist = float.MaxValue;

            for (int i = _activeStalks.Count - 1; i >= 0; i--)
            {
                var stalk = _activeStalks[i];
                if (!stalk.IsActive) continue;

                float dist = Vector2.Distance(localPos, stalk.RectTransform.anchoredPosition);
                if (dist < STALK_TAP_RADIUS && dist < closestDist)
                {
                    closest = stalk;
                    closestDist = dist;
                }
            }

            if (closest == null) return;

            // Basagi hasat et
            HarvestStalk(closest);
        }

        /// <summary>
        /// Basagi hasat eder, puan ve combo hesaplar.
        /// </summary>
        private void HarvestStalk(StalkData stalk)
        {
            stalk.IsActive = false;
            stalk.GameObject.SetActive(false);
            _activeStalks.Remove(stalk);

            // Combo kontrolu
            float currentTime = Time.time;
            if (currentTime - _lastHarvestTime <= COMBO_TIMEOUT)
            {
                _comboCount++;
            }
            else
            {
                _comboCount = 1;
            }
            _lastHarvestTime = currentTime;

            // Combo carpani
            float comboMultiplier = 1f;
            if (_comboCount >= COMBO_TIER2_THRESHOLD)
                comboMultiplier = COMBO_TIER2_MULTIPLIER;
            else if (_comboCount >= COMBO_TIER1_THRESHOLD)
                comboMultiplier = COMBO_TIER1_MULTIPLIER;

            // Puan hesapla
            int basePoints = stalk.IsGolden ? GOLDEN_POINTS : NORMAL_POINTS;
            int totalPoints = Mathf.RoundToInt(basePoints * comboMultiplier);

            AddScore(totalPoints);

            // Combo UI guncelle
            UpdateComboUI();

            // Feedback
            if (FeedbackManager.Instance != null)
            {
                if (stalk.IsGolden)
                    FeedbackManager.Instance.PlayCoinEarned(totalPoints);
                else
                    FeedbackManager.Instance.PlayButtonClick();
            }
        }

        // =====================================================================
        // Combo UI
        // =====================================================================

        private void UpdateComboUI()
        {
            if (_comboText == null) return;

            if (_comboCount >= COMBO_TIER1_THRESHOLD)
            {
                _comboText.gameObject.SetActive(true);
                float mult = _comboCount >= COMBO_TIER2_THRESHOLD
                    ? COMBO_TIER2_MULTIPLIER
                    : COMBO_TIER1_MULTIPLIER;

                _comboText.text = $"COMBO x{mult:F1}!";
                _comboText.color = _comboCount >= COMBO_TIER2_THRESHOLD
                    ? new Color(1f, 0.2f, 0.2f, 1f)  // Kirmizi — yuksek combo
                    : new Color(1f, 0.6f, 0f, 1f);    // Turuncu — dusuk combo
            }
            else
            {
                _comboText.gameObject.SetActive(false);
            }
        }
    }
}
