using System;
using Blobs.Core.Merge;
using DG.Tweening;
using UnityEngine;

namespace Blobs.Merge.Animation
{
    /// <summary>
    /// Animates RemoveBlobEvent: shrink/pop + optional particles. Ghost override: play preRemoveExpression (e.g. Shocked) before remove when recipe has it.
    /// </summary>
    public class RemoveBlobEventAnimator : IEventAnimator
    {
        public Sequence BuildSequence(IMergeEvent e, IBoardPresenter board)
        {
            if (e is not RemoveBlobEvent evt) return null   ;
            var presenter = board?.GetBlob(evt.BlobId);
            if (presenter == null) return null;
            return presenter.Remove();
            
        }

        public Sequence BuildUndoSequence(IMergeEvent e, IBoardPresenter board)
        {
            if (e is not RemoveBlobEvent evt) return null;
            if (!evt.TryGetRestoredBlobId(out var id)) return null;
            var presenter = board?.GetBlob(id);
            if (presenter == null) return null;
            return presenter.Remove();
           
        }
    }
}
