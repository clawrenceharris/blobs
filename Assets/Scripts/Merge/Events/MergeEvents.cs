using UnityEngine;

namespace Blobs.Core.Merge
{
   

    public sealed class MoveBlobEvent : IMergeEvent
{
    public string BlobId;
    public Vector2Int From;
    public Vector2Int To;

    public void Execute(BoardModel board)
    {
        var b = board.GetBlob(BlobId);
        if (b != null) board.MoveBlob(b, To);
    }

    public void Undo(BoardModel board)
    {
        var b = board.GetBlob(BlobId);
        if (b != null) board.MoveBlob(b, From);
    }
}


    public sealed class RemoveBlobEvent : IMergeEvent
    {
        public string BlobId;
        private Blob _cached; // to restore on undo

        public void Execute(BoardModel board)
        {
            
            _cached = board.GetBlob(BlobId);
            if (_cached != null) board.RemoveBlob(BlobId);
        }

        public void Undo(BoardModel board)
        {
            if (_cached != null) board.PlaceBlob(_cached);
        }

        /// <summary>Returns the restored blob ID for undo animation (e.g. view lookup).</summary>
        public bool TryGetRestoredBlobId(out string id)
        {
            if (_cached != null)
            {
                id = _cached.ID;
                return true;
            }
            id = null;
            return false;
        }
    }

    public sealed class SpawnBlobEvent : IMergeEvent
    {
        public Blob BlobToSpawn;

        public void Execute(BoardModel board) => board.PlaceBlob(BlobToSpawn);
        public void Undo(BoardModel board) => board.RemoveBlob(BlobToSpawn.ID);
    }

    public sealed class ResizeBlobEvent : IMergeEvent
    {
        public string BlobId;
        public BlobSize From;
        public BlobSize To;

        public void Execute(BoardModel board)
        {
            var b = board.GetBlob(BlobId);
            if (b != null) b.Size = To;
        }

        public void Undo(BoardModel board)
        {
            var b = board.GetBlob(BlobId);
            if (b != null) b.Size = From;
        }
    }

    /// <summary>
    /// Visual-only event: play a facial expression on a blob (e.g. Shocked before Sigil clear).
    /// Execute/Undo are no-ops; consumed only by animation.
    /// </summary>
    public sealed class ExpressionEvent : IMergeEvent
    {
        public string BlobId;
        public string ExpressionKey;
        public float Duration;

        public void Execute(BoardModel board) { }
        public void Undo(BoardModel board) { }
    }

    /// <summary>
    /// Visual-only event: play VFX at a position (particles, etc.).
    /// Execute/Undo are no-ops; consumed only by animation.
    /// </summary>
    public sealed class PlayVfxEvent : IMergeEvent
    {
        public string VfxKey;
        public Vector2Int GridPos;

        public void Execute(BoardModel board) { }
        public void Undo(BoardModel board) { }
    }
}