using UnityEngine;

namespace Blobs.Core.Merge
{


    public sealed class MoveBlobEvent : IMergeEvent
    {
        public string BlobId{ get; set; } 
        public Vector2Int From { get; set; }
        public Vector2Int To { get; set; }

        public void Execute(IBoardPresenter board)
        {
             board.MoveBlob(BlobId, To);
        }

        public void Undo(IBoardPresenter board)
        {
            board.MoveBlob(BlobId, From);
        }
    }

    public sealed class MergeBlobsEvent : IMergeEvent
    {
        public string BlobToMoveId { get; set; }
        public string BlobToRemoveId { get; set; }
        private Blob _cached; // to restore on undo
        public Vector2Int To { get; set; }

        public Vector2Int From { get; set; }

        public void Execute(IBoardPresenter board)
        {

            _cached = board.GetBlob(BlobToRemoveId)?.Model;
            if (_cached != null) {
                board.RemoveBlob(_cached.ID);
                board.MoveBlob(BlobToMoveId, _cached.GridPosition);
            }

        }

        public void Undo(IBoardPresenter board)
        {
            if (_cached != null) {
                board.MoveBlob(BlobToMoveId, From);
                board.RespawnBlob(_cached.ID);
            
            }
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



    public sealed class RemoveBlobEvent : IMergeEvent
    {
        public string BlobId { get; set; }
        private Blob _cached; // to restore on undo

        public void Execute(IBoardPresenter board)
        {
            
            _cached = board.GetBlob(BlobId)?.Model;
            if (_cached != null) board.RemoveBlob(BlobId);
        }

        public void Undo(IBoardPresenter board)
        {
            if (_cached != null) board.RespawnBlob(_cached.ID);
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
        public Blob BlobToSpawn { get; set; }

        public void Execute(IBoardPresenter board) => board.SpawnBlob(BlobToSpawn);
        public void Undo(IBoardPresenter board) => board.RemoveBlob(BlobToSpawn.ID);
    }

    public sealed class ResizeBlobEvent : IMergeEvent
    {
        public string BlobId { get; set; }
        public BlobSize From { get; set; }
        public BlobSize To { get; set; }

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
        public string BlobId { get; set; }
        public string ExpressionKey { get; set; }
        public float Duration { get; set; }

        public void Execute(IBoardPresenter board) { }
        public void Undo(IBoardPresenter board) { }
    }

    /// <summary>
    /// Visual-only event: play VFX at a position (particles, etc.).
    /// Execute/Undo are no-ops; consumed only by animation.
    /// </summary>
    public sealed class PlayVfxEvent : IMergeEvent
    {
        public string VfxKey { get; set; }

        /// <summary>
        /// The ID of the blob that triggers this effect
        /// </summary>
        public string BlobTriggerId { get; set; } // Possibly null

        /// <summary>
        /// The ID of the tile that triggers this effect
        /// </summary>
        public string TileTriggerId { get; set; }    // Possibly null
        public Vector2Int GridPos { get; set; } 

        public void Execute(IBoardPresenter board) { }
        public void Undo(IBoardPresenter board) { }
    }
}