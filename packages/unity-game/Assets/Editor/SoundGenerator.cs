// =============================================================================
// SoundGenerator.cs
// Programatik ses efektleri ureticisi. Sine/square wave ile WAV dosyalari olusturur.
// Batch mode: -executeMethod RiceFactory.Editor.SoundGenerator.SetupFromCommandLine
// =============================================================================

#if UNITY_EDITOR

using System;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace RiceFactory.Editor
{
    public static class SoundGenerator
    {
        private const string TAG = "[SoundGenerator]";
        private const string SFX_DIR = "Assets/Audio/SFX";
        private const string RESOURCES_SFX_DIR = "Assets/Resources/Audio/SFX";
        private const int SAMPLE_RATE = 44100;

        // =================================================================
        // Giris Noktalari
        // =================================================================

        [MenuItem("RiceFactory/Generate Sound Effects")]
        public static void GenerateAll()
        {
            Debug.Log($"{TAG} Ses efektleri uretimi basliyor...");

            EnsureDirectory(SFX_DIR);
            EnsureDirectory(RESOURCES_SFX_DIR);

            GenerateCoinSound();
            GenerateUpgradeSound();
            GenerateProductionSound();
            GeneratePrestigeSound();
            GenerateButtonSound();
            GenerateUnlockSound();
            GenerateErrorSound();
            GenerateLevelUpSound();
            GenerateCollectSound();

            AssetDatabase.Refresh();
            Debug.Log($"{TAG} Tum ses efektleri basariyla uretildi.");
        }

        /// <summary>
        /// Batch mode giris noktasi.
        /// Unity -batchmode -executeMethod RiceFactory.Editor.SoundGenerator.SetupFromCommandLine -quit
        /// </summary>
        public static void SetupFromCommandLine()
        {
            Debug.Log($"{TAG} Batch mode ses uretimi basliyor...");
            GenerateAll();
            Debug.Log($"{TAG} Batch mode ses uretimi tamamlandi.");
        }

        // =================================================================
        // Ses Efektleri
        // =================================================================

        /// <summary>Kisa tingirdi: yuksek frekans, 0.15s, ascending pitch.</summary>
        private static void GenerateCoinSound()
        {
            float duration = 0.15f;
            int samples = (int)(SAMPLE_RATE * duration);
            var data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                float normalizedT = (float)i / samples;

                // Ascending pitch: 800Hz -> 2400Hz
                float freq = Mathf.Lerp(800f, 2400f, normalizedT);
                float phase = 2f * Mathf.PI * freq * t;

                // Sine wave + hafif harmonik
                float sample = Mathf.Sin(phase) * 0.7f + Mathf.Sin(phase * 2f) * 0.2f;

                // Envelope: hizli basla, hizli bit
                float envelope = 1f - normalizedT;
                envelope *= envelope; // Exponential decay

                data[i] = sample * envelope * 0.8f;
            }

            SaveWav("sfx_coin", data, duration);
        }

        /// <summary>Yukselen ton: 0.3s, sweep up.</summary>
        private static void GenerateUpgradeSound()
        {
            float duration = 0.3f;
            int samples = (int)(SAMPLE_RATE * duration);
            var data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                float normalizedT = (float)i / samples;

                // Sweep up: 300Hz -> 1200Hz
                float freq = Mathf.Lerp(300f, 1200f, normalizedT * normalizedT);
                float phase = 2f * Mathf.PI * freq * t;

                float sample = Mathf.Sin(phase) * 0.6f + Mathf.Sin(phase * 1.5f) * 0.3f;

                // Envelope: fade in sonra fade out
                float envelope = Mathf.Sin(normalizedT * Mathf.PI);

                data[i] = sample * envelope * 0.7f;
            }

            SaveWav("sfx_upgrade", data, duration);
        }

        /// <summary>Tiklama/pop: 0.1s, snappy.</summary>
        private static void GenerateProductionSound()
        {
            float duration = 0.1f;
            int samples = (int)(SAMPLE_RATE * duration);
            var data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                float normalizedT = (float)i / samples;

                // Pop: yuksek frekanstan dusuke
                float freq = Mathf.Lerp(1500f, 400f, normalizedT);
                float phase = 2f * Mathf.PI * freq * t;

                float sample = Mathf.Sin(phase);

                // Cok hizli decay
                float envelope = Mathf.Pow(1f - normalizedT, 4f);

                data[i] = sample * envelope * 0.6f;
            }

            SaveWav("sfx_production", data, duration);
        }

        /// <summary>Fanfare: 0.5s, ascending chord.</summary>
        private static void GeneratePrestigeSound()
        {
            float duration = 0.5f;
            int samples = (int)(SAMPLE_RATE * duration);
            var data = new float[samples];

            // 3 nota: C5 (523Hz), E5 (659Hz), G5 (784Hz) ardisik
            float[] freqs = { 523f, 659f, 784f };
            float noteLength = duration / 3f;

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                float normalizedT = (float)i / samples;

                // Hangi nota
                int noteIndex = Mathf.Min((int)(normalizedT * 3f), 2);
                float noteT = (normalizedT * 3f) - noteIndex;

                float freq = freqs[noteIndex];
                float phase = 2f * Mathf.PI * freq * t;

                // Majör akort + harmonikler
                float sample = Mathf.Sin(phase) * 0.5f
                             + Mathf.Sin(phase * 2f) * 0.2f
                             + Mathf.Sin(phase * 3f) * 0.1f;

                // Her nota icin envelope
                float envelope = Mathf.Sin(noteT * Mathf.PI);
                // Genel fade out
                float globalEnv = 1f - normalizedT * 0.3f;

                data[i] = sample * envelope * globalEnv * 0.7f;
            }

            SaveWav("sfx_prestige", data, duration);
        }

        /// <summary>Hafif tik: 0.05s, clean click.</summary>
        private static void GenerateButtonSound()
        {
            float duration = 0.05f;
            int samples = (int)(SAMPLE_RATE * duration);
            var data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                float normalizedT = (float)i / samples;

                // Kisa, net tiklama
                float freq = 1000f;
                float phase = 2f * Mathf.PI * freq * t;

                float sample = Mathf.Sin(phase);

                // Anlik basla, hizli bit
                float envelope = Mathf.Pow(1f - normalizedT, 6f);

                data[i] = sample * envelope * 0.5f;
            }

            SaveWav("sfx_button", data, duration);
        }

        /// <summary>Acilma sesi: 0.4s, chime.</summary>
        private static void GenerateUnlockSound()
        {
            float duration = 0.4f;
            int samples = (int)(SAMPLE_RATE * duration);
            var data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                float normalizedT = (float)i / samples;

                // Chime: iki frekans ust uste
                float freq1 = 880f;  // A5
                float freq2 = 1320f; // E6
                float phase1 = 2f * Mathf.PI * freq1 * t;
                float phase2 = 2f * Mathf.PI * freq2 * t;

                // Ikinci ton biraz gecikmeyle baslar
                float mix2 = normalizedT > 0.15f ? 1f : normalizedT / 0.15f;

                float sample = Mathf.Sin(phase1) * 0.5f + Mathf.Sin(phase2) * 0.4f * mix2;

                // Envelope: hizli atak, yavas decay
                float envelope;
                if (normalizedT < 0.05f)
                    envelope = normalizedT / 0.05f;
                else
                    envelope = Mathf.Pow(1f - (normalizedT - 0.05f) / 0.95f, 2f);

                data[i] = sample * envelope * 0.7f;
            }

            SaveWav("sfx_unlock", data, duration);
        }

        /// <summary>Dusuk ton buzz: 0.2s, descending.</summary>
        private static void GenerateErrorSound()
        {
            float duration = 0.2f;
            int samples = (int)(SAMPLE_RATE * duration);
            var data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                float normalizedT = (float)i / samples;

                // Descending: 300Hz -> 150Hz
                float freq = Mathf.Lerp(300f, 150f, normalizedT);
                float phase = 2f * Mathf.PI * freq * t;

                // Square wave benzeri (sert ses)
                float sample = Mathf.Sin(phase) > 0 ? 0.5f : -0.5f;
                // Biraz sine karistir (yumusatma)
                sample = sample * 0.6f + Mathf.Sin(phase) * 0.4f;

                // Envelope
                float envelope = 1f - normalizedT;

                data[i] = sample * envelope * 0.5f;
            }

            SaveWav("sfx_error", data, duration);
        }

        /// <summary>Kutlama: 0.6s, ascending arpeggio.</summary>
        private static void GenerateLevelUpSound()
        {
            float duration = 0.6f;
            int samples = (int)(SAMPLE_RATE * duration);
            var data = new float[samples];

            // C major arpeggio: C5, E5, G5, C6
            float[] freqs = { 523f, 659f, 784f, 1047f };
            float noteLength = duration / 4f;

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                float normalizedT = (float)i / samples;

                int noteIndex = Mathf.Min((int)(normalizedT * 4f), 3);
                float noteT = (normalizedT * 4f) - noteIndex;

                float freq = freqs[noteIndex];
                float phase = 2f * Mathf.PI * freq * t;

                float sample = Mathf.Sin(phase) * 0.5f
                             + Mathf.Sin(phase * 2f) * 0.25f
                             + Mathf.Sin(phase * 3f) * 0.1f;

                // Her nota icin parlak envelope
                float noteEnvelope = Mathf.Sin(noteT * Mathf.PI);
                noteEnvelope = Mathf.Max(noteEnvelope, 0.3f * (1f - noteT));

                // Genel parlaklik artisi
                float globalEnv = 0.6f + normalizedT * 0.4f;
                // Son %20'de fade out
                if (normalizedT > 0.8f)
                    globalEnv *= (1f - normalizedT) / 0.2f;

                data[i] = sample * noteEnvelope * globalEnv * 0.7f;
            }

            SaveWav("sfx_levelup", data, duration);
        }

        /// <summary>Para toplama: 0.2s, bright ding.</summary>
        private static void GenerateCollectSound()
        {
            float duration = 0.2f;
            int samples = (int)(SAMPLE_RATE * duration);
            var data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                float normalizedT = (float)i / samples;

                // Bright ding: yuksek frekans
                float freq = 1760f; // A6
                float phase = 2f * Mathf.PI * freq * t;

                float sample = Mathf.Sin(phase) * 0.6f
                             + Mathf.Sin(phase * 2f) * 0.2f;

                // Bell-like envelope
                float envelope;
                if (normalizedT < 0.02f)
                    envelope = normalizedT / 0.02f;
                else
                    envelope = Mathf.Pow(1f - (normalizedT - 0.02f) / 0.98f, 3f);

                data[i] = sample * envelope * 0.6f;
            }

            SaveWav("sfx_collect", data, duration);
        }

        // =================================================================
        // WAV Dosya Yazimi
        // =================================================================

        /// <summary>Float sample verisini WAV dosyasi olarak kaydeder (mono, 16-bit).</summary>
        private static void SaveWav(string name, float[] samples, float duration)
        {
            int sampleCount = samples.Length;
            int byteRate = SAMPLE_RATE * 2; // 16-bit mono
            int dataSize = sampleCount * 2;

            // Assets/Audio/SFX/ altina kaydet
            string sfxPath = Path.Combine(Application.dataPath, "Audio", "SFX");
            if (!Directory.Exists(sfxPath))
                Directory.CreateDirectory(sfxPath);

            string filePath = Path.Combine(sfxPath, name + ".wav");

            // Ayrica Resources/Audio/SFX/ altina da kaydet (AudioManager.PlaySFX(string) icin)
            string resourcesPath = Path.Combine(Application.dataPath, "Resources", "Audio", "SFX");
            if (!Directory.Exists(resourcesPath))
                Directory.CreateDirectory(resourcesPath);

            string resourceFilePath = Path.Combine(resourcesPath, name + ".wav");

            byte[] wavData = CreateWavBytes(samples, sampleCount, byteRate, dataSize);

            File.WriteAllBytes(filePath, wavData);
            File.WriteAllBytes(resourceFilePath, wavData);

            Debug.Log($"{TAG} Ses efekti kaydedildi: {name}.wav ({duration:F2}s, {sampleCount} samples)");
        }

        /// <summary>WAV byte dizisi olusturur.</summary>
        private static byte[] CreateWavBytes(float[] samples, int sampleCount, int byteRate, int dataSize)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                // RIFF header
                writer.Write(new char[] { 'R', 'I', 'F', 'F' });
                writer.Write(36 + dataSize); // ChunkSize
                writer.Write(new char[] { 'W', 'A', 'V', 'E' });

                // fmt sub-chunk
                writer.Write(new char[] { 'f', 'm', 't', ' ' });
                writer.Write(16);           // SubChunkSize (PCM)
                writer.Write((short)1);     // AudioFormat (PCM)
                writer.Write((short)1);     // NumChannels (mono)
                writer.Write(SAMPLE_RATE);  // SampleRate
                writer.Write(byteRate);     // ByteRate
                writer.Write((short)2);     // BlockAlign
                writer.Write((short)16);    // BitsPerSample

                // data sub-chunk
                writer.Write(new char[] { 'd', 'a', 't', 'a' });
                writer.Write(dataSize);     // SubChunk2Size

                // Sample verileri (float -> 16-bit PCM)
                for (int i = 0; i < sampleCount; i++)
                {
                    float clamped = Mathf.Clamp(samples[i], -1f, 1f);
                    short pcm = (short)(clamped * 32767f);
                    writer.Write(pcm);
                }

                return stream.ToArray();
            }
        }

        // =================================================================
        // Yardimci
        // =================================================================

        /// <summary>Dizin yoksa olusturur.</summary>
        private static void EnsureDirectory(string assetPath)
        {
            var fullPath = Path.Combine(Application.dataPath, "..", assetPath);
            if (!Directory.Exists(fullPath))
                Directory.CreateDirectory(fullPath);
        }
    }
}

#endif
