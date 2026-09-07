using UnityEngine;

namespace Blobs.Presentation
{
    /// <summary>
    /// Plays the authored one-shot audio channel for a merge impact.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MergeImpactAudioFeedback : MonoBehaviour, IMergeImpactFeedback
    {
        [Tooltip("Assign the merge impact clip here. A missing clip intentionally produces no audio.")]
        [SerializeField] private AudioClip _clip;
        [SerializeField, Range(0f, 1f)] private float _volume = 0.85f;
        [SerializeField, Range(0.1f, 3f)] private float _pitch = 1f;
        [SerializeField] private AudioSource _audioSource;

        /// <inheritdoc />
        public void PlayImpact(MergeImpactFeedbackContext context)
        {
            if (_clip == null)
                return;

            if (!TryResolveAudioSource())
            {
                Debug.LogError(
                    $"Merge impact audio on '{name}' has a clip but no authored AudioSource.",
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

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_clip != null && _audioSource == null && GetComponent<AudioSource>() == null)
            {
                Debug.LogError(
                    $"Merge impact audio on '{name}' requires an authored AudioSource.",
                    this);
            }
        }
#endif
    }
}
