// =============================================================================
// MiniGameBase.cs
// Tum mini-game'lerin temel abstract sinifi.
// Zamanlayici, skor, grade hesaplama ve sonuc raporlama islevleri sunar.
// Her mini-game bu siniftan turetilir ve Canvas uzerinde calisir.
// =============================================================================

using System;
using UnityEngine;
using RiceFactory.Core;
using RiceFactory.Core.Events;

namespace RiceFactory.MiniGames
{
    // -------------------------------------------------------------------------
    // Mini-Game Konfigurasyonu
    // -------------------------------------------------------------------------

    /// <summary>
    /// Mini-game baslatma ayarlari. MiniGameManager tarafindan saglanir.
    /// </summary>
    [Serializable]
    public class MiniGameConfig
    {
        /// <summary>Mini-game benzersiz kimlik.</summary>
        public string MiniGameId;

        /// <summary>Toplam sure (saniye).</summary>
        public float Duration;

        /// <summary>Grade esik degerleri — S, A, B, C sirali (buyukten kucuge).</summary>
        public int ThresholdS;
        public int ThresholdA;
        public int ThresholdB;
        // C = 0, her zaman minimum

        /// <summary>Temel odul miktari (coin).</summary>
        public int BaseReward;

        /// <summary>Boost suresi (saniye).</summary>
        public float BoostDuration;

        /// <summary>Zorluk carpani (ilerleyen oyunda artabilir).</summary>
        public float DifficultyMultiplier = 1f;
    }

    // -------------------------------------------------------------------------
    // Mini-Game Durumu
    // -------------------------------------------------------------------------

    /// <summary>Mini-game'in mevcut durumu.</summary>
    public enum MiniGameState
    {
        Idle,       // Henuz baslamadi
        Playing,    // Oynanıyor
        Paused,     // Duraklatildi
        Finished    // Bitti
    }

    // -------------------------------------------------------------------------
    // MiniGameBase Abstract Sinifi
    // -------------------------------------------------------------------------

    /// <summary>
    /// Tum mini-game'lerin turetildigi abstract MonoBehaviour.
    /// Zamanlayici, skor yonetimi, grade hesaplama ve sonuc callback'i saglar.
    /// Canvas uzerinde calisir, touch + mouse uyumlu input destekler.
    /// </summary>
    public abstract class MiniGameBase : MonoBehaviour
    {
        // =====================================================================
        // Alanlar
        // =====================================================================

        protected MiniGameConfig _config;
        protected MiniGameState _state = MiniGameState.Idle;
        protected int _score;
        protected float _timeRemaining;
        protected float _elapsedTime;

        // Sonuc callback'i — MiniGameManager bu callback'i dinler
        public event Action<MiniGameGrade, int> OnGameCompleted;

        // =====================================================================
        // Ozelllikler
        // =====================================================================

        /// <summary>Mini-game konfigurasyonu.</summary>
        public MiniGameConfig Config => _config;

        /// <summary>Mini-game ID'si.</summary>
        public string MiniGameId => _config?.MiniGameId ?? "";

        /// <summary>Mevcut skor.</summary>
        public int Score => _score;

        /// <summary>Mini-game durumu.</summary>
        public MiniGameState State => _state;

        /// <summary>Kalan sure (saniye).</summary>
        public float TimeRemaining => _timeRemaining;

        /// <summary>Gecen sure (saniye).</summary>
        public float ElapsedTime => _elapsedTime;

        /// <summary>Toplam sure (saniye).</summary>
        public float TotalDuration => _config?.Duration ?? 0f;

        /// <summary>Kalan surenin yuzdesi (0-1).</summary>
        public float TimeRatio => _config != null && _config.Duration > 0
            ? Mathf.Clamp01(_timeRemaining / _config.Duration)
            : 0f;

        // =====================================================================
        // Genel Yaşam Döngüsü
        // =====================================================================

        /// <summary>
        /// Mini-game'i konfigure eder. StartGame() oncesi cagirilmali.
        /// </summary>
        public void Initialize(MiniGameConfig config)
        {
            if (config == null)
            {
                Debug.LogError("[MiniGameBase] Null config ile baslatma reddedildi.");
                return;
            }

            _config = config;
            _score = 0;
            _timeRemaining = config.Duration;
            _elapsedTime = 0f;
            _state = MiniGameState.Idle;

            OnInitialize(config);
        }

        /// <summary>
        /// Mini-game'i baslatir. Zamanlayici calismaya baslar.
        /// </summary>
        public void StartGame()
        {
            if (_config == null)
            {
                Debug.LogError("[MiniGameBase] Config atanmadan oyun baslatilamaz.");
                return;
            }

            if (_state == MiniGameState.Playing)
            {
                Debug.LogWarning("[MiniGameBase] Oyun zaten oynaniyor.");
                return;
            }

            _state = MiniGameState.Playing;
            _timeRemaining = _config.Duration;
            _elapsedTime = 0f;
            _score = 0;

            OnGameStart();
        }

        /// <summary>
        /// Oyunu bitirir, grade hesaplar ve sonucu raporlar.
        /// Alt siniflar veya zamanlayici tarafindan cagirilir.
        /// </summary>
        public void EndGame(MiniGameGrade grade, int score)
        {
            if (_state == MiniGameState.Finished) return;

            _state = MiniGameState.Finished;
            _score = score;

            OnGameEnd(grade, score);
            OnGameCompleted?.Invoke(grade, score);

            // FeedbackManager ile sonuc geri bildirimi
            PlayCompletionFeedback(grade);
        }

        /// <summary>
        /// Kalan sureyi dondurur. UI tarafindan kullanilir.
        /// </summary>
        public float GetTimeRemaining()
        {
            return _timeRemaining;
        }

        // =====================================================================
        // Unity Update — Zamanlayici
        // =====================================================================

        protected virtual void Update()
        {
            if (_state != MiniGameState.Playing) return;

            _timeRemaining -= Time.deltaTime;
            _elapsedTime += Time.deltaTime;

            // Zamanlayici bitti
            if (_timeRemaining <= 0f)
            {
                _timeRemaining = 0f;
                OnTimeUp();
            }

            OnGameUpdate(Time.deltaTime);
        }

        // =====================================================================
        // Grade Hesaplama
        // =====================================================================

        /// <summary>
        /// Skora gore grade hesaplar. Config'teki esik degerlerini kullanir.
        /// </summary>
        protected MiniGameGrade CalculateGrade(int score)
        {
            if (_config == null) return MiniGameGrade.C;

            if (score >= _config.ThresholdS) return MiniGameGrade.S;
            if (score >= _config.ThresholdA) return MiniGameGrade.A;
            if (score >= _config.ThresholdB) return MiniGameGrade.B;
            return MiniGameGrade.C;
        }

        // =====================================================================
        // Skor Yardimcilari
        // =====================================================================

        /// <summary>Skora puan ekler.</summary>
        protected void AddScore(int points)
        {
            _score += points;
            OnScoreChanged(_score);
        }

        /// <summary>Skordan puan cikarir (minimum 0).</summary>
        protected void SubtractScore(int points)
        {
            _score = Mathf.Max(0, _score - points);
            OnScoreChanged(_score);
        }

        // =====================================================================
        // Feedback
        // =====================================================================

        /// <summary>
        /// Grade'e gore tamamlanma geri bildirimi.
        /// </summary>
        private void PlayCompletionFeedback(MiniGameGrade grade)
        {
            var feedback = FeedbackManager.Instance;
            if (feedback == null) return;

            switch (grade)
            {
                case MiniGameGrade.S:
                    feedback.PlayPrestige(); // Altin flash + confetti
                    break;
                case MiniGameGrade.A:
                    feedback.PlayLevelUp();
                    break;
                case MiniGameGrade.B:
                    feedback.PlayUpgrade();
                    break;
                case MiniGameGrade.C:
                    feedback.PlayButtonClick();
                    break;
            }
        }

        // =====================================================================
        // Zaman Dolunca — Varsayilan Davranis
        // =====================================================================

        /// <summary>
        /// Sure dolduğunda cagirilir. Varsayilan olarak grade hesaplayip oyunu bitirir.
        /// Alt siniflar override edebilir.
        /// </summary>
        protected virtual void OnTimeUp()
        {
            var grade = CalculateGrade(_score);
            EndGame(grade, _score);
        }

        // =====================================================================
        // Alt Sinif Hook'lari (Abstract / Virtual)
        // =====================================================================

        /// <summary>Initialize sirasinda cagirilir — alt sinif hazirlik yapar.</summary>
        protected abstract void OnInitialize(MiniGameConfig config);

        /// <summary>Oyun basladiginda cagirilir.</summary>
        protected abstract void OnGameStart();

        /// <summary>Her frame cagirilir (sadece Playing durumunda).</summary>
        protected abstract void OnGameUpdate(float deltaTime);

        /// <summary>Oyun bittiginde cagirilir.</summary>
        protected abstract void OnGameEnd(MiniGameGrade grade, int score);

        /// <summary>Skor degistiginde cagirilir — UI guncelleme icin.</summary>
        protected virtual void OnScoreChanged(int newScore) { }
    }
}
