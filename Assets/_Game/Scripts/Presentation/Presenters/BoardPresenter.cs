using System;
using System.Collections.Generic;
using Blobs.Application;
using Blobs.Core;
using Blobs.Content;
using DG.Tweening;
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
        [SerializeField, Min(0f)] private float _moveDuration = 0.16f;
        [SerializeField, Min(0f)] private float _spawnDuration = 0.14f;
        [SerializeField, Min(0f)] private float _despawnDuration = 0.12f;
        public float CellSize => _cellSize;
        public int VisibleBlobCount => _blobViews.Count;
        public int VisibleTileCount => _tileViews.Count;
        private LevelVisualThemeAsset _theme;
        public GameSessionSnapshot CurrentSnapshot => _state?.CreateSnapshot();

        public event Action<GameSessionSnapshot> SnapshotChanged;
        private IGameplayState _state;
        public void Initialize(IGameplayState state, LevelVisualThemeAsset theme)
        {
            Unsubscribe();
            _state = state;
            _theme = theme;
            _state.MoveResolved += HandleMoveResolved;
            _state.MoveUndone += HandleMoveUndone;
            _state.StateRestored += Rebuild;
            Rebuild(_state.CreateSnapshot());
        }

        private void HandleMoveResolved(MoveResult result)
        {
            if (result.Succeeded)
                ApplyEffects(result.Effects, _state.CreateSnapshot());
        }

        private void HandleMoveUndone(UndoResult result)
        {
            if (result.Succeeded)
                ApplyEffects(result.Effects, _state.CreateSnapshot());
        }

        public void Rebuild(GameSessionSnapshot snapshot)
        {
            Clear();

            foreach (var tile in snapshot.Tiles)
                CreateTileView(tile, _theme);
            foreach (var blob in snapshot.Blobs)
                CreateBlobView(blob, _theme);
        }

        public void ApplyEffects(IReadOnlyList<IBoardEffect> effects, GameSessionSnapshot fallbackSnapshot)
        {
            foreach (var effect in effects)
            {
                switch (effect)
                {
                    case RemoveBlobEffect remove:
                        RemoveBlobView(remove.BlobId);
                        break;
                    case MoveBlobEffect move:
                        MoveBlobView(move.BlobId, move.To, ShouldAnimateEffects());
                        break;
                    case SpawnBlobEffect spawn:
                        CreateBlobView(spawn.Blob, _theme, ShouldAnimateEffects());
                        break;
                    default:
                        Rebuild(fallbackSnapshot);
                        return;
                }
            }
        }

        public void Clear()
        {
            foreach (var view in _blobViews.Values)
            {
                if (view == null)
                    continue;

                view.transform.DOKill();
                if (ApplicationIsPlaying())
                    Destroy(view.gameObject);
                else
                    DestroyImmediate(view.gameObject);
            }

            _blobViews.Clear();

            foreach (var view in _tileViews.Values)
            {
                if (view == null)
                    continue;

                if (ApplicationIsPlaying())
                    Destroy(view.gameObject);
                else
                    DestroyImmediate(view.gameObject);
            }

            _tileViews.Clear();
        }

        private void CreateTileView(TileState tile, LevelVisualThemeAsset theme)
        {
            var view = InstantiateTileView();
            view.Initialize(tile, theme, _cellSize, _origin);
            _tileViews.Add(tile.Id, view);
        }

        private void CreateBlobView(BlobState blob, LevelVisualThemeAsset theme)
        {
            CreateBlobView(blob, theme, false);
        }

        private void CreateBlobView(BlobState blob, LevelVisualThemeAsset theme, bool animate)
        {
            RemoveBlobView(blob.Id);
            var view = InstantiateBlobView();
            view.Initialize(blob, theme, _cellSize, _origin);
            _blobViews.Add(blob.Id, view);
            if (animate)
                view.PlaySpawn(_spawnDuration);
        }

        private void MoveBlobView(string blobId, GridPosition to, bool animate)
        {
            if (_blobViews.TryGetValue(blobId, out var view) && view != null)
            {
                if (animate)
                    view.AnimateMoveTo(to, _moveDuration);
                else
                    view.SetGridPosition(to);
            }
        }

        private void RemoveBlobView(string blobId)
        {
            if (!_blobViews.TryGetValue(blobId, out var view))
                return;

            _blobViews.Remove(blobId);

            if (view != null)
            {
                view.transform.DOKill();
                if (ShouldAnimateEffects())
                {
                    view.PlayDespawn(_despawnDuration)
                        .OnComplete(() =>
                        {
                            if (view != null)
                                Destroy(view.gameObject);
                        });
                }
                else if (ApplicationIsPlaying())
                {
                    Destroy(view.gameObject);
                }
                else
                {
                    DestroyImmediate(view.gameObject);
                }
            }
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


        private static bool ApplicationIsPlaying()
        {
            return UnityEngine.Application.isPlaying;
        }

        private bool ShouldAnimateEffects()
        {
            return ApplicationIsPlaying() && isActiveAndEnabled;
        }



        private void ApplyEffects(IReadOnlyList<IBoardEffect> effects)
        {
            var snapshot = _state.CreateSnapshot();
            ApplyEffects(effects, snapshot);
            SnapshotChanged?.Invoke(snapshot);
        }

        private void Unsubscribe()
        {
            if (_state == null)
                return;
            _state.MoveResolved -= HandleMoveResolved;
            _state.MoveUndone -= HandleMoveUndone;
            _state.StateRestored -= Rebuild;
            SnapshotChanged = null;
            _state = null;
            _theme = null;
            Clear();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

    }
}
