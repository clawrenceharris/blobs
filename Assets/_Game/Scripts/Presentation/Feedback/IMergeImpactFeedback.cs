using UnityEngine;

namespace Blobs.Presentation
{
    /// <summary>
    /// Implemented by independent presentation channels that respond to merge choreography cues.
    /// The orchestrator decides when a cue fires; each channel decides how its own channel
    /// expresses that moment.
    /// </summary>
    public interface IMergeImpactFeedback
    {
        /// <summary>Raised as the merge begins its wind-up, before the source travels.</summary>
        void BeginAnticipation(MergeImpactFeedbackContext context)
        {
        }

        /// <summary>Raised the instant the two blobs make contact.</summary>
        void PlayImpact(MergeImpactFeedbackContext context);

        /// <summary>Raised once the survivor has recovered to its resting shape.</summary>
        void PlaySettled(MergeImpactFeedbackContext context)
        {
        }
    }

    /// <summary>
    /// Shared presentation data supplied to audio, VFX, haptics, camera, and future feedback
    /// channels. Presentation-only: no Core state leaks through this context.
    /// </summary>
    public readonly struct MergeImpactFeedbackContext
    {
        public MergeImpactFeedbackContext(
            Vector3 worldPosition,
            Vector2 direction,
            Color sourceColor,
            Color targetColor,
            Color resultColor,
            BlobView source,
            BlobView target,
            float intensity = 1f,
            Skin sourceSkin = default,
            Skin targetSkin = default,
            Skin resultSkin = default,
            Vector3? destinationWorldPosition = null)
        {
            ContactWorldPosition = worldPosition;
            DestinationWorldPosition = destinationWorldPosition ?? worldPosition;
            Direction = direction;
            SourceColor = sourceColor;
            TargetColor = targetColor;
            ResultColor = resultColor;
            Source = source;
            Target = target;
            Intensity = intensity;
            SourceSkin = ResolveSkin(sourceSkin, sourceColor);
            TargetSkin = ResolveSkin(targetSkin, targetColor);
            ResultSkin = ResolveSkin(resultSkin, resultColor);
        }

        /// <summary>The visible seam where the two blob silhouettes first meet.</summary>
        public Vector3 ContactWorldPosition { get; }

        /// <summary>The centre of the tile that owns the resolved merge.</summary>
        public Vector3 DestinationWorldPosition { get; }

        /// <summary>
        /// Backwards-compatible impact anchor. New spatial feedback should choose explicitly
        /// between <see cref="ContactWorldPosition"/> and <see cref="DestinationWorldPosition"/>.
        /// </summary>
        public Vector3 WorldPosition => ContactWorldPosition;
        public Vector2 Direction { get; }
        public Color SourceColor { get; }
        public Color TargetColor { get; }
        public Color ResultColor { get; }
        public BlobView Source { get; }
        public BlobView Target { get; }
        public float Intensity { get; }
        public Skin SourceSkin { get; }
        public Skin TargetSkin { get; }
        public Skin ResultSkin { get; }

        private static Skin ResolveSkin(Skin skin, Color fallback)
        {
            return skin.BaseColor.maxColorComponent > 0f || skin.BaseColor.a > 0f
                ? skin
                : new Skin(fallback, fallback, fallback);
        }
    }
}
