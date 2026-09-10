using System;
using System.Collections.Generic;
using System.Linq;

namespace Blobs.Core
{


    /// <summary>A continuous ethereal path. Only its landing affects occupancy.</summary>
    public sealed class GhostHauntEffect : IBoardEffect
    {
        public GhostHauntEffect(string ghostId, IReadOnlyList<GridPosition> path, string landingBlobId = null)
        {
            if (path == null || path.Count == 0)
                throw new ArgumentException("A ghost return requires a nonempty path.", nameof(path));
            GhostId = ghostId;
            Path = path;
            LandingBlobId = landingBlobId;
        }

        public string GhostId { get; }
        public IReadOnlyList<GridPosition> Path { get; }
        public GridPosition HauntDestination => Path.Last();

        /// <summary>
        /// Whether the ghost's final destination is where it should rest and be removed from the board.
        /// </summary>
        public bool Rests { get; }

        /// <summary>
        /// The ID of the blob that the ghost will land on after the haunt is complete. Null if the ghost is in rest.
        /// </summary>
        public string LandingBlobId { get; private set; }



        public void Apply(BoardState board)
        {

            LandingBlobId ??= board.GetBlobAt(HauntDestination)?.Id;
            if (LandingBlobId != null)
                board.RemoveBlob(LandingBlobId);
            board.MoveBlob(GhostId, HauntDestination);
        }
    }
}
