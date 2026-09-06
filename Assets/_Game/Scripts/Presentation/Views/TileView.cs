using System.Collections.Generic;
using Blobs.Core;
using UnityEngine;

namespace Blobs.Presentation
{
    /// <summary>
    /// Unity view for one logical tile.
    /// </summary>
    public sealed class TileView : MonoBehaviour
    {
        private readonly List<ITileStateBinding> _stateBindings = new();

        public string TileId { get; private set; }
        public GridPosition GridPosition { get; private set; }
        public TileType TileType { get; private set; }

        /// <summary>
        /// Initializes tile transform/collider state from Core tile data.
        /// </summary>
        public void Initialize(TileState tile, float cellSize, Vector2 origin)
        {
            TileId = tile.Id;
            GridPosition = tile.Position;
            TileType = tile.Type;
            name = "Tile " + tile.Id;
            transform.localPosition = GridToLocal(tile.Position, cellSize, origin);
            transform.localScale = Vector3.one * Mathf.Max(0.1f, cellSize * 0.8f);
            EnsureCollider();
            BindState(tile);
        }

        private void BindState(TileState tile)
        {
            _stateBindings.Clear();
            foreach (MonoBehaviour component in GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component is ITileStateBinding binding)
                    _stateBindings.Add(binding);
            }

            var context = new TilePresentationContext(this, tile);
            foreach (ITileStateBinding binding in _stateBindings)
                binding.Bind(context);
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
