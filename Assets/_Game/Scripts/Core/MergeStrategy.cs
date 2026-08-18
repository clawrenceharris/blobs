using System.Linq;

namespace Blobs.Core
{
    /// <summary>
    /// Resolves what happens when a moving blob collides with an occupant on its path.
    /// Strategies emit collision-tile effects only; the resolver owns locomotion, so the
    /// same strategy works for intermediate chain merges and for the final target.
    /// </summary>
    public interface IMergeStrategy
    {
        CollisionPlan BuildPlan(MoveContext context);
    }

    /// <summary>
    /// Standard color-clash merge: occupant is removed, the mover survives on its tile
    /// and may continue along its path. Requires differing colors.
    /// </summary>
    public sealed class NormalMergeStrategy : IMergeStrategy
    {
        public CollisionPlan BuildPlan(MoveContext context)
        {
            if (context.Source.TryGetModel<ColorBlobModel>(out var sourceColor) && context.Target.TryGetModel<ColorBlobModel>(out var targetColor) &&
              sourceColor != null && targetColor != null && sourceColor.Color == targetColor.Color)
            {
                return CollisionPlan.Failed(
                    MoveFailureReason.NormalMergeRequiresDifferentColors);
            }

            return CollisionPlan.Continue(
                new RemoveBlobEffect(context.Target));
        }
    }

    /// <summary>
    /// Flag capture: requires matching color and a board containing only the mover and
    /// the flag. Consumes the mover; the flag stays in place.
    /// </summary>
    public sealed class FlagMergeStrategy : IMergeStrategy
    {
        public CollisionPlan BuildPlan(MoveContext context)
        {
            if (context.Source.TryGetModel<ColorBlobModel>(out var sourceColor) && context.Target.TryGetModel<ColorBlobModel>(out var targetColor) &&
                sourceColor.Color != targetColor.Color)
            {
                return CollisionPlan.Failed(
                    MoveFailureReason.FlagRequiresMatchingColor);
            }

            // The board may contain exactly the mover and the flag at capture time.
            if (context.Board.Blobs.Select(b => b.IsClearable).Count() == 1)
            {
                return CollisionPlan.Failed(
                    MoveFailureReason.FlagRequiresNoOtherBlobs);
            }

            return CollisionPlan.ConsumeMover(
                new MergeIntoFlagEffect(
                    context.Source,
                    context.Target));
        }
    }

    public sealed class RockMergeStrategy : IMergeStrategy
    {
        public CollisionPlan BuildPlan(MoveContext context)
        {
            return CollisionPlan.Continue();
        }
    }


}
