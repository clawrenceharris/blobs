using UnityEngine;

namespace Blobs.Presentation
{
    /// <summary>
    /// Reusable prefab behavior that contributes a one-shot audio cue when its blob is contacted.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BlobContactAudioFeedback : MonoBehaviour, IBlobContactFeedback
    {
        [SerializeField] private AudioClip clip;
        [SerializeField, Range(0f, 1f)] private float volume = 0.8f;
        [SerializeField, Range(0.1f, 3f)] private float pitch = 1f;
        [SerializeField] private AudioSource audioSource;

        /// <inheritdoc />
        public void PlayContactFeedback(BlobContactFeedbackContext context)
        {
            if (clip == null)
                return;

            if (!TryResolveAudioSource())
            {
                Debug.LogError(
                    $"Blob contact audio on '{name}' has a clip but no authored AudioSource.",
                    this);
                return;
            }

            audioSource.pitch = pitch;
            audioSource.PlayOneShot(clip, volume);
        }

        private bool TryResolveAudioSource()
        {
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                return false;

            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            return true;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (clip != null && audioSource == null && GetComponent<AudioSource>() == null)
            {
                Debug.LogError(
                    $"Blob contact audio on '{name}' requires an authored AudioSource.",
                    this);
            }
        }
#endif
    }
}
