using System;
using Blobs.Content;
using Blobs.Core;
using DG.Tweening;
using UnityEngine;

namespace Blobs.Presentation
{
    /// <summary>
    /// Neutral graybox merge: move the source directly onto the target and remove the consumed
    /// participant at contact. It deliberately avoids squash, growth, or absorption staging.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SimpleMergeAnimationOrchestrator : MergeAnimationOrchestratorBase
    {
        [SerializeField, Min(0.01f)] private float slideDuration = 0.15f;

        public override Sequence CreateMergeBeat(
            BlobView source,
            BlobView target,
            bool sourceSurvives,
            Vector2Int gridDirection,
            Action onContact,
            Action onTargetConsumed,
            BlobMergeImpactSettings settings,
            GridPosition? destination = null)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (target == null) throw new ArgumentNullException(nameof(target));

            BlobView survivor = sourceSurvives ? source : target;
            BlobView consumed = sourceSurvives ? target : source;
            Transform sourceVisual = source.VisualRoot != null ? source.VisualRoot : source.transform;
            Transform targetVisual = target.VisualRoot != null ? target.VisualRoot : target.transform;
            Vector3 sourceScale = source.VisualRoot != null ? source.BaseVisualScale : source.transform.localScale;
            Vector3 targetScale = target.VisualRoot != null ? target.BaseVisualScale : target.transform.localScale;
            var sourceSorting = source.SortingGroup;
            var targetSorting = target.SortingGroup;
            int sourceOrder = sourceSorting != null ? sourceSorting.sortingOrder : 0;
            int sourceLayer = sourceSorting != null ? sourceSorting.sortingLayerID : 0;
            bool started = false;
            bool completed = false;
            bool consumedWasActive = consumed.gameObject.activeSelf;

            void RestoreVisuals()
            {
                if (sourceVisual != null) sourceVisual.localScale = sourceScale;
                if (targetVisual != null) targetVisual.localScale = targetScale;
                if (sourceSorting != null)
                {
                    sourceSorting.sortingOrder = sourceOrder;
                    sourceSorting.sortingLayerID = sourceLayer;
                }
            }

            Sequence beat = DOTween.Sequence();
            beat.AppendCallback(() =>
            {
                started = true;
                source.BlobMotionAnimator?.SetMerging();
                target.BlobMotionAnimator?.SetMerging();
                RestoreVisuals();
                // Keep the moving source visible throughout the direct overlap.
                if (sourceSorting != null && targetSorting != null)
                {
                    sourceSorting.sortingLayerID = targetSorting.sortingLayerID;
                    sourceSorting.sortingOrder = targetSorting.sortingOrder + 1;
                }
            });
            beat.Append(source.AnimateMoveTo(destination ?? target.GridPosition,
                Mathf.Max(0.01f, slideDuration), Ease.Linear));
            beat.AppendCallback(() =>
            {
                onContact?.Invoke();
                consumed.gameObject.SetActive(false);
            });
            beat.OnComplete(() =>
            {
                completed = true;
                RestoreVisuals();
                survivor.BlobMotionAnimator?.SetIdle();
                onTargetConsumed?.Invoke();
            });
            beat.OnKill(() =>
            {
                if (!started || completed) return;
                RestoreVisuals();
                if (consumed != null) consumed.gameObject.SetActive(consumedWasActive);
                if (survivor != null) survivor.BlobMotionAnimator?.SetIdle();
            });
            return beat;
        }
    }
}
