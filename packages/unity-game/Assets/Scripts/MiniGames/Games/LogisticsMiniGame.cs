// =============================================================================
// LogisticsMiniGame.cs
// Lojistik Mini-Game — Kuresel Dagitim
// Sure: 25 saniye. Harita uzerinde teslimat rotasi ciz — en kisa yolu bul.
// 5-8 teslimat noktasi, aralarinda cizgi cizerek bagla.
// Optimal rotanin %110'u icinde S, %130 A, %160 B, ustu C.
// Grade: S>=90 (yuzdelik), A>=77, B>=63, C>=0
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
    /// Lojistik mini-game. Harita uzerinde teslimat noktalari arasinda rota cizerek
    /// en kisa yolu bulmaya calis.
    /// - Optimal rotanin %110'u icinde: S grade (90+ puan)
    /// - %130 icinde: A grade (77+ puan)
    /// - %160 icinde: B grade (63+ puan)
    /// - Ustu: C grade
    /// </summary>
    public class LogisticsMiniGame : MiniGameBase
    {
        // =====================================================================
        // Sabitler
        // =====================================================================

        private const int MIN_DELIVERY_POINTS = 5;
        private const int MAX_DELIVERY_POINTS = 8;
        private const float POINT_RADIUS = 35f;            // Nokta dokunma algilama yaricapi
        private const float LINE_WIDTH = 4f;                // Cizgi kalinligi

        // Grade sinirlari — optimal rotaya oranla
        private const float S_THRESHOLD = 1.10f;            // %110
        private const float A_THRESHOLD = 1.30f;            // %130
        private const float B_THRESHOLD = 1.60f;            // %160

        // Puan donusumleri
        private const int S_SCORE = 95;
        private const int A_SCORE = 80;
        private const int B_SCORE = 65;
        private const int C_SCORE = 40;

        // =====================================================================
        // UI Referanslari
        // =====================================================================

        [Header("Lojistik Mini-Game UI")]
        [SerializeField] private RectTransform _mapArea;           // Harita alani
        [SerializeField] private TextMeshProUGUI _scoreText;
        [SerializeField] private TextMeshProUGUI _timerText;
        [SerializeField] private TextMeshProUGUI _distanceText;    // Mevcut rota mesafesi
        [SerializeField] private TextMeshProUGUI _pointsLeftText;  // Kalan teslimat noktasi
        [SerializeField] private Button _confirmButton;            // "Rotayi Onayla" butonu
        [SerializeField] private Button _resetButton;              // "Sifirla" butonu

        // =====================================================================
        // Oyun Durumu
        // =====================================================================

        // Teslimat noktalari
        private readonly List<DeliveryPoint> _deliveryPoints = new();

        // Oyuncunun cizdigi rota (siralı nokta index'leri)
        private readonly List<int> _playerRoute = new();

        // Cizgi gorselleri
        private readonly List<GameObject> _lineObjects = new();

        // Optimal rota mesafesi (greedy nearest-neighbor ile hesaplanir)
        private float _optimalDistance;

        // Nesne havuzlari
        private readonly List<GameObject> _pointPool = new();

        // Rota tamamlandi mi?
        private bool _routeConfirmed;

        // =====================================================================
        // Teslimat Noktasi Verisi
        // =====================================================================

        private class DeliveryPoint
        {
            public int Index;
            public Vector2 Position;
            public GameObject GameObject;
            public Image Image;
            public TextMeshProUGUI Label;
            public bool IsVisited;
        }

        // =====================================================================
        // MiniGameBase Overrides
        // =====================================================================

        protected override void OnInitialize(MiniGameConfig config)
        {
            if (_scoreText != null) _scoreText.text = "0";
            if (_timerText != null) _timerText.text = config.Duration.ToString("F0");
            if (_distanceText != null) _distanceText.text = "Mesafe: 0";
            if (_pointsLeftText != null) _pointsLeftText.text = "";

            _deliveryPoints.Clear();
            _playerRoute.Clear();
            _routeConfirmed = false;

            // Buton baglantilari
            if (_confirmButton != null)
            {
                _confirmButton.onClick.RemoveAllListeners();
                _confirmButton.onClick.AddListener(OnConfirmRoute);
                _confirmButton.interactable = false;
            }
            if (_resetButton != null)
            {
                _resetButton.onClick.RemoveAllListeners();
                _resetButton.onClick.AddListener(OnResetRoute);
            }

            // Eski cizgileri temizle
            ClearLines();
        }

        protected override void OnGameStart()
        {
            GenerateDeliveryPoints();
            _optimalDistance = CalculateOptimalDistance();

            UpdatePointsLeftDisplay();
        }

        protected override void OnGameUpdate(float deltaTime)
        {
            if (_timerText != null)
                _timerText.text = Mathf.CeilToInt(_timeRemaining).ToString();

            if (!_routeConfirmed)
            {
                HandlePointSelection();
            }
        }

        protected override void OnGameEnd(MiniGameGrade grade, int score)
        {
            if (_confirmButton != null)
                _confirmButton.interactable = false;
            if (_resetButton != null)
                _resetButton.interactable = false;
        }

        protected override void OnScoreChanged(int newScore)
        {
            if (_scoreText != null)
                _scoreText.text = newScore.ToString();
        }

        // =====================================================================
        // Sure Dolumu — Override
        // =====================================================================

        protected override void OnTimeUp()
        {
            // Sure doldugunda mevcut rotayi otomatik onayla
            if (!_routeConfirmed && _playerRoute.Count >= 2)
            {
                EvaluateRoute();
            }
            else
            {
                // Hic rota cizilemedi
                EndGame(MiniGameGrade.C, C_SCORE);
            }
        }

        // =====================================================================
        // Teslimat Noktasi Olusturma
        // =====================================================================

        private void GenerateDeliveryPoints()
        {
            if (_mapArea == null) return;

            int pointCount = Random.Range(MIN_DELIVERY_POINTS, MAX_DELIVERY_POINTS + 1);
            var areaRect = _mapArea.rect;

            // Minimum mesafe — noktalarin birbirine cok yakin olmasini onle
            float minDistance = Mathf.Min(areaRect.width, areaRect.height) * 0.15f;

            for (int i = 0; i < pointCount; i++)
            {
                Vector2 pos;
                int attempts = 0;

                // Diger noktalardan yeterince uzak pozisyon bul
                do
                {
                    float x = Random.Range(areaRect.xMin + 40f, areaRect.xMax - 40f);
                    float y = Random.Range(areaRect.yMin + 40f, areaRect.yMax - 40f);
                    pos = new Vector2(x, y);
                    attempts++;
                }
                while (IsTooCloseToExisting(pos, minDistance) && attempts < 50);

                // Nokta gorseli olustur
                var pointObj = GetOrCreatePointObject();
                var rectTransform = pointObj.GetComponent<RectTransform>();
                rectTransform.anchoredPosition = pos;

                var image = pointObj.GetComponent<Image>();
                if (image != null)
                    image.color = new Color(0.2f, 0.5f, 0.9f, 1f); // Mavi

                // Etiket
                var label = pointObj.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                    label.text = (i + 1).ToString();

                pointObj.SetActive(true);

                var deliveryPoint = new DeliveryPoint
                {
                    Index = i,
                    Position = pos,
                    GameObject = pointObj,
                    Image = image,
                    Label = label,
                    IsVisited = false
                };

                _deliveryPoints.Add(deliveryPoint);
            }
        }

        private bool IsTooCloseToExisting(Vector2 pos, float minDist)
        {
            foreach (var point in _deliveryPoints)
            {
                if (Vector2.Distance(pos, point.Position) < minDist)
                    return true;
            }
            return false;
        }

        // =====================================================================
        // Optimal Rota Hesabi (Nearest Neighbor Heuristic)
        // =====================================================================

        /// <summary>
        /// Greedy nearest-neighbor ile yaklasik optimal rota mesafesini hesaplar.
        /// TSP icin yeterli bir yaklasim — mini-game icin ideal.
        /// </summary>
        private float CalculateOptimalDistance()
        {
            if (_deliveryPoints.Count <= 1) return 0f;

            // Her baslangic noktasindan dene, en kisa olani sec
            float bestDistance = float.MaxValue;

            for (int start = 0; start < _deliveryPoints.Count; start++)
            {
                float totalDist = 0f;
                var visited = new bool[_deliveryPoints.Count];
                int current = start;
                visited[current] = true;
                int visitedCount = 1;

                while (visitedCount < _deliveryPoints.Count)
                {
                    float nearestDist = float.MaxValue;
                    int nearestIndex = -1;

                    for (int j = 0; j < _deliveryPoints.Count; j++)
                    {
                        if (visited[j]) continue;
                        float d = Vector2.Distance(
                            _deliveryPoints[current].Position,
                            _deliveryPoints[j].Position);
                        if (d < nearestDist)
                        {
                            nearestDist = d;
                            nearestIndex = j;
                        }
                    }

                    if (nearestIndex < 0) break;

                    totalDist += nearestDist;
                    visited[nearestIndex] = true;
                    current = nearestIndex;
                    visitedCount++;
                }

                if (totalDist < bestDistance)
                    bestDistance = totalDist;
            }

            return bestDistance;
        }

        // =====================================================================
        // Nokta Secim Input
        // =====================================================================

        private void HandlePointSelection()
        {
            bool inputDown = false;
            Vector2 inputPos = Vector2.zero;

            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                inputDown = true;
                inputPos = Input.GetTouch(0).position;
            }
            else if (Input.GetMouseButtonDown(0))
            {
                inputDown = true;
                inputPos = Input.mousePosition;
            }

            if (!inputDown) return;

            // Dokunulan noktayi bul
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _mapArea, inputPos, null, out Vector2 localPos);

            DeliveryPoint closest = null;
            float closestDist = POINT_RADIUS;

            foreach (var point in _deliveryPoints)
            {
                if (point.IsVisited) continue;
                float dist = Vector2.Distance(localPos, point.Position);
                if (dist < closestDist)
                {
                    closest = point;
                    closestDist = dist;
                }
            }

            if (closest == null) return;

            SelectPoint(closest);
        }

        private void SelectPoint(DeliveryPoint point)
        {
            point.IsVisited = true;

            // Gorsel guncelle — ziyaret edilmis renk
            if (point.Image != null)
                point.Image.color = new Color(0.2f, 0.8f, 0.3f, 1f); // Yesil

            // Onceki noktayla cizgi ciz
            if (_playerRoute.Count > 0)
            {
                int prevIndex = _playerRoute[_playerRoute.Count - 1];
                DrawLine(_deliveryPoints[prevIndex].Position, point.Position);
            }

            _playerRoute.Add(point.Index);

            // Mesafe guncelle
            UpdateDistanceDisplay();
            UpdatePointsLeftDisplay();

            if (FeedbackManager.Instance != null)
                FeedbackManager.Instance.PlayButtonClick();

            // Tum noktalar ziyaret edildiyse onay butonu aktif
            if (_playerRoute.Count >= _deliveryPoints.Count)
            {
                if (_confirmButton != null)
                    _confirmButton.interactable = true;
            }
        }

        // =====================================================================
        // Rota Onaylama / Sifirlama
        // =====================================================================

        private void OnConfirmRoute()
        {
            if (_routeConfirmed) return;
            EvaluateRoute();
        }

        private void OnResetRoute()
        {
            if (_routeConfirmed) return;

            _playerRoute.Clear();
            ClearLines();

            foreach (var point in _deliveryPoints)
            {
                point.IsVisited = false;
                if (point.Image != null)
                    point.Image.color = new Color(0.2f, 0.5f, 0.9f, 1f); // Mavi
            }

            if (_confirmButton != null)
                _confirmButton.interactable = false;

            UpdateDistanceDisplay();
            UpdatePointsLeftDisplay();
        }

        // =====================================================================
        // Rota Degerlendirmesi
        // =====================================================================

        private void EvaluateRoute()
        {
            _routeConfirmed = true;

            float playerDistance = CalculatePlayerRouteDistance();

            if (_optimalDistance <= 0f)
            {
                EndGame(MiniGameGrade.S, S_SCORE);
                return;
            }

            float ratio = playerDistance / _optimalDistance;
            int routeScore;
            MiniGameGrade grade;

            if (ratio <= S_THRESHOLD)
            {
                grade = MiniGameGrade.S;
                routeScore = S_SCORE;
            }
            else if (ratio <= A_THRESHOLD)
            {
                grade = MiniGameGrade.A;
                routeScore = A_SCORE;
            }
            else if (ratio <= B_THRESHOLD)
            {
                grade = MiniGameGrade.B;
                routeScore = B_SCORE;
            }
            else
            {
                grade = MiniGameGrade.C;
                routeScore = C_SCORE;
            }

            // Eksik noktalar icin puan dusur
            float completionRatio = (float)_playerRoute.Count / _deliveryPoints.Count;
            routeScore = Mathf.RoundToInt(routeScore * completionRatio);

            EndGame(grade, routeScore);
        }

        private float CalculatePlayerRouteDistance()
        {
            float total = 0f;
            for (int i = 1; i < _playerRoute.Count; i++)
            {
                total += Vector2.Distance(
                    _deliveryPoints[_playerRoute[i - 1]].Position,
                    _deliveryPoints[_playerRoute[i]].Position);
            }
            return total;
        }

        // =====================================================================
        // Cizgi Cizme
        // =====================================================================

        private void DrawLine(Vector2 from, Vector2 to)
        {
            if (_mapArea == null) return;

            var lineObj = new GameObject("RouteLine");
            lineObj.transform.SetParent(_mapArea, false);

            var lineRect = lineObj.AddComponent<RectTransform>();
            var lineImage = lineObj.AddComponent<Image>();
            lineImage.color = new Color(0.2f, 0.8f, 0.3f, 0.7f); // Yesil, yari saydam
            lineImage.raycastTarget = false;

            // Cizgiyi iki nokta arasinda konumlandir
            Vector2 direction = to - from;
            float distance = direction.magnitude;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            lineRect.anchoredPosition = (from + to) / 2f;
            lineRect.sizeDelta = new Vector2(distance, LINE_WIDTH);
            lineRect.localRotation = Quaternion.Euler(0, 0, angle);

            // Cizgi siralamasi — noktalarin arkasinda
            lineRect.SetAsFirstSibling();

            _lineObjects.Add(lineObj);
        }

        private void ClearLines()
        {
            foreach (var line in _lineObjects)
            {
                if (line != null)
                    Destroy(line);
            }
            _lineObjects.Clear();
        }

        // =====================================================================
        // UI Guncelleme
        // =====================================================================

        private void UpdateDistanceDisplay()
        {
            if (_distanceText == null) return;
            float dist = CalculatePlayerRouteDistance();
            _distanceText.text = $"Mesafe: {dist:F0}";
        }

        private void UpdatePointsLeftDisplay()
        {
            if (_pointsLeftText == null) return;
            int remaining = 0;
            foreach (var point in _deliveryPoints)
            {
                if (!point.IsVisited) remaining++;
            }
            _pointsLeftText.text = $"Kalan: {remaining}/{_deliveryPoints.Count}";
        }

        // =====================================================================
        // Nesne Havuzu
        // =====================================================================

        private GameObject GetOrCreatePointObject()
        {
            foreach (var obj in _pointPool)
            {
                if (obj != null && !obj.activeSelf)
                    return obj;
            }

            var newObj = new GameObject("DeliveryPoint");
            newObj.transform.SetParent(_mapArea, false);

            var rect = newObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(POINT_RADIUS * 2, POINT_RADIUS * 2);

            var img = newObj.AddComponent<Image>();
            img.raycastTarget = false;

            // Numara etiketi
            var labelObj = new GameObject("Label");
            labelObj.transform.SetParent(newObj.transform, false);

            var labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var tmp = labelObj.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 18;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            tmp.color = Color.white;
            tmp.fontStyle = TMPro.FontStyles.Bold;

            _pointPool.Add(newObj);
            return newObj;
        }
    }
}
