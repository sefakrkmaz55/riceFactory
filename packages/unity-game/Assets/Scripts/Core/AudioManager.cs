// =============================================================================
// AudioManager.cs
// Ses yonetimi: BGM ve SFX ayri kontrol, ses seviyesi ayarlari.
// Object pooling ile SFX performansi optimize edilir.
// DontDestroyOnLoad ile sahne gecislerinde hayatta kalir.
// Ses ayarlari PlayerPrefs uzerinden kalici saklanir.
// =============================================================================

using System.Collections.Generic;
using UnityEngine;

namespace RiceFactory.Core
{
    // -------------------------------------------------------------------------
    // Ses Kanali Enum'u
    // -------------------------------------------------------------------------

    /// <summary>Ses seviyesi ayarlanabilecek kanallar.</summary>
    public enum AudioChannel
    {
        Master,
        Music,
        SFX
    }

    // -------------------------------------------------------------------------
    // Audio Manager Arayuzu
    // -------------------------------------------------------------------------

    /// <summary>AudioManager icin interface.</summary>
    public interface IAudioManager
    {
        float MasterVolume { get; }
        float MusicVolume { get; }
        float SFXVolume { get; }
        bool IsMusicPlaying { get; }

        /// <summary>BGM acik/kapali durumu.</summary>
        bool IsBGMEnabled { get; }

        /// <summary>SFX acik/kapali durumu.</summary>
        bool IsSFXEnabled { get; }

        /// <summary>BGM ses seviyesi (0-1).</summary>
        float BGMVolume { get; }

        void PlayBGM(AudioClip clip, bool loop = true, float fadeInDuration = 1f);
        void StopBGM(float fadeOutDuration = 1f);
        void PlaySFX(AudioClip clip, float pitchVariation = 0.05f);

        /// <summary>String isimle SFX calar (kayitli kliplerden).</summary>
        void PlaySFX(string clipName);

        void SetVolume(AudioChannel channel, float volume);
        void SaveVolumeSettings();
        void LoadVolumeSettings();

        /// <summary>BGM'yi acip kapatir.</summary>
        void SetBGMEnabled(bool enabled);

        /// <summary>SFX'i acip kapatir.</summary>
        void SetSFXEnabled(bool enabled);

        /// <summary>BGM ses seviyesini ayarlar (0-1).</summary>
        void SetBGMVolume(float volume);

        /// <summary>SFX ses seviyesini ayarlar (0-1).</summary>
        void SetSFXVolume(float volume);
    }

    // -------------------------------------------------------------------------
    // Audio Manager Implementasyonu (MonoBehaviour)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Merkezi ses yoneticisi. BGM (arka plan muzigi) ve SFX (ses efektleri)
    /// ayri kontrol edilir. SFX icin object pooling kullanilir.
    ///
    /// Singleton + DontDestroyOnLoad pattern.
    /// Boot sahnesinde olusturulur ve tum oyun boyunca kalir.
    /// </summary>
    public class AudioManager : MonoBehaviour, IAudioManager
    {
        // =====================================================================
        // Singleton
        // =====================================================================

        public static AudioManager Instance { get; private set; }

        // =====================================================================
        // Ses Seviyeleri
        // =====================================================================

        public float MasterVolume { get; private set; } = 1f;
        public float MusicVolume { get; private set; } = 0.7f;
        public float SFXVolume { get; private set; } = 1f;
        public bool IsMusicPlaying => _musicSource != null && _musicSource.isPlaying;

        // BGM/SFX acik-kapali durumlari
        public bool IsBGMEnabled { get; private set; } = true;
        public bool IsSFXEnabled { get; private set; } = true;

        /// <summary>BGM ses seviyesi (interface property).</summary>
        public float BGMVolume => MusicVolume;

        // =====================================================================
        // Dahili Referanslar
        // =====================================================================

        private AudioSource _musicSource;
        private readonly Queue<AudioSource> _sfxPool = new();
        private readonly List<AudioSource> _activeSfxSources = new();
        private Transform _sfxContainer;

        // Object pool ayarlari
        private const int INITIAL_POOL_SIZE = 5;
        private const int MAX_POOL_SIZE = 15;

        // Fade
        private float _fadeTarget;
        private float _fadeDuration;
        private float _fadeTimer;
        private bool _isFading;
        private bool _stopAfterFade;
        private AudioClip _pendingBGMClip;
        private bool _pendingBGMLoop;

        // PlayerPrefs keys
        private const string KEY_MASTER_VOL = "audio_master_volume";
        private const string KEY_MUSIC_VOL = "audio_music_volume";
        private const string KEY_SFX_VOL = "audio_sfx_volume";

        // =====================================================================
        // Unity Lifecycle
        // =====================================================================

        private void Awake()
        {
            // Singleton kontrolu
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeAudioSources();
            LoadVolumeSettings();
        }

        private void Update()
        {
            ProcessFade();
            CleanupFinishedSFX();
        }

        // =====================================================================
        // Baslatma
        // =====================================================================

        /// <summary>Music source ve SFX pool'u olusturur.</summary>
        private void InitializeAudioSources()
        {
            // Music source
            var musicObj = new GameObject("MusicSource");
            musicObj.transform.SetParent(transform);
            _musicSource = musicObj.AddComponent<AudioSource>();
            _musicSource.playOnAwake = false;
            _musicSource.loop = true;
            _musicSource.volume = MusicVolume * MasterVolume;

            // SFX container
            var sfxObj = new GameObject("SFXPool");
            sfxObj.transform.SetParent(transform);
            _sfxContainer = sfxObj.transform;

            // Baslangic havuzu olustur
            for (int i = 0; i < INITIAL_POOL_SIZE; i++)
            {
                CreateSFXSource();
            }
        }

        /// <summary>Havuz icin yeni bir AudioSource olusturur.</summary>
        private AudioSource CreateSFXSource()
        {
            var obj = new GameObject("SFXSource");
            obj.transform.SetParent(_sfxContainer);
            var source = obj.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            _sfxPool.Enqueue(source);
            return source;
        }

        // =====================================================================
        // BGM (Arka Plan Muzigi)
        // =====================================================================

        /// <summary>
        /// Arka plan muzigini baslatir. Mevcut muzik varsa crossfade yapar.
        /// </summary>
        /// <param name="clip">Calinacak muzik klibi.</param>
        /// <param name="loop">Tekrar etsin mi.</param>
        /// <param name="fadeInDuration">Gecis suresi (saniye).</param>
        public void PlayBGM(AudioClip clip, bool loop = true, float fadeInDuration = 1f)
        {
            if (clip == null)
            {
                Debug.LogWarning("[AudioManager] PlayBGM: clip null.");
                return;
            }

            // Ayni muzik zaten caliyorsa bir sey yapma
            if (_musicSource.clip == clip && _musicSource.isPlaying) return;

            if (_musicSource.isPlaying && fadeInDuration > 0f)
            {
                // Mevcut muzigi fade-out yap, sonra yenisini baslat
                _pendingBGMClip = clip;
                _pendingBGMLoop = loop;
                StartFade(0f, fadeInDuration * 0.5f, stopAfterFade: true);
            }
            else
            {
                // Direkt baslat
                _musicSource.clip = clip;
                _musicSource.loop = loop;
                _musicSource.volume = 0f;
                _musicSource.Play();
                StartFade(MusicVolume * MasterVolume, fadeInDuration, stopAfterFade: false);
            }
        }

        /// <summary>
        /// Arka plan muzigini durdurur.
        /// </summary>
        /// <param name="fadeOutDuration">Fade-out suresi (saniye).</param>
        public void StopBGM(float fadeOutDuration = 1f)
        {
            if (!_musicSource.isPlaying) return;

            _pendingBGMClip = null;

            if (fadeOutDuration > 0f)
            {
                StartFade(0f, fadeOutDuration, stopAfterFade: true);
            }
            else
            {
                _musicSource.Stop();
                _musicSource.clip = null;
            }
        }

        // =====================================================================
        // SFX (Ses Efektleri)
        // =====================================================================

        /// <summary>
        /// Object pool'dan bir AudioSource alarak ses efekti calar.
        /// Kucuk pitch varyasyonu ile tekrarlayan seslerin monoton olmasi engellenir.
        /// </summary>
        /// <param name="clip">Calinacak ses klibi.</param>
        /// <param name="pitchVariation">Pitch varyasyon araligi (0.05 = +-5%).</param>
        public void PlaySFX(AudioClip clip, float pitchVariation = 0.05f)
        {
            if (clip == null) return;

            var source = GetSFXSource();
            if (source == null)
            {
                Debug.LogWarning("[AudioManager] SFX havuzu dolu, ses atilanyor.");
                return;
            }

            source.clip = clip;
            source.volume = SFXVolume * MasterVolume;
            source.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
            source.Play();

            _activeSfxSources.Add(source);
        }

        /// <summary>Havuzdan bos bir SFX source alir. Yoksa yeni olusturur.</summary>
        private AudioSource GetSFXSource()
        {
            // Havuzdan al
            while (_sfxPool.Count > 0)
            {
                var source = _sfxPool.Dequeue();
                if (source != null) return source;
            }

            // Havuz bos, yeni olustur (limit dahilinde)
            if (_activeSfxSources.Count < MAX_POOL_SIZE)
            {
                return CreateSFXSource();
            }

            return null;
        }

        /// <summary>Bitmis SFX source'larini havuza geri koyar.</summary>
        private void CleanupFinishedSFX()
        {
            for (int i = _activeSfxSources.Count - 1; i >= 0; i--)
            {
                var source = _activeSfxSources[i];
                if (source == null)
                {
                    _activeSfxSources.RemoveAt(i);
                    continue;
                }

                if (!source.isPlaying)
                {
                    source.clip = null;
                    _sfxPool.Enqueue(source);
                    _activeSfxSources.RemoveAt(i);
                }
            }
        }

        // =====================================================================
        // Ses Seviyesi Ayarlari
        // =====================================================================

        /// <summary>
        /// Belirli bir ses kanalinin seviyesini ayarlar.
        /// Deger 0-1 arasinda clamp edilir.
        /// </summary>
        public void SetVolume(AudioChannel channel, float volume)
        {
            volume = Mathf.Clamp01(volume);

            switch (channel)
            {
                case AudioChannel.Master:
                    MasterVolume = volume;
                    break;
                case AudioChannel.Music:
                    MusicVolume = volume;
                    break;
                case AudioChannel.SFX:
                    SFXVolume = volume;
                    break;
            }

            // Aktif muzik source'unu guncelle
            if (!_isFading)
            {
                _musicSource.volume = MusicVolume * MasterVolume;
            }

            SaveVolumeSettings();
        }

        /// <summary>Ses ayarlarini PlayerPrefs'e kaydeder.</summary>
        public void SaveVolumeSettings()
        {
            PlayerPrefs.SetFloat(KEY_MASTER_VOL, MasterVolume);
            PlayerPrefs.SetFloat(KEY_MUSIC_VOL, MusicVolume);
            PlayerPrefs.SetFloat(KEY_SFX_VOL, SFXVolume);
            PlayerPrefs.Save();
        }

        /// <summary>Ses ayarlarini PlayerPrefs'ten yukler.</summary>
        public void LoadVolumeSettings()
        {
            MasterVolume = PlayerPrefs.GetFloat(KEY_MASTER_VOL, 1f);
            MusicVolume = PlayerPrefs.GetFloat(KEY_MUSIC_VOL, 0.7f);
            SFXVolume = PlayerPrefs.GetFloat(KEY_SFX_VOL, 1f);
            IsBGMEnabled = PlayerPrefs.GetInt("audio_bgm_enabled", 1) == 1;
            IsSFXEnabled = PlayerPrefs.GetInt("audio_sfx_enabled", 1) == 1;

            if (_musicSource != null)
            {
                _musicSource.volume = MusicVolume * MasterVolume;
            }
        }

        // =====================================================================
        // BGM/SFX Acma-Kapama ve Kolaylik Metotlari
        // =====================================================================

        /// <summary>BGM'yi acar veya kapatir. Kapatildiginda muzik durdurulur.</summary>
        public void SetBGMEnabled(bool enabled)
        {
            IsBGMEnabled = enabled;
            PlayerPrefs.SetInt("audio_bgm_enabled", enabled ? 1 : 0);
            PlayerPrefs.Save();

            if (!enabled)
            {
                StopBGM(0.3f);
            }
            else if (_musicSource != null)
            {
                _musicSource.volume = MusicVolume * MasterVolume;
            }
        }

        /// <summary>SFX'i acar veya kapatir.</summary>
        public void SetSFXEnabled(bool enabled)
        {
            IsSFXEnabled = enabled;
            PlayerPrefs.SetInt("audio_sfx_enabled", enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        /// <summary>BGM ses seviyesini ayarlar (0-1).</summary>
        public void SetBGMVolume(float volume)
        {
            SetVolume(AudioChannel.Music, volume);
        }

        /// <summary>SFX ses seviyesini ayarlar (0-1).</summary>
        public void SetSFXVolume(float volume)
        {
            SetVolume(AudioChannel.SFX, volume);
        }

        /// <summary>
        /// String isimle SFX calar. Resources/Audio/SFX/ klasorunden yukler.
        /// Bulunamazsa uyari verir.
        /// </summary>
        public void PlaySFX(string clipName)
        {
            if (!IsSFXEnabled || string.IsNullOrEmpty(clipName)) return;

            var clip = Resources.Load<AudioClip>($"Audio/SFX/{clipName}");
            if (clip == null)
            {
                Debug.LogWarning($"[AudioManager] SFX bulunamadi: Audio/SFX/{clipName}");
                return;
            }

            PlaySFX(clip);
        }

        // =====================================================================
        // Fade Sistemi
        // =====================================================================

        /// <summary>Muzik volume fade baslatir.</summary>
        private void StartFade(float target, float duration, bool stopAfterFade)
        {
            _fadeTarget = target;
            _fadeDuration = Mathf.Max(duration, 0.01f);
            _fadeTimer = 0f;
            _isFading = true;
            _stopAfterFade = stopAfterFade;
        }

        /// <summary>Her frame fade durumunu isler.</summary>
        private void ProcessFade()
        {
            if (!_isFading) return;

            _fadeTimer += Time.deltaTime;
            float t = Mathf.Clamp01(_fadeTimer / _fadeDuration);
            _musicSource.volume = Mathf.Lerp(_musicSource.volume, _fadeTarget, t);

            if (t >= 1f)
            {
                _isFading = false;
                _musicSource.volume = _fadeTarget;

                if (_stopAfterFade)
                {
                    _musicSource.Stop();

                    // Bekleyen yeni muzik varsa baslat
                    if (_pendingBGMClip != null)
                    {
                        _musicSource.clip = _pendingBGMClip;
                        _musicSource.loop = _pendingBGMLoop;
                        _musicSource.volume = 0f;
                        _musicSource.Play();

                        _pendingBGMClip = null;
                        StartFade(MusicVolume * MasterVolume, _fadeDuration, stopAfterFade: false);
                    }
                }
            }
        }
    }
}
