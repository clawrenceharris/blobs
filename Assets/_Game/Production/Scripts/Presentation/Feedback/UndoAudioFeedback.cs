using UnityEngine;

namespace Blobs.Presentation
{
    /// <summary>
    /// Plays the authored one-shot audio channel for Undo. A missing clip is silent.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UndoAudioFeedback : MonoBehaviour, IUndoPlaybackFeedback
    {
        [Tooltip("Assign the undo clip here. A missing clip intentionally produces no audio.")]
        [SerializeField] private AudioClip _clip;
        [SerializeField, Range(0f, 1f)] private float _volume = 0.7f;
        [SerializeField, Range(0.1f, 3f)] private float _pitch = 1.1f;
        [SerializeField] private AudioSource _audioSource;

        public void PlayUndo()
        {
            if (_clip == null)
                return;

            if (!TryResolveAudioSource())
            {
                Debug.LogError(
                    $"Undo audio on '{name}' has a clip but no authored AudioSource.",
                    this);
                return;
            }

            _audioSource.pitch = _pitch;
            _audioSource.PlayOneShot(_clip, _volume);
        }

        private bool TryResolveAudioSource()
        {
            if (_audioSource == null)
                _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
                return false;

            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 0f;
            return true;
        }
    }
}
