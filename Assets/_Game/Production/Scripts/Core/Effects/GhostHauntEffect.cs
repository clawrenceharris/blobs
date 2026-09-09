using System;
using System.Collections.Generic;
using System.Linq;

namespace Blobs.Core
{
    public readonly struct ReturnStep
    {
        public GridPosition Position { get; }
        public string BlobId { get; }
        public ReturnStep(string ghostId, GridPosition position, string blobId)
        {
            Position = position;
            BlobId = blobId;
        }
    }

    /// <summary>A continuous ethereal path. Only its landing affects occupancy.</summary>
    public sealed class GhostHauntEffect : IBoardEffect
    {
        public GhostHauntEffect(string ghostId, string landingBlobId, IReadOnlyList<GridPosition> path, bool isCleared = false)
        {
            if (path == null || path.Count == 0)
                throw new ArgumentException("A ghost return requires a nonempty path.", nameof(path));
            GhostId = ghostId;
            Path = path;
            Rests = isCleared;
            LandingBlobId = landingBlobId;
        }

        public string GhostId { get; }
        public IReadOnlyList<GridPosition> Path { get; }
        public GridPosition HauntDestination => Path[Path.Count - 1];

        /// <summary>
        /// Whether the ghost's final destination is where it should rest and be removed from the board.
        /// </summary>
        public bool Rests { get; }

        /// <summary>
        /// The ID of the blob that the ghost will land on after the haunt is complete. Null if the ghost is in rest.
        /// </summary>
        public string LandingBlobId { get; private set; }



        /// <summary>
        /// Called when the ghost is cleared from the board due to a collision with a gravestone tile.
        /// </summary>
        /// <param name="ghostId">The ID of the ghost that is being cleared.</param>
        /// <param name="landingBlobId">The ID of the blob that the ghost is landing on.</param>
        /// <param name="steps">The steps that the ghost is taking.</param>
        /// <returns>A new GhostHauntEffect representing the ghost being cleared.</returns>

        public static GhostHauntEffect Rest(string ghostId, IReadOnlyList<GridPosition> path)
        {
            return new GhostHauntEffect(ghostId, null, path, true);
        }

        /// <summary>
        /// Called when the ghost is successfully haunts a source blob without being cleared by a gravestone tile.
        /// </summary>
        /// <param name="ghostId">The ID of the ghost that is haunting the source blob.</param>
        /// <param name="steps">The steps that the ghost is taking.</param>
        /// <param name="landingBlobId">The ID of the blob that the ghost is landing on.</param>
        /// <returns>A new GhostHauntEffect representing the ghost haunting the source blob.</returns>

        public static GhostHauntEffect Haunt(string ghostId, IReadOnlyList<GridPosition> path)
        {
            return new GhostHauntEffect(ghostId, null, path);
        }



        public void Apply(BoardState board)
        {
            // Clearing is a separate semantic effect; never occupy an occupied Sigil.
            if (Rests)
                return;
            LandingBlobId = board.GetBlobAt(HauntDestination)?.Id;
            if (LandingBlobId != null)
                board.RemoveBlob(LandingBlobId);
            board.MoveBlob(GhostId, HauntDestination);
        }
    }
}
