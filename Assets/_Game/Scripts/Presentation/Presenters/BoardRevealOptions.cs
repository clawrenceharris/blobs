using Blobs.Content;

namespace Blobs.Presentation
{
    /// <summary>Choreography for <see cref="BoardPresenter.CreateRevealSequence"/>. Times are seconds.</summary>
    public readonly struct BoardRevealOptions
    {
        public BoardRevealOptions(
            BlobRevealStyle style,
            float duration,
            float stagger,
            float cellDuration,
            float distance)
        {
            Style = style;
            Duration = duration;
            Stagger = stagger;
            CellDuration = cellDuration;
            Distance = distance;
        }

        public BlobRevealStyle Style { get; }

        /// <summary>Time for each blob to land.</summary>
        public float Duration { get; }

        /// <summary>Delay between consecutive blobs. Cells use half of it.</summary>
        public float Stagger { get; }

        /// <summary>Time for each newly opened cell to grow in.</summary>
        public float CellDuration { get; }

        /// <summary>Travel distance in cells for moving styles.</summary>
        public float Distance { get; }

        public static BoardRevealOptions From(TutorialTimingAssetData timing)
        {
            return new BoardRevealOptions(
                timing.revealStyle,
                timing.revealDuration,
                timing.revealStagger,
                timing.cellRevealDuration,
                timing.revealDistance);
        }
    }
}
