using System;
using System.Collections.Generic;
using Blobs.Application;
using Blobs.Core;
using UnityEngine;
using UnityEngine.Serialization;

namespace Blobs.Content
{
    /// <summary>
    /// Authored tutorial on one shared board. The board asset holds every piece the tutorial will
    /// ever use; each step reveals some of them and adds captions, an optional ghost swipe, and the
    /// player action that advances to the next beat. Pieces not yet revealed stay off the board.
    /// </summary>
    [CreateAssetMenu(
        fileName = "Tutorial_New",
        menuName = "Blobs/Tutorial Sequence")]
    public sealed class TutorialSequenceAsset : ScriptableObject
    {
        [SerializeField, Tooltip("Full tutorial board: size, palette, objective, and every blob and tile. " +
            "Blobs and cells listed in a later step's reveals start hidden.")]
        private LevelDefinitionAsset board;

        [SerializeField, Tooltip("Optional level assets used only as holders for blobs that arrive later. " +
            "Their blobs may share cells with board blobs (for example a blob that appears where another " +
            "was eaten). Only blob ids and blob data are read; each must be revealed by some step.")]
        private List<LevelDefinitionAsset> stagedPieces = new();

        [SerializeField, Tooltip("Timing used by every step that does not override it.")]
        private TutorialTimingAssetData defaultTiming = new();

        [SerializeField] private List<TutorialStepAssetData> steps = new();

        public LevelDefinitionAsset Board => board;
        public IReadOnlyList<LevelDefinitionAsset> StagedPieces => stagedPieces;
        public IReadOnlyList<TutorialStepAssetData> Steps => steps;
        public TutorialTimingAssetData DefaultTiming => defaultTiming ??= new TutorialTimingAssetData();
        public LevelColorPaletteAsset Palette => board != null ? board.Palette : null;

        /// <summary>Timing for the transition into step <paramref name="index"/>.</summary>
        public TutorialTimingAssetData GetTiming(int index)
        {
            if (index >= 0 && index < steps.Count)
            {
                TutorialStepAssetData step = steps[index];
                if (step != null && step.overrideTiming && step.timing != null)
                    return step.timing;
            }

            return DefaultTiming;
        }
    }

    [Serializable]
    public sealed class TutorialStepAssetData
    {
        [Tooltip("Playable steps accept moves. Reaction slides show centered text over the dimmed board.")]
        public TutorialStepKind kind = TutorialStepKind.Playable;

        [Header("Captions")]
        [FormerlySerializedAs("caption")]
        [TextArea(1, 3), Tooltip("Top of the canvas. Used by Playable steps.")]
        public string captionTop;

        [TextArea(1, 3), Tooltip("Bottom of the canvas. Optional second line for Playable steps.")]
        public string captionBottom;

        [TextArea(1, 3), Tooltip("Centered on the canvas. Used by Reaction slides.")]
        public string captionCenter;

        [Tooltip("Playable: FailedMoveAttempt / SuccessfulMove / LevelComplete. Reaction: TapContinue.")]
        public TutorialAdvanceGate advanceWhen = TutorialAdvanceGate.SuccessfulMove;

        [Tooltip("Only for FailedMoveAttempt. None accepts any rejected attempt.")]
        public MoveFailureReason failureFilter = MoveFailureReason.None;

        [Header("Board changes (Playable only)")]
        [Tooltip("Blob ids that leave the board when this step starts, before reveals enter. " +
            "Use with a reveal on the same cell to swap one blob for another.")]
        public List<string> removeBlobIds = new();

        [Tooltip("Blob ids from the board or staged pieces that enter when this step starts, in entrance " +
            "order. Board blobs not listed on any step are on the board from the start.")]
        public List<string> revealBlobIds = new();

        [Tooltip("Board cells that open when this step starts. They are hidden until then.")]
        public List<Vector2Int> revealCells = new();

        [Header("Coach")]
        public bool showPointer;
        public Vector2Int coachFrom;
        public Vector2Int coachTo;

        [Header("Timing")]
        [Tooltip("Use the timing below instead of the sequence defaults for the transition into this step.")]
        public bool overrideTiming;
        public TutorialTimingAssetData timing = new();
    }
}
