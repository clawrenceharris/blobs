using System.Collections.Generic;

namespace Blobs.Core
{
    public sealed class MoveResolver
    {
        public MoveResult Resolve(BoardState board, MoveIntent intent)
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

            var effects = new List<IBoardEffect>
            {
                new RemoveBlobEffect(target),
                new MoveBlobEffect(source.Id, source.Position, target.Position),
                new RemoveBlobEffect(source.WithPosition(target.Position))
            };

            var inverseEffects = ApplyEffects(board, effects);
            return new MoveResult(true, MoveFailureReason.None, effects, inverseEffects, ObjectiveEvaluator.IsComplete(board));
        }

        public IReadOnlyList<IBoardEffect> ApplyEffects(BoardState board, IReadOnlyList<IBoardEffect> effects)
        {
            var inverses = new List<IBoardEffect>();

            foreach (var effect in effects)
                inverses.Add(effect.Apply(board));

            inverses.Reverse();
            return inverses;
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
