using UnityEngine;
using System.Collections.Generic;

namespace Blobs.Services
{
    /// <summary>
    /// Persistent Audio Manager using DontDestroyOnLoad.
    /// Manages BGM and SFX separately with inspector-configurable audio clips.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [System.Serializable]
        public class AudioEntry
        {
            public string audioName;
            public AudioClip clip;
        }

        [Header("Audio Clips")]
        [SerializeField] private List<AudioEntry> audioClips = new List<AudioEntry>();

        [Header("Volume Settings")]
        [Range(0f, 1f)]
        [SerializeField] private float bgmVolume = 1f;
        [Range(0f, 1f)]
        [SerializeField] private float sfxVolume = 1f;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource sfxSource;

        // Dictionary for fast clip lookup
        private Dictionary<string, AudioClip> clipLookup;

        #region Unity Lifecycle

        private void Awake()
        {
            // Singleton with duplicate prevention
            if (Instance != null && Instance != this)
            {
                Debug.Log("[AudioManager] Duplicate instance destroyed.");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Initialize audio sources if not assigned
            SetupAudioSources();

            // Build lookup dictionary
            BuildClipLookup();

            Debug.Log("[AudioManager] Initialized successfully.");
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        #endregion

        #region Setup

        private void SetupAudioSources()
        {
            // Create BGM source if not assigned
            if (bgmSource == null)
            {
                bgmSource = gameObject.AddComponent<AudioSource>();
                bgmSource.playOnAwake = false;
                bgmSource.loop = true;
            }
            bgmSource.volume = bgmVolume;

            // Create SFX source if not assigned
            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
                sfxSource.loop = false;
            }
            sfxSource.volume = sfxVolume;
        }

        private void BuildClipLookup()
        {
            clipLookup = new Dictionary<string, AudioClip>();

            foreach (var entry in audioClips)
            {
                if (string.IsNullOrEmpty(entry.audioName) || entry.clip == null)
                {
                    Debug.LogWarning("[AudioManager] Skipping invalid audio entry.");
                    continue;
                }

                if (clipLookup.ContainsKey(entry.audioName))
                {
                    Debug.LogWarning($"[AudioManager] Duplicate audio name '{entry.audioName}' - skipping.");
                    continue;
                }

                clipLookup.Add(entry.audioName, entry.clip);
            }
        }

        #endregion

        #region Public API - BGM

        /// <summary>
        /// Play background music by name. Loops automatically.
        /// </summary>
        public void PlayBGM(string name)
        {
            if (!TryGetClip(name, out AudioClip clip))
            {
                Debug.LogWarning($"[AudioManager] BGM clip '{name}' not found.");
                return;
            }

            // Don't restart if same clip is already playing
            if (bgmSource.clip == clip && bgmSource.isPlaying)
                return;

            bgmSource.clip = clip;
            bgmSource.Play();
        }

        /// <summary>
        /// Stop currently playing background music.
        /// </summary>
        public void StopBGM()
        {
            bgmSource.Stop();
            bgmSource.clip = null;
        }

        /// <summary>
        /// Pause background music.
        /// </summary>
        public void PauseBGM()
        {
            bgmSource.Pause();
        }

        /// <summary>
        /// Resume paused background music.
        /// </summary>
        public void ResumeBGM()
        {
            bgmSource.UnPause();
        }

        #endregion

        #region Public API - SFX

        /// <summary>
        /// Play a sound effect by name. One-shot, doesn't interrupt other SFX.
        /// </summary>
        public void PlaySFX(string name)
        {
            if (!TryGetClip(name, out AudioClip clip))
            {
                Debug.LogWarning($"[AudioManager] SFX clip '{name}' not found.");
                return;
            }

            sfxSource.PlayOneShot(clip, sfxVolume);
        }

        #endregion

        #region Public API - Volume Control

        /// <summary>
        /// Set BGM volume (0-1).
        /// </summary>
        public void SetBGMVolume(float value)
        {
            bgmVolume = Mathf.Clamp01(value);
            if (bgmSource != null)
            {
                bgmSource.volume = bgmVolume;
            }
        }

        /// <summary>
        /// Set SFX volume (0-1).
        /// </summary>
        public void SetSFXVolume(float value)
        {
            sfxVolume = Mathf.Clamp01(value);
            if (sfxSource != null)
            {
                sfxSource.volume = sfxVolume;
            }
        }

        /// <summary>
        /// Get current BGM volume.
        /// </summary>
        public float GetBGMVolume() => bgmVolume;

        /// <summary>
        /// Get current SFX volume.
        /// </summary>
        public float GetSFXVolume() => sfxVolume;

        #endregion

        #region Helpers

        private bool TryGetClip(string name, out AudioClip clip)
        {
            if (clipLookup == null)
            {
                clip = null;
                return false;
            }

            return clipLookup.TryGetValue(name, out clip);
        }

        /// <summary>
        /// Check if an audio clip exists.
        /// </summary>
        public bool HasClip(string name)
        {
            return clipLookup != null && clipLookup.ContainsKey(name);
        }

        #endregion
    }
}
