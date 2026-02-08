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
    public class MoveBlobEventAnimator : IEventAnimator
    {
        
        public Sequence BuildSequence(IMergeEvent e, IBoardPresenter board)
        {
            if (e is not MoveBlobEvent evt) return null     ;
            var presenter = board?.GetBlob(evt.BlobId);
            if (presenter == null) return null;

            return presenter.MoveToGrid(evt.To);
        }

        public Sequence BuildUndoSequence(IMergeEvent e, IBoardPresenter board)
        {
            if (e is not MoveBlobEvent evt) return null;
            var presenter = board?.GetBlob(evt.BlobId);
            if (presenter == null) return null;

            return presenter.MoveToGrid(evt.From);
            

        }
    }
}
