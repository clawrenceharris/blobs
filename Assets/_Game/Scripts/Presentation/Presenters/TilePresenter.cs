using System.Collections.Generic;
using System.Linq;
using Blobs.Core;
using UnityEngine;

namespace Blobs.Presentation
{
    /// <summary>
    /// Owns creation, tracking, and removal of tile and board-surface views.
    /// </summary>
    public sealed class TilePresenter : MonoBehaviour
    {
        private readonly Dictionary<string, TileView> _views = new();

        [SerializeField] private TileView tileViewPrefab;
        [SerializeField] private Transform tileRoot;
        [SerializeField] private GameObject cellPrefab;
        [SerializeField] private Transform cellRoot;

        private BoardSurfaceView _boardSurfaceView;
        private float _cellSize;
        private Vector2 _origin;

        public int VisibleCount => _views.Count;
        public int VisibleSurfaceCellCount =>
            _boardSurfaceView != null ? _boardSurfaceView.VisibleCellCount : 0;

        /// <summary>
        /// Configures the shared board coordinate system and clears views from any prior session.
        /// </summary>
        public void Initialize(float cellSize, Vector2 origin)
        {
            Clear();
            _cellSize = cellSize;
            _origin = origin;
        }

        /// <summary>
        /// Replaces all tile and surface views with an authoritative board representation.
        /// </summary>
        public void Rebuild(BoardState board)
        {
            Clear();

            foreach (TileState tile in board.Tiles)
                Create(tile);

            EnsureBoardSurfaceView().Rebuild(
                BuildSurfacePositions(board),
                _cellSize,
                _origin);
        }

        public bool TryGetView(string tileId, out TileView view)
        {
            return _views.TryGetValue(tileId, out view) && view != null;
        }

        /// <summary>
        /// Checks tracked tile identities and logical positions against authoritative states.
        /// </summary>
        public bool IsSynchronizedWith(IEnumerable<TileState> tiles, int expectedCount)
        {
            if (_views.Count != expectedCount)
                return false;

            foreach (TileState tile in tiles)
            {
                if (!TryGetView(tile.Id, out TileView view) ||
                    view.TileId != tile.Id ||
                    view.GridPosition != tile.Position)
                {
                    return false;
                }
            }

            return true;
        }

        public bool Create(TileState tile)
        {
            if (tile == null || _views.ContainsKey(tile.Id))
                return false;

            TileView view = InstantiateView();
            view.Initialize(tile, _cellSize, _origin);
            _views.Add(tile.Id, view);
            return true;
        }

        public bool Remove(string tileId)
        {
            if (!TryGetView(tileId, out TileView view))
                return false;

            _views.Remove(tileId);
            DestroyView(view.gameObject);
            return true;
        }

        /// <summary>
        /// Builds the optional legacy cell-prefab layer for non-empty board positions.
        /// </summary>
        public void BuildCells(BoardState board)
        {
            if (cellPrefab == null || cellRoot == null)
                return;

            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    GridPosition position = new(x, y);
                    if (board.EmptyPositions.Any(p => p.Equals(position)))
                        continue;

                    GameObject cell = Instantiate(
                        cellPrefab,
                        ToWorldPosition(position),
                        Quaternion.identity,
                        cellRoot);
                    cell.name = $"Cell_{position.X}_{position.Y}";
                }
            }
        }

        public void Clear()
        {
            ClearCells();
            _boardSurfaceView?.ClearCells();

            foreach (TileView view in _views.Values)
            {
                if (view != null)
                    DestroyView(view.gameObject);
            }

            _views.Clear();
        }

        private TileView InstantiateView()
        {
            Transform parent = tileRoot != null ? tileRoot : transform;
            if (tileViewPrefab != null)
                return Instantiate(tileViewPrefab, parent);

            var instance = new GameObject("Production Tile");
            instance.transform.SetParent(parent, false);
            return instance.AddComponent<TileView>();
        }

        private BoardSurfaceView EnsureBoardSurfaceView()
        {
            if (_boardSurfaceView != null)
                return _boardSurfaceView;

            Transform parent = tileRoot != null ? tileRoot : transform;
            _boardSurfaceView = parent.GetComponentInChildren<BoardSurfaceView>(true);
            if (_boardSurfaceView != null)
                return _boardSurfaceView;

            var surfaceObject = new GameObject("Board Surface");
            surfaceObject.transform.SetParent(parent, false);
            _boardSurfaceView = surfaceObject.AddComponent<BoardSurfaceView>();
            return _boardSurfaceView;
        }

        private static ISet<GridPosition> BuildSurfacePositions(
            BoardState board)
        {
            var occupied = new HashSet<GridPosition>();
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    var position = new GridPosition(x, y);
                    if (!board.EmptyPositions.Any(p => p.Equals(position)))
                        occupied.Add(position);
                }
            }

            return occupied;
        }

        private void ClearCells()
        {
            if (cellRoot == null)
                return;

            for (int i = cellRoot.childCount - 1; i >= 0; i--)
                DestroyView(cellRoot.GetChild(i).gameObject);
        }

        private Vector3 ToWorldPosition(GridPosition position)
        {
            return new Vector3(position.X, position.Y, 0f) * _cellSize;
        }

        private static void DestroyView(GameObject view)
        {
            if (UnityEngine.Application.isPlaying)
                Destroy(view);
            else
                DestroyImmediate(view);
        }
    }
}
