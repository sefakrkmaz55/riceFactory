// =============================================================================
// AdManager.cs
// Rewarded ad yonetimi: gosterme, cooldown, gunluk limit.
// #if ADS_ENABLED ile koşullu derleme. Reklam yokken direkt odul verir.
// Referans: docs/MONETIZATION.md Bolum 2 — Rewarded Ads
// =============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using RiceFactory.Core;
using RiceFactory.Core.Events;

namespace RiceFactory.Ads
{
    // -------------------------------------------------------------------------
    // Interface
    // -------------------------------------------------------------------------

    /// <summary>
    /// Reklam yonetim sistemi arayuzu.
    /// Rewarded ad gosterme, limit ve cooldown kontrolu.
    /// </summary>
    public interface IAdManager
    {
        /// <summary>Belirtilen noktada rewarded ad gosterir. Basarili olursa callback cagirilir.</summary>
        void ShowRewardedAd(AdPlacement placement, Action onRewardGranted, Action onFailed = null);

        /// <summary>Belirtilen noktanin simdi gosterime uygun olup olmadigini kontrol eder.</summary>
        bool IsAdAvailable(AdPlacement placement);

        /// <summary>Bugun gosterilen toplam reklam sayisi.</summary>
        int AdsWatchedToday { get; }

        /// <summary>Bugun kalan reklam hakki.</summary>
        int RemainingAdsToday { get; }

        /// <summary>Belirtilen nokta icin kalan cooldown suresi (saniye). 0 = hazir.</summary>
        float GetCooldownRemaining(AdPlacement placement);

        /// <summary>Gunluk sayaclari sifirlar.</summary>
        void ResetDaily();
    }

    // -------------------------------------------------------------------------
    // Reklam Yerlestirme Noktalari
    // -------------------------------------------------------------------------

    /// <summary>
    /// Reklam yerlesim noktalari (MONETIZATION.md 2.1).
    /// Her noktanin kendi cooldown suresi vardir.
    /// </summary>
    public enum AdPlacement
    {
        /// <summary>Geri donus ekrani — offline kazanc x2.</summary>
        OfflineBoost,

        /// <summary>Uretim boost — tum uretim x2, 30 dk.</summary>
        ProductionBoost,

        /// <summary>Hizli arastirma — sure -%30.</summary>
        ResearchSpeed,

        /// <summary>Siparis tahtasi yenileme — 3 yeni siparis.</summary>
        OrderRefresh,

        /// <summary>Mini-game cooldown sifirlama.</summary>
        MiniGameRefresh,

        /// <summary>Cark cevir — rastgele odul.</summary>
        SpinWheel,

        /// <summary>Ucretsiz elmas — 5-15 elmas.</summary>
        FreeGems,

        /// <summary>Prestige sonrasi bonus x2.</summary>
        DoublePrestige
    }

    // -------------------------------------------------------------------------
    // Event Tanimlamalari
    // -------------------------------------------------------------------------

    /// <summary>Rewarded ad basariyla izlendiginde tetiklenir.</summary>
    public struct AdRewardedEvent : IGameEvent
    {
        public AdPlacement Placement;
        public int AdsWatchedToday;
    }

    // -------------------------------------------------------------------------
    // Implementasyon
    // -------------------------------------------------------------------------

    /// <summary>
    /// Reklam yoneticisi. Gunluk 12 limit, noktaya ozel cooldown.
    /// ADS_ENABLED tanimli degilse reklam gostermeden direkt odul verir (test modu).
    /// </summary>
    public class AdManager : IAdManager
    {
        // --- Sabitler (MONETIZATION.md 2.2) ---
        private const int DAILY_AD_LIMIT = 12;
        private const float DEFAULT_COOLDOWN_SECONDS = 180f; // 3 dakika genel minimum

        // --- Noktaya ozel cooldown sureleri (saniye) ---
        private static readonly Dictionary<AdPlacement, float> PlacementCooldowns = new()
        {
            { AdPlacement.OfflineBoost, 0f },              // Her geri donuste (server kontrol)
            { AdPlacement.ProductionBoost, 1800f },         // 30 dk
            { AdPlacement.ResearchSpeed, 0f },              // Arastirma basina 1 kez (ozel kontrol)
            { AdPlacement.OrderRefresh, 900f },             // 15 dk
            { AdPlacement.MiniGameRefresh, 7200f },         // 2 saat
            { AdPlacement.SpinWheel, 14400f },              // 4 saat
            { AdPlacement.FreeGems, 21600f },               // 6 saat
            { AdPlacement.DoublePrestige, 0f }              // Prestige basina 1 kez
        };

        private readonly IEventManager _eventManager;

        // Gunluk sayac
        private int _adsWatchedToday;
        private DateTime _lastResetDate;

        // Son reklam gosterim zamanlari (placement -> realtimeSinceStartup)
        private readonly Dictionary<AdPlacement, float> _lastAdTime = new();

        // Son genel reklam gosterim zamani (3 dk minimum aralik)
        private float _lastAnyAdTime;

        public int AdsWatchedToday => _adsWatchedToday;
        public int RemainingAdsToday => Mathf.Max(0, DAILY_AD_LIMIT - _adsWatchedToday);

        public AdManager(IEventManager eventManager)
        {
            _eventManager = eventManager ?? throw new ArgumentNullException(nameof(eventManager));
            _lastResetDate = DateTime.UtcNow.Date;
            _adsWatchedToday = 0;
            _lastAnyAdTime = -DEFAULT_COOLDOWN_SECONDS; // Ilk reklam hemen gosterilebilsin
        }

        // =====================================================================
        // REKLAM GOSTERME
        // =====================================================================

        public void ShowRewardedAd(AdPlacement placement, Action onRewardGranted, Action onFailed = null)
        {
            CheckDayReset();

            // Uygunluk kontrol
            if (!IsAdAvailable(placement))
            {
                Debug.LogWarning($"[AdManager] Reklam gosterilemez: {placement}. " +
                    $"Kalan: {RemainingAdsToday}, Cooldown: {GetCooldownRemaining(placement):F0}s");
                onFailed?.Invoke();
                return;
            }

#if ADS_ENABLED
            ShowRewardedAdNative(placement, () =>
            {
                OnAdCompleted(placement);
                onRewardGranted?.Invoke();
            }, () =>
            {
                Debug.LogWarning($"[AdManager] Reklam basarisiz veya iptal: {placement}");
                onFailed?.Invoke();
            });
#else
            // ADS_ENABLED tanimli degil — test modu: direkt odul ver
            Debug.Log($"[AdManager] (Test) Reklam simulasyonu: {placement}. Direkt odul veriliyor.");
            OnAdCompleted(placement);
            onRewardGranted?.Invoke();
#endif
        }

        // =====================================================================
        // UYGUNLUK KONTROLU
        // =====================================================================

        public bool IsAdAvailable(AdPlacement placement)
        {
            CheckDayReset();

            // Gunluk limit
            if (_adsWatchedToday >= DAILY_AD_LIMIT)
                return false;

            // Genel minimum aralik (3 dk)
            float timeSinceLastAd = Time.realtimeSinceStartup - _lastAnyAdTime;
            if (timeSinceLastAd < DEFAULT_COOLDOWN_SECONDS)
                return false;

            // Noktaya ozel cooldown
            if (GetCooldownRemaining(placement) > 0f)
                return false;

#if ADS_ENABLED
            // Native SDK'da reklam yuklu mu kontrol
            return IsNativeAdReady();
#else
            return true;
#endif
        }

        // =====================================================================
        // COOLDOWN
        // =====================================================================

        public float GetCooldownRemaining(AdPlacement placement)
        {
            if (!_lastAdTime.TryGetValue(placement, out float lastTime))
                return 0f;

            float cooldownDuration = PlacementCooldowns.GetValueOrDefault(placement, DEFAULT_COOLDOWN_SECONDS);
            if (cooldownDuration <= 0f)
                return 0f;

            float elapsed = Time.realtimeSinceStartup - lastTime;
            float remaining = cooldownDuration - elapsed;
            return Mathf.Max(0f, remaining);
        }

        // =====================================================================
        // GUNLUK RESET
        // =====================================================================

        public void ResetDaily()
        {
            _adsWatchedToday = 0;
            _lastResetDate = DateTime.UtcNow.Date;
            _lastAdTime.Clear();
            _lastAnyAdTime = -DEFAULT_COOLDOWN_SECONDS;
            Debug.Log("[AdManager] Gunluk reklam sayaclari sifirlandi.");
        }

        // =====================================================================
        // DAHILI
        // =====================================================================

        private void OnAdCompleted(AdPlacement placement)
        {
            _adsWatchedToday++;
            _lastAdTime[placement] = Time.realtimeSinceStartup;
            _lastAnyAdTime = Time.realtimeSinceStartup;

            _eventManager.Publish(new AdRewardedEvent
            {
                Placement = placement,
                AdsWatchedToday = _adsWatchedToday
            });

            Debug.Log($"[AdManager] Reklam izlendi: {placement}. Bugun: {_adsWatchedToday}/{DAILY_AD_LIMIT}");
        }

        private void CheckDayReset()
        {
            var today = DateTime.UtcNow.Date;
            if (today > _lastResetDate)
            {
                ResetDaily();
            }
        }

        // =====================================================================
        // NATIVE AD SDK KATMANI (ADS_ENABLED)
        // =====================================================================

#if ADS_ENABLED
        /// <summary>
        /// Gercek reklam SDK'si uzerinden rewarded ad gosterir.
        /// AdMob / ironSource / MAX entegrasyonuna gore uyarlanacak.
        /// </summary>
        private void ShowRewardedAdNative(AdPlacement placement, Action onSuccess, Action onFailed)
        {
            // TODO: Mediation SDK entegrasyonu
            // Ornek: Google AdMob rewarded ad
            //
            // var adUnitId = GetAdUnitId(placement);
            // var request = new AdRequest.Builder().Build();
            // RewardedAd.Load(adUnitId, request, (ad, error) => {
            //     if (error != null) { onFailed(); return; }
            //     ad.OnAdFullScreenContentClosed += () => { onSuccess(); };
            //     ad.Show((reward) => { /* reward granted */ });
            // });

            Debug.LogWarning($"[AdManager] Native reklam SDK'si henuz entegre edilmedi. Placement: {placement}");
            onFailed?.Invoke();
        }

        private bool IsNativeAdReady()
        {
            // TODO: SDK'dan reklam hazir mi kontrol
            return true;
        }
#endif
    }
}
