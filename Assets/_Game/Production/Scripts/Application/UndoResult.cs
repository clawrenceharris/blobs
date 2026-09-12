using System.Collections.Generic;
using Blobs.Core;

namespace Blobs.Application
{
    /// <summary>
    /// Recorded board-changing action used to restore Core state and reverse presentation.
    /// Presentation consumes the original forward timeline; it does not re-resolve rules.
    /// </summary>
    public sealed class UndoResult
    {
        public UndoResult(
            MoveResult forwardAction,
            GameSessionSnapshot restoredSnapshot,
            IReadOnlyDictionary<string, BlobState> restorationBlobs)
        {
            ForwardAction = forwardAction;
            RestoredSnapshot = restoredSnapshot;
            RestorationBlobs = restorationBlobs;
        }

        public MoveResult ForwardAction { get; }
        public GameSessionSnapshot RestoredSnapshot { get; }
        public IReadOnlyDictionary<string, BlobState> RestorationBlobs { get; }
    }
}
