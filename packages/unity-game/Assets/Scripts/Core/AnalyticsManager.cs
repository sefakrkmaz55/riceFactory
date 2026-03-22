// =============================================================================
// AnalyticsManager.cs
// Firebase Analytics entegrasyonu.
// #if FIREBASE_ENABLED ile kosullu derleme: Firebase yoksa debug log bastirir.
// ServiceLocator uzerinden IAnalyticsManager olarak erisim saglar.
// Oturum takibi (session_start, session_end, session_duration) dahildir.
// =============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

#if FIREBASE_ENABLED
using Firebase.Analytics;
#endif

namespace RiceFactory.Core
{
    // -------------------------------------------------------------------------
    // Analytics Manager Arayuzu
    // -------------------------------------------------------------------------

    /// <summary>
    /// Analytics islemleri icin arayuz. Test edilebilirlik ve
    /// ServiceLocator uyumu icin kullanilir.
    /// </summary>
    public interface IAnalyticsManager
    {
        /// <summary>Parametresiz event loglar.</summary>
        void LogEvent(string eventName);

        /// <summary>Parametreli event loglar.</summary>
        void LogEvent(string eventName, Dictionary<string, object> parameters);

        /// <summary>Kullanici ozelligi ayarlar (segmentasyon icin).</summary>
        void SetUserProperty(string propertyName, string value);

        /// <summary>Oturum baslatir. Uygulama acilisinda cagirilir.</summary>
        void StartSession();

        /// <summary>Oturumu sonlandirir. Uygulama kapanisinda cagirilir.</summary>
        void EndSession();
    }

    // -------------------------------------------------------------------------
    // Analytics Manager Implementasyonu
    // -------------------------------------------------------------------------

    /// <summary>
    /// Firebase Analytics sarmalayicisi. FIREBASE_ENABLED tanimli degilse
    /// tum event'ler Debug.Log ile konsola yazdirilir.
    ///
    /// Oturum yonetimi:
    ///   - StartSession(): session_start event'i + baslangic zamani kaydi
    ///   - EndSession(): session_end event'i + sure hesaplama
    ///   - OnApplicationPause/Focus: otomatik oturum yonetimi
    /// </summary>
    public class AnalyticsManager : MonoBehaviour, IAnalyticsManager
    {
        // ---- Oturum Takibi ----
        private DateTime _sessionStartTime;
        private bool _isSessionActive;

        // ---- Singleton ----
        private static AnalyticsManager _instance;

        // ---- Sabitler ----
        private const string Tag = "[AnalyticsManager]";

        // =====================================================================
        // Unity Yasam Dongusu
        // =====================================================================

        private void Awake()
        {
            // Singleton kontrol
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            // ServiceLocator'a kayit
            ServiceLocator.Register<IAnalyticsManager>(this);

            Debug.Log($"{Tag} Baslatildi.");
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                EndSession();
                ServiceLocator.Unregister<IAnalyticsManager>();
                _instance = null;
            }
        }

        /// <summary>
        /// Uygulama arka plana gittiginde oturumu sonlandirir,
        /// geri dondugunce yeni oturum baslatir.
        /// </summary>
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                EndSession();
            }
            else
            {
                StartSession();
            }
        }

        /// <summary>
        /// Uygulama odak kaybettiginde/kazandiginda oturum yonetimi.
        /// </summary>
        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                EndSession();
            }
            else
            {
                StartSession();
            }
        }

        // =====================================================================
        // Oturum Yonetimi
        // =====================================================================

        /// <summary>
        /// Yeni oturum baslatir. session_start event'i loglanir.
        /// </summary>
        public void StartSession()
        {
            if (_isSessionActive) return;

            _isSessionActive = true;
            _sessionStartTime = DateTime.UtcNow;

            LogEvent(AnalyticsEvents.SessionStart, new Dictionary<string, object>
            {
                { AnalyticsParams.Platform, Application.platform.ToString() },
                { AnalyticsParams.Version, Application.version }
            });

            Debug.Log($"{Tag} Oturum basladi.");
        }

        /// <summary>
        /// Mevcut oturumu sonlandirir. session_end event'i ve sure loglanir.
        /// </summary>
        public void EndSession()
        {
            if (!_isSessionActive) return;

            _isSessionActive = false;
            var duration = (DateTime.UtcNow - _sessionStartTime).TotalSeconds;

            LogEvent(AnalyticsEvents.SessionEnd, new Dictionary<string, object>
            {
                { AnalyticsParams.DurationSeconds, Math.Round(duration, 1) }
            });

            Debug.Log($"{Tag} Oturum sona erdi. Sure: {duration:F1}s");
        }

        // =====================================================================
        // Event Loglama
        // =====================================================================

        /// <summary>
        /// Parametresiz event loglar.
        /// </summary>
        public void LogEvent(string eventName)
        {
            if (string.IsNullOrEmpty(eventName))
            {
                Debug.LogWarning($"{Tag} Bos event ismi reddedildi.");
                return;
            }

#if FIREBASE_ENABLED
            FirebaseAnalytics.LogEvent(eventName);
#else
            Debug.Log($"{Tag} [DEBUG] Event: {eventName}");
#endif
        }

        /// <summary>
        /// Parametreli event loglar. Firebase Parameter dizisine donusturulur.
        /// </summary>
        public void LogEvent(string eventName, Dictionary<string, object> parameters)
        {
            if (string.IsNullOrEmpty(eventName))
            {
                Debug.LogWarning($"{Tag} Bos event ismi reddedildi.");
                return;
            }

            if (parameters == null || parameters.Count == 0)
            {
                LogEvent(eventName);
                return;
            }

#if FIREBASE_ENABLED
            var firebaseParams = new List<Parameter>();

            foreach (var kvp in parameters)
            {
                if (kvp.Value == null) continue;

                switch (kvp.Value)
                {
                    case string strVal:
                        firebaseParams.Add(new Parameter(kvp.Key, strVal));
                        break;
                    case int intVal:
                        firebaseParams.Add(new Parameter(kvp.Key, intVal));
                        break;
                    case long longVal:
                        firebaseParams.Add(new Parameter(kvp.Key, longVal));
                        break;
                    case float floatVal:
                        firebaseParams.Add(new Parameter(kvp.Key, floatVal));
                        break;
                    case double doubleVal:
                        firebaseParams.Add(new Parameter(kvp.Key, doubleVal));
                        break;
                    case bool boolVal:
                        firebaseParams.Add(new Parameter(kvp.Key, boolVal ? 1L : 0L));
                        break;
                    default:
                        firebaseParams.Add(new Parameter(kvp.Key, kvp.Value.ToString()));
                        break;
                }
            }

            FirebaseAnalytics.LogEvent(eventName, firebaseParams.ToArray());
#else
            // Debug modunda parametreleri konsola yazdir
            var paramStr = new System.Text.StringBuilder();
            foreach (var kvp in parameters)
            {
                if (paramStr.Length > 0) paramStr.Append(", ");
                paramStr.Append($"{kvp.Key}={kvp.Value}");
            }

            Debug.Log($"{Tag} [DEBUG] Event: {eventName} | {paramStr}");
#endif
        }

        // =====================================================================
        // Kullanici Ozellikleri
        // =====================================================================

        /// <summary>
        /// Kullanici ozelligi ayarlar. Firebase Analytics segmentasyonu icin.
        /// Ornek: SetUserProperty("franchise_count", "5")
        /// </summary>
        public void SetUserProperty(string propertyName, string value)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                Debug.LogWarning($"{Tag} Bos property ismi reddedildi.");
                return;
            }

#if FIREBASE_ENABLED
            FirebaseAnalytics.SetUserProperty(propertyName, value);
#else
            Debug.Log($"{Tag} [DEBUG] UserProperty: {propertyName} = {value}");
#endif
        }
    }
}
