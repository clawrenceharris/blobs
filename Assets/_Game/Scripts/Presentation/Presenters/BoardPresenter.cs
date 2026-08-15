using System;
using System.Collections.Generic;
using Blobs.Application;
using Blobs.Core;
using Blobs.Content;
using DG.Tweening;
using UnityEngine;

namespace Blobs.Presentation
{
    /// <summary>
    /// Owns the Unity board view. It observes Application state/result events and translates Core effects
    /// into blob/tile views and animation without deciding gameplay rules.
    /// </summary>
    public sealed class BoardPresenter : MonoBehaviour
    {
        private readonly Dictionary<string, BlobView> _blobViews = new Dictionary<string, BlobView>();
        private readonly Dictionary<string, TileView> _tileViews = new Dictionary<string, TileView>();
        private readonly List<BlobView> _retiringBlobViews = new List<BlobView>();
        private Sequence _effectSequence;
        [SerializeField]
        private BlobViewCatalogAsset _blobViewCatalog;

        private IBlobViewFactory _blobViewFactory;
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

        /// <summary>
        /// Current session snapshot used by tests and UI. Null until the presenter is initialized.
        /// </summary>
        public GameSessionSnapshot CurrentSnapshot => _state?.CreateSnapshot();

        /// <summary>
        /// Raised after this presenter applies effects from a move.
        /// </summary>
        public event Action<GameSessionSnapshot> SnapshotChanged;
        private IGameplayState _state;

        /// <summary>
        /// Connects the presenter to gameplay state and builds the initial board views.
        /// </summary>
        public void Initialize(
            IGameplayState state,
            LevelVisualThemeAsset theme,
            IBlobViewFactory blobViewFactory = null)
        {
            Unsubscribe();
            _state = state;
            _theme = theme;

            _blobViewFactory =
                blobViewFactory ?? new BlobViewFactory(_blobViewCatalog);

            _state.MoveResolved += HandleMoveResolved;
            _state.StateRestored += Rebuild;
            Rebuild(_state.CreateSnapshot());
        }

        private void HandleMoveResolved(MoveResult result)
        {
            if (result.Succeeded)
                ApplyEffects(result.Effects, _state.CreateSnapshot());
        }


        /// <summary>
        /// Rebuilds all board views from a snapshot. Used for initial render, restart, and desync recovery.
        /// </summary>
        public void Rebuild(GameSessionSnapshot snapshot)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));

            Clear();

            foreach (var tile in snapshot.Tiles)
                CreateTileView(tile, _theme);
            foreach (var blob in snapshot.Blobs)
                CreateBlobView(blob, _theme);

            SnapshotChanged?.Invoke(snapshot);
        }

        /// <summary>
        /// Applies ordered Core effects to the existing board views, falling back to a full rebuild for unsupported effects.
        /// </summary>
        public void ApplyEffects(IReadOnlyList<IBoardEffect> effects, GameSessionSnapshot fallbackSnapshot)
        {
            if (effects == null)
                throw new ArgumentNullException(nameof(effects));
            if (fallbackSnapshot == null)
                throw new ArgumentNullException(nameof(fallbackSnapshot));

            KillEffectSequence();
            Sequence sequence = ShouldAnimateEffects() ? DOTween.Sequence() : null;
            bool appliedAll = true;

            for (int i = 0; i < effects.Count; i++)
            {
                IBoardEffect effect = effects[i];

                // Core removes an occupied target before moving the source into its cell. Present
                // that adjacent pair as one readable merge beat: source arrives, then target clears.
                if (effect is RemoveBlobEffect mergeTarget &&
                    i + 1 < effects.Count &&
                    effects[i + 1] is MoveBlobEffect mergeSource &&
                    mergeSource.To == mergeTarget.At)
                {
                    appliedAll &= MoveBlobView(mergeSource.BlobId, mergeSource.To, sequence);
                    appliedAll &= RemoveBlobView(mergeTarget.BlobId, sequence);
                    i++;

                    if (!appliedAll)
                        break;

                    continue;
                }

                switch (effect)
                {
                    case MoveBlobEffect move:
                        appliedAll &= MoveBlobView(move.BlobId, move.To, sequence);
                        break;
                    case RemoveBlobEffect remove:
                        appliedAll &= RemoveBlobView(remove.BlobId, sequence);
                        break;

                    case SpawnBlobEffect spawn:
                        appliedAll &= CreateBlobView(spawn.Blob, _theme, sequence);
                        break;
                    case MergeIntoFlagEffect mergeIntoFlag:
                        appliedAll &=
                            MergeIntoFlagView(mergeIntoFlag, sequence);
                        break;
                    default:
                        appliedAll = false;
                        break;
                }

                if (!appliedAll)
                    break;
            }

            if (!appliedAll || !IsSynchronizedWith(fallbackSnapshot))
            {
                sequence?.Kill();
                DestroyRetiringBlobViews();
                Rebuild(fallbackSnapshot);
                return;
            }

            if (sequence != null && sequence.active && sequence.Duration() > 0f)
            {
                _effectSequence = sequence;
                sequence.OnComplete(() => _effectSequence = null);
            }
            else
            {
                sequence?.Kill();
            }

            SnapshotChanged?.Invoke(fallbackSnapshot);
        }

        /// <summary>
        /// Returns the view mapped to a stable Core blob ID.
        /// </summary>
        public bool TryGetBlobView(string blobId, out BlobView view)
        {
            return _blobViews.TryGetValue(blobId, out view) && view != null;
        }

        /// <summary>
        /// Checks logical IDs and positions against a snapshot without depending on animation timing.
        /// </summary>
        public bool IsSynchronizedWith(GameSessionSnapshot snapshot)
        {
            if (snapshot == null ||
                _blobViews.Count != snapshot.Blobs.Count ||
                _tileViews.Count != snapshot.Tiles.Count)
            {
                return false;
            }

            foreach (BlobState blob in snapshot.Blobs)
            {
                if (!_blobViews.TryGetValue(blob.Id, out BlobView view) ||
                    view == null ||
                    view.BlobId != blob.Id ||
                    view.GridPosition != blob.Position)
                {
                    return false;
                }
            }

            foreach (TileState tile in snapshot.Tiles)
            {
                if (!_tileViews.TryGetValue(tile.Id, out TileView view) ||
                    view == null ||
                    view.TileId != tile.Id ||
                    view.GridPosition != tile.Position)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Destroys all current blob and tile views.
        /// </summary>
        public void Clear()
        {
            KillEffectSequence();

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
            CreateBlobView(blob, theme, null);
        }

        private bool CreateBlobView(
            BlobState blob,
            LevelVisualThemeAsset theme,
            Sequence sequence)
        {
            if (blob == null || _blobViews.ContainsKey(blob.Id))
                return false;

            Transform parent =
                _blobRoot != null ? _blobRoot : transform;

            BlobView view = _blobViewFactory.Create(
                blob,
                theme,
                parent,
                _cellSize,
                _origin);

            _blobViews.Add(blob.Id, view);

            if (sequence != null)
                sequence.Append(view.PlaySpawn(_spawnDuration));

            return true;
        }

        private bool MoveBlobView(string blobId, GridPosition to, Sequence sequence)
        {
            if (!_blobViews.TryGetValue(blobId, out var view) || view == null)
                return false;

            if (sequence != null)
                sequence.Append(view.AnimateMoveTo(to, _moveDuration));
            else
                view.SetGridPosition(to);

            return true;
        }
        private static void DestroyBlobView(BlobView view)
        {
            if (view == null)
                return;

            if (ApplicationIsPlaying())
                UnityEngine.Object.Destroy(view.gameObject);
            else
                UnityEngine.Object.DestroyImmediate(view.gameObject);
        }
        private bool RemoveBlobView(string blobId, Sequence sequence)
        {
            if (!_blobViews.TryGetValue(blobId, out BlobView view) ||
                view == null)
            {
                return false;
            }

            _blobViews.Remove(blobId);

            if (sequence == null)
            {
                DestroyBlobView(view);
                return true;
            }

            _retiringBlobViews.Add(view);

            sequence
                .Append(view.PlayDespawn(_despawnDuration))
                .AppendCallback(
                    () => DestroyRetiringBlobView(view));

            return true;
        }
        private void KillEffectSequence()
        {
            if (_effectSequence != null)
            {
                Sequence sequence = _effectSequence;
                _effectSequence = null;
                sequence.Complete(true);
                sequence.Kill();
            }

            DestroyRetiringBlobViews();
        }

        private void DestroyRetiringBlobViews()
        {
            for (int i = _retiringBlobViews.Count - 1; i >= 0; i--)
                DestroyRetiringBlobView(_retiringBlobViews[i]);

            _retiringBlobViews.Clear();
        }
        private bool MergeIntoFlagView(MergeIntoFlagEffect effect, Sequence outerSequence)
        {
            if (!_blobViews.TryGetValue(
                    effect.SourceId,
                    out BlobView sourceView) ||
                sourceView == null)
            {
                return false;
            }

            if (!_blobViews.TryGetValue(
                    effect.FlagId,
                    out BlobView flagView) ||
                flagView == null)
            {
                return false;
            }

            // Update the logical view registry immediately. Core has already
            // removed the source, so final snapshot comparison expects it gone.
            _blobViews.Remove(effect.SourceId);

            if (outerSequence == null)
            {
                sourceView.SetGridPosition(effect.To);
                DestroyBlobView(sourceView);
                return true;
            }

            _retiringBlobViews.Add(sourceView);

            var captureBeat = DOTween.Sequence();

            captureBeat.Append(
                sourceView.PlayConsumedInto(
                    effect.To,
                    _moveDuration,
                    _despawnDuration));

            Tween targetFeedback =
                flagView.PlaySourceAccepted(
                    _moveDuration + _despawnDuration);

            if (targetFeedback != null)
                captureBeat.Join(targetFeedback);

            captureBeat.OnComplete(
                () => DestroyRetiringBlobView(sourceView));

            outerSequence.Append(captureBeat);

            return true;
        }

        private void DestroyRetiringBlobView(BlobView view)
        {
            _retiringBlobViews.Remove(view);
            DestroyBlobView(view);
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



        private static bool ApplicationIsPlaying()
        {
            return UnityEngine.Application.isPlaying;
        }

        private bool ShouldAnimateEffects()
        {
            return ApplicationIsPlaying() && isActiveAndEnabled;
        }

        private void Unsubscribe()
        {
            if (_state == null)
                return;
            _state.MoveResolved -= HandleMoveResolved;
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
