using System;
using System.Collections.Generic;
using Blobs.Application;
using Blobs.Core;
using Blobs.Content;
using DG.Tweening;
using UnityEngine;
using System.Linq;

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
        [SerializeField] private GameObject cellPrefab;
        [SerializeField] private Transform cellRoot;
        private IBlobViewFactory _blobViewFactory;
        [SerializeField] private TileView tileViewPrefab;
        [SerializeField] private Transform blobRoot;
        [SerializeField] private Transform tileRoot;
        [SerializeField] private float cellSize = 1.25f;
        [SerializeField] private Vector2 origin;
        [SerializeField, Min(0f)] private float moveDuration = 0.16f;
        [SerializeField, Min(0f)] private float spawnDuration = 0.14f;
        [SerializeField, Min(0f)] private float despawnDuration = 0.12f;
        [SerializeField] private MergeAnimationOrchestrator _mergeAnimationOrchestrator;
        public float CellSize => cellSize;
        public int VisibleBlobCount => _blobViews.Count;
        public int VisibleTileCount => _tileViews.Count;

        public int VisibleSurfaceCellCount =>
            _boardSurfaceView != null ? _boardSurfaceView.VisibleCellCount : 0;
        private BoardSurfaceView _boardSurfaceView;
        private int _width;
        private int _height;

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
            LevelColorPaletteAsset palette,
            IBlobViewFactory blobViewFactory = null,
            int width = 0,
            int height = 0,
            BoardSurfaceLayoutAsset layout = null)
        {
            Unsubscribe();
            _state = state;
            _width = width;
            _height = height;
            _blobViewFactory =
                blobViewFactory ?? new BlobViewFactory(_blobViewCatalog, palette);

            _state.MoveResolved += HandleMoveResolved;
            _state.StateRestored += Rebuild;
            Rebuild(_state.CreateSnapshot());
        }

        private void HandleMoveResolved(MoveResult result)
        {
            if (!result.Succeeded)
                return;

            bool targetIsRock =
                !string.IsNullOrEmpty(result.TargetBlobId) &&
                _blobViews.TryGetValue(result.TargetBlobId, out BlobView targetView) &&
                targetView != null &&
                targetView.BlobType == BlobType.Rock;

            if (result.Steps != null && result.Steps.Count > 0)
                ApplyStepsInternal(
                    result.Steps,
                    _state.CreateSnapshot(),
                    playRockThumpOnFinalMove: targetIsRock);
            else
            {
                ApplyEffects(result.Effects, _state.CreateSnapshot());

                // An adjacent rock produces a successful intent with no movement
                // steps. It still gets contact audio, but never merge visuals/state.
                if (targetIsRock && ShouldAnimateEffects())
                    EnsureMergeAnimationOrchestrator().PlayRockThumpAudio();
            }

        }




        /// <summary>
        /// Rebuilds all board views from a snapshot. Used for initial render, restart, and desync recovery.
        /// </summary>
        public void Rebuild(GameSessionSnapshot snapshot)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));

            Clear();


            foreach (var tile in snapshot.Board.Tiles)
                CreateTileView(tile);
            EnsureBoardSurfaceView().Rebuild(
            BuildSurfacePositions(snapshot),
            cellSize,
            origin);

            // BuildCells(snapshot.Board);
            foreach (var blob in snapshot.Board.Blobs)
                CreateBlobView(blob);

            SnapshotChanged?.Invoke(snapshot);
        }

        public void BuildCells(BoardState board)
        {
            for (int i = 0; i < board.Width; i++)
            {
                for (int j = 0; j < board.Height; j++)
                {
                    GridPosition position = new(i, j);

                    if (board.EmptyPositions.Any(p => p.Equals(position)))
                    {
                        continue;
                    }

                    GameObject cell = Instantiate(
                        cellPrefab,
                        ToWorldPosition(position),
                        Quaternion.identity,
                        cellRoot
                    );

                    cell.name = $"Cell_{position.X}_{position.Y}";

                }
            }
        }
        private void ClearCells()
        {
            foreach (Transform child in cellRoot)
                Destroy(child.gameObject);
        }

        private Vector3 ToWorldPosition(GridPosition position)
        {
            return new Vector3(position.X, position.Y, 0f) * cellSize;
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
                    appliedAll &= CreateNormalMergeBeat(
                        mergeSource,
                        mergeTarget,
                        sequence,
                        appendToSequence: true);
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
                        appliedAll &= CreateBlobView(spawn.Blob, sequence);
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
        /// Applies a step-based move timeline. Steps play sequentially; effects within one
        /// step compose into a single beat where locomotion and departure spawns run in
        /// parallel (e.g. a Trail blob dropping a puddle blob as it leaves a tile) while
        /// merge despawns play as arrival feedback at the end of the beat.
        /// </summary>
        public void ApplySteps(IReadOnlyList<MoveStep> steps, GameSessionSnapshot fallbackSnapshot)
        {
            ApplyStepsInternal(
                steps,
                fallbackSnapshot,
                playRockThumpOnFinalMove: false);
        }

        private void ApplyStepsInternal(
            IReadOnlyList<MoveStep> steps,
            GameSessionSnapshot fallbackSnapshot,
            bool playRockThumpOnFinalMove)
        {
            if (steps == null)
                throw new ArgumentNullException(nameof(steps));
            if (fallbackSnapshot == null)
                throw new ArgumentNullException(nameof(fallbackSnapshot));

            KillEffectSequence();
            Sequence sequence = ShouldAnimateEffects() ? DOTween.Sequence() : null;
            bool appliedAll = true;

            int lastMoveStepIndex = -1;
            for (int i = 0; i < steps.Count; i++)
            {
                foreach (IBoardEffect effect in steps[i].Effects)
                {
                    if (effect is MoveBlobEffect || effect is MergeIntoFlagEffect)
                        lastMoveStepIndex = i;
                }
            }

            for (int i = 0; i < steps.Count && appliedAll; i++)
            {
                appliedAll &= ApplyStep(
                    steps[i],
                    isFinalMoveBeat: i == lastMoveStepIndex,
                    playRockThumpOnArrival:
                        playRockThumpOnFinalMove && i == lastMoveStepIndex,
                    sequence);
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

        private bool ApplyStep(
            MoveStep step,
            bool isFinalMoveBeat,
            bool playRockThumpOnArrival,
            Sequence outerSequence)
        {
            Sequence beat = outerSequence != null ? DOTween.Sequence() : null;
            bool applied = true;

            if (step.Kind == MoveStepKind.Merge &&
                TryFindNormalMerge(step, out MoveBlobEffect mergeSource,
                    out RemoveBlobEffect mergeTarget))
            {
                applied &= CreateNormalMergeBeat(
                    mergeSource,
                    mergeTarget,
                    beat,
                    appendToSequence: false);

                foreach (IBoardEffect effect in step.Effects)
                {
                    if (effect is SpawnBlobEffect spawn)
                        applied &= CreateBlobViewJoined(spawn.Blob, beat);

                    if (!applied)
                        return false;
                }

                if (beat != null)
                    outerSequence.Append(beat);

                return applied;
            }

            // Compose in visual order regardless of board-application order:
            // locomotion anchors the beat, spawns join at the beat start, and
            // merge despawns follow as arrival feedback.
            foreach (IBoardEffect effect in step.Effects)
            {
                switch (effect)
                {
                    case MoveBlobEffect move:
                        applied &= MoveBlobViewJoined(
                            move.BlobId,
                            move.To,
                            beat,
                            isFinalMoveBeat ? Ease.OutQuad : Ease.Linear,
                            playRockThumpOnArrival);
                        break;
                    case MergeIntoFlagEffect mergeIntoFlag:
                        applied &= MergeIntoFlagView(mergeIntoFlag, beat);
                        break;
                }

                if (!applied)
                    return false;
            }

            foreach (IBoardEffect effect in step.Effects)
            {
                if (effect is SpawnBlobEffect spawn)
                    applied &= CreateBlobViewJoined(spawn.Blob, beat);

                if (!applied)
                    return false;
            }

            foreach (IBoardEffect effect in step.Effects)
            {
                if (effect is RemoveBlobEffect remove)
                    applied &= RemoveBlobView(remove.BlobId, beat);

                if (!applied)
                    return false;
            }

            if (beat != null)
                outerSequence.Append(beat);

            return applied;
        }

        private bool MoveBlobViewJoined(
            string blobId,
            GridPosition to,
            Sequence beat,
            Ease ease,
            bool playRockThumpOnArrival)
        {
            if (!_blobViews.TryGetValue(blobId, out var view) || view == null)
                return false;

            if (beat != null)
            {
                beat.AppendCallback(() => view.BlobMotionAnimator?.SetMoving());
                Tween movement = view.AnimateMoveTo(to, moveDuration, ease);
                movement.OnComplete(() =>
                {
                    view.BlobMotionAnimator?.SetIdle();
                    if (playRockThumpOnArrival)
                        EnsureMergeAnimationOrchestrator().PlayRockThumpAudio();
                });
                beat.Join(movement);
            }
            else
                view.SetGridPosition(to);

            return true;
        }

        private bool CreateBlobViewJoined(
            BlobState blob,
            Sequence beat)
        {
            if (!CreateBlobView(blob, null))
                return false;

            if (beat != null &&
                _blobViews.TryGetValue(blob.Id, out BlobView view) &&
                view != null)
            {
                beat.Join(view.PlaySpawn(spawnDuration));
            }

            return true;
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
                _blobViews.Count != snapshot.Board.Blobs.Count ||
                _tileViews.Count != snapshot.Board.Tiles.Count)
            {
                return false;
            }

            foreach (BlobState blob in snapshot.Board.Blobs)
            {
                if (!_blobViews.TryGetValue(blob.Id, out BlobView view) ||
                    view == null ||
                    view.BlobId != blob.Id ||
                    view.GridPosition != blob.Position)
                {
                    return false;
                }
            }

            foreach (TileState tile in snapshot.Board.Tiles)
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

        private ISet<GridPosition> BuildSurfacePositions(
            GameSessionSnapshot snapshot)
        {
            var occupied = new HashSet<GridPosition>();
            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    var position = new GridPosition(x, y);
                    if (snapshot.Board.EmptyPositions.Any(p => p.Equals(position)))
                    {
                        continue;
                    }
                    occupied.Add(position);
                }
            }

            return occupied;
        }

        /// <summary>
        /// Destroys all current blob and tile views.
        /// </summary>
        public void Clear()
        {
            KillEffectSequence();
            ClearCells();
            _boardSurfaceView?.ClearCells();

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

        private void CreateTileView(TileState tile)
        {
            var view = InstantiateTileView();
            view.Initialize(tile, cellSize, origin);
            _tileViews.Add(tile.Id, view);
        }

        private void CreateBlobView(BlobState blob)
        {
            CreateBlobView(blob, null);
        }

        private bool CreateBlobView(
            BlobState blob,
            Sequence sequence)
        {
            if (blob == null || _blobViews.ContainsKey(blob.Id))
                return false;

            Transform parent =
                blobRoot != null ? blobRoot : transform;

            BlobView view = _blobViewFactory.Create(
                blob,
                _state,
                parent,
                cellSize,
                origin);

            _blobViews.Add(blob.Id, view);

            sequence?.Append(view.PlaySpawn(spawnDuration));

            return true;
        }

        private bool MoveBlobView(string blobId, GridPosition to, Sequence sequence)
        {
            if (!_blobViews.TryGetValue(blobId, out var view) || view == null)
                return false;

            if (sequence != null)
            {
                sequence.AppendCallback(() => view.BlobMotionAnimator?.SetMoving());
                Tween movement = view.AnimateMoveTo(to, moveDuration);
                movement.OnComplete(() => view.BlobMotionAnimator?.SetIdle());
                sequence.Append(movement);
            }
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
                .Append(view.PlayDespawn(despawnDuration))
                .AppendCallback(
                    () => DestroyRetiringBlobView(view));

            return true;
        }

        private bool CreateNormalMergeBeat(
            MoveBlobEffect move,
            RemoveBlobEffect remove,
            Sequence sequence,
            bool appendToSequence)
        {
            if (!_blobViews.TryGetValue(move.BlobId, out BlobView sourceView) ||
                sourceView == null ||
                !_blobViews.TryGetValue(remove.BlobId, out BlobView targetView) ||
                targetView == null)
            {
                return false;
            }

            _blobViews.Remove(remove.BlobId);

            if (sequence == null)
            {
                sourceView.SetGridPosition(move.To);
                sourceView.BlobMotionAnimator?.SetIdle();
                DestroyBlobView(targetView);
                return true;
            }

            _retiringBlobViews.Add(targetView);
            Vector2Int direction = new(
                move.To.X - move.From.X,
                move.To.Y - move.From.Y);
            Sequence mergeBeat = EnsureMergeAnimationOrchestrator()
                .CreateMergeBeat(
                    sourceView,
                    targetView,
                    direction,
                    () => DestroyRetiringBlobView(targetView));

            if (appendToSequence)
                sequence.Append(mergeBeat);
            else
                sequence.Join(mergeBeat);

            return true;
        }

        private static bool TryFindNormalMerge(
            MoveStep step,
            out MoveBlobEffect move,
            out RemoveBlobEffect remove)
        {
            move = null;
            remove = null;

            foreach (IBoardEffect effect in step.Effects)
            {
                if (effect is MoveBlobEffect candidateMove)
                {
                    move = candidateMove;
                    break;
                }
            }

            if (move == null)
                return false;

            foreach (IBoardEffect effect in step.Effects)
            {
                if (effect is RemoveBlobEffect candidateRemove &&
                    candidateRemove.BlobId != move.BlobId &&
                    candidateRemove.At == move.To)
                {
                    remove = candidateRemove;
                    break;
                }
            }

            return remove != null;
        }
        private void KillEffectSequence()
        {
            if (_effectSequence != null)
            {
                Sequence sequence = _effectSequence;
                _effectSequence = null;
                sequence.Kill(false);
            }

            DestroyRetiringBlobViews();

            foreach (BlobView view in _blobViews.Values)
            {
                if (view == null)
                    continue;

                view.SetGridPosition(view.GridPosition);
                view.BlobMotionAnimator?.SetIdle();
            }
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

            captureBeat.AppendCallback(() =>
            {
                sourceView.BlobMotionAnimator?.SetMerging();
                flagView.BlobMotionAnimator?.SetMerging();
            });

            captureBeat.Append(
                sourceView.PlayConsumedInto(
                    effect.To,
                    moveDuration,
                    despawnDuration));

            captureBeat.InsertCallback(moveDuration, () =>
                EnsureMergeAnimationOrchestrator().PlayImpact(
                    flagView.transform.position,
                    sourceView.MergeEffectColor,
                    flagView));

            Tween targetFeedback =
                flagView.PlaySourceAccepted(
                    moveDuration + despawnDuration);

            if (targetFeedback != null)
                captureBeat.Join(targetFeedback);

            captureBeat.OnComplete(
                () =>
                {
                    flagView.BlobMotionAnimator?.SetIdle();
                    DestroyRetiringBlobView(sourceView);
                });

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
            var parent = tileRoot != null ? tileRoot : transform;

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



        private MergeAnimationOrchestrator EnsureMergeAnimationOrchestrator()
        {
            if (_mergeAnimationOrchestrator != null)
                return _mergeAnimationOrchestrator;

            _mergeAnimationOrchestrator = GetComponent<MergeAnimationOrchestrator>();
            if (_mergeAnimationOrchestrator == null)
                _mergeAnimationOrchestrator = gameObject.AddComponent<MergeAnimationOrchestrator>();
            return _mergeAnimationOrchestrator;
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
            Clear();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

    }
}
