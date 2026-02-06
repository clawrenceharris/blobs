using System;
using Blobs.Core.Merge;
using DG.Tweening;
using UnityEngine;

namespace Blobs.Merge.Animation
{
    /// <summary>
    /// Animates ResizeBlobEvent: elastic scale to new size. Uses recipe for duration/easing.
    /// </summary>
    public class ResizeBlobEventAnimator : IEventAnimator
    {
        public Sequence BuildSequence(IMergeEvent e, IBoardPresenter board)
        {
            if (e is not ResizeBlobEvent evt) return null;
            var presenter = board?.GetBlob(evt.BlobId);
            if (presenter == null) return null;
            var scale = evt.To switch { BlobSize.Small => 0.5f, BlobSize.Big => 1.4f, _ => 1f };
            return presenter.ScaleTo(scale);
      
        }

        public Sequence BuildUndoSequence(IMergeEvent e, IBoardPresenter board)
        {
            if (e is not ResizeBlobEvent evt) return null;
            var presenter = board?.GetBlob(evt.BlobId);
            if (presenter == null) return null;
            var scale = evt.From switch { BlobSize.Small => 0.5f, BlobSize.Big => 1.4f, _ => 1f };
            return presenter.ScaleTo(scale);
            
        }
    }
}
