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
            ApplySkin(blob, theme);
            EnsureInputTarget(blob.Id);
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
        /// </summary>
        public Tween AnimateMoveTo(GridPosition position, float duration)
        {
            var target = GridToLocal(position, _cellSize, _origin);
            transform.DOKill();
            GridPosition = position;
            return transform.DOLocalMove(target, duration).SetEase(Ease.OutQuad);
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



        private void EnsureInputTarget(string blobId)
        {
            var target = GetComponent<BlobInputTarget>();
            if (target == null)
                target = gameObject.AddComponent<BlobInputTarget>();

            target.Initialize(blobId);
        }

        private static Vector3 GridToLocal(GridPosition position, float cellSize, Vector2 origin)
        {
            return new Vector3(origin.x + position.X * cellSize, origin.y + position.Y * cellSize, 0f);
        }

        private void OnDestroy()
        {
            transform.DOKill();
        }

    }
}
