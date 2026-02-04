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
            var presenter = board?.GetBlob(evt.BlobId);
            if (presenter == null) return null;
            var rec = recipe as BlobAnimationRecipe;

            // Defaults if no recipe is provided (defensive)
            float moveDuration = rec != null && rec.moveDuration > 0 ? rec.moveDuration : 0.35f;
            float anticipationDuration = rec != null && rec.anticipationDuration > 0 ? rec.anticipationDuration : 0f;
            float overshootDuration = rec != null && rec.overshootDuration > 0 ? rec.overshootDuration : 0f;
            float squashAmount = rec != null && rec.squashAmount > 0 ? rec.squashAmount : 0.85f;

            var view = presenter.View;
            var transform = view.transform;
            var baseScale = transform.localScale;
            var seq = DOTween.Sequence();

            // 1) Anticipation: quick squash in travel direction before moving
            if (anticipationDuration > 0f)
            {
                // Simple "squash & stretch": preserve approximate area
                float stretchX = 2f - squashAmount;
                 Vector3 squishScale = new Vector3(
                baseScale.x * rec.selectionStretchAmount,
                baseScale.y * rec.selectionSquishAmount,
                baseScale.z
        );
                var anticipScale = new Vector3(baseScale.x * stretchX, baseScale.y * squashAmount, baseScale.z);
                seq.Append(transform.DOScale(anticipScale, anticipationDuration));
            }
            // Core move tween (position), eased by recipe
            var moveTween = presenter.MoveToGrid(evt.To, moveDuration);
            if (rec != null)
            {
                moveTween.SetEase(rec.moveEase);
            }


           

            
            seq.Append(moveTween);
            

            // 3) Overshoot / settle: slight stretch then return to base scale
            if (overshootDuration > 0f)
            {
                float half = overshootDuration * 0.5f;
                var overshootScale = new Vector3(baseScale.x * 1.05f, baseScale.y * 0.95f, baseScale.z);
                seq.Append(transform.DOScale(overshootScale, half));
                seq.Append(transform.DOScale(baseScale, half));
            }
            else
            {
                // Ensure we end exactly at base scale after anticipation
                seq.Append(transform.DOScale(baseScale, 0.01f));
            }

            return seq;
        }

        public Sequence BuildUndoSequence(IMergeEvent e, IBoardPresenter board, object recipe)
        {
            if (e is not MoveBlobEvent evt) return null;
            var presenter = board?.GetBlob(evt.BlobId);
            if (presenter == null) return null;
            var rec = recipe as BlobAnimationRecipe;

            float moveDuration = rec != null && rec.moveDuration > 0 ? rec.moveDuration : 0.35f;
            float anticipationDuration = rec != null && rec.anticipationDuration > 0 ? rec.anticipationDuration : 0f;
            float overshootDuration = rec != null && rec.overshootDuration > 0 ? rec.overshootDuration : 0f;
            float squashAmount = rec != null && rec.squashAmount > 0 ? rec.squashAmount : 0.85f;

            var view = presenter.View;
            var transform = view.transform;
            var baseScale = transform.localScale;

            var moveTween = presenter.MoveToGrid(evt.From, moveDuration);
            if (rec != null)
            {
                moveTween.SetEase(rec.moveEase);
            }

            var seq = DOTween.Sequence();

            if (anticipationDuration > 0f)
            {
                float stretchX = 2f - squashAmount;
                var anticipScale = new Vector3(baseScale.x * stretchX, baseScale.y * squashAmount, baseScale.z);
                seq.Append(transform.DOScale(anticipScale, anticipationDuration));
            }

            seq.Append(moveTween);
            

            if (overshootDuration > 0f)
            {
                float half = overshootDuration * 0.5f;
                var overshootScale = new Vector3(baseScale.x * 1.05f, baseScale.y * 0.95f, baseScale.z);
                seq.Append(transform.DOScale(overshootScale, half));
                seq.Append(transform.DOScale(baseScale, half));
            }
            else
            {
                seq.Append(transform.DOScale(baseScale, 0.01f));
            }

            return seq;
        }
    }
}
