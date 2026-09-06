using System;
using System.Collections.Generic;
using System.Linq;
using Blobs.Debugging;

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
            if (context.Source.Components.Color.HasValue && context.Target.Components.Color.HasValue &&
              context.Source.Components.Color.Value.Color == context.Target.Components.Color.Value.Color)
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
            if (context.Source.Components.Color.HasValue && context.Target.Components.Color.HasValue &&
                context.Source.Components.Color.Value.Color != context.Target.Components.Color.Value.Color)
            {
                return CollisionPlan.Failed(
                    MoveFailureReason.FlagRequiresMatchingColor);
            }

            // The board may contain exactly the mover and the flag at capture time.
            if (context.Board.Blobs.Where(b => b.Type.IsClearable()).Count() > 1)
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

    public sealed class GhostMergeStrategy : IMergeStrategy
    {
        public CollisionPlan BuildPlan(MoveContext context)
        {

            var steps = new List<MoveStep>();
            bool isVisible = false;

            BlobState mover = context.Target;


            GridPosition current = context.Target.Position;
            GridPosition goal = context.StartPosition;

            int stepX = goal.X == current.X ? 0 : goal.X > current.X ? 1 : -1;
            int stepY = goal.Y == current.Y ? 0 : goal.Y > current.Y ? 1 : -1;


            int stepCount = 0;
            while (current != goal)
            {
                if (stepCount > 100)
                {
                    return CollisionPlan.Failed(MoveFailureReason.MoveTimeout);
                }
                stepCount++;
                var next = new GridPosition(current.X + stepX, current.Y + stepY);
                var stepEffects = new List<IBoardEffect>();
                bool moverConsumed = false;
                if (!isVisible)
                {
                    stepEffects.Add(new MoveBlobEffect(mover.Id, current, next));
                }
                else
                {
                    if (!moverConsumed)
                        stepEffects.Add(new MoveBlobEffect(mover.Id, mover.Position, next));
                }




                steps.Add(new MoveStep(MoveStepKind.Traverse, stepEffects));

                // A consuming merge or reaching the intent's target ends locomotion.
                if (moverConsumed)
                    break;

                mover = context.Board.GetBlob(mover.Id);
                current = next;
            }

            return CollisionPlan.ConsumeMover(new RemoveBlobEffect(context.Source)).WithFollowUpSteps(steps);
        }
    }


}