using System;
using System.Collections.Generic;
using System.Linq;

namespace Blobs.Core
{
    /// <summary>
    /// Resolves what happens when a moving blob collides with an occupant on its path.
    /// Strategies emit collision-tile effects only; the resolver owns locomotion, so the
    /// same strategy works for intermediate chain merges and for the final target.
    /// </summary>
    public interface ICollisionStrategy
    {
        CollisionPlan BuildPlan(MoveContext context);
    }

    /// <summary>
    /// Standard color-clash merge: occupant is removed, the mover survives on its tile
    /// and may continue along its path. Requires differing colors.
    /// </summary>
    public sealed class NormalCollisionStrategy : ICollisionStrategy
    {
        public CollisionPlan BuildPlan(MoveContext context)
        {
            if (context.Source.Components.Color.HasValue && context.Target.Components.Color.HasValue &&
              context.Source.Components.Color.Value.Color == context.Target.Components.Color.Value.Color)
            {
                return CollisionPlan.Failed(
                    MoveFailureReason.NormalMergeRequiresDifferentColors);
            }

            return CollisionPlan.Continue(MergeEffect.NormalMerge(context));
        }
    }

    /// <summary>
    /// Flag capture: requires matching color and a board containing only the mover and
    /// the flag. Consumes the mover; the flag stays in place.
    /// </summary>
    public sealed class FlagCollisionStrategy : ICollisionStrategy
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

            return CollisionPlan.ConsumeMover(MergeEffect.ReverseMerge(context));
        }
    }

    public sealed class RockCollisionStrategy : ICollisionStrategy
    {
        public CollisionPlan BuildPlan(MoveContext context)
        {
            // Rocks block merges so we just continue with no effect.
            return CollisionPlan.Continue();
        }
    }

    public sealed class GhostCollisionStrategy : ICollisionStrategy
    {
        public CollisionPlan BuildPlan(MoveContext context)
        {
            GridPosition current = context.Target.Position;
            GridPosition goal = context.StartPosition;
            if (!current.IsAlignedWith(goal) || current == goal)
                return CollisionPlan.Failed(MoveFailureReason.NotAligned);

            int dx = Math.Sign(goal.X - current.X);
            int dy = Math.Sign(goal.Y - current.Y);
            var path = new List<GridPosition>();
            bool clears = false;
            while (current != goal)
            {
                current = new GridPosition(current.X + dx, current.Y + dy);
                if (!context.Board.IsInside(current))
                    return CollisionPlan.Failed(MoveFailureReason.BlockedPath);

                path.Add(current);
                if (context.Board.GetTileAt(current)?.Type == TileType.Sigil)
                {
                    clears = true;
                    break;
                }
            }

            var followUp = new List<IBoardEffect>();
            if (clears)
            {
                followUp.Add(GhostHauntEffect.Rest(context.Target.Id, path));

            }
            else
                followUp.Add(GhostHauntEffect.Haunt(context.Target.Id, path));

            return CollisionPlan.ConsumeMover(
                MergeEffect.ReverseMerge(context)).WithFollowUpSteps(new[]
                {
                    new MoveStep(MoveStepKind.Traverse, followUp)
                });
        }
    }
}
