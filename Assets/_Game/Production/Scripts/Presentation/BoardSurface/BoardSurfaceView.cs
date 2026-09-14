using System;
using System.Collections.Generic;
using Blobs.Content;
using Blobs.Core;
using UnityEngine;

namespace Blobs.Presentation
{
    /// <summary>
    /// Builds the visual board as one occupancy-baked slab.
    /// </summary>
    public sealed class BoardSurfaceView : MonoBehaviour
    {
        [SerializeField] private BoardSurfacePaletteAsset palette;
        [SerializeField] private string sortingLayerName = "Default";
        [SerializeField] private int sortingOrder = -100;
        [SerializeField, Min(32)] private int pixelsPerCell = 160;

        private readonly Dictionary<GridPosition, BoardSurfaceNeighborMask> _masks =
            new Dictionary<GridPosition, BoardSurfaceNeighborMask>();
        private GameObject _surfaceObject;
        private BoardSurfaceBaker.Bake _bake;

        public int VisibleCellCount => _masks.Count;

        public void SetPalette(BoardSurfacePaletteAsset value)
        {
            palette = value;
        }

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

            foreach (GridPosition position in occupied)
            {
                _masks.Add(position, BoardSurfaceNeighborMaskResolver.Resolve(occupied, position));
            }

            BoardSurfaceBaker.Style style = BoardSurfaceBaker.WithPalette(
                BoardSurfaceBaker.DefaultStyle(pixelsPerCell),
                palette);
            _bake = BoardSurfaceBaker.BakeOccupied(occupied, style);
            _surfaceObject = new GameObject("Board Surface Bake");
            _surfaceObject.transform.SetParent(transform, false);
            _surfaceObject.transform.localPosition = new Vector3(
                origin.x + (_bake.MinX + _bake.MaxX) * 0.5f * cellSize,
                origin.y + (_bake.MinY + _bake.MaxY) * 0.5f * cellSize,
                0f);
            _surfaceObject.transform.localScale = Vector3.one * cellSize;

            SpriteRenderer renderer = _surfaceObject.AddComponent<SpriteRenderer>();
            renderer.sprite = _bake.Sprite;
            renderer.sortingLayerName = sortingLayerName;
            renderer.sortingOrder = sortingOrder;
        }

        public bool TryGetMask(
            GridPosition position,
            out BoardSurfaceNeighborMask mask)
        {
            return _masks.TryGetValue(position, out mask);
        }

        public void ClearCells()
        {
            if (_surfaceObject != null)
            {
                if (UnityEngine.Application.isPlaying)
                    Destroy(_surfaceObject);
                else
                    DestroyImmediate(_surfaceObject);
                _surfaceObject = null;
            }

            _bake?.Dispose();
            _bake = null;
            _masks.Clear();
        }

        private void OnDestroy()
        {
            ClearCells();
        }
    }
}
