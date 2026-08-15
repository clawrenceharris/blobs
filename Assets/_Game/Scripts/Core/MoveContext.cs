using System.Collections.Generic;

namespace Blobs.Core
{
    public sealed class MoveContext
    {
        public MoveContext(
            BoardState board,
            BlobState source,
            BlobState target)
        {
            Board = board;
            Source = source;
            Target = target;
        }

        public BoardState Board { get; }
        public BlobState Source { get; }
        public BlobState Target { get; }
    }



}