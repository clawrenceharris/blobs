using System;
using System.Collections.Generic;
using Blobs.Application;
using Blobs.Core;
using Blobs.Content;
using UnityEngine;

namespace Blobs.Presentation
{
    public sealed class BoardPresenter : MonoBehaviour
    {
        private readonly Dictionary<string, BlobView> _blobViews = new Dictionary<string, BlobView>();
        private readonly Dictionary<string, TileView> _tileViews = new Dictionary<string, TileView>();
        [SerializeField] private BlobView _blobViewPrefab;
        [SerializeField] private TileView _tileViewPrefab;
        [SerializeField] private Transform _blobRoot;
        [SerializeField] private Transform _tileRoot;
        [SerializeField] private float _cellSize = 1.25f;
        [SerializeField] private Vector2 _origin;
        public event Action<string> BlobSelected;
        public float CellSize => _cellSize;
        private LevelVisualThemeAsset _theme;
        public void Initialize(LevelVisualThemeAsset theme)
        {
            _theme = theme;
        }
        public void Rebuild(GameSessionSnapshot snapshot)
        {
            Clear();

            foreach (var blob in snapshot.Blobs)
                CreateBlobView(blob, _theme);
            foreach (var tile in snapshot.Tiles)
                CreateTileView(tile, _theme);
        }

        public void Clear()
        {
            foreach (var view in _blobViews.Values)
            {
                if (view == null)
                    continue;

                if (ApplicationIsPlaying())
                    Destroy(view.gameObject);
                else
                    DestroyImmediate(view.gameObject);
            }

            _blobViews.Clear();
        }

        private void CreateTileView(TileState tile, LevelVisualThemeAsset theme)
        {
            var view = InstantiateTileView();
            view.Initialize(tile, theme, _cellSize, _origin);
            _tileViews.Add(tile.Id, view);
        }

        private void CreateBlobView(BlobState blob, LevelVisualThemeAsset theme)
        {
            var view = InstantiateBlobView();
            view.Initialize(blob, theme, _cellSize, _origin);
            view.Selected += HandleBlobSelected;
            _blobViews.Add(blob.Id, view);
        }

        private TileView InstantiateTileView()
        {
            var parent = _tileRoot != null ? _tileRoot : transform;

            if (_tileViewPrefab != null)
                return Instantiate(_tileViewPrefab, parent);

            var instance = new GameObject("Production Tile");
            instance.transform.SetParent(parent, false);
            return instance.AddComponent<TileView>();
        }
        private BlobView InstantiateBlobView()
        {
            var parent = _blobRoot != null ? _blobRoot : transform;

            if (_blobViewPrefab != null)
                return Instantiate(_blobViewPrefab, parent);

            var instance = new GameObject("Production Blob");
            instance.transform.SetParent(parent, false);
            return instance.AddComponent<BlobView>();
        }

        private void HandleBlobSelected(BlobView view)
        {
            BlobSelected?.Invoke(view.BlobId);
        }

        private static bool ApplicationIsPlaying()
        {
            return UnityEngine.Application.isPlaying;
        }
    }
}
