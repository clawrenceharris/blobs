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
    /// <remarks>
    /// Use collision strategies for rules that depend on the actual occupant reached
    /// during path simulation. This is the correct home for blocking rocks, normal
    /// merges, flag captures, and other contact behavior. Use <see cref="IMoveStrategy"/>
    /// only for source/target intent validation or route shaping that must happen before
    /// the path is walked.
    /// </remarks>
    public interface ICollisionStrategy
    {
        /// <summary>
        /// Builds the plan for the single occupied cell the mover has reached.
        /// The resolver applies this plan, then decides whether the mover enters the
        /// cell, stops before it, is consumed, or continues walking.
        /// </summary>
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

            if (context.Source.Components.Color?.Color == context.Target.Components.Color?.Color)
                return CollisionPlan.Failed(MoveFailureReason.NormalMergeRequiresDifferentColors);

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
            if (context.Target.Id != context.Intent.Target.Id)
            {
                return CollisionPlan.Failed(MoveFailureReason.FlagCaptureRequired);
            }
            if (context.Source.Type != BlobType.Normal)
            {
                return CollisionPlan.Failed(MoveFailureReason.FlagRequiresNormalSource);

            }
            if (context.Source.Components.Color?.Color != context.Target.Components.Color?.Color)
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

    /// <summary>
    /// Rock contact: the mover is allowed to travel up to the rock, but cannot enter
    /// the rock's occupied cell. Because this is a collision strategy, rocks behave the
    /// same whether they are the selected target or an intermediate occupant.
    /// </summary>
    public sealed class RockCollisionStrategy : ICollisionStrategy
    {
        public CollisionPlan BuildPlan(MoveContext context)
        {
            return CollisionPlan.BlockMover();
        }
    }


    /// <summary>
    /// Ghost contact: the mover is consumed at the ghost's cell, then ghost aftermath
    /// is appended as follow-up steps after the main locomotion timeline.
    /// </summary>
    public sealed class GhostCollisionStrategy : ICollisionStrategy
    {
        public CollisionPlan BuildPlan(MoveContext context)
        {
            GridPosition current = context.Target.Position;
            GridPosition goal = context.Plan.StartPosition;
            if (!current.IsAlignedWith(goal) || current == goal)
                return CollisionPlan.Failed(MoveFailureReason.NotAligned);

            var path = new List<GridPosition>();
            var direction = (goal - current).Normalized();
            bool rests = context.Board.GetTileAt(current)?.Type.IsGrave() == true;
            if (rests) path.Add(current);
            while (!rests && current != goal)
            {

                current += direction;
                path.Add(current);
                var tile = context.Board.GetTileAt(current);

                if (tile != null && tile.Type.IsGrave())
                {
                    rests = true;
                    break;
                }
            }

            var followUp = new List<IBoardEffect>();
            if (rests)
                followUp.Add(new GhostRestEffect(context.Target.Id, path));
            else
                followUp.Add(new GhostHauntEffect(context.Target.Id, path));

            return CollisionPlan.ConsumeMover(
                MergeEffect.ReverseMerge(context)).WithFollowUpSteps(new[]
                {
                    new MoveStep(MoveStepKind.Traverse, followUp)
                });
        }
    }
}
