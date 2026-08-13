using System;
using Blobs.Core;
using UnityEngine;
using Blobs.Content;
namespace Blobs.Presentation
{
    [RequireComponent(typeof(BlobRenderer))]
    public sealed class BlobView : MonoBehaviour
    {
        public BlobRenderer BlobRenderer { get; private set; }

        public event Action<BlobView> Selected;

        public string BlobId { get; private set; }
        private float _cellSize;
        private Vector2 _origin;

        public void Initialize(BlobState blob,  LevelVisualThemeAsset theme, float cellSize, Vector2 origin)
        {
            BlobId = blob.Id;
            _cellSize = cellSize;
            _origin = origin;
            name = "Blob " + blob.Id;
            SetGridPosition(blob.Position);
            transform.localScale = Vector3.one * Mathf.Max(0.1f, cellSize * 0.8f);
            BlobRenderer = GetComponent<BlobRenderer>();
            ApplySkin(blob, theme);
            EnsureCollider();
        }

        public void SetGridPosition(GridPosition position)
        {
            transform.localPosition = GridToLocal(position, _cellSize, _origin);
        }

        public void ApplySkin(BlobState blob, LevelVisualThemeAsset theme)
        {
            var skinApplier = new BlobSkinApplier();
            var skinResolver = new BlobSkinResolver();
            var skin = skinResolver.ResolveSkin(blob, theme);
            skinApplier.Apply(this, skin.Value);
        }

        private void OnMouseDown()
        {
            Selected?.Invoke(this);
        }

       

        private void EnsureCollider()
        {
            if (GetComponent<Collider2D>() != null)
                return;

            var circle = gameObject.AddComponent<CircleCollider2D>();
            circle.radius = 0.5f;
        }

        private static Vector3 GridToLocal(GridPosition position, float cellSize, Vector2 origin)
        {
            return new Vector3(origin.x + position.X * cellSize, origin.y + position.Y * cellSize, 0f);
        }

    }
}
