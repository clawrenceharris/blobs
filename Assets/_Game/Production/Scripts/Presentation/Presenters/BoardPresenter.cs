using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Blobs.Application;
using Blobs.Content;
using Blobs.Core;
using UnityEngine;

namespace Blobs.Presentation
{
    /// <summary>
    /// Coordinates board-level presentation in response to gameplay state and effects.
    /// Blob, tile, and board-surface lifecycles are delegated to focused presenters.
    /// </summary>
    [RequireComponent(typeof(BlobPresenter))]
    [RequireComponent(typeof(TilePresenter))]
    [RequireComponent(typeof(BoardSurfacePresenter))]
    public sealed class BoardPresenter : MonoBehaviour
    {
        [SerializeField] private BlobPresenter _blobPresenter;
        [SerializeField] private TilePresenter _tilePresenter;
        [SerializeField] private BoardSurfacePresenter _boardSurfacePresenter;
        [SerializeField] private float cellSize = 1.25f;
        [SerializeField] private Vector2 origin;

        private PresentationTimeline _effectTimeline;
        private CancellationTokenSource _playbackCancellation;
        private GameSessionSnapshot _pendingSnapshot;
        public bool IsPresenting => _playbackCancellation != null;
        private BoardEffectPresentationPipeline _effectPipeline;
        private readonly List<IBoardEffectPresentationHandler> _additionalEffectHandlers = new();
        private readonly List<IMoveStepPresentationHandler> _additionalMoveStepHandlers = new();
        private IGameplayState _state;

        public float CellSize => cellSize;
        public int VisibleBlobCount => _blobPresenter != null ? _blobPresenter.VisibleCount : 0;
        public int VisibleTileCount => _tilePresenter != null ? _tilePresenter.VisibleCount : 0;
        public int VisibleSurfaceCellCount =>
            _boardSurfacePresenter != null ? _boardSurfacePresenter.VisibleCellCount : 0;

        /// <summary>
        /// Current session snapshot used by tests and UI. Null until the presenter is initialized.
        /// </summary>
        public GameSessionSnapshot CurrentSnapshot => _state?.CreateSnapshot();

        /// <summary>
        /// Raised after this presenter applies effects from a move.
        /// </summary>
        public event Action<GameSessionSnapshot> SnapshotChanged;

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
            ITileViewFactory tileViewFactory = null)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            Unsubscribe();
            ResolvePresenters();
            _state = state;
            _blobPresenter.Initialize(state, palette, cellSize, origin, blobViewFactory);
            _tilePresenter.Initialize(cellSize, origin, tileViewFactory);
            _boardSurfacePresenter.Initialize(cellSize, origin);

            _state.MoveResolved += HandleMoveResolved;
            _state.UndoResolved += HandleUndoResolved;
            _state.StateRestored += Rebuild;
            Rebuild(_state.CreateSnapshot());
        }

        private void HandleUndoResolved(UndoResult undo)
        {
            if (undo == null)
                return;

            GameSessionSnapshot interrupted = _pendingSnapshot;
            KillEffectTimeline();
            if (interrupted != null)
                Rebuild(interrupted);

            PlayUndoFeedback();

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

        private void PlayUndoFeedback()
        {
            IUndoPlaybackFeedback[] channels = GetComponents<IUndoPlaybackFeedback>();
            for (int i = 0; i < channels.Length; i++)
                channels[i].PlayUndo();
        }

        private void HandleMoveResolved(MoveResult result)
        {
            if (!result.Succeeded)
                return;

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

            ResolvePresenters();
            KillEffectTimeline();
            _boardSurfacePresenter.Rebuild(snapshot.Board);
            _tilePresenter.Rebuild(snapshot.Board);
            _blobPresenter.Rebuild(snapshot.Board.Blobs);
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
            _playbackCancellation = cancellation;
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
                    _boardSurfacePresenter.Rebuild(snapshot.Board);
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
                    _boardSurfacePresenter.Rebuild(snapshot.Board);
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
                    _boardSurfacePresenter.Rebuild(snapshot.Board);
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
                    _effectTimeline = null;
                    _pendingSnapshot = null;
                }
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
        /// Destroys all current blob, tile, and board-surface views.
        /// </summary>
        public void Clear()
        {
            KillEffectTimeline();
            _blobPresenter?.Clear();
            _tilePresenter?.Clear();
            _boardSurfacePresenter?.Clear();
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
            return UnityEngine.Application.isPlaying && isActiveAndEnabled;
        }

        /// <summary>
        /// Resolves the explicitly required sibling presenters and fails immediately if scene
        /// composition is invalid. Runtime creation would leave their authored assets unwired.
        /// </summary>
        private void ResolvePresenters()
        {
            if (_blobPresenter == null)
                _blobPresenter = GetComponent<BlobPresenter>();

            if (_tilePresenter == null)
                _tilePresenter = GetComponent<TilePresenter>();

            if (_boardSurfacePresenter == null)
                _boardSurfacePresenter = GetComponent<BoardSurfacePresenter>();

            if (_blobPresenter == null ||
                _tilePresenter == null ||
                _boardSurfacePresenter == null)
            {
                throw new InvalidOperationException(
                    "BoardPresenter requires BlobPresenter, TilePresenter, and " +
                    "BoardSurfacePresenter components on the same GameObject.");
            }

            if (_effectPipeline != null)
                return;

            _effectPipeline = new BoardEffectPresentationPipeline(
                _blobPresenter,
                _tilePresenter);
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
