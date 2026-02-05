using System;
using Blobs.Core.Merge;
using DG.Tweening;
using UnityEngine;

namespace Blobs.Merge.Animation
{
    /// <summary>
    /// Animates ExpressionEvent: sets Animator trigger/bool for facial expression (e.g. Shocked before Sigil clear).
    /// Uses recipe for duration when provided. Undo is a no-op (expression was transient).
    /// </summary>
    public class ExpressionEventAnimator : IEventAnimator
    {
        public void BuildSequence(IMergeEvent e, IBoardPresenter board, Action onComplete)
        {
            // if (e is not ExpressionEvent evt) return null;
            // var presenter = board?.GetBlob(evt.BlobId);
            // if (presenter?.View == null) return null;
            // var anim = presenter.View.GetComponent<Animator>();
            // if (anim == null || string.IsNullOrEmpty(evt.ExpressionKey)) return null;
            // var rec = recipe as BlobAnimationRecipe;
            // var duration = rec != null && rec.beforeRemoveExpressionDuration > 0 ? rec.beforeRemoveExpressionDuration : evt.Duration;
            // anim.SetTrigger(evt.ExpressionKey);
            // return DOTween.Sequence().AppendInterval(duration);
        }

        public void BuildUndoSequence(IMergeEvent e, IBoardPresenter board, Action onComplete)
        {
        }
    }
}
