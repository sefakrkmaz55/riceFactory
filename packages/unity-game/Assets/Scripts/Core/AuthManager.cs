// =============================================================================
// AuthManager.cs
// Firebase Authentication wrapper. Anonim giris, Google/Apple hesap baglama,
// oturum durumu yonetimi.
// FIREBASE_ENABLED tanimli degilse sahte kullanici kimligiyle calisir.
// =============================================================================

using System;
using System.Threading.Tasks;
using UnityEngine;

#if FIREBASE_ENABLED
using Firebase.Auth;
#endif

namespace RiceFactory.Core
{
    // -------------------------------------------------------------------------
    // Auth Manager Arayuzu
    // -------------------------------------------------------------------------

    /// <summary>
    /// Kimlik dogrulama islemlerini tanimlayan arayuz.
    /// </summary>
    public interface IAuthManager
    {
        /// <summary>Kullanici oturum acmis mi?</summary>
        bool IsSignedIn { get; }

        /// <summary>Kullanici anonim mi?</summary>
        bool IsAnonymous { get; }

        /// <summary>Aktif kullanicinin benzersiz kimlik numarasi.</summary>
        string UserId { get; }

        /// <summary>Kullanicinin gorunen adi (varsa).</summary>
        string DisplayName { get; }

        /// <summary>Auth durumu degistiginde tetiklenir.</summary>
        event Action<AuthState> OnAuthStateChanged;

        /// <summary>Auth baslatma ve anonim giris.</summary>
        Task InitializeAsync();

        /// <summary>Anonim hesabi Google hesabina bagla.</summary>
        Task<bool> LinkWithGoogleAsync();

        /// <summary>Anonim hesabi Apple hesabina bagla (iOS).</summary>
        Task<bool> LinkWithAppleAsync();

        /// <summary>Oturumu kapat.</summary>
        void SignOut();
    }

    // -------------------------------------------------------------------------
    // Auth State
    // -------------------------------------------------------------------------

    /// <summary>Kimlik dogrulama durum bilgisi.</summary>
    public struct AuthState
    {
        public bool IsSignedIn;
        public bool IsAnonymous;
        public string UserId;
        public string DisplayName;
    }

    // -------------------------------------------------------------------------
    // Auth Manager Implementasyonu
    // -------------------------------------------------------------------------

    /// <summary>
    /// Firebase Authentication wrapper.
    /// - Ilk acilista otomatik anonim giris.
    /// - Google/Apple hesap baglama destegi.
    /// - Firebase yokken sahte (mock) kimlik ile calisir.
    /// </summary>
    public class AuthManager : IAuthManager
    {
        // =====================================================================
        // Sabitler
        // =====================================================================

        private const string PREFS_OFFLINE_USER_ID = "rf_offline_user_id";

        // =====================================================================
        // Properties
        // =====================================================================

        public bool IsSignedIn { get; private set; }
        public bool IsAnonymous { get; private set; } = true;
        public string UserId { get; private set; }
        public string DisplayName { get; private set; } = "";

        public event Action<AuthState> OnAuthStateChanged;

        // =====================================================================
        // Firebase referanslari
        // =====================================================================

#if FIREBASE_ENABLED
        private FirebaseAuth _auth;
#endif

        private readonly IFirebaseManager _firebaseManager;

        // =====================================================================
        // Constructor
        // =====================================================================

        public AuthManager(IFirebaseManager firebaseManager)
        {
            _firebaseManager = firebaseManager ?? throw new ArgumentNullException(nameof(firebaseManager));
        }

        // =====================================================================
        // Baslatma
        // =====================================================================

        /// <summary>
        /// Auth sistemini baslatir. Firebase varsa SDK uzerinden,
        /// yoksa lokal sahte kullanici olusturur.
        /// </summary>
        public async Task InitializeAsync()
        {
#if FIREBASE_ENABLED
            if (_firebaseManager.IsInitialized)
            {
                _auth = FirebaseAuth.DefaultInstance;
                _auth.StateChanged += OnFirebaseAuthStateChanged;

                if (_auth.CurrentUser == null)
                {
                    // Ilk acilis: anonim giris
                    try
                    {
                        var result = await _auth.SignInAnonymouslyAsync();
                        UpdateStateFromFirebase();
                        Debug.Log($"[AuthManager] Anonim giris basarili. UID: {UserId}");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[AuthManager] Anonim giris hatasi: {ex.Message}");
                        FallbackToOfflineUser();
                    }
                }
                else
                {
                    UpdateStateFromFirebase();
                    Debug.Log($"[AuthManager] Mevcut oturum. UID: {UserId}, Anonim: {IsAnonymous}");
                }

                return;
            }
#endif
            // Firebase yok veya baslatma basarisiz — offline mod
            FallbackToOfflineUser();
            await Task.CompletedTask;
        }

        // =====================================================================
        // Google Sign-In Baglama
        // =====================================================================

        /// <summary>
        /// Anonim hesabi Google hesabina baglar.
        /// Firebase yokken her zaman false dondurur.
        /// </summary>
        public async Task<bool> LinkWithGoogleAsync()
        {
#if FIREBASE_ENABLED
            if (!_firebaseManager.IsInitialized || _auth?.CurrentUser == null)
            {
                Debug.LogWarning("[AuthManager] Google baglama: Firebase aktif degil.");
                return false;
            }

            try
            {
                // Google Sign-In SDK ile token alinir.
                // Not: GoogleSignIn paketi ayri olarak eklenmeli.
                // string idToken = await GoogleSignIn.DefaultInstance.SignIn();
                // var credential = GoogleAuthProvider.GetCredential(idToken, null);
                // await _auth.CurrentUser.LinkWithCredentialAsync(credential);

                // TODO: Google Sign-In SDK entegrasyonu tamamlandiginda ustteki
                // satirlar aktif edilecek. Simdilik placeholder:
                Debug.LogWarning("[AuthManager] Google Sign-In SDK henuz entegre edilmedi.");
                await Task.CompletedTask;
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AuthManager] Google baglama hatasi: {ex.Message}");
                return false;
            }
#else
            Debug.Log("[AuthManager] Google baglama: Firebase aktif degil, islem atlanıyor.");
            await Task.CompletedTask;
            return false;
#endif
        }

        // =====================================================================
        // Apple Sign-In Baglama
        // =====================================================================

        /// <summary>
        /// Anonim hesabi Apple hesabina baglar (iOS).
        /// Firebase yokken her zaman false dondurur.
        /// </summary>
        public async Task<bool> LinkWithAppleAsync()
        {
#if FIREBASE_ENABLED
            if (!_firebaseManager.IsInitialized || _auth?.CurrentUser == null)
            {
                Debug.LogWarning("[AuthManager] Apple baglama: Firebase aktif degil.");
                return false;
            }

            try
            {
                // Sign in with Apple SDK kullanimi
                // Not: Apple Sign-In paketi ayri olarak eklenmeli.
                // var rawNonce = GenerateNonce();
                // var appleResult = await SignInWithApple.LoginAsync(rawNonce);
                // var credential = OAuthProvider.GetCredential(
                //     "apple.com", appleResult.IdToken, rawNonce, appleResult.AuthorizationCode);
                // await _auth.CurrentUser.LinkWithCredentialAsync(credential);

                // TODO: Apple Sign-In SDK entegrasyonu tamamlandiginda ustteki
                // satirlar aktif edilecek. Simdilik placeholder:
                Debug.LogWarning("[AuthManager] Apple Sign-In SDK henuz entegre edilmedi.");
                await Task.CompletedTask;
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AuthManager] Apple baglama hatasi: {ex.Message}");
                return false;
            }
#else
            Debug.Log("[AuthManager] Apple baglama: Firebase aktif degil, islem atlanıyor.");
            await Task.CompletedTask;
            return false;
#endif
        }

        // =====================================================================
        // Cikis Yapma
        // =====================================================================

        /// <summary>
        /// Aktif oturumu kapatir. Firebase yokken state sifirlanir.
        /// </summary>
        public void SignOut()
        {
#if FIREBASE_ENABLED
            if (_firebaseManager.IsInitialized && _auth != null)
            {
                _auth.SignOut();
                Debug.Log("[AuthManager] Firebase oturumu kapatildi.");
                // State, OnFirebaseAuthStateChanged callback'inde guncellenecek.
                return;
            }
#endif
            // Offline mod — state sifirla
            IsSignedIn = false;
            UserId = "";
            DisplayName = "";
            IsAnonymous = true;

            Debug.Log("[AuthManager] Offline oturum kapatildi.");
            NotifyStateChanged();
        }

        // =====================================================================
        // Dahili Yardimcilar
        // =====================================================================

#if FIREBASE_ENABLED
        /// <summary>Firebase auth state callback.</summary>
        private void OnFirebaseAuthStateChanged(object sender, EventArgs e)
        {
            UpdateStateFromFirebase();
            NotifyStateChanged();
        }

        /// <summary>Firebase kullanici bilgilerinden state guncelle.</summary>
        private void UpdateStateFromFirebase()
        {
            var user = _auth?.CurrentUser;
            if (user != null)
            {
                IsSignedIn = true;
                UserId = user.UserId;
                IsAnonymous = user.IsAnonymous;
                DisplayName = user.DisplayName ?? "";
            }
            else
            {
                IsSignedIn = false;
                UserId = "";
                IsAnonymous = true;
                DisplayName = "";
            }
        }
#endif

        /// <summary>
        /// Firebase olmadan veya baslatma basarisiz oldugunda
        /// lokal sahte kullanici olusturur.
        /// </summary>
        private void FallbackToOfflineUser()
        {
            // Daha once olusturulmus offline ID varsa onu kullan
            UserId = PlayerPrefs.GetString(PREFS_OFFLINE_USER_ID, "");

            if (string.IsNullOrEmpty(UserId))
            {
                UserId = "offline_" + Guid.NewGuid().ToString("N").Substring(0, 12);
                PlayerPrefs.SetString(PREFS_OFFLINE_USER_ID, UserId);
                PlayerPrefs.Save();
            }

            IsSignedIn = true;
            IsAnonymous = true;
            DisplayName = "";

            Debug.Log($"[AuthManager] Offline mod aktif. Sahte UID: {UserId}");
            NotifyStateChanged();
        }

        /// <summary>Abone olanlara durum degisikligini bildirir.</summary>
        private void NotifyStateChanged()
        {
            OnAuthStateChanged?.Invoke(new AuthState
            {
                IsSignedIn = IsSignedIn,
                IsAnonymous = IsAnonymous,
                UserId = UserId,
                DisplayName = DisplayName
            });
        }

#if FIREBASE_ENABLED
        /// <summary>Apple Sign-In icin kriptografik nonce uretir.</summary>
        private string GenerateNonce()
        {
            var bytes = new byte[32];
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }
#endif
    }
}
