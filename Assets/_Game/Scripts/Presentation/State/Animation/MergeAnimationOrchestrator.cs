using System;
using System.Collections.Generic;
using Blobs.Content;
using Blobs.Core;
using DG.Tweening;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Blobs.Presentation
{
    /// <summary>
    /// Builds normal-merge deformation and broadcasts impact feedback. Each beat captures
    /// its supplied settings so queued interactions do not share mutable presenter settings.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MergeAnimationOrchestrator : MonoBehaviour
    {
        private readonly List<IMergeImpactFeedback> _impactFeedbackChannels = new();



        /// <summary>
        /// Creates a self-contained merge beat. State changes are callbacks inside the
        /// sequence, so queued chain merges do not enter Merging before their visual turn.
        /// </summary>
        public Sequence CreateMergeBeat(
            BlobView source,
            BlobView target,
            Vector2Int gridDirection,
            Action onContact,
            Action onTargetConsumed,
            BlobMergeImpactSettings settings,
            GridPosition? destination = null
            )
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            Transform sourceVisual = source.VisualRoot != null
                ? source.VisualRoot
                : source.transform;
            Transform targetVisual = target.VisualRoot != null
                ? target.VisualRoot
                : target.transform;
            Vector3 sourceBaseScale = source.BaseVisualScale;
            Vector3 targetBaseScale = target.BaseVisualScale;
            Vector3 sourceBasePosition = sourceVisual.localPosition;
            Vector3 targetBasePosition = targetVisual.localPosition;
            SortingGroup sourceSorting = source.SortingGroup;
            SortingGroup targetSorting = target.SortingGroup;
            int sourceOriginalLayer = sourceSorting != null
                ? sourceSorting.sortingLayerID
                : 0;
            int targetOriginalLayer = targetSorting != null
                ? targetSorting.sortingLayerID
                : 0;
            int sourceOriginalOrder = sourceSorting != null
                ? sourceSorting.sortingOrder
                : 0;
            int targetOriginalOrder = targetSorting != null
                ? targetSorting.sortingOrder
                : 0;
            int mergeBaseOrder = Mathf.Max(sourceOriginalOrder, targetOriginalOrder);
            Vector3 direction = new(gridDirection.x, gridDirection.y, 0f);
            if (direction.sqrMagnitude < 0.001f)
                direction = Vector3.right;
            direction.Normalize();

            Vector2 travelMultipliers = DirectionalScale(settings.TravelScale, direction);
            Vector2 anticipationMultipliers = DirectionalScale(settings.AnticipationScale, direction);

            Sequence beat = DOTween.Sequence();
            beat.AppendCallback(() =>
            {
                source.BlobMotionAnimator?.SetMerging();
                target.BlobMotionAnimator?.SetMerging();
                sourceVisual.localPosition = sourceBasePosition;
                targetVisual.localPosition = targetBasePosition;
                sourceVisual.localScale = sourceBaseScale;
                targetVisual.localScale = targetBaseScale;

                if (sourceSorting != null)
                    sourceSorting.sortingOrder = mergeBaseOrder + settings.SourceSortingOffset;
                if (targetSorting != null)
                    targetSorting.sortingOrder = mergeBaseOrder + settings.TargetSortingOffset;
            });

            beat.Append(sourceVisual
                .DOLocalMove(sourceBasePosition - direction * settings.AnticipationBackstep,
                    settings.AnticipationDuration)
                .SetEase(Ease.OutQuad));
            beat.Join(sourceVisual
                .DOScale(Multiply(sourceBaseScale, anticipationMultipliers),
                    settings.AnticipationDuration)
                .SetEase(Ease.OutQuad));
            beat.Join(targetVisual
                .DOScale(Multiply(targetBaseScale, settings.TargetBraceScale),
                    settings.AnticipationDuration)
                .SetEase(Ease.OutQuad));

            beat.Append(source.AnimateMoveTo(
                destination ?? target.GridPosition,
                settings.TravelDuration,
                Ease.InQuad));
            beat.Join(sourceVisual
                .DOScale(Multiply(sourceBaseScale, travelMultipliers),
                    settings.TravelDuration)
                .SetEase(Ease.InOutQuad));
            beat.Join(sourceVisual
                .DOLocalMove(sourceBasePosition, settings.TravelDuration)
                .SetEase(Ease.OutQuad));

            beat.AppendCallback(() =>
            {
                PlayImpact(
                    target.transform.position,
                    source.MergeEffectColor,
                    target);
                onContact?.Invoke();
            });
            beat.Append(targetVisual
                .DOScale(Vector3.zero, settings.ConsumeDuration)
                .SetEase(Ease.InBack));
            beat.Join(sourceVisual
                .DOScale(Multiply(sourceBaseScale, settings.SurvivorImpactScale),
                    settings.ConsumeDuration)
                .SetEase(Ease.OutQuad));

            beat.Append(sourceVisual
                .DOScale(sourceBaseScale, settings.SettleDuration)
                .SetEase(Ease.OutBack));
            beat.Join(sourceVisual
                .DOLocalMove(sourceBasePosition, settings.SettleDuration)
                .SetEase(Ease.OutQuad));
            beat.OnComplete(() =>
            {
                RestoreSorting(
                    sourceSorting,
                    sourceOriginalLayer,
                    sourceOriginalOrder,
                    targetSorting,
                    targetOriginalLayer,
                    targetOriginalOrder);
                sourceVisual.localPosition = sourceBasePosition;
                sourceVisual.localScale = sourceBaseScale;
                source.BlobMotionAnimator?.SetIdle();
                onTargetConsumed?.Invoke();
            });
            beat.OnKill(() => RestoreSorting(
                sourceSorting,
                sourceOriginalLayer,
                sourceOriginalOrder,
                targetSorting,
                targetOriginalLayer,
                targetOriginalOrder));
            return beat;
        }



        /// <summary>
        /// Broadcasts the shared impact context to every enabled feedback channel on this object.
        /// </summary>
        public void PlayImpact(Vector3 worldPosition, Color blobColor, BlobView sortingAnchor)
        {
            if (_impactFeedbackChannels.Count == 0)
                RefreshImpactFeedbackChannels();

            var context = new MergeImpactFeedbackContext(
                worldPosition,
                blobColor,
                sortingAnchor);
            foreach (IMergeImpactFeedback channel in _impactFeedbackChannels)
            {
                if (channel is Behaviour behaviour && !behaviour.isActiveAndEnabled)
                    continue;

                channel.PlayImpact(context);
            }
        }

        /// <summary>
        /// Refreshes prefab-composed feedback channels after runtime composition changes.
        /// </summary>
        public void RefreshImpactFeedbackChannels()
        {
            _impactFeedbackChannels.Clear();
            foreach (MonoBehaviour component in GetComponents<MonoBehaviour>())
            {
                if (component is IMergeImpactFeedback feedback)
                    _impactFeedbackChannels.Add(feedback);
            }
        }

        private void Awake()
        {
            RefreshImpactFeedbackChannels();
        }

        private void OnEnable()
        {
            RefreshImpactFeedbackChannels();
        }

        private void OnValidate()
        {
            RefreshImpactFeedbackChannels();
        }

        private static Vector2 DirectionalScale(Vector2 scale, Vector3 direction)
        {
            return Mathf.Abs(direction.x) >= Mathf.Abs(direction.y)
                ? new Vector2(scale.y, scale.x)
                : scale;
        }

        private static Vector3 Multiply(Vector3 baseScale, Vector2 multiplier)
        {
            return new Vector3(
                baseScale.x * multiplier.x,
                baseScale.y * multiplier.y,
                baseScale.z);
        }

        private static void RestoreSorting(
            SortingGroup source,
            int sourceLayer,
            int sourceOrder,
            SortingGroup target,
            int targetLayer,
            int targetOrder)
        {
            if (source != null)
            {
                source.sortingLayerID = sourceLayer;
                source.sortingOrder = sourceOrder;
            }

            if (target != null)
            {
                target.sortingLayerID = targetLayer;
                target.sortingOrder = targetOrder;
            }
        }

    }
}
