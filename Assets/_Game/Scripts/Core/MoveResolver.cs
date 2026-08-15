using System.Collections.Generic;

namespace Blobs.Core
{
    /// <summary>
    /// Deterministic Core rule entry point for normal source-to-target merge resolution.
    /// It validates the intent, emits ordered effects, and applies those effects to board state.
    /// </summary>
    public sealed class MoveResolver
    {
        /// <summary>
        /// Resolves a move intent against the supplied board. A successful normal merge removes the
        /// target blob, then moves the source blob into the target position.
        /// </summary>
        public MoveResult Resolve(
            BoardState board,
            MoveIntent intent,
            LevelObjectiveDefinition objective = null)
        {
            var source = board.GetBlob(intent.SourceId);
            if (source == null)
                return MoveResult.Failed(MoveFailureReason.SourceMissing);

            var target = board.GetBlob(intent.TargetId);
            if (target == null)
                return MoveResult.Failed(MoveFailureReason.TargetMissing);

            if (source.Id == target.Id)
                return MoveResult.Failed(MoveFailureReason.SameBlob);
            if (!source.Position.IsAlignedWith(target.Position))
                return MoveResult.Failed(MoveFailureReason.NotAligned);
            if (source.Type != BlobType.Normal || target.Type != BlobType.Normal)
                return MoveResult.Failed(MoveFailureReason.UnsupportedBlobType);
            if (source.Color == target.Color)
                return MoveResult.Failed(MoveFailureReason.ColorMismatch);
            if (PathHasBlockingBlob(board, source.Position, target.Position))
                return MoveResult.Failed(MoveFailureReason.BlockedPath);

            // The target must be removed before the source moves so board occupancy remains valid.
            var effects = new List<IBoardEffect>
            {
                new RemoveBlobEffect(target),
                new MoveBlobEffect(source.Id, source.Position, target.Position),

            };
            ApplyEffects(board, effects);

            return new MoveResult(
                true,
                MoveFailureReason.None,
                effects,
                ObjectiveEvaluator.IsComplete(board, objective));
        }

        /// <summary>
        /// Applies an ordered effect list to board state. This is intentionally validation-free
        /// because effects are assumed to come from a completed resolution pass.
        /// </summary>
        public void ApplyEffects(BoardState board, IReadOnlyList<IBoardEffect> effects)
        {

            foreach (var effect in effects)
                effect.Apply(board);

        }

        private static bool PathHasBlockingBlob(BoardState board, GridPosition from, GridPosition to)
        {
            var stepX = to.X == from.X ? 0 : to.X > from.X ? 1 : -1;
            var stepY = to.Y == from.Y ? 0 : to.Y > from.Y ? 1 : -1;
            var current = new GridPosition(from.X + stepX, from.Y + stepY);

            while (current != to)
            {
                if (board.GetBlobAt(current) != null)
                    return true;

                current = new GridPosition(current.X + stepX, current.Y + stepY);
            }

            return false;
        }
    }
}
