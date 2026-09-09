using System;
using System.Collections.Generic;
using Blobs.Core;
using UnityEngine;

namespace Blobs.Presentation
{
    /// <summary>
    /// Builds the continuous visual board surface from independent logical tile cells.
    /// </summary>
    public sealed class BoardSurfaceView : MonoBehaviour
    {
        private const string DefaultSpriteSetResource = "Board/BoardSurfaceSpriteSet";

        [SerializeField] private BoardSurfaceSpriteSet spriteSet;
        [SerializeField] private string sortingLayerName = "Default";
        [SerializeField] private int sortingOrder = -100;

        private readonly List<GameObject> _cells = new List<GameObject>();
        private readonly Dictionary<GridPosition, BoardSurfaceNeighborMask> _masks =
            new Dictionary<GridPosition, BoardSurfaceNeighborMask>();
        private BoardSurfaceSpriteComposer _composer;
        private BoardSurfaceSpriteSet _activeSpriteSet;

        public int VisibleCellCount => _cells.Count;

        public void Rebuild(
            IReadOnlyList<TileState> tiles,
            float cellSize,
            Vector2 origin)
        {
            if (tiles == null)
                throw new ArgumentNullException(nameof(tiles));

            var occupied = new HashSet<GridPosition>();
            foreach (TileState tile in tiles)
                occupied.Add(tile.Position);

            Rebuild(occupied, cellSize, origin);
        }

        /// <summary>
        /// Rebuilds the surface from the exact set of occupied logical cells. This overload is the
        /// shape boundary: callers can supply rectangles, corridors, cutouts, or irregular boards.
        /// </summary>
        public void Rebuild(
            ISet<GridPosition> occupied,
            float cellSize,
            Vector2 origin)
        {
            if (occupied == null)
                throw new ArgumentNullException(nameof(occupied));

            ClearCells();
            if (occupied.Count == 0)
                return;

            BoardSurfaceSpriteSet resolved = ResolveSpriteSet();
            if (resolved == null || !resolved.IsConfigured)
            {
                Debug.LogWarning(
                    "Board surface sprite set is missing or incomplete. " +
                    "Regenerate Assets/_Game/Production/Art/Board/Generated.",
                    this);
                return;
            }

            EnsureComposer(resolved);
            foreach (GridPosition position in occupied)
            {
                BoardSurfaceNeighborMask mask =
                    BoardSurfaceNeighborMaskResolver.Resolve(occupied, position);
                CreateCell(position, mask, cellSize, origin);
                _masks.Add(position, mask);
            }
        }

        public bool TryGetMask(
            GridPosition position,
            out BoardSurfaceNeighborMask mask)
        {
            return _masks.TryGetValue(position, out mask);
        }

        public void ClearCells()
        {
            for (int i = _cells.Count - 1; i >= 0; i--)
            {
                if (_cells[i] == null)
                    continue;
                if (UnityEngine.Application.isPlaying)
                    Destroy(_cells[i]);
                else
                    DestroyImmediate(_cells[i]);
            }

            _cells.Clear();
            _masks.Clear();
        }

        private void CreateCell(
            GridPosition position,
            BoardSurfaceNeighborMask mask,
            float cellSize,
            Vector2 origin)
        {
            var cell = new GameObject($"Board Surface ({position.X}, {position.Y})");
            cell.transform.SetParent(transform, false);
            cell.transform.localPosition = new Vector3(
                origin.x + position.X * cellSize,
                origin.y + position.Y * cellSize,
                0f);
            cell.transform.localScale = Vector3.one * cellSize;

            SpriteRenderer renderer = cell.AddComponent<SpriteRenderer>();
            bool useAlternateFill = ((position.X + position.Y) & 1) != 0;
            renderer.sprite = _composer.GetOrCreate(mask, useAlternateFill);
            renderer.sortingLayerName = sortingLayerName;
            renderer.sortingOrder = sortingOrder;
            _cells.Add(cell);
        }

        private BoardSurfaceSpriteSet ResolveSpriteSet()
        {
            if (spriteSet != null)
                return spriteSet;
            return Resources.Load<BoardSurfaceSpriteSet>(DefaultSpriteSetResource);
        }

        private void EnsureComposer(BoardSurfaceSpriteSet resolved)
        {
            if (_composer != null && _activeSpriteSet == resolved)
                return;

            _composer?.Dispose();
            _activeSpriteSet = resolved;
            _composer = new BoardSurfaceSpriteComposer(resolved);
        }

        private void OnDestroy()
        {
            ClearCells();
            _composer?.Dispose();
            _composer = null;
            _activeSpriteSet = null;
        }
    }
}
