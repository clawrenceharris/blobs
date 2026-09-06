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

            EnsureAudioSource();
            audioSource.pitch = pitch;
            audioSource.PlayOneShot(clip, volume);
        }

        private void EnsureAudioSource()
        {
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();

            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }
    }
}
