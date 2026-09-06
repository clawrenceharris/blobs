using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;

namespace Blobs.Presentation
{
    /// <summary>
    /// Owns the presentation-only timeline for a normal merge. All fields are intentionally
    /// inspector-tunable so timing and deformation can be art-directed before final VFX art exists.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MergeAnimationOrchestrator : MonoBehaviour
    {
        [Header("Timing")]
        [SerializeField, Min(0.01f)] private float _anticipationDuration = 0.09f;
        [SerializeField, Min(0.01f)] private float _travelDuration = 0.18f;
        [SerializeField, Min(0.01f)] private float _consumeDuration = 0.11f;
        [SerializeField, Min(0.01f)] private float _settleDuration = 0.18f;

        [Header("Deformation")]
        [SerializeField, Min(0f)] private float _anticipationBackstep = 0.08f;
        [SerializeField] private Vector2 _anticipationScale = new(1.10f, 0.88f);
        [SerializeField] private Vector2 _travelScale = new(0.92f, 1.12f);
        [SerializeField] private Vector2 _targetBraceScale = new(1.06f, 0.94f);
        [SerializeField] private Vector2 _survivorImpactScale = new(1.14f, 0.86f);

        [Header("Merge Sorting")]
        [SerializeField, Min(1)] private int _sourceSortingOffset = 10;
        [SerializeField, Min(1)] private int _targetSortingOffset = 20;

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
            Action onTargetConsumed)
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

            Vector2 travelMultipliers = DirectionalScale(_travelScale, direction);
            Vector2 anticipationMultipliers = DirectionalScale(_anticipationScale, direction);

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
                    sourceSorting.sortingOrder = mergeBaseOrder + _sourceSortingOffset;
                if (targetSorting != null)
                    targetSorting.sortingOrder = mergeBaseOrder + _targetSortingOffset;
            });

            beat.Append(sourceVisual
                .DOLocalMove(sourceBasePosition - direction * _anticipationBackstep,
                    _anticipationDuration)
                .SetEase(Ease.OutQuad));
            beat.Join(sourceVisual
                .DOScale(Multiply(sourceBaseScale, anticipationMultipliers),
                    _anticipationDuration)
                .SetEase(Ease.OutQuad));
            beat.Join(targetVisual
                .DOScale(Multiply(targetBaseScale, _targetBraceScale),
                    _anticipationDuration)
                .SetEase(Ease.OutQuad));

            beat.Append(source.AnimateMoveTo(
                target.GridPosition,
                _travelDuration,
                Ease.InQuad));
            beat.Join(sourceVisual
                .DOScale(Multiply(sourceBaseScale, travelMultipliers),
                    _travelDuration)
                .SetEase(Ease.InOutQuad));
            beat.Join(sourceVisual
                .DOLocalMove(sourceBasePosition, _travelDuration)
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
                .DOScale(Vector3.zero, _consumeDuration)
                .SetEase(Ease.InBack));
            beat.Join(sourceVisual
                .DOScale(Multiply(sourceBaseScale, _survivorImpactScale),
                    _consumeDuration)
                .SetEase(Ease.OutQuad));

            beat.Append(sourceVisual
                .DOScale(sourceBaseScale, _settleDuration)
                .SetEase(Ease.OutBack));
            beat.Join(sourceVisual
                .DOLocalMove(sourceBasePosition, _settleDuration)
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
            _anticipationDuration = Mathf.Max(0.01f, _anticipationDuration);
            _travelDuration = Mathf.Max(0.01f, _travelDuration);
            _consumeDuration = Mathf.Max(0.01f, _consumeDuration);
            _settleDuration = Mathf.Max(0.01f, _settleDuration);
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
