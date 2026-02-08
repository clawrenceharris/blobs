using System;
using Blobs.Animation;
using Blobs.Core.Merge;
using DG.Tweening;

namespace Blobs.Merge.Animation
{
    /// <summary>
    /// Animates MoveBlobEvent: optional anticipation (squash/lean) then move, optional overshoot/settle.
    /// Ghost override: when recipe.useFadeForMove, adds fade out before move and fade in after (elastic ghost behavior).
    /// </summary>
    public class MergeBlobsEventAnimator : IEventAnimator
    {
        
        public Sequence BuildSequence(IMergeEvent e, IBoardPresenter board)
        {
            if (e is not MergeBlobsEvent evt) return null     ;
            var blobToMove = board.GetBlob(evt.BlobToMoveId);
            var blobToRemove = board.GetBlob(evt.BlobToRemoveId);
            if (blobToMove == null || blobToRemove == null) return null;
            var rec = AnimationRecipeProvider.Instance.GetBlobRecipe<BlobAnimationRecipe>(blobToMove.Model.Type);
            
            if (blobToRemove.Model.ID == blobToMove.Model.ID)
            {
                return blobToMove.MoveToGrid(evt.To).Append(blobToRemove.Remove());
            }
            
            return blobToMove.Merge(blobToRemove).AppendCallback(() => blobToRemove.PlayMergeEffect());
            
        }

        public Sequence BuildUndoSequence(IMergeEvent e, IBoardPresenter board)
        {
            if (e is not MergeBlobsEvent evt) return null;
            if (!evt.TryGetRestoredBlobId(out var hitBlobId)) return null;
            var blobToMove = board.GetBlob(evt.BlobToMoveId);
            var blobToRemove = board.GetBlob(evt.BlobToRemoveId);
            if (blobToMove == null || blobToRemove == null) return null;
            return blobToMove.MoveToGrid(evt.From).Append(blobToRemove?.Respawn());

            

        }
    }
}
