using System;
using Blobs.Content;
using Blobs.Core;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

namespace Blobs.Presentation
{
    /// <summary>Graybox merge: slide behind the target, then grow the surviving mover over it.</summary>
    [DisallowMultipleComponent]
    public sealed class SimpleMergeAnimationOrchestrator : MergeAnimationOrchestratorBase
    {
        [SerializeField, Min(0.01f)] private float slideDuration = 0.15f;
        [FormerlySerializedAs("pulseDuration")]
        [SerializeField, Min(0.01f)] private float scaleDuration = 0.12f;
        [SerializeField, Min(1f)] private float pulseScale = 1.15f;
        [SerializeField, Min(0.01f)] private float overshootDuration = 0.06f;
        [SerializeField, Min(0.01f)] private float settleDuration = 0.06f;

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
            Transform survivorVisual = sourceSurvives ? sourceVisual : targetVisual;
            Vector3 survivorScale = sourceSurvives ? sourceScale : targetScale;
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
                // Keep the target legible until the moving piece reaches its center.
                if (sourceSorting != null && targetSorting != null)
                {
                    sourceSorting.sortingLayerID = targetSorting.sortingLayerID;
                    sourceSorting.sortingOrder = targetSorting.sortingOrder - 1;
                }
            });
            beat.Append(source.AnimateMoveTo(destination ?? target.GridPosition,
                Mathf.Max(0.01f, slideDuration), Ease.Linear));
            beat.AppendCallback(() =>
            {
                onContact?.Invoke();
                if (sourceSurvives)
                {
                    // Both silhouettes now overlap. Hide the mover before bringing it in front.
                    sourceVisual.localScale = Vector3.zero;
                    if (sourceSorting != null && targetSorting != null)
                        sourceSorting.sortingOrder = targetSorting.sortingOrder + 1;
                }
                else
                {
                    // Reverse merges (e.g. Ghost/Flag) retain the target according to Core.
                    consumed.gameObject.SetActive(false);
                }
            });
            if (sourceSurvives)
            {
                beat.Append(survivorVisual.DOScale(survivorScale, Mathf.Max(0.01f, scaleDuration))
                    .From(Vector3.zero, setImmediately: false)
                    .SetEase(Ease.InOutQuad));
            }
            // Remove the covered piece before the survivor's finishing pulse.
            beat.AppendCallback(() => consumed.gameObject.SetActive(false));
            Vector3 peakScale = new(survivorScale.x * pulseScale, survivorScale.y * pulseScale, survivorScale.z);
            beat.Append(survivorVisual.DOScale(peakScale, Mathf.Max(0.01f, overshootDuration))
                .SetEase(Ease.OutQuad));
            beat.Append(survivorVisual.DOScale(survivorScale, Mathf.Max(0.01f, settleDuration))
                .SetEase(Ease.InOutQuad));
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
