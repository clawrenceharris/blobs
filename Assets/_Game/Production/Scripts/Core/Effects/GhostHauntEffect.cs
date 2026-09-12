using System;
using System.Collections.Generic;
using System.Linq;

namespace Blobs.Core
{


    /// <summary>A continuous ethereal path. Only its landing affects occupancy.</summary>
    public sealed class GhostHauntEffect : IBoardEffect
    {
        public GhostHauntEffect(
            string ghostId,
            IReadOnlyList<GridPosition> path,
            string landingBlobId = null,
            GridPosition? origin = null)
        {
            if (path == null || path.Count == 0)
                throw new ArgumentException("A ghost return requires a nonempty path.", nameof(path));
            GhostId = ghostId;
            Path = path;
            LandingBlobId = landingBlobId;
            if (origin.HasValue)
            {
                From = origin.Value;
                HasOrigin = true;
            }
        }

        public string GhostId { get; }
        public IReadOnlyList<GridPosition> Path { get; }
        public GridPosition HauntDestination => Path.Last();
        public GridPosition From { get; private set; }
        public bool HasOrigin { get; private set; }

        /// <summary>
        /// Whether the ghost's final destination is where it should rest and be removed from the board.
        /// </summary>
        public bool Rests { get; }

        /// <summary>
        /// The ID of the blob that the ghost will land on after the haunt is complete. Null if the ghost is in rest.
        /// </summary>
        public string LandingBlobId { get; private set; }

        public BlobState LandingBlob { get; private set; }



        public void Apply(BoardState board)
        {
            BlobState ghost = board.GetBlob(GhostId);
            From = ghost != null ? ghost.Position : Path[0];
            HasOrigin = true;

            if (LandingBlob == null)
            {
                LandingBlob = LandingBlobId != null
                    ? board.GetBlob(LandingBlobId)
                    : board.GetBlobAt(HauntDestination);
                LandingBlobId = LandingBlob?.Id;
            }

            if (LandingBlobId != null)
                board.RemoveBlob(LandingBlobId);
            board.MoveBlob(GhostId, HauntDestination);
        }
    }
}
