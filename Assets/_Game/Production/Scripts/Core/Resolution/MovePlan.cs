using System;
using System.Collections.Generic;

namespace Blobs.Core
{
    /// <summary>
    /// Initial route produced from a selected source/target move intent before
    /// per-tile simulation begins.
    /// </summary>
    /// <remarks>
    /// A move plan may rewrite the source, target, start, or end that the resolver uses
    /// to walk the path. It does not describe what happens when the mover reaches an
    /// occupied cell; that belongs to <see cref="CollisionPlan"/> and
    /// <see cref="ICollisionStrategy"/>.
    /// </remarks>
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

        /// <summary>
        /// Whether the selected source/target intent is valid enough for path
        /// simulation to begin.
        /// </summary>
        public bool Succeeded { get; }

        /// <summary>
        /// Reason the initial move intent was rejected.
        /// </summary>
        public MoveFailureReason FailureReason { get; }

        /// <summary>
        /// Blob that the resolver treats as the mover when simulation begins.
        /// </summary>
        public BlobState Source { get; }

        /// <summary>
        /// Intended final target for the move. Intermediate occupants are still handled
        /// as collisions as the resolver reaches them.
        /// </summary>
        public BlobState Target { get; }

        /// <summary>
        /// Position where the resolver starts walking the planned path.
        /// </summary>
        public GridPosition StartPosition { get; }

        /// <summary>
        /// Position the resolver walks toward unless a collision ends locomotion first.
        /// </summary>
        public GridPosition EndPosition { get; }

        /// <summary>
        /// Reserved follow-up steps for move-level mechanics that need to append their
        /// own timeline after the main path. Most collision aftermath should use
        /// <see cref="CollisionPlan.FollowUpSteps"/> instead.
        /// </summary>
        public IReadOnlyList<MoveStep> Steps { get; }

        /// <summary>
        /// Returns a copy that walks toward a different end position.
        /// </summary>
        public MovePlan WithEndPosition(GridPosition end)
        {
            return new MovePlan(
                succeeded: Succeeded,
                start: StartPosition,
                end: end,
                failureReason: FailureReason,
                steps: Steps,
                source: Source,
                target: Target);
        }

        /// <summary>
        /// Returns a copy that treats a different blob as the intended final target.
        /// </summary>
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


        /// <summary>
        /// Returns a copy that starts path simulation from a different position.
        /// </summary>
        public MovePlan WithStartPosition(GridPosition start)
        {
            return new MovePlan(
                succeeded: Succeeded,
                start: start,
                end: EndPosition,
                failureReason: FailureReason,
                steps: Steps,
                source: Source,
                target: Target);
        }

        /// <summary>
        /// Creates the unmodified direct plan from the selected source to the selected
        /// target.
        /// </summary>
        public static MovePlan Default(BlobState source, BlobState target)
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
        /// Marks the move intent as valid with an explicit start and end position.
        /// </summary>
        public MovePlan Success(GridPosition start, GridPosition end)
        {
            return new MovePlan(
                succeeded: true,
                start: start,
                end: end,
                failureReason: MoveFailureReason.None,
                steps: Steps,
                source: Source,
                target: Target);
        }

        /// <summary>
        /// Marks the move intent as valid with an explicit source and intended target.
        /// </summary>
        public MovePlan Success(BlobState source, BlobState target)
        {
            return new MovePlan(
                succeeded: true,
                start: source.Position,
                end: target.Position,
                failureReason: MoveFailureReason.None,
                steps: Steps,
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
