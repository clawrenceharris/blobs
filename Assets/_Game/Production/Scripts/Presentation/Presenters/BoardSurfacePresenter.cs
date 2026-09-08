using System;
using System.Collections.Generic;
using Blobs.Core;
using UnityEngine;

namespace Blobs.Presentation
{
    /// <summary>
    /// Owns the visual board surface independently from logical tile views.
    /// </summary>
    public sealed class BoardSurfacePresenter : MonoBehaviour
    {
        [SerializeField] private BoardSurfaceView _view;

        private float _cellSize;
        private Vector2 _origin;

        public int VisibleCellCount => _view != null ? _view.VisibleCellCount : 0;

        public void Initialize(float cellSize, Vector2 origin)
        {
            _view = ResolveRequiredView();
            _view.ClearCells();
            _cellSize = cellSize;
            _origin = origin;
        }

        /// <summary>
        /// Rebuilds the visual surface from board dimensions and authored cutouts. Logical tile
        /// occupancy is intentionally unrelated because many valid board cells have no tile.
        /// </summary>
        public void Rebuild(BoardState board)
        {
            if (board == null)
                throw new ArgumentNullException(nameof(board));

            ResolveRequiredView().Rebuild(BuildSurfacePositions(board), _cellSize, _origin);
        }

        public void Clear()
        {
            if (_view == null)
                _view = GetComponentInChildren<BoardSurfaceView>(true);

            _view?.ClearCells();
        }

        private BoardSurfaceView ResolveRequiredView()
        {
            if (_view != null)
                return _view;

            _view = GetComponentInChildren<BoardSurfaceView>(true);
            if (_view != null)
                return _view;

            throw new InvalidOperationException(
                "BoardSurfacePresenter requires an authored BoardSurfaceView in its hierarchy.");
        }

        private static ISet<GridPosition> BuildSurfacePositions(BoardState board)
        {
            var occupied = new HashSet<GridPosition>();
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    var position = new GridPosition(x, y);
                    if (!board.EmptyPositions.Contains(position))
                        occupied.Add(position);
                }
            }

            return occupied;
        }
    }
}
