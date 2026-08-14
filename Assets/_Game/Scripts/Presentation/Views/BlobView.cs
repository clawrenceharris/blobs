using System;
using Blobs.Core;
using UnityEngine;
using Blobs.Content;
using Blobs.Input;
using DG.Tweening;
namespace Blobs.Presentation
{
    [RequireComponent(typeof(BlobRenderer))]
    public sealed class BlobView : MonoBehaviour
    {
        public BlobRenderer BlobRenderer { get; private set; }


        public string BlobId { get; private set; }
        private float _cellSize;
        private Vector2 _origin;
        private Vector3 _baseScale;

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

        public void SetGridPosition(GridPosition position)
        {
            transform.DOKill();
            transform.localPosition = GridToLocal(position, _cellSize, _origin);
        }

        public void AnimateMoveTo(GridPosition position, float duration)
        {
            var target = GridToLocal(position, _cellSize, _origin);
            transform.DOKill();
            transform.DOMove(target, duration).SetEase(Ease.OutQuad);
        }

        public void PlaySpawn(float duration)
        {
            transform.DOKill();
            transform.localScale = Vector3.zero;
            transform.DOScale(_baseScale, duration).SetEase(Ease.OutBack);
        }

        public Tween PlayDespawn(float duration)
        {
            transform.DOKill();
            return transform.DOScale(Vector3.zero, duration).SetEase(Ease.InBack);
        }

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
