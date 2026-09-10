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
            GridPosition start,
            GridPosition end,
            BlobState source,
            BlobState target,
            IReadOnlyList<MoveStep> steps = null,
            MoveFailureReason failureReason = MoveFailureReason.None
            )
        {
            Succeeded = succeeded;
            FailureReason = failureReason;
            StartPosition = start;
            EndPosition = end;
            Steps = steps ?? Array.Empty<MoveStep>();
            Source = source;
            Target = target;
        }

        public bool Succeeded { get; }
        public MoveFailureReason FailureReason { get; }

        public BlobState Source { get; }
        public BlobState Target { get; }

        public GridPosition StartPosition { get; }
        public GridPosition EndPosition { get; }

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
        public MovePlan EndAt(GridPosition end)
        {
            return new MovePlan(
                succeeded: true,
                start: StartPosition,
                end: end,
                failureReason: FailureReason,
                steps: Steps,
                source: Source,
                target: Target);
        }

        public MovePlan WithTarget(BlobState target)
        {
            return new MovePlan(
                succeeded: Succeeded,
                start: StartPosition,
                end: EndPosition,
                failureReason: FailureReason,
                steps: Steps,
                source: Source,
                target: target);
        }


        public MovePlan StartAt(GridPosition start)
        {
            return new MovePlan(
                succeeded: true,
                start: start,
                end: EndPosition,
                failureReason: FailureReason,
                steps: Steps,
                source: Source,
                target: Target);
        }

        public static MovePlan Move(BlobState source, BlobState target)
        {
            return new MovePlan(
                succeeded: true,
                start: source.Position,
                end: target.Position,
                failureReason: MoveFailureReason.None,
                steps: Array.Empty<MoveStep>(),
                source: source,
                target: target);
        }

        /// <summary>
        /// Move rejected. The whole move intent fails atomically with this reason.
        /// </summary>
        public MovePlan Failed(MoveFailureReason reason)
        {
            return new MovePlan(
                succeeded: false,
                start: StartPosition,
                end: EndPosition,
                failureReason: reason,
                steps: Steps,
                source: Source,
                target: Target);
        }



        /// <summary>
        /// Returns a copy of this plan with follow-up steps appended after locomotion.
        /// </summary>
        public MovePlan WithSteps(IReadOnlyList<MoveStep> steps)
        {
            return new MovePlan(
                succeeded: Succeeded,
                start: StartPosition,
                end: EndPosition,
                failureReason: FailureReason,
                steps: steps,
                source: Source,
                target: Target);
        }

        public override string ToString()
        {
            return $"MovePlan: Succeeded:{Succeeded} FailureReason:{FailureReason} StartPosition:{StartPosition} EndPosition:{EndPosition} Source:{Source} Target:{Target} Steps:{Steps}";
        }
    }
}
