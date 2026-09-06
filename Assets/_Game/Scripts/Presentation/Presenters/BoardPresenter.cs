using System;
using System.Collections.Generic;
using Blobs.Application;
using Blobs.Content;
using Blobs.Core;
using DG.Tweening;
using UnityEngine;

namespace Blobs.Presentation
{
    /// <summary>
    /// Coordinates board-level presentation in response to gameplay state and effects.
    /// Blob and tile view lifecycles are delegated to their focused presenters.
    /// </summary>
    public sealed class BoardPresenter : MonoBehaviour
    {
        [SerializeField] private BlobPresenter _blobPresenter;
        [SerializeField] private TilePresenter _tilePresenter;
        [SerializeField] private float cellSize = 1.25f;
        [SerializeField] private Vector2 origin;

        private Sequence _effectSequence;
        private IGameplayState _state;

        public float CellSize => cellSize;
        public int VisibleBlobCount => _blobPresenter != null ? _blobPresenter.VisibleCount : 0;
        public int VisibleTileCount => _tilePresenter != null ? _tilePresenter.VisibleCount : 0;
        public int VisibleSurfaceCellCount =>
            _tilePresenter != null ? _tilePresenter.VisibleSurfaceCellCount : 0;

        /// <summary>
        /// Current session snapshot used by tests and UI. Null until the presenter is initialized.
        /// </summary>
        public GameSessionSnapshot CurrentSnapshot => _state?.CreateSnapshot();

        /// <summary>
        /// Raised after this presenter applies effects from a move.
        /// </summary>
        public event Action<GameSessionSnapshot> SnapshotChanged;

        /// <summary>
        /// Connects the presenter to gameplay state and builds the initial board views.
        /// </summary>
        public void Initialize(
            IGameplayState state,
            LevelColorPaletteAsset palette,
            IBlobViewFactory blobViewFactory = null)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            Unsubscribe();
            EnsurePresenters();
            _state = state;
            _blobPresenter.Initialize(state, palette, cellSize, origin, blobViewFactory);
            _tilePresenter.Initialize(cellSize, origin);

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
            KillEffectSequence();
            _tilePresenter.Rebuild(snapshot.Board);
            _blobPresenter.Rebuild(snapshot.Board.Blobs);
            SnapshotChanged?.Invoke(snapshot);
        }

        /// <summary>
        /// Builds the optional legacy cell-prefab layer through the tile presenter.
        /// </summary>
        public void BuildCells(BoardState board)
        {
            EnsurePresenters();
            _tilePresenter.BuildCells(board);
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

            KillEffectSequence();
            Sequence sequence = ShouldAnimateEffects() ? DOTween.Sequence() : null;
            bool appliedAll = true;

            for (int i = 0; i < effects.Count; i++)
            {
                IBoardEffect effect = effects[i];

                // Core removes an occupied target before moving the source. Present the pair as one beat.
                if (effect is RemoveBlobEffect mergeTarget &&
                    i + 1 < effects.Count &&
                    effects[i + 1] is MoveBlobEffect mergeSource &&
                    mergeSource.To == mergeTarget.At)
                {
                    appliedAll &= _blobPresenter.CreateNormalMergeBeat(
                        mergeSource,
                        mergeTarget,
                        sequence,
                        appendToSequence: true,
                        onContact: null);
                    i++;
                }
                else
                {
                    switch (effect)
                    {
                        case MoveBlobEffect move:
                            appliedAll &= _blobPresenter.Move(move.BlobId, move.To, sequence);
                            break;
                        case RemoveBlobEffect remove:
                            appliedAll &= _blobPresenter.Remove(remove.BlobId, sequence);
                            break;
                        case SpawnBlobEffect spawn:
                            appliedAll &= _blobPresenter.Create(spawn.Blob, sequence);
                            break;
                        case MergeIntoFlagEffect mergeIntoFlag:
                            appliedAll &= _blobPresenter.MergeIntoFlag(mergeIntoFlag, sequence);
                            break;
                        default:
                            appliedAll = false;
                            break;
                    }
                }

                if (!appliedAll)
                    break;
            }

            CompleteEffectApplication(sequence, appliedAll, fallbackSnapshot);
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

            KillEffectSequence();
            Sequence sequence = ShouldAnimateEffects() ? DOTween.Sequence() : null;
            bool appliedAll = true;
            int lastMoveStepIndex = FindLastMoveStepIndex(steps);

            for (int i = 0; i < steps.Count && appliedAll; i++)
            {
                appliedAll &= ApplyStep(
                    steps[i],
                    isFinalMoveBeat: i == lastMoveStepIndex,
                    onContact:
                        i == lastMoveStepIndex ? contactFeedback : null,
                    sequence);
            }

            if (sequence != null)
                sequence.AppendCallback(() => contactFeedback?.Invoke());
            else
                contactFeedback?.Invoke();

            CompleteEffectApplication(sequence, appliedAll, fallbackSnapshot);
        }

        private bool ApplyStep(
            MoveStep step,
            bool isFinalMoveBeat,
            Action onContact,
            Sequence outerSequence)
        {
            Sequence beat = outerSequence != null ? DOTween.Sequence() : null;
            bool applied = true;

            if (step.Kind == MoveStepKind.Merge &&
                TryFindNormalMerge(step, out MoveBlobEffect mergeSource,
                    out RemoveBlobEffect mergeTarget))
            {
                applied &= _blobPresenter.CreateNormalMergeBeat(
                    mergeSource,
                    mergeTarget,
                    beat,
                    appendToSequence: false,
                    onContact: onContact);

                foreach (IBoardEffect effect in step.Effects)
                {
                    if (effect is SpawnBlobEffect spawn)
                        applied &= _blobPresenter.CreateJoined(spawn.Blob, beat);
                    if (!applied)
                        return false;
                }

                if (beat != null)
                    outerSequence.Append(beat);
                return applied;
            }

            // Locomotion anchors a beat, departure spawns join it, and removals follow arrival.
            foreach (IBoardEffect effect in step.Effects)
            {
                switch (effect)
                {
                    case MoveBlobEffect move:
                        applied &= _blobPresenter.MoveJoined(
                            move.BlobId,
                            move.To,
                            beat,
                            isFinalMoveBeat ? Ease.OutQuad : Ease.Linear,
                            onContact);
                        break;
                    case MergeIntoFlagEffect mergeIntoFlag:
                        applied &= _blobPresenter.MergeIntoFlag(
                            mergeIntoFlag,
                            beat,
                            onContact);
                        break;
                }

                if (!applied)
                    return false;
            }

            foreach (IBoardEffect effect in step.Effects)
            {
                if (effect is SpawnBlobEffect spawn)
                    applied &= _blobPresenter.CreateJoined(spawn.Blob, beat);
                if (!applied)
                    return false;
            }

            foreach (IBoardEffect effect in step.Effects)
            {
                if (effect is RemoveBlobEffect remove)
                    applied &= _blobPresenter.Remove(remove.BlobId, beat);
                if (!applied)
                    return false;
            }

            if (beat != null)
                outerSequence.Append(beat);
            return true;
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
        /// Checks logical IDs and positions against a snapshot without depending on animation timing.
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
            KillEffectSequence();
            _blobPresenter?.Clear();
            _tilePresenter?.Clear();
        }

        private void CompleteEffectApplication(
            Sequence sequence,
            bool appliedAll,
            GameSessionSnapshot fallbackSnapshot)
        {
            if (!appliedAll || !IsSynchronizedWith(fallbackSnapshot))
            {
                sequence?.Kill();
                _blobPresenter.CompleteInterruptedAnimations();
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

        private static int FindLastMoveStepIndex(IReadOnlyList<MoveStep> steps)
        {
            int lastMoveStepIndex = -1;
            for (int i = 0; i < steps.Count; i++)
            {
                foreach (IBoardEffect effect in steps[i].Effects)
                {
                    if (effect is MoveBlobEffect || effect is MergeIntoFlagEffect)
                        lastMoveStepIndex = i;
                }
            }

            return lastMoveStepIndex;
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

        private void KillEffectSequence()
        {
            if (_effectSequence != null)
            {
                Sequence sequence = _effectSequence;
                _effectSequence = null;
                sequence.Kill(false);
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
