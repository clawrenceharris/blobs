using System;
using Blobs.Animation;
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
            var rec = AnimationRecipeProvider.Instance.GetBlobRecipe<BlobAnimationRecipe>(presenter.Model.Type);
                // Defaults if no recipe is provided (defensive)
            float anticipationDuration = rec != null && rec.mergeAnticipationDuration > 0 ? rec.mergeAnticipationDuration : 0f;
            float squashAmount = rec != null && rec.mergeSquashAmount > 0 ? rec.mergeSquashAmount : 0.85f;

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
                // var anticipScale = new Vector3(baseScale.x * stretchX, baseScale.y * squashAmount, baseScale.z);
                seq.Append(transform.DOScale(squishScale, anticipationDuration));
            }

            var mergeTween = presenter.MoveToGrid(evt.To).AppendInterval(0.2f).Join(hitBlobPresenter.Remove());

              
            return mergeTween;
        }

        public Sequence BuildUndoSequence(IMergeEvent e, IBoardPresenter board)
        {
            if (e is not MergeBlobsEvent evt) return null;
            if (!evt.TryGetRestoredBlobId(out var hitBlobId)) return null;
            Debug.Log($"Respawning blob {hitBlobId}");
            var presenter = board.GetBlob(evt.BlobId);
            var hitBlobPresenter = board.GetBlob(hitBlobId);
            if (presenter == null) return null;
            return presenter.MoveToGrid(evt.From).Append(hitBlobPresenter?.Respawn());

            

        }
    }
}
