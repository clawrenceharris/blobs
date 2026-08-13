using System;
using Blobs.Core;
using UnityEngine;
using Blobs.Content;
namespace Blobs.Presentation
{
    public sealed class TileView : MonoBehaviour
    {

        public event Action<BlobView> Selected;

        public string TileId { get; private set; }

        public void Initialize(TileState tile,  LevelVisualThemeAsset theme, float cellSize, Vector2 origin)
        {
            TileId = tile.Id;
            name = "Blob " + tile.Id;
            transform.localPosition = GridToLocal(tile.Position, cellSize, origin);
            transform.localScale = Vector3.one * Mathf.Max(0.1f, cellSize * 0.8f);
            EnsureCollider();
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
