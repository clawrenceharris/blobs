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
            MovePlan plan,
            BlobState source,
            BlobState target,
            MoveIntent intent,
            bool isFinalTarget = true)
        {
            Board = board;
            Plan = plan;
            Source = source;
            Target = target;
            Intent = intent;
            IsFinalTarget = isFinalTarget;
        }

        public BoardState Board { get; }

        /// <summary>Current state of the moving blob (position reflects mid-path progress).</summary>
        public BlobState Source { get; }

        /// <summary>The occupant being collided with at this beat.</summary>
        public BlobState Target { get; }


        /// <summary>
        /// The plan for the move.
        /// </summary>
        public MovePlan Plan { get; }


        /// <summary>
        /// The initial move intent.
        /// </summary>

        public MoveIntent Intent { get; }


        /// <summary>
        /// True when <see cref="Target"/> is the blob named by the move intent,
        /// false for occupants encountered earlier on the path.
        /// </summary>
        public bool IsFinalTarget { get; }
    }
}
