using System;
using System.Collections.Generic;
using System.Linq;

namespace Blobs.Core
{
    public readonly struct ReturnStep
    {
        public string GhostId { get; }
        public GridPosition Position { get; }

        public ReturnStep(string ghostId, GridPosition position)
        {
            GhostId = ghostId;
            Position = position;
        }
    }

    /// <summary>A continuous ethereal path. Only its landing affects occupancy.</summary>
    public sealed class GhostReturnEffect : IBoardEffect
    {
        public GhostReturnEffect(string ghostId, IReadOnlyList<ReturnStep> steps,
            bool isClearing = false, string landingBlobId = null)
        {
            if (steps == null || steps.Count == 0)
                throw new ArgumentException("A ghost return requires a nonempty path.", nameof(steps));
            GhostId = ghostId;
            Steps = Array.AsReadOnly(steps.ToArray());
            IsClearing = isClearing;
            LandingBlobId = landingBlobId;
        }

        public string GhostId { get; }
        public IReadOnlyList<ReturnStep> Steps { get; }
        public ReturnStep LastStep => Steps[Steps.Count - 1];
        public bool IsClearing { get; }
        public string LandingBlobId { get; }

        // Resolve after all source departures, including newly spawned trail blobs.
        internal GhostReturnEffect ResolveLanding(BoardState board)
        {
            return new GhostReturnEffect(GhostId, Steps, IsClearing,
                IsClearing ? null : board.GetBlobAt(LastStep.Position)?.Id);
        }

        public void Apply(BoardState board)
        {
            // Clearing is a separate semantic effect; never occupy an occupied Sigil.
            if (IsClearing)
                return;
            if (LandingBlobId != null)
                board.RemoveBlob(LandingBlobId);
            board.MoveBlob(GhostId, LastStep.Position);
        }
    }
}
