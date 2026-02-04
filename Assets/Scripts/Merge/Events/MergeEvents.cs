using UnityEngine;

namespace Blobs.Core.Merge
{
   

    public sealed class MoveBlobEvent : IMergeEvent
{
    public string BlobId;
    public Vector2Int From;
    public Vector2Int To;

    public void Execute(IBoardPresenter board)
    {
        var blob = board.GetBlob(BlobId)?.Model;
        if (blob != null) board.MoveBlob(blob.ID, To);
    }

    public void Undo(IBoardPresenter board)
    {
        var blob = board.GetBlob(BlobId)?.Model;
        if (blob != null) board.MoveBlob(blob.ID, From);
    }
}


    public sealed class RemoveBlobEvent : IMergeEvent
    {
        public string BlobId;
        private Blob _cached; // to restore on undo

        public void Execute(IBoardPresenter board)
        {
            
            _cached = board.GetBlob(BlobId)?.Model;
            if (_cached != null) board.RemoveBlob(BlobId);
        }

        public void Undo(IBoardPresenter board)
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

        public void Execute(IBoardPresenter board) => board.PlaceBlob(BlobToSpawn);
        public void Undo(IBoardPresenter board) => board.RemoveBlob(BlobToSpawn.ID);
    }

    public sealed class ResizeBlobEvent : IMergeEvent
    {
        public string BlobId;
        public BlobSize From;
        public BlobSize To;

        public void Execute(IBoardPresenter board)
        {
            var blob = board.GetBlob(BlobId)?.Model;
            if (blob != null) blob.Size = To;
        }

        public void Undo(IBoardPresenter board)
        {
            var blob = board.GetBlob(BlobId)?.Model;
            if (blob != null) blob.Size = From;
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

        public void Execute(IBoardPresenter board) { }
        public void Undo(IBoardPresenter board) { }
    }

    /// <summary>
    /// Visual-only event: play VFX at a position (particles, etc.).
    /// Execute/Undo are no-ops; consumed only by animation.
    /// </summary>
    public sealed class PlayVfxEvent : IMergeEvent
    {
        public string VfxKey;
        public Vector2Int GridPos;

        public void Execute(IBoardPresenter board) { }
        public void Undo(IBoardPresenter board) { }
    }
}