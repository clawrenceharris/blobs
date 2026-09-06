using UnityEngine;

namespace Blobs.Presentation
{
    /// <summary>
    /// Implemented by independent presentation channels that respond to a merge impact.
    /// </summary>
    public interface IMergeImpactFeedback
    {
        void PlayImpact(MergeImpactFeedbackContext context);
    }

    /// <summary>
    /// Shared presentation data supplied to audio, VFX, haptics, camera, and future feedback channels.
    /// </summary>
    public readonly struct MergeImpactFeedbackContext
    {
        public MergeImpactFeedbackContext(
            Vector3 worldPosition,
            Color blobColor,
            BlobView sortingAnchor)
        {
            WorldPosition = worldPosition;
            BlobColor = blobColor;
            SortingAnchor = sortingAnchor;
        }

        public Vector3 WorldPosition { get; }
        public Color BlobColor { get; }
        public BlobView SortingAnchor { get; }
    }
}
