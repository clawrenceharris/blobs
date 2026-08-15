using System;
using System.Collections.Generic;

namespace Blobs.Core
{
    /// <summary>
    /// Outcome of resolving a single collision between a moving blob and the occupant
    /// of a cell on its path. Strategies describe only what happens at the collision
    /// tile; locomotion is owned by the resolver.
    /// </summary>
    public sealed class CollisionPlan
    {
        private CollisionPlan(
            bool succeeded,
            MoveFailureReason failureReason,
            IReadOnlyList<IBoardEffect> effects,
            bool consumesMover,
            IReadOnlyList<MoveStep> followUpSteps)
        {
            Succeeded = succeeded;
            FailureReason = failureReason;
            Effects = effects ?? Array.Empty<IBoardEffect>();
            ConsumesMover = consumesMover;
            FollowUpSteps = followUpSteps ?? Array.Empty<MoveStep>();
        }

        public bool Succeeded { get; }
        public MoveFailureReason FailureReason { get; }

        /// <summary>
        /// Effects that resolve the collision tile (e.g. remove the occupant) before the
        /// mover enters it. The resolver appends the mover's locomotion effect itself.
        /// </summary>
        public IReadOnlyList<IBoardEffect> Effects { get; }

        /// <summary>
        /// True when the collision consumes the moving blob. Locomotion ends at this tile
        /// even if the intent targeted a farther blob.
        /// </summary>
        public bool ConsumesMover { get; }

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
                true, MoveFailureReason.None, effects, consumesMover: false, followUpSteps: null);
        }

        /// <summary>
        /// Collision resolved; the mover is consumed and locomotion ends at this tile.
        /// </summary>
        public static CollisionPlan ConsumeMover(params IBoardEffect[] effects)
        {
            return new CollisionPlan(
                true, MoveFailureReason.None, effects, consumesMover: true, followUpSteps: null);
        }

        /// <summary>
        /// Collision rejected. The whole move intent fails atomically with this reason.
        /// </summary>
        public static CollisionPlan Failed(MoveFailureReason reason)
        {
            return new CollisionPlan(
                false, reason, null, consumesMover: false, followUpSteps: null);
        }

        /// <summary>
        /// Returns a copy of this plan with follow-up steps appended after locomotion.
        /// </summary>
        public CollisionPlan WithFollowUpSteps(IReadOnlyList<MoveStep> steps)
        {
            return new CollisionPlan(
                Succeeded, FailureReason, Effects, ConsumesMover, steps);
        }
    }
}
