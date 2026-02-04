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
        public Sequence BuildSequence(IMergeEvent e, IBoardPresenter board, object recipe)
        {
            if (e is not MoveBlobEvent evt) return null;
            var presenter = board?.GetBlobById(evt.BlobId);
            if (presenter == null) return null;
            var rec = recipe as BlobAnimationRecipeSO;
            var moveTween = presenter.MoveToGrid(evt.To, rec.moveDuration);
            if (rec != null && rec.useFadeForMove && presenter.View.Visuals.SpriteRenderer != null)
            {
                var sr = presenter.View.Visuals.SpriteRenderer;
                var seq = DOTween.Sequence();
                seq.Append(sr.DOFade(0f, rec.fadeOutDuration > 0 ? rec.fadeOutDuration : 0.2f));
                seq.Append(moveTween);
                seq.Append(sr.DOFade(1f, rec.fadeInDuration > 0 ? rec.fadeInDuration : 0.2f));
                return seq;
            }
            return DOTween.Sequence().Append(moveTween);
        }

        public Sequence BuildUndoSequence(IMergeEvent e, IBoardPresenter board, object recipe)
        {
            if (e is not MoveBlobEvent evt) return null;
            var presenter = board?.GetBlobById(evt.BlobId);
            if (presenter == null) return null;
            var rec = recipe as BlobAnimationRecipeSO;
            var moveTween = presenter.MoveToGrid(evt.From, rec.moveDuration);
            if (rec != null && rec.useFadeForMove && presenter.View.Visuals.SpriteRenderer != null)
            {
                var sr = presenter.View.Visuals.SpriteRenderer;
                var seq = DOTween.Sequence();
                seq.Append(sr.DOFade(0f, rec.fadeOutDuration > 0 ? rec.fadeOutDuration : 0.2f));
                seq.Append(moveTween);
                seq.Append(sr.DOFade(1f, rec.fadeInDuration > 0 ? rec.fadeInDuration : 0.2f));
                return seq;
            }
            return DOTween.Sequence().Append(moveTween);
        }
    }
}
