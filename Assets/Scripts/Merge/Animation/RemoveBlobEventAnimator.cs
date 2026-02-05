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
        public void BuildSequence(IMergeEvent e, IBoardPresenter board, Action onComplete = null)
        {
            if (e is not RemoveBlobEvent evt) return;
            var presenter = board?.GetBlob(evt.BlobId);
            if (presenter == null) return;
            // var rec = recipe as BlobAnimationRecipe;
            presenter.Remove(onComplete);;
            // if (rec != null && rec.removeDuration > 0)
            //     removeTween.SetEase(rec.removeEase);
            // if (rec != null && !string.IsNullOrEmpty(rec.beforeRemoveExpression) && presenter.View != null)
            // {
            //     if (presenter.View.TryGetComponent<Animator>(out var anim))
            //     {
            //         var seq = DOTween.Sequence();
            //         seq.AppendCallback(() => anim.SetTrigger(rec.beforeRemoveExpression));
            //         seq.AppendInterval(rec.beforeRemoveExpressionDuration > 0 ? rec.beforeRemoveExpressionDuration : 0.2f);
            //         seq.Append(removeTween);
            //         return seq;
            //     }
            // }
            // return DOTween.Sequence().Append(removeTween);
        }

        public void BuildUndoSequence(IMergeEvent e, IBoardPresenter board, Action onComplete = null)
        {
            if (e is not RemoveBlobEvent evt) return;
            if (!evt.TryGetRestoredBlobId(out var id)) return;
            var presenter = board?.GetBlob(id);
            if (presenter == null) return;
            // var rec = recipe as BlobAnimationRecipe;
            presenter.Spawn(onComplete);
            // if (rec != null && rec.spawnDuration > 0)
                // spawnTween.SetEase(rec.spawnEase);
            // return DOTween.Sequence().Append(spawnTween);
        }
    }
}
