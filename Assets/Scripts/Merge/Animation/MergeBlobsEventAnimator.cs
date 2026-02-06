using System;
using Blobs.Core.Merge;
using DG.Tweening;
using UnityEngine;

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
            var presenter = board.GetBlob(evt.BlobId);
            var hitBlobPresenter = board.GetBlob(evt.HitBlobId);
            if (presenter == null || hitBlobPresenter == null) return null;

            Debug.Log("Source Blob" + evt.BlobId);

            return hitBlobPresenter.Merge().AppendInterval(0.2f).Append(presenter.MoveToGrid(hitBlobPresenter.Model.GridPosition));
        }

        public Sequence BuildUndoSequence(IMergeEvent e, IBoardPresenter board)
        {
            if (e is not MergeBlobsEvent evt) return null;
            if (!evt.TryGetRestoredBlobId(out var hitBlobId)) return null;

            var presenter = board?.GetBlob(evt.BlobId);
            var hitBlobPresenter = board.GetBlob(hitBlobId);
            if (presenter == null || hitBlobPresenter == null) return null;
            return presenter.MoveToGrid(evt.StartPos).Join(hitBlobPresenter.Spawn());

            

        }
    }
}
