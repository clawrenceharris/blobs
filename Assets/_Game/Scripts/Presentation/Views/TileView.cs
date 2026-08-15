using System;
using Blobs.Core;
using UnityEngine;
using Blobs.Content;
namespace Blobs.Presentation
{
    /// <summary>
    /// Unity view for one logical tile.
    /// </summary>
    public sealed class TileView : MonoBehaviour
    {

        public string TileId { get; private set; }
        public GridPosition GridPosition { get; private set; }

        /// <summary>
        /// Initializes tile transform/collider state from Core tile data.
        /// </summary>
        public void Initialize(TileState tile, LevelVisualThemeAsset theme, float cellSize, Vector2 origin)
        {
            TileId = tile.Id;
            GridPosition = tile.Position;
            name = "Tile " + tile.Id;
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
