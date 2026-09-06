using UnityEngine;
using UnityEngine.Events;

namespace Blobs.Presentation
{
    /// <summary>
    /// Exposes merge-impact timing to an optional platform-specific haptics adapter.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MergeImpactHapticsFeedback : MonoBehaviour, IMergeImpactFeedback
    {
        [SerializeField] private UnityEvent _onImpact = new();

        /// <inheritdoc />
        public void PlayImpact(MergeImpactFeedbackContext context)
        {
            _onImpact?.Invoke();
        }
    }
}
