using System;
using System.Collections.Generic;
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
    public sealed class BoardPresenter : MonoBehaviour
    {
        [SerializeField] private BlobPresenter _blobPresenter;
        [SerializeField] private TilePresenter _tilePresenter;
        [SerializeField] private BoardSurfacePresenter _boardSurfacePresenter;
        [SerializeField] private float cellSize = 1.25f;
        [SerializeField] private Vector2 origin;

        private PresentationTimeline _effectTimeline;
        private BoardEffectPresentationPipeline _effectPipeline;
        private readonly List<IBoardEffectPresentationHandler> _additionalEffectHandlers = new();
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
            EnsurePresenters();
            _state = state;
            _blobPresenter.Initialize(state, palette, cellSize, origin, blobViewFactory);
            _tilePresenter.Initialize(cellSize, origin, tileViewFactory);
            _boardSurfacePresenter.Initialize(cellSize, origin);

            _state.MoveResolved += HandleMoveResolved;
            _state.StateRestored += Rebuild;
            Rebuild(_state.CreateSnapshot());
        }

        private void HandleMoveResolved(MoveResult result)
        {
            if (!result.Succeeded)
                return;

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

            EnsurePresenters();
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

            KillEffectTimeline();
            PresentationTimeline timeline = PresentationTimeline.Create(ShouldAnimateEffects());
            bool appliedAll = _effectPipeline.PresentOrderedEffects(effects, timeline);

            CompleteEffectApplication(timeline, appliedAll, fallbackSnapshot);
        }

        /// <summary>
        /// Applies a step-based move timeline with each step presented as one animation beat.
        /// </summary>
        public void ApplySteps(IReadOnlyList<MoveStep> steps, GameSessionSnapshot fallbackSnapshot)
        {
            ApplyStepsInternal(steps, fallbackSnapshot, contactFeedback: null);
        }

        private void ApplyStepsInternal(
            IReadOnlyList<MoveStep> steps,
            GameSessionSnapshot fallbackSnapshot,
            Action contactFeedback)
        {
            if (steps == null)
                throw new ArgumentNullException(nameof(steps));
            if (fallbackSnapshot == null)
                throw new ArgumentNullException(nameof(fallbackSnapshot));

            KillEffectTimeline();
            PresentationTimeline timeline = PresentationTimeline.Create(ShouldAnimateEffects());
            bool appliedAll = _effectPipeline.PresentSteps(
                steps,
                timeline,
                contactFeedback);

            timeline.AppendCallback(contactFeedback);

            CompleteEffectApplication(timeline, appliedAll, fallbackSnapshot);
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

        private void CompleteEffectApplication(
            PresentationTimeline timeline,
            bool appliedAll,
            GameSessionSnapshot fallbackSnapshot)
        {
            if (!appliedAll || !IsSynchronizedWith(fallbackSnapshot))
            {
                timeline.Kill();
                _blobPresenter.CompleteInterruptedAnimations();
                Rebuild(fallbackSnapshot);
                return;
            }

            if (timeline.IsActive && timeline.Duration > 0f)
            {
                _effectTimeline = timeline;
                timeline.OnComplete(() => _effectTimeline = null);
            }
            else
            {
                timeline.Kill();
            }

            SnapshotChanged?.Invoke(fallbackSnapshot);
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
            if (_effectTimeline != null)
            {
                PresentationTimeline timeline = _effectTimeline;
                _effectTimeline = null;
                timeline.Kill();
            }

            _blobPresenter?.CompleteInterruptedAnimations();
        }

        private bool ShouldAnimateEffects()
        {
            return UnityEngine.Application.isPlaying && isActiveAndEnabled;
        }

        private void EnsurePresenters()
        {
            if (_blobPresenter == null)
                _blobPresenter = GetComponent<BlobPresenter>();
            if (_blobPresenter == null)
                _blobPresenter = gameObject.AddComponent<BlobPresenter>();

            if (_tilePresenter == null)
                _tilePresenter = GetComponent<TilePresenter>();
            if (_tilePresenter == null)
                _tilePresenter = gameObject.AddComponent<TilePresenter>();

            if (_boardSurfacePresenter == null)
                _boardSurfacePresenter = GetComponent<BoardSurfacePresenter>();
            if (_boardSurfacePresenter == null)
                _boardSurfacePresenter = gameObject.AddComponent<BoardSurfacePresenter>();

            if (_effectPipeline != null)
                return;

            _effectPipeline = new BoardEffectPresentationPipeline(
                _blobPresenter,
                _tilePresenter);
            foreach (IBoardEffectPresentationHandler handler in _additionalEffectHandlers)
                _effectPipeline.Register(handler);
        }

        private void Unsubscribe()
        {
            if (_state == null)
                return;

            _state.MoveResolved -= HandleMoveResolved;
            _state.StateRestored -= Rebuild;
            _state = null;
            Clear();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }
    }
}
