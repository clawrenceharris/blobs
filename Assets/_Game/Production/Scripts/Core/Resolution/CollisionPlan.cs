using System;
using System.Collections.Generic;

namespace Blobs.Core
{

    public enum CollisionKind
    {
        /// <summary>The mover enters the collision cell and may keep walking.</summary>
        MoverSurvived,

        /// <summary>The mover is removed or otherwise consumed at the collision cell.</summary>
        MoverConsumed,

        /// <summary>The mover stops before entering the occupied collision cell.</summary>
        MoverBlocked,

        /// <summary>The collision is invalid and the whole move intent fails.</summary>
        Failed,
    }
    public static class CollisionKindExtensions
    {
        public static bool BlocksMover(this CollisionKind kind)
        {
            return kind == CollisionKind.MoverBlocked;
        }
        public static bool ConsumesMover(this CollisionKind kind)
        {
            return kind == CollisionKind.MoverConsumed;
        }
        public static bool Succeeded(this CollisionKind kind)
        {
            return kind == CollisionKind.MoverSurvived || kind == CollisionKind.MoverConsumed;
        }
        public static bool Failed(this CollisionKind kind)
        {
            return kind == CollisionKind.Failed;
        }
    }
    /// <summary>
    /// Outcome of resolving a single collision between a moving blob and the occupant
    /// of a cell on its path. Strategies describe only what happens at the collision
    /// tile; locomotion is owned by the resolver.
    /// </summary>
    /// <remarks>
    /// A collision plan is evaluated after the resolver has already walked to an
    /// occupied cell. Use it for contact rules such as merging, consuming the mover, or
    /// blocking before the occupant. Do not use <see cref="MovePlan"/> for these rules
    /// unless the route itself must be changed before traversal begins.
    /// </remarks>
    public sealed class CollisionPlan
    {
        private CollisionPlan(
            bool succeeded,
            MoveFailureReason failureReason,
            IReadOnlyList<IBoardEffect> effects,
            CollisionKind kind,
            IReadOnlyList<MoveStep> followUpSteps = null)
        {
            Succeeded = succeeded;
            FailureReason = failureReason;
            Effects = effects ?? Array.Empty<IBoardEffect>();
            Kind = kind;
            FollowUpSteps = followUpSteps ?? Array.Empty<MoveStep>();
        }

        public bool Succeeded { get; }
        public MoveFailureReason FailureReason { get; }

        /// <summary>
        /// Effects that resolve the collision tile, such as removing the occupant,
        /// before the resolver decides whether to append the mover's locomotion effect.
        /// </summary>
        public IReadOnlyList<IBoardEffect> Effects { get; }

        /// <summary>
        /// How the collision affects locomotion after collision-tile effects are
        /// applied.
        /// </summary>
        public CollisionKind Kind { get; }

        /// <summary>
        /// Steps appended after the main locomotion timeline. This is the hook for
        /// mechanics like the Ghost blob that perform their own movement in response
        /// to a merge.
        /// </summary>
        public IReadOnlyList<MoveStep> FollowUpSteps { get; }

        /// <summary>
        /// Collision resolved; the mover survives, occupies the tile, and may continue.
        /// </summary>
        public static CollisionPlan Continue(params IBoardEffect[] effects)
        {
            return new CollisionPlan(
                true, MoveFailureReason.None, effects, CollisionKind.MoverSurvived);
        }

        /// <summary>
        /// Collision resolved; the mover is consumed and locomotion ends at this tile.
        /// </summary>
        public static CollisionPlan ConsumeMover(params IBoardEffect[] effects)
        {
            return new CollisionPlan(
                true, MoveFailureReason.None, effects, CollisionKind.MoverConsumed);
        }

        /// <summary>
        /// Collision resolved; the mover is blocked before entering the occupied tile
        /// and locomotion ends. This is the natural result for rock contact.
        /// </summary>
        public static CollisionPlan BlockMover(params IBoardEffect[] effects)
        {
            return new CollisionPlan(
                true, MoveFailureReason.None, effects, CollisionKind.MoverBlocked);
        }


        /// <summary>
        /// Collision rejected. The whole move intent fails atomically with this reason.
        /// </summary>
        public static CollisionPlan Failed(MoveFailureReason reason)
        {
            return new CollisionPlan(
                false, reason, null, CollisionKind.Failed, followUpSteps: null);
        }

        /// <summary>
        /// Returns a copy of this plan with follow-up steps appended after locomotion.
        /// </summary>
        public CollisionPlan WithFollowUpSteps(IReadOnlyList<MoveStep> steps)
        {
            return new CollisionPlan(Succeeded, FailureReason, Effects, Kind, steps);
        }
    }
}
