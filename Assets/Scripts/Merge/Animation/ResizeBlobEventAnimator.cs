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
        public void BuildSequence(IMergeEvent e, IBoardPresenter board, Action onComplete)
        {
            if (e is not ResizeBlobEvent evt) return;
            var presenter = board?.GetBlob(evt.BlobId);
            if (presenter == null) return;
            // var rec = recipe as BlobAnimationRecipe;
            var scale = evt.To switch { BlobSize.Small => 0.5f, BlobSize.Big => 1.4f, _ => 1f };
            presenter.ScaleTo(scale, onComplete);
            // if (rec != null && rec.resizeDuration > 0)
            //     scaleTween.SetEase(rec.resizeEase);
            // return DOTween.Sequence().Append(scaleTween);
        }

        public void BuildUndoSequence(IMergeEvent e, IBoardPresenter board, Action onComplete)
        {
            if (e is not ResizeBlobEvent evt) return;
            var presenter = board?.GetBlob(evt.BlobId);
            if (presenter == null) return;
            // var rec = recipe as BlobAnimationRecipe;
            var scale = evt.From switch { BlobSize.Small => 0.5f, BlobSize.Big => 1.4f, _ => 1f };
            presenter.ScaleTo(scale, onComplete);
            // if (rec != null && rec.resizeDuration > 0)
            //     scaleTween.SetEase(rec.resizeEase);
            // return DOTween.Sequence().Append(scaleTween);
        }
    }
}
