using System.Collections.Generic;

namespace Blobs.Core
{
    /// <summary>
    /// Deterministic Core rule entry point for normal source-to-target merge resolution.
    /// It validates the intent, emits ordered effects, and applies those effects to board state.
    /// </summary>
    public sealed class MoveResolver
    {
        private readonly IBlobRuleBook _rules;

        public MoveResolver(IBlobRuleBook rules = null)
        {
            _rules = rules ?? BlobRuleBook.CreateDefault();
        }

        public MoveFailureReason ValidateSourceSelection(
            BoardState board,
            string blobId)
        {
            BlobState blob = board.GetBlob(blobId);

            if (blob == null)
                return MoveFailureReason.SourceMissing;

            return _rules.GetTraits(blob.Type).CanBeSource
                ? MoveFailureReason.None
                : MoveFailureReason.SourceCannotMove;
        }

        public MoveResult Resolve(
            BoardState board,
            MoveIntent intent,
            LevelObjectiveDefinition objective = null)
        {
            BlobState source = board.GetBlob(intent.SourceId);
            if (source == null)
                return MoveResult.Failed(MoveFailureReason.SourceMissing);

            BlobState target = board.GetBlob(intent.TargetId);
            if (target == null)
                return MoveResult.Failed(MoveFailureReason.TargetMissing);

            if (source.Id == target.Id)
                return MoveResult.Failed(MoveFailureReason.SameBlob);

            if (!_rules.GetTraits(source.Type).CanBeSource)
                return MoveResult.Failed(MoveFailureReason.SourceCannotMove);

            if (!source.Position.IsAlignedWith(target.Position))
                return MoveResult.Failed(MoveFailureReason.NotAligned);

            if (PathHasBlockingBlob(
                    board,
                    source.Position,
                    target.Position))
            {
                return MoveResult.Failed(MoveFailureReason.BlockedPath);
            }

            if (!_rules.TryGetMergeStrategy(
                    source.Type,
                    target.Type,
                    out IMergeStrategy strategy))
            {
                return MoveResult.Failed(
                    MoveFailureReason.UnsupportedInteraction);
            }

            var context = new MoveContext(board, source, target);
            MergePlan plan = strategy.BuildPlan(context);

            if (!plan.Succeeded)
                return MoveResult.Failed(plan.FailureReason);

            ApplyEffects(board, plan.Effects);

            return new MoveResult(
                true,
                MoveFailureReason.None,
                plan.Effects,
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
