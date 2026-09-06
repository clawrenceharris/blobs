using System;

namespace Blobs.Core
{
    /// <summary>
    /// Context for one collision on a move path. <see cref="Board"/> is the resolver's
    /// simulation board, so mid-path collisions observe the true occupancy after any
    /// earlier merges and spawns in the same move.
    /// </summary>
    public readonly struct MoveContext
    {
        public MoveContext(
            BoardState board,
            GridPosition startPosition,
            BlobState source,
            BlobState target,
            bool isFinalTarget = true)
        {
            Board = board;
            Source = source;
            Target = target;
            IsFinalTarget = isFinalTarget;
            StartPosition = startPosition;
        }

        public BoardState Board { get; }

        /// <summary>Current state of the moving blob (position reflects mid-path progress).</summary>
        public BlobState Source { get; }

        /// <summary>The occupant being collided with at this beat.</summary>
        public BlobState Target { get; }


        /// <summary>
        /// The start position of the merge.
        /// </summary>
        public GridPosition StartPosition { get; }


        /// <summary>
        /// True when <see cref="Target"/> is the blob named by the move intent,
        /// false for occupants encountered earlier on the path.
        /// </summary>
        public bool IsFinalTarget { get; }
    }
}
