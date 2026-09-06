using System.Collections.Generic;
using Blobs.Core;
using UnityEngine;

namespace Blobs.Presentation
{
    /// <summary>
    /// Owns creation, tracking, and removal of logical tile views.
    /// </summary>
    public sealed class TilePresenter : MonoBehaviour
    {
        private readonly Dictionary<string, TileView> _views = new();

        [SerializeField] private TileViewCatalogAsset _tileViewCatalog;
        [SerializeField] private Transform tileRoot;

        private ITileViewFactory _viewFactory;
        private float _cellSize;
        private Vector2 _origin;

        public int VisibleCount => _views.Count;

        /// <summary>
        /// Configures the shared board coordinate system and clears views from any prior session.
        /// </summary>
        public void Initialize(
            float cellSize,
            Vector2 origin,
            ITileViewFactory viewFactory = null)
        {
            Clear();
            _cellSize = cellSize;
            _origin = origin;
            _viewFactory = viewFactory;
        }

        /// <summary>
        /// Replaces all logical tile views with an authoritative board representation.
        /// </summary>
        public void Rebuild(BoardState board)
        {
            if (board == null)
                throw new System.ArgumentNullException(nameof(board));

            Clear();

            foreach (TileState tile in board.Tiles)
                Create(tile);
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
                    view.GridPosition != tile.Position ||
                    view.TileType != tile.Type)
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

            TileView view = ResolveViewFactory().Create(
                tile,
                tileRoot != null ? tileRoot : transform,
                _cellSize,
                _origin);
            if (view == null)
                return false;

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

        public void Clear()
        {
            foreach (TileView view in _views.Values)
            {
                if (view != null)
                    DestroyView(view.gameObject);
            }

            _views.Clear();
        }

        private ITileViewFactory ResolveViewFactory()
        {
            if (_viewFactory != null)
                return _viewFactory;
            if (_tileViewCatalog == null)
            {
                throw new System.InvalidOperationException(
                    "TilePresenter requires a TileViewCatalogAsset or an injected ITileViewFactory.");
            }

            _viewFactory = new TileViewFactory(_tileViewCatalog);
            return _viewFactory;
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
