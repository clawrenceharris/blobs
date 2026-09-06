using System;
using System.Collections.Generic;
using Blobs.Core;
using UnityEngine;
using Blobs.Content;
using Blobs.Input;
using DG.Tweening;
using Blobs.Application;
using UnityEngine.Rendering;
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
        private readonly List<IBlobContactFeedback> _contactFeedback = new();
        private readonly BlobSkinApplier _skinApplier = new();
        private readonly BlobSkinResolver _skinResolver = new();

        [SerializeField] private Transform _visualRoot;
        [SerializeField] private SortingGroup _sortingGroup;
        public Transform VisualRoot => _visualRoot;
        public SortingGroup SortingGroup => _sortingGroup;

        public string BlobId { get; private set; }
        public BlobType BlobType { get; private set; }
        public GridPosition GridPosition { get; private set; }
        private BlobColor? _presentedColor;
        private BlobColor? _presentedTrailColor;
        private float _cellSize;
        private Vector2 _origin;
        private Vector3 _baseScale;
        public Vector3 BaseVisualScale { get; private set; } = Vector3.one;

        private BlobMotionAnimator _blobMotionAnimator;

        public BlobMotionAnimator BlobMotionAnimator => _blobMotionAnimator;
        public Color MergeEffectColor { get; private set; } = Color.white;

        /// <summary>
        /// Initializes the view from immutable Core blob state.
        /// </summary>
        public void Initialize(
            BlobState blob,
            IGameplayState state,
            LevelColorPaletteAsset colorPalette,
            float cellSize,
            Vector2 origin)
        {
            BlobId = blob.Id;
            BlobType = blob.Type;
            _presentedColor = blob.Components.Color?.Color;
            _presentedTrailColor = blob.Components.Trail?.TrailColor;
            _cellSize = cellSize;
            _origin = origin;
            name = "Blob " + blob.Id;
            SetGridPosition(blob.Position);
            _baseScale = Vector3.one * Mathf.Max(0.1f, cellSize * 0.8f);
            transform.localScale = _baseScale;
            BaseVisualScale = _visualRoot != null
                ? _visualRoot.localScale
                : Vector3.one;
            if (_sortingGroup == null)
                _sortingGroup = GetComponent<SortingGroup>();
            _blobMotionAnimator = TryGetComponent(out BlobMotionAnimator animator) ? animator : null;
            _blobMotionAnimator?.Configure(state);
            BlobRenderer = GetComponent<BlobRenderer>();

            CacheOptionalBehaviors();
            ApplySkin(blob, colorPalette);
        }

        /// <summary>
        /// Returns whether this view represents every piece of immutable blob state that can
        /// affect prefab selection or rendering. Animation state is intentionally excluded.
        /// </summary>
        public bool IsPresenting(BlobState blob)
        {
            return blob != null &&
                BlobId == blob.Id &&
                BlobType == blob.Type &&
                GridPosition == blob.Position &&
                _presentedColor == blob.Components.Color?.Color &&
                _presentedTrailColor == blob.Components.Trail?.TrailColor;
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
            transform.localScale = Vector3.zero;
            return transform.DOScale(_baseScale, duration).SetEase(Ease.OutBack);
        }

        /// <summary>
        /// Plays the despawn animation and returns the tween so the caller can destroy the view on completion.
        /// </summary>
        public Tween PlayDespawn(float duration)
        {
            return transform.DOScale(Vector3.zero, duration).SetEase(Ease.InBack);
        }

        /// <summary>
        /// Applies visual skinning for the supplied blob state and color palette.
        /// </summary>
        public void ApplySkin(BlobState blob, LevelColorPaletteAsset colorPalette)
        {
            if (blob.Components.Color.HasValue)
            {
                Skin skin = _skinResolver.ResolveSkin(blob.Components.Color.Value.Color, colorPalette);
                _skinApplier.Apply(this, skin);
                MergeEffectColor = colorPalette.GetRequired(blob.Components.Color.Value.Color).BaseColor;
            }

            BlobColorBinding[] bindings =
                GetComponentsInChildren<BlobColorBinding>(true);

            foreach (BlobColorBinding binding in bindings)
            {
                binding.Apply(
                    blob,
                    colorPalette
                );
            }
        }

        private static Vector3 GridToLocal(GridPosition position, float cellSize, Vector2 origin)
        {
            return new Vector3(origin.x + position.X * cellSize, origin.y + position.Y * cellSize, 0f);
        }

        private void OnDestroy()
        {
            transform.DOKill();
            if (_visualRoot != null)
                _visualRoot.DOKill();
        }

        private void CacheOptionalBehaviors()
        {
            _mergeTargetFeedback = null;
            _contactFeedback.Clear();

            MonoBehaviour[] components =
                GetComponents<MonoBehaviour>();

            foreach (MonoBehaviour component in components)
            {
                if (component is IMergeTargetFeedback feedback)
                    _mergeTargetFeedback = feedback;

                if (component is IBlobContactFeedback contactFeedback)
                    _contactFeedback.Add(contactFeedback);
            }
        }

        /// <summary>
        /// Invokes every contact-feedback behavior composed on this target blob prefab.
        /// </summary>
        public void PlayContactFeedback(BlobView source)
        {
            var context = new BlobContactFeedbackContext(source, this);
            foreach (IBlobContactFeedback feedback in _contactFeedback)
                feedback.PlayContactFeedback(context);
        }

        public Tween PlaySourceAccepted(float duration)
        {
            return _mergeTargetFeedback?.PlaySourceAccepted(duration);
        }

    }
}
