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
        /// Initializes tile transform state from Core tile data.
        /// Collider shape remains an authored concern so future tile types can choose an appropriate shape.
        /// </summary>
        public void Initialize(TileState tile, float cellSize, Vector2 origin)
        {
            if (GetComponent<Collider2D>() == null)
            {
                throw new System.InvalidOperationException(
                    $"TileView '{name}' requires an authored Collider2D.");
            }

            TileId = tile.Id;
            GridPosition = tile.Position;
            TileType = tile.Type;
            name = "Tile " + tile.Id;
            transform.localPosition = GridToLocal(tile.Position, cellSize, origin);
            transform.localScale = Vector3.one * Mathf.Max(0.1f, cellSize * 0.8f);
            BindState(tile);
        }

        /// <summary>
        /// Returns whether this view still represents the authoritative logical tile state.
        /// </summary>
        public bool IsPresenting(TileState tile)
        {
            return tile != null &&
                TileId == tile.Id &&
                GridPosition == tile.Position &&
                TileType == tile.Type;
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

        private static Vector3 GridToLocal(GridPosition position, float cellSize, Vector2 origin)
        {
            return new Vector3(origin.x + position.X * cellSize, origin.y + position.Y * cellSize, 0f);
        }
    }
}
