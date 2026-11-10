using System;
using Blobs.Application;
using UnityEngine;

namespace Blobs.Content
{
    /// <summary>How newly revealed blobs enter the board.</summary>
    public enum BlobRevealStyle
    {
        /// <summary>Scales up in place with a small overshoot.</summary>
        Pop,
        /// <summary>Falls into its cell from above with a bounce.</summary>
        DropIn,
        /// <summary>Slides in from the left with an overshoot.</summary>
        SlideFromLeft,
        /// <summary>Slides in from the right with an overshoot.</summary>
        SlideFromRight,
        /// <summary>Rises into its cell from below with an overshoot.</summary>
        RiseFromBelow,
        /// <summary>Appears without animation at its turn in the stagger.</summary>
        Instant
    }

    /// <summary>Which caption slot fades in first when a playable step has both.</summary>
    public enum TutorialCaptionOrder
    {
        TopFirst,
        BottomFirst,
        Together
    }

    /// <summary>Order of the board reveal and the captions when a playable step starts.</summary>
    public enum TutorialEntranceOrder
    {
        /// <summary>New pieces land, then captions explain them.</summary>
        RevealThenCaptions,
        /// <summary>Captions set up the beat, then new pieces land.</summary>
        CaptionsThenReveal,
        /// <summary>Reveal and captions start at the same time.</summary>
        Together
    }

    /// <summary>
    /// Choreography for one tutorial transition. The sequence asset holds defaults; any step can
    /// override them. All times are in seconds.
    /// </summary>
    [Serializable]
    public sealed class TutorialTimingAssetData
    {
        [Header("Exit (previous step)")]
        [Min(0f), Tooltip("Pause after the gate is met so the player sees the result before anything changes.")]
        public float holdBeforeTransition = 0.35f;

        [Min(0f), Tooltip("Fade-out time for the previous step's captions.")]
        public float captionFadeOutDuration = 0.2f;

        [Min(0f), Tooltip("Empty beat between the old captions leaving and the new step entering.")]
        public float gapAfterFadeOut = 0.1f;

        [Header("Entrance (playable steps)")]
        [Tooltip("Whether new pieces land before, after, or together with the captions.")]
        public TutorialEntranceOrder entranceOrder = TutorialEntranceOrder.CaptionsThenReveal;

        [Min(0f), Tooltip("After captions finish and before board pieces enter (Captions Then Reveal). " +
            "Give the player time to read.")]
        public float captionReadPause = 1.25f;

        [Min(0f), Tooltip("Extra delay between the caption group and the reveal group. Ignored when Together.")]
        public float entranceGap = 0.15f;

        [Header("Board changes")]
        [Min(0f), Tooltip("Time for removed blobs to shrink away. Removals play during the exit, before reveals.")]
        public float removeDuration = 0.25f;

        public BlobRevealStyle revealStyle = BlobRevealStyle.DropIn;

        [Min(0f), Tooltip("Time for each revealed blob to land.")]
        public float revealDuration = 0.45f;

        [Min(0f), Tooltip("Delay between consecutive revealed blobs, in list order.")]
        public float revealStagger = 0.12f;

        [Min(0f), Tooltip("Time for each newly opened cell to grow in. Cells always enter before blobs.")]
        public float cellRevealDuration = 0.25f;

        [Min(0f), Tooltip("Travel distance in cells for DropIn, Slide, and Rise styles.")]
        public float revealDistance = 1.5f;

        [Header("Captions")]
        public TutorialCaptionOrder captionOrder = TutorialCaptionOrder.TopFirst;

        [Min(0f)] public float captionFadeInDuration = 0.4f;

        [Min(0f), Tooltip("Delay between caption slots (top/bottom/center). Ignored when Together. " +
            "Use 1+ seconds when lines should read as separate beats.")]
        public float captionStagger = 1f;

        [Header("Pointer")]
        [Min(0f), Tooltip("Delay after the entrance finishes before the ghost swipe starts.")]
        public float pointerDelay = 0.6f;

        [Header("Board fade")]
        [Range(0f, 1f), Tooltip("Board opacity behind a Dim reaction slide.")]
        public float reactionDimAlpha = 0.2f;

        [Min(0f), Tooltip("Time to fade the board out for a reaction and back in for the next playable step.")]
        public float boardFadeDuration = 0.3f;

        [Header("Reaction slides")]
        [Min(0f), Tooltip("Minimum time a reaction stays up before a tap can dismiss it.")]
        public float reactionMinDisplay = 0.85f;

        [Min(0f), Tooltip("Advance automatically after this many seconds. Leave at 0 to wait for a tap.")]
        public float reactionAutoAdvance = 2.5f;

        [Header("Input")]
        [Tooltip("Ignore board input until the entrance finishes so early taps cannot skip a beat.")]
        public bool lockInputUntilEntranceEnds = true;

        public TutorialTimingAssetData Clone() => (TutorialTimingAssetData)MemberwiseClone();
    }
}
