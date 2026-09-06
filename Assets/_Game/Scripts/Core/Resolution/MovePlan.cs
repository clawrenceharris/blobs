using System;
using System.Collections.Generic;

namespace Blobs.Core
{
    /// <summary>
    /// Outcome of resolving a single collision between a moving blob and the occupant
    /// of a cell on its path. Strategies describe only what happens at the collision
    /// tile; locomotion is owned by the resolver.
    /// </summary>
    public sealed class MovePlan
    {
        private MovePlan(
            bool succeeded,
            Move movement,
            MoveFailureReason failureReason,
            IReadOnlyList<MoveStep> steps = null)
        {
            Succeeded = succeeded;
            FailureReason = failureReason;
            Move = movement;
            Steps = steps ?? Array.Empty<MoveStep>();
        }

        public bool Succeeded { get; }
        public MoveFailureReason FailureReason { get; }

        public Move Move { get; }

        /// <summary>
        /// Steps appended after the main locomotion timeline. This is the hook for
        /// mechanics like the Ghost blob that perform their own movement in response
        /// to a merge.
        /// </summary>
        public IReadOnlyList<MoveStep> Steps { get; }

        /// <summary>
        /// Modify the move plan to a new move.
        /// </summary>
        /// <summary>
        /// Move resolved; the move is valid and the mover may continue.
        /// </summary>
        public static MovePlan Continue(Move movement)
        {
            return new MovePlan(
                true, movement, MoveFailureReason.None);
        }



        /// <summary>
        /// Move rejected. The whole move intent fails atomically with this reason.
        /// </summary>
        public static MovePlan Failed(MoveFailureReason reason)
        {
            return new MovePlan(
                false, Move.None(), reason);
        }



        /// <summary>
        /// Returns a copy of this plan with follow-up steps appended after locomotion.
        /// </summary>
        public MovePlan WithSteps(IReadOnlyList<MoveStep> steps)
        {
            return new MovePlan(
                Succeeded, Move, FailureReason, steps);
        }
    }
}
