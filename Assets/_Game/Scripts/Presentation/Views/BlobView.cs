using System;
using Blobs.Core;
using UnityEngine;
using Blobs.Content;
using Blobs.Input;
using DG.Tweening;
namespace Blobs.Presentation
{
    /// <summary>
    /// Unity view for one blob. It converts Core grid positions into transform state and owns
    /// blob-specific animation primitives.
    /// </summary>
    [RequireComponent(typeof(BlobRenderer))]
    public sealed class BlobView : MonoBehaviour
    {
        public BlobRenderer BlobRenderer { get; private set; }

        private IMergeTargetFeedback _mergeTargetFeedback;

        public string BlobId { get; private set; }
        public GridPosition GridPosition { get; private set; }
        private float _cellSize;
        private Vector2 _origin;
        private Vector3 _baseScale;

        /// <summary>
        /// Initializes the view from immutable Core blob state.
        /// </summary>
        public void Initialize(BlobState blob, LevelVisualThemeAsset theme, float cellSize, Vector2 origin)
        {
            BlobId = blob.Id;
            _cellSize = cellSize;
            _origin = origin;
            name = "Blob " + blob.Id;
            SetGridPosition(blob.Position);
            _baseScale = Vector3.one * Mathf.Max(0.1f, cellSize * 0.8f);
            transform.localScale = _baseScale;
            BlobRenderer = GetComponent<BlobRenderer>();
            CacheOptionalBehaviors();
            ApplySkin(blob, theme);
        }

        /// <summary>
        /// Immediately places the view at a logical grid position.
        /// </summary>
        public void SetGridPosition(GridPosition position)
        {
            transform.DOKill();
            GridPosition = position;
            transform.localPosition = GridToLocal(position, _cellSize, _origin);
        }

        /// <summary>
        /// Animates this view to a logical grid position without changing Core state.
        /// Intermediate tiles of a multi-tile move should use <see cref="Ease.Linear"/>
        /// so consecutive per-tile tweens read as one continuous slide.
        /// </summary>
        public Tween AnimateMoveTo(GridPosition position, float duration, Ease ease = Ease.OutQuad)
        {
            var target = GridToLocal(position, _cellSize, _origin);
            transform.DOKill();
            GridPosition = position;
            return transform.DOLocalMove(target, duration).SetEase(ease);
        }

        /// <summary>
        /// Animates this view to be consumed into the target position.
        /// </summary>
        /// <param name="targetPosition">The target position to animate to</param>
        /// <param name="moveDuration">The duration of the move animation</param>
        /// <param name="despawnDuration">The duration of the despawn animation</param>
        /// <returns>The tween for the animation</returns>
        public Tween PlayConsumedInto(
            GridPosition targetPosition,
            float moveDuration,
            float despawnDuration)
        {
            transform.DOKill();

            GridPosition = targetPosition;

            Vector3 target = GridToLocal(
                targetPosition,
                _cellSize,
                _origin);

            return DOTween.Sequence()
                .Append(
                    transform
                        .DOLocalMove(target, moveDuration)
                        .SetEase(Ease.InQuad))
                .Append(
                    transform
                        .DOScale(Vector3.zero, despawnDuration)
                        .SetEase(Ease.InBack));
        }

        /// <summary>
        /// Plays the spawn scale-in animation.
        /// </summary>
        public Tween PlaySpawn(float duration)
        {
            transform.DOKill();
            transform.localScale = Vector3.zero;
            return transform.DOScale(_baseScale, duration).SetEase(Ease.OutBack);
        }

        /// <summary>
        /// Plays the despawn animation and returns the tween so the caller can destroy the view on completion.
        /// </summary>
        public Tween PlayDespawn(float duration)
        {
            transform.DOKill();
            return transform.DOScale(Vector3.zero, duration).SetEase(Ease.InBack);
        }

        /// <summary>
        /// Applies visual skinning for the supplied blob state and level theme.
        /// </summary>
        public void ApplySkin(BlobState blob, LevelVisualThemeAsset theme)
        {
            var skinApplier = new BlobSkinApplier();
            var skinResolver = new BlobSkinResolver();
            var skin = skinResolver.ResolveSkin(blob, theme);
            skinApplier.Apply(this, skin.Value);
        }

        private static Vector3 GridToLocal(GridPosition position, float cellSize, Vector2 origin)
        {
            return new Vector3(origin.x + position.X * cellSize, origin.y + position.Y * cellSize, 0f);
        }

        private void OnDestroy()
        {
            transform.DOKill();
        }

        private void CacheOptionalBehaviors()
        {
            _mergeTargetFeedback = null;

            MonoBehaviour[] components =
                GetComponents<MonoBehaviour>();

            foreach (MonoBehaviour component in components)
            {
                if (component is IMergeTargetFeedback feedback)
                {
                    _mergeTargetFeedback = feedback;
                    break;
                }
            }
        }
        public Tween PlaySourceAccepted(float duration)
        {
            return _mergeTargetFeedback?.PlaySourceAccepted(duration);
        }

    }
}
