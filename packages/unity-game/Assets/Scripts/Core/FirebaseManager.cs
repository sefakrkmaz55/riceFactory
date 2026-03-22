// =============================================================================
// FirebaseManager.cs
// Firebase SDK baslatma ve merkezi yonetim sinifi.
// Singleton pattern ile tum Firebase servislerine erisim noktasi saglar.
// FIREBASE_ENABLED define'i olmadan proje derlenebilir; Firebase yokken
// dummy/fallback davranis sunar.
// =============================================================================

using System;
using System.Threading.Tasks;
using UnityEngine;

#if FIREBASE_ENABLED
using Firebase;
using Firebase.Extensions;
#endif

namespace RiceFactory.Core
{
    // -------------------------------------------------------------------------
    // Firebase Manager Arayuzu
    // -------------------------------------------------------------------------

    /// <summary>
    /// Firebase baslatma durumunu ve hazirlik kontrolunu saglayan arayuz.
    /// </summary>
    public interface IFirebaseManager
    {
        /// <summary>Firebase SDK basariyla baslatildi mi?</summary>
        bool IsInitialized { get; }

        /// <summary>Firebase baslatma islemi tamamlandi mi (basarili veya basarisiz)?</summary>
        bool IsReady { get; }

        /// <summary>Firebase SDK'yi baslatir.</summary>
        Task InitializeAsync();

        /// <summary>Baslatma durumu degistiginde tetiklenir.</summary>
        event Action<bool> OnInitialized;
    }

    // -------------------------------------------------------------------------
    // Firebase Manager Implementasyonu
    // -------------------------------------------------------------------------

    /// <summary>
    /// Firebase SDK baslatma ve bagimlilik kontrolu.
    /// CheckAndFixDependenciesAsync ile SDK hazirligini dogrular.
    /// Firebase yokken (FIREBASE_ENABLED tanimlanmamissa) her zaman
    /// "hazir degil" durumunda calisir, diger manager'lar fallback kullanir.
    /// </summary>
    public class FirebaseManager : IFirebaseManager
    {
        // =====================================================================
        // Singleton
        // =====================================================================

        private static FirebaseManager _instance;

        /// <summary>Singleton instance. Lazy initialization ile olusturulur.</summary>
        public static FirebaseManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new FirebaseManager();
                }
                return _instance;
            }
        }

        // =====================================================================
        // Properties
        // =====================================================================

        /// <summary>Firebase SDK basariyla baslatildi mi?</summary>
        public bool IsInitialized { get; private set; }

        /// <summary>Baslatma sureci tamamlandi mi (basarili veya basarisiz)?</summary>
        public bool IsReady { get; private set; }

        /// <summary>Baslatma durumu degistiginde tetiklenir. true = basarili.</summary>
        public event Action<bool> OnInitialized;

#if FIREBASE_ENABLED
        /// <summary>Firebase uygulamasi referansi.</summary>
        public FirebaseApp App { get; private set; }
#endif

        // =====================================================================
        // Constructor (private — Singleton)
        // =====================================================================

        private FirebaseManager() { }

        // =====================================================================
        // Baslatma
        // =====================================================================

        /// <summary>
        /// Firebase SDK'yi baslatir. CheckAndFixDependenciesAsync ile
        /// gerekli bagimliliklari kontrol eder ve duzeltir.
        /// Firebase yokken hemen "hazir degil" durumuna gecer.
        /// </summary>
        public async Task InitializeAsync()
        {
            if (IsReady)
            {
                Debug.LogWarning("[FirebaseManager] Zaten baslatildi, tekrar baslatma atlanıyor.");
                return;
            }

#if FIREBASE_ENABLED
            try
            {
                Debug.Log("[FirebaseManager] Firebase bagimliliklari kontrol ediliyor...");

                var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();

                if (dependencyStatus == DependencyStatus.Available)
                {
                    App = FirebaseApp.DefaultInstance;
                    IsInitialized = true;
                    IsReady = true;

                    Debug.Log("[FirebaseManager] Firebase basariyla baslatildi.");
                }
                else
                {
                    IsInitialized = false;
                    IsReady = true;

                    Debug.LogError($"[FirebaseManager] Firebase bagimliliklari cozulemedi: {dependencyStatus}");
                }
            }
            catch (Exception ex)
            {
                IsInitialized = false;
                IsReady = true;

                Debug.LogError($"[FirebaseManager] Firebase baslatma hatasi: {ex.Message}");
            }
#else
            // Firebase SDK yok — fallback mod
            IsInitialized = false;
            IsReady = true;

            Debug.Log("[FirebaseManager] FIREBASE_ENABLED tanimli degil. Offline/fallback modda calisiyor.");

            await Task.CompletedTask;
#endif

            OnInitialized?.Invoke(IsInitialized);
        }

        // =====================================================================
        // Yardimci
        // =====================================================================

        /// <summary>
        /// Test ve sahne gecisleri icin state sifirlama.
        /// Uretim kodunda kullanilmamali.
        /// </summary>
        internal static void ResetForTesting()
        {
            if (_instance != null)
            {
                _instance.IsInitialized = false;
                _instance.IsReady = false;
#if FIREBASE_ENABLED
                _instance.App = null;
#endif
            }
            _instance = null;
        }
    }
}
