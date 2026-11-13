using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Blobs.Application;
using Blobs.Content;
using Blobs.Core;
using UnityEngine;
using DG.Tweening;
using System.Linq;

namespace Blobs.Presentation
{
    /// <summary>
    /// Coordinates board-level presentation in response to gameplay state and effects.
    /// Blob, tile are delegated to focused presenters.
    /// </summary>
    [RequireComponent(typeof(BlobPresenter))]
    [RequireComponent(typeof(TilePresenter))]
    public sealed class BoardPresenter : MonoBehaviour
    {
        [SerializeField] private BlobPresenter _blobPresenter;
        [SerializeField] private TilePresenter _tilePresenter;
        [SerializeField, Tooltip("Optional. Must share the blob root's board space; sort it between tiles and blobs.")]
        private SwipeIndicatorView _swipeIndicatorView;
        [SerializeField, Tooltip("Optional. Board-space coach pointer for level-authored swipe demos.")]
        private CoachPointerView _coachPointerView;
        [SerializeField, Range(0f, 1f)] private float cellOpacity = 0.15f;
        [SerializeField] private float cellSize = 1.25f;
        [SerializeField] private GameObject cellPrefab;
        [SerializeField] private Transform cellRoot;

        [SerializeField] private Vector2 origin;
        private PresentationTimeline _effectTimeline;
        private CancellationTokenSource _playbackCancellation;
        private bool _playbackPaused;
        private UniTaskCompletionSource _playbackCompletion;
        private GameSessionSnapshot _pendingSnapshot;
        public bool IsPresenting => _playbackCancellation != null;
        private BoardEffectPresentationPipeline _effectPipeline;
        private readonly List<IBoardEffectPresentationHandler> _additionalEffectHandlers = new();
        private readonly List<IMoveStepPresentationHandler> _additionalMoveStepHandlers = new();
        private IGameplayState _state;
        private SwipeIndicatorPresenter _swipeIndicatorPresenter;
        private CoachPresenter _coachPresenter;
        private bool _effectsAnimated = true;

        public float CellSize => cellSize;
        public Vector2 Origin => origin;

        /// <summary>Board-space coach pointer shared with level and tutorial coaching. May be null.</summary>
        public CoachPointerView CoachPointerView => _coachPointerView;

        private readonly Dictionary<SpriteRenderer, float> _authoredAlpha = new();
        private readonly List<SpriteRenderer> _rendererBuffer = new();
        private float _boardAlpha = 1f;

        /// <summary>
        /// When true, queued commands ignore board input. Used to keep taps from skipping a tutorial
        /// entrance or acting on the board behind a reaction slide.
        /// </summary>
        public bool IsInteractionLocked { get; private set; }

        public void SetInteractionLocked(bool locked)
        {
            IsInteractionLocked = locked;
        }

        /// <summary>Opacity multiplier applied to every cell, blob, and tile renderer.</summary>
        public float BoardAlpha => _boardAlpha;

        public void SetBoardAlpha(float alpha)
        {
            _boardAlpha = Mathf.Clamp01(alpha);
            ApplyBoardAlpha(cellRoot);
            ApplyBoardAlpha(_blobPresenter != null ? _blobPresenter.Root : null);
            ApplyBoardAlpha(_tilePresenter != null ? _tilePresenter.Root : null);
            if (_boardAlpha >= 1f)
                _authoredAlpha.Clear();
            if (_boardAlpha <= 0f && _swipeIndicatorView != null)
                _swipeIndicatorView.HideImmediately();
        }

        /// <summary>Fades the whole board from its opacity when the tween starts. Meant to be nested in a caller's sequence.</summary>
        public Tween FadeBoardTo(float alpha, float duration)
        {
            return DOTween.To(() => _boardAlpha, SetBoardAlpha, Mathf.Clamp01(alpha), duration)
                .SetEase(Ease.InOutSine);
        }

        private void ApplyBoardAlpha(Transform root)
        {
            if (root == null)
                return;

            root.GetComponentsInChildren(true, _rendererBuffer);
            foreach (SpriteRenderer renderer in _rendererBuffer)
            {
                if (IsOverlay(renderer.transform))
                    continue;
                if (!_authoredAlpha.TryGetValue(renderer, out float authored))
                {
                    authored = renderer.color.a;
                    _authoredAlpha.Add(renderer, authored);
                }

                Color color = renderer.color;
                color.a = authored * _boardAlpha;
                renderer.color = color;
            }
            _rendererBuffer.Clear();
        }

        /// <summary>The coach pointer and swipe indicator own their opacity even when parented under a board root.</summary>
        private bool IsOverlay(Transform candidate)
        {
            return (_coachPointerView != null && candidate.IsChildOf(_coachPointerView.transform)) ||
                (_swipeIndicatorView != null && candidate.IsChildOf(_swipeIndicatorView.transform));
        }

        /// <summary>
        /// Builds an entrance meant to be nested in a caller's sequence for pieces that are already in Core state: cells grow
        /// in first, then blobs enter one after another in list order. Views that do not exist yet are
        /// created hidden immediately so nothing pops in before its turn.
        /// </summary>
        public Sequence CreateRevealSequence(
            IReadOnlyList<GridPosition> cells,
            IReadOnlyList<string> blobIds,
            BoardRevealOptions options)
        {
            Sequence sequence = DOTween.Sequence();
            float cellStagger = options.Stagger * 0.5f;
            float at = 0f;
            int cellCount = 0;

            if (cells != null)
            {
                foreach (GridPosition position in cells)
                {
                    GameObject cell = FindCell(position) ?? CreateCell(position.X, position.Y);
                    if (cell == null)
                        continue;

                    Transform cellTransform = cell.transform;
                    Vector3 scale = cellTransform.localScale;
                    cellTransform.localScale = Vector3.zero;
                    sequence.Insert(at, cellTransform.DOScale(scale, options.CellDuration).SetEase(Ease.OutBack));
                    at += cellStagger;
                    cellCount++;
                }
            }

            at = cellCount > 0 ? at - cellStagger + options.CellDuration * 0.6f : 0f;

            if (blobIds == null || blobIds.Count == 0 || _state == null)
                return sequence;

            BoardState board = _state.CreateSnapshot().Board;
            foreach (string id in blobIds)
            {
                if (!TryGetBlobView(id, out BlobView view))
                {
                    BlobState blob = board.GetBlob(id);
                    if (blob == null || !_blobPresenter.TryCreateView(blob, out view))
                        continue;
                }

                InsertBlobEntrance(sequence, view.transform, at, options);
                at += options.Stagger;
            }

            if (_boardAlpha < 1f)
                SetBoardAlpha(_boardAlpha);
            return sequence;
        }

        /// <summary>
        /// Builds a shrink-away for blobs already removed from Core state. Their views are retired
        /// immediately and destroyed when the tween completes, including when it is completed early.
        /// </summary>
        public Sequence CreateRemovalSequence(IReadOnlyList<string> blobIds, float duration)
        {
            Sequence sequence = DOTween.Sequence();
            if (blobIds == null)
                return sequence;

            foreach (string id in blobIds)
            {
                if (!_blobPresenter.TryRetireView(id, out BlobView view))
                    continue;

                Transform viewTransform = view.transform;
                Vector3 scale = viewTransform.localScale;
                sequence.Insert(0f, DOTween.To(
                        () => 0f,
                        progress =>
                        {
                            if (view == null)
                                return;
                            if (progress >= 1f)
                                _blobPresenter.DestroyRetiringView(view);
                            else
                                viewTransform.localScale = scale * (1f - progress);
                        },
                        1f,
                        Mathf.Max(0.0001f, duration))
                    .SetEase(Ease.InBack));
            }

            return sequence;
        }

        private void InsertBlobEntrance(Sequence sequence, Transform view, float at, BoardRevealOptions options)
        {
            Vector3 scale = view.localScale;
            Vector3 target = view.localPosition;
            float duration = Mathf.Max(0.0001f, options.Duration);
            float distance = options.Distance * cellSize;
            view.localScale = Vector3.zero;

            Vector3 offset;
            Ease ease = Ease.OutBack;
            switch (options.Style)
            {
                case BlobRevealStyle.Pop:
                    sequence.Insert(at, view.DOScale(scale, duration).SetEase(Ease.OutBack));
                    return;
                case BlobRevealStyle.Instant:
                    // Tween (not callback) so completing the sequence early still shows the blob.
                    sequence.Insert(at, view.DOScale(scale, 0.0001f));
                    return;
                case BlobRevealStyle.DropIn:
                    offset = Vector3.up * distance;
                    ease = Ease.OutBounce;
                    break;
                case BlobRevealStyle.SlideFromLeft:
                    offset = Vector3.left * distance;
                    break;
                case BlobRevealStyle.SlideFromRight:
                    offset = Vector3.right * distance;
                    break;
                case BlobRevealStyle.RiseFromBelow:
                    offset = Vector3.down * distance;
                    break;
                default:
                    offset = Vector3.zero;
                    break;
            }

            view.localPosition = target + offset;
            sequence.Insert(at, view.DOScale(scale, Mathf.Min(duration, 0.15f)).SetEase(Ease.OutQuad));
            sequence.Insert(at, view.DOLocalMove(target, duration).SetEase(ease));
        }

        private GameObject FindCell(GridPosition position)
        {
            if (cellRoot == null)
                return null;

            Transform cell = cellRoot.Find(CellName(position.X, position.Y));
            return cell != null ? cell.gameObject : null;
        }
        private string _dragCommitSource;
        private LevelColorPaletteAsset _colorPalette;

        public BlobSelectionResult CommitDragPreview(string sourceId, Func<BlobSelectionResult> commit)
        {
            _dragCommitSource = sourceId;
            try { return commit(); }
            finally { _dragCommitSource = null; }
        }
        public int VisibleBlobCount => _blobPresenter != null ? _blobPresenter.VisibleCount : 0;
        public int VisibleTileCount => _tilePresenter != null ? _tilePresenter.VisibleCount : 0;

        /// <summary>
        /// Current session snapshot used by tests and UI. Null until the presenter is initialized.
        /// </summary>
        public GameSessionSnapshot CurrentSnapshot => _state?.CreateSnapshot();

        /// <summary>
        /// Raised after this presenter applies effects from a move.
        /// </summary>
        public event Action<GameSessionSnapshot> SnapshotChanged;

        /// <summary>Enables or disables effect choreography for mechanic comparison slices.</summary>
        public void SetEffectsAnimated(bool animated)
        {
            _effectsAnimated = animated;
        }



        /// <summary>
        /// Releases gameplay subscriptions and clears presentation views.
        /// </summary>
        public void Dispose()
        {
            Unsubscribe();
        }

        /// <summary>
        /// Adds or replaces the presentation handler for one Core effect type.
        /// This is the extension point for new effects that should not require changes to the board coordinator.
        /// </summary>
        public void RegisterEffectHandler(IBoardEffectPresentationHandler handler)
        {
            BoardEffectPresentationPipeline.ValidateHandler(handler);

            for (int i = _additionalEffectHandlers.Count - 1; i >= 0; i--)
            {
                if (_additionalEffectHandlers[i].EffectType == handler.EffectType)
                    _additionalEffectHandlers.RemoveAt(i);
            }

            _additionalEffectHandlers.Add(handler);
            _effectPipeline?.Register(handler);
        }

        /// <summary>
        /// Registers choreography for a semantic move step that spans multiple effects.
        /// The most recently registered matching handler is evaluated first.
        /// </summary>
        public void RegisterMoveStepHandler(IMoveStepPresentationHandler handler)
        {
            BoardEffectPresentationPipeline.ValidateHandler(handler);
            _additionalMoveStepHandlers.Remove(handler);
            _additionalMoveStepHandlers.Add(handler);
            _effectPipeline?.Register(handler);
        }

        /// <summary>
        /// Connects the presenter to gameplay state and builds the initial board views.
        /// </summary>
        public void Initialize(
            IGameplayState state,
            LevelColorPaletteAsset palette,
            IBlobViewFactory blobViewFactory = null,
            ITileViewFactory tileViewFactory = null,
            LevelDefinitionAsset level = null)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            var snapshot = state.CreateSnapshot();

            Unsubscribe();
            ResolvePresenters(snapshot.Board);
            IsInteractionLocked = false;
            _boardAlpha = 1f;
            _authoredAlpha.Clear();
            _state = state;
            _colorPalette = palette;
            _blobPresenter.Initialize(state, palette, cellSize, origin, blobViewFactory);
            _tilePresenter.Initialize(cellSize, origin, tileViewFactory);
            if (_swipeIndicatorView != null)
            {
                _swipeIndicatorPresenter = new SwipeIndicatorPresenter(
                    _swipeIndicatorView, state, palette, cellSize, origin);
            }
            if (_coachPointerView != null && level != null)
            {
                _coachPresenter = new CoachPresenter(
                    _coachPointerView, state, level, cellSize, origin);
            }
            _state.MoveResolved += HandleMoveResolved;
            _state.UndoResolved += HandleUndoResolved;
            _state.StateRestored += Rebuild;
            Rebuild(snapshot);
        }

        private void CreateCells(GameSessionSnapshot snapshot)
        {
            var board = snapshot.Board;
            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    if (snapshot.Board.EmptyPositions.Any(p => p.X == x && p.Y == y))
                    {
                        continue;
                    }
                    CreateCell(x, y);
                }

            }
        }

        private GameObject CreateCell(int x, int y)
        {
            if (cellPrefab == null || cellRoot == null)
                return null;

            var cell = Instantiate(cellPrefab, cellRoot);
            cell.transform.localPosition = new Vector3(x * cellSize, y * cellSize, 0);
            cell.name = CellName(x, y);
            cell.GetComponent<SpriteRenderer>().color = _colorPalette.CellColor;
            return cell;
        }

        private static string CellName(int x, int y) => $"Cell_{x}_{y}";

        private void DestroyCells()
        {
            if (cellRoot == null)
                return;

            foreach (Transform child in cellRoot)
            {
                // Renamed so FindCell never returns a cell that is pending destruction this frame.
                child.name = "Cell_Destroyed";
                Destroy(child.gameObject);
            }
        }

        private void HandleUndoResolved(UndoResult undo)
        {
            if (undo == null)
                return;

            GameSessionSnapshot interrupted = _pendingSnapshot;
            KillEffectTimeline();
            if (interrupted != null)
                Rebuild(interrupted);

            Action undoFeedback = InvokeOnce(
               _blobPresenter.CreateUndoFeedback(
                   undo.ForwardAction.SourceBlobId,
                   undo.ForwardAction.TargetBlobId));
            undoFeedback?.Invoke();


            IReadOnlyList<MoveStep> steps = undo.ForwardAction?.Steps;
            if (steps != null && steps.Count > 0)
            {
                PresentAsync(
                    steps,
                    null,
                    undo.RestoredSnapshot,
                    contactFeedback: null,
                    default,
                    reverse: true,
                    undo.RestorationBlobs).Forget(Debug.LogException);
                return;
            }

            PresentAsync(
                null,
                undo.ForwardAction?.Effects ?? Array.Empty<IBoardEffect>(),
                undo.RestoredSnapshot,
                contactFeedback: null,
                default,
                reverse: true,
                undo.RestorationBlobs).Forget(Debug.LogException);
        }



        private void HandleMoveResolved(MoveResult result)
        {
            if (!result.Succeeded)
                return;

            if (_dragCommitSource == result.SourceBlobId)
            {
                Rebuild(_state.CreateSnapshot());
                if (TryGetBlobView(result.SourceBlobId, out var survivor))
                {
                    var scale = survivor.transform.localScale;
                    survivor.transform.localScale = scale * 1.12f;
                    survivor.transform.DOScale(scale, .16f).SetEase(Ease.OutQuad)
                        .SetLink(survivor.gameObject, LinkBehaviour.KillOnDestroy);
                }
                return;
            }

            if (_pendingSnapshot != null)
                Rebuild(_pendingSnapshot);

            Action contactFeedback = InvokeOnce(
                _blobPresenter.CreateContactFeedback(
                    result.SourceBlobId,
                    result.TargetBlobId));

            if (result.Steps != null && result.Steps.Count > 0)
            {
                ApplyStepsInternal(
                    result.Steps,
                    _state.CreateSnapshot(),
                    contactFeedback);
            }
            else
            {
                ApplyEffects(result.Effects, _state.CreateSnapshot());

                // A successful adjacent contact can have no movement steps or effects.
                if (ShouldAnimateEffects())
                    contactFeedback?.Invoke();
            }
        }

        /// <summary>
        /// Rebuilds all board views from a snapshot. Used for initial render, restart, and desync recovery.
        /// </summary>
        public void Rebuild(GameSessionSnapshot snapshot)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));
            DestroyCells();
            CreateCells(snapshot);
            ResolvePresenters(snapshot.Board);
            KillEffectTimeline();
            _tilePresenter.Rebuild(snapshot.Board);
            _blobPresenter.Rebuild(snapshot.Board.Blobs);
            if (_boardAlpha < 1f)
                SetBoardAlpha(_boardAlpha);
            SnapshotChanged?.Invoke(snapshot);
        }

        /// <summary>
        /// Applies ordered Core effects to existing views, rebuilding from the snapshot if necessary.
        /// </summary>
        public void ApplyEffects(IReadOnlyList<IBoardEffect> effects, GameSessionSnapshot fallbackSnapshot)
        {
            if (effects == null)
                throw new ArgumentNullException(nameof(effects));
            if (fallbackSnapshot == null)
                throw new ArgumentNullException(nameof(fallbackSnapshot));

            ApplyEffectsAsync(effects, fallbackSnapshot).Forget(Debug.LogException);
        }

        public void ApplySteps(IReadOnlyList<MoveStep> steps, GameSessionSnapshot fallbackSnapshot)
        {
            ApplyStepsAsync(steps, fallbackSnapshot).Forget(Debug.LogException);
        }

        private void ApplyStepsInternal(IReadOnlyList<MoveStep> steps,
            GameSessionSnapshot fallbackSnapshot, Action contactFeedback)
        {
            ApplyStepsAsync(steps, fallbackSnapshot, contactFeedback).Forget(Debug.LogException);
        }

        public UniTask ApplyEffectsAsync(IReadOnlyList<IBoardEffect> effects,
            GameSessionSnapshot fallbackSnapshot, CancellationToken cancellationToken = default)
        {
            if (effects == null) throw new ArgumentNullException(nameof(effects));
            return PresentAsync(null, effects, fallbackSnapshot, null, cancellationToken);
        }

        public UniTask ApplyStepsAsync(IReadOnlyList<MoveStep> steps,
            GameSessionSnapshot fallbackSnapshot, Action contactFeedback = null,
            CancellationToken cancellationToken = default)
        {
            if (steps == null) throw new ArgumentNullException(nameof(steps));
            return PresentAsync(steps, null, fallbackSnapshot, contactFeedback, cancellationToken);
        }

        public UniTask ApplyStepsReversedAsync(
            IReadOnlyList<MoveStep> steps,
            GameSessionSnapshot restoredSnapshot,
            IReadOnlyDictionary<string, BlobState> restorationBlobs,
            CancellationToken cancellationToken = default)
        {
            if (steps == null) throw new ArgumentNullException(nameof(steps));
            if (restoredSnapshot == null) throw new ArgumentNullException(nameof(restoredSnapshot));
            return PresentAsync(
                steps,
                null,
                restoredSnapshot,
                null,
                cancellationToken,
                reverse: true,
                restorationBlobs);
        }

        public UniTask ApplyEffectsReversedAsync(
            IReadOnlyList<IBoardEffect> effects,
            GameSessionSnapshot restoredSnapshot,
            IReadOnlyDictionary<string, BlobState> restorationBlobs,
            CancellationToken cancellationToken = default)
        {
            if (effects == null) throw new ArgumentNullException(nameof(effects));
            if (restoredSnapshot == null) throw new ArgumentNullException(nameof(restoredSnapshot));
            return PresentAsync(
                null,
                effects,
                restoredSnapshot,
                null,
                cancellationToken,
                reverse: true,
                restorationBlobs);
        }

        private async UniTask PresentAsync(IReadOnlyList<MoveStep> steps,
            IReadOnlyList<IBoardEffect> effects, GameSessionSnapshot snapshot,
            Action contactFeedback, CancellationToken externalToken,
            bool reverse = false,
            IReadOnlyDictionary<string, BlobState> restorationBlobs = null)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            externalToken.ThrowIfCancellationRequested();
            // A new move starts from the previous move's committed state, even if its
            // animation was still running. Rebuild/undo supplies its own snapshot instead.
            GameSessionSnapshot interrupted = _pendingSnapshot;
            KillEffectTimeline();
            if (interrupted != null) Rebuild(interrupted);

            var cancellation = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
            var timeline = PresentationTimeline.Create(ShouldAnimateEffects());
            var playbackCompletion = new UniTaskCompletionSource();
            _playbackCancellation = cancellation;
            _playbackCompletion = playbackCompletion;
            _effectTimeline = timeline;
            _pendingSnapshot = snapshot;
            try
            {
                bool applied;
                if (reverse)
                {
                    applied = steps != null
                        ? await _effectPipeline.PresentStepsReversedAsync(
                            steps, timeline, restorationBlobs, cancellation.Token)
                        : await _effectPipeline.PresentOrderedEffectsReversedAsync(
                            effects, timeline, restorationBlobs, cancellation.Token);
                }
                else
                {
                    applied = steps != null
                        ? await _effectPipeline.PresentStepsAsync(
                            steps, timeline, contactFeedback, cancellation.Token)
                        : await _effectPipeline.PresentOrderedEffectsAsync(
                            effects, timeline, cancellation.Token);
                }
                cancellation.Token.ThrowIfCancellationRequested();
                if (!applied || !IsSynchronizedWith(snapshot))
                {
                    timeline.Kill();
                    _blobPresenter.Rebuild(snapshot.Board.Blobs);
                    _tilePresenter.Rebuild(snapshot.Board);
                }
                contactFeedback?.Invoke();
                SnapshotChanged?.Invoke(snapshot);
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
            {
                if (_playbackCancellation == cancellation)
                {
                    timeline.Kill();
                    _blobPresenter.Rebuild(snapshot.Board.Blobs);
                    _tilePresenter.Rebuild(snapshot.Board);
                }
                if (externalToken.IsCancellationRequested) throw;
            }
            catch
            {
                if (_playbackCancellation == cancellation)
                {
                    timeline.Kill();
                    _blobPresenter.Rebuild(snapshot.Board.Blobs);
                    _tilePresenter.Rebuild(snapshot.Board);
                }
                throw;
            }
            finally
            {
                timeline.Kill();
                if (_playbackCancellation == cancellation)
                {
                    _playbackCancellation = null;
                    _playbackCompletion = null;
                    _effectTimeline = null;
                    _pendingSnapshot = null;
                }
                playbackCompletion.TrySetResult();
                cancellation.Dispose();
            }
        }

        /// <summary>
        /// Returns the view mapped to a stable Core blob ID.
        /// </summary>
        public bool TryGetBlobView(string blobId, out BlobView view)
        {
            if (_blobPresenter != null)
                return _blobPresenter.TryGetView(blobId, out view);

            view = null;
            return false;
        }

        /// <summary>
        /// Checks all render-relevant logical state without depending on animation timing.
        /// </summary>
        public bool IsSynchronizedWith(GameSessionSnapshot snapshot)
        {
            return snapshot != null &&
                _blobPresenter != null &&
                _tilePresenter != null &&
                _blobPresenter.IsSynchronizedWith(
                    snapshot.Board.Blobs,
                    snapshot.Board.Blobs.Count) &&
                _tilePresenter.IsSynchronizedWith(
                    snapshot.Board.Tiles,
                    snapshot.Board.Tiles.Count);
        }

        /// <summary>
        /// Destroys all current blob, tile views.
        /// </summary>
        public void Clear()
        {
            KillEffectTimeline();
            _blobPresenter?.Clear();
            _tilePresenter?.Clear();
        }



        private static Action InvokeOnce(Action callback)
        {
            if (callback == null)
                return null;

            bool invoked = false;
            return () =>
            {
                if (invoked)
                    return;

                invoked = true;
                callback();
            };
        }

        public UniTask WaitUntilIdleAsync()
        {
            UniTaskCompletionSource completion = _playbackCompletion;
            return completion != null ? completion.Task : UniTask.CompletedTask;
        }

        public UniTask WaitUntilUnpausedAsync()
        {
            return _playbackPaused ? UniTask.Create(async () =>
            {
                while (_playbackPaused)
                {
                    await UniTask.DelayFrame(1);
                }
            }) : UniTask.CompletedTask;
        }

        public void ResumePlayback()
        {
            _playbackPaused = false;

        }

        public async void PausePlayback()
        {
            _playbackPaused = true;
            await WaitUntilUnpausedAsync();
        }

        private void KillEffectTimeline()
        {
            var cancellation = _playbackCancellation;
            _playbackCancellation = null;
            _pendingSnapshot = null;
            cancellation?.Cancel();
            _effectTimeline?.Kill();
            _effectTimeline = null;
            _blobPresenter?.CompleteInterruptedAnimations();
        }

        private void OnDisable()
        {
            GameSessionSnapshot pending = _pendingSnapshot;
            KillEffectTimeline();
            if (pending != null) Rebuild(pending);
        }

        private bool ShouldAnimateEffects()
        {
            return _effectsAnimated && UnityEngine.Application.isPlaying && isActiveAndEnabled;
        }

        /// <summary>
        /// Resolves the explicitly required sibling presenters and fails immediately if scene
        /// composition is invalid. Runtime creation would leave their authored assets unwired.
        /// </summary>
        private void ResolvePresenters(BoardState board)
        {
            if (_blobPresenter == null)
                _blobPresenter = GetComponent<BlobPresenter>();

            if (_tilePresenter == null)
                _tilePresenter = GetComponent<TilePresenter>();

            if (_blobPresenter == null || _tilePresenter == null)
            {
                throw new InvalidOperationException(
                    "BoardPresenter requires BlobPresenter and TilePresenter components on the same GameObject.");
            }

            if (_effectPipeline != null)
                return;

            _effectPipeline = new BoardEffectPresentationPipeline(
                _blobPresenter,
                _tilePresenter,
                board);
            foreach (IBoardEffectPresentationHandler handler in _additionalEffectHandlers)
                _effectPipeline.Register(handler);
            foreach (IMoveStepPresentationHandler handler in _additionalMoveStepHandlers)
                _effectPipeline.Register(handler);
        }

        private void Unsubscribe()
        {
            if (_state == null)
                return;

            _state.MoveResolved -= HandleMoveResolved;
            _state.UndoResolved -= HandleUndoResolved;
            _state.StateRestored -= Rebuild;
            _swipeIndicatorPresenter?.Dispose();
            _swipeIndicatorPresenter = null;
            _coachPresenter?.Dispose();
            _coachPresenter = null;
            _state = null;
            _blobPresenter?.DisconnectFromState();
            Clear();
        }

        private void OnDestroy()
        {
            Dispose();
        }


    }
}
