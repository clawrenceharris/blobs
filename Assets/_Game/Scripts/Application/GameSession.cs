using System;
using System.Collections.Generic;
using System.Linq;
using Blobs.Core;

namespace Blobs.Application
{
    /// <summary>
    /// Command surface for gameplay input. Input and UI adapters use this interface so they do not
    /// need direct access to board state or Presentation objects.
    /// </summary>
    public interface IGameplayCommands
    {
        /// <summary>
        /// Selects a blob at a grid position or completes a source-to-target move if a source is already selected.
        /// </summary>
        BlobSelectionResult SelectBlobAt(GridPosition position);

        /// <summary>Starts a fresh drag without completing a previous selection.</summary>
        BlobSelectionResult BeginDragAt(GridPosition position);
        /// <summary>Extends the active drag toward a pointer cell without committing a move.</summary>
        BlobSelectionResult UpdateDragAt(GridPosition position);
        /// <summary>Attempts a move from the captured source to the drag tail resolved from the release cell.</summary>
        BlobSelectionResult EndDragAt(GridPosition position);
        /// <summary>Abandons the drag and clears its selection without moving.</summary>
        void CancelDrag();

        /// <summary>
        /// Restores the complete state before the latest board-changing action.
        /// </summary>
        bool Undo();

        /// <summary>
        /// Restores the authored initial level state and clears transient session state.
        /// </summary>
        void Restart();
    }

    /// <summary>
    /// Read/event surface for presenters. Presentation observes this interface instead of issuing commands.
    /// </summary>
    public interface IGameplayState
    {
        /// <summary>
        /// Creates an immutable snapshot for rebuilding views or UI.
        /// </summary>
        GameSessionSnapshot CreateSnapshot();

        /// <summary>
        /// True when history contains a board-changing action that is legal to undo.
        /// Winning actions lock undo immediately.
        /// </summary>
        bool CanUndo { get; }

        /// <summary>
        /// Raised after any session state change that should refresh UI or snapshot-driven presenters.
        /// </summary>
        event Action<GameSessionSnapshot> SnapshotChanged;

        /// <summary>
        /// Raised after a successful move has updated Core state.
        /// </summary>
        event Action<MoveResult> MoveResolved;

        /// <summary>
        /// Raised after a whole-action Undo restores Core state. Presentation reverses the recorded timeline.
        /// </summary>
        event Action<UndoResult> UndoResolved;

        /// <summary>
        /// Raised after restart reconstructs the authored initial state.
        /// </summary>
        event Action<GameSessionSnapshot> StateRestored;


        /// <summary>
        /// Raised after a blob is selected.
        /// </summary>
        event Action<BlobSelectionResult> BlobSelected;

        /// <summary>
        /// Raised when a drag begins, its tail cell changes, or it ends for any reason.
        /// </summary>
        event Action<DragPreview> DragPreviewChanged;
    }

    /// <summary>
    /// Application-level gameplay session. Owns source/target selection, move execution, restart,
    /// move count, and completion state while delegating rules to Core.
    /// </summary>
    public class GameSession : IGameplayCommands, IGameplayState, ITutorialSession
    {
        private readonly MoveResolver _resolver;
        private readonly List<UndoActionRecord> _history;
        private readonly LevelDefinition _level;
        private BoardState _board;
        private ObjectiveProgress _objectives;
        private string _selectedBlobId;
        private string _dragSourceId;
        private GridPosition _dragOrigin;
        private GridPosition _dragTail;
        private int _committedMoveCount;
        private BoardState _restartBoard;
        private ObjectiveProgress _restartObjectives;
        private HashSet<string> _allowedSourceBlobIds;
        public BoardState CurrentState => _board;
        /// <summary>
        /// Raised after a successful move or restart changes the session snapshot.
        /// </summary>
        public event Action<GameSessionSnapshot> SnapshotChanged;

        /// <inheritdoc />
        public event Action<MoveResult> MoveResolved;

        /// <inheritdoc />
        public event Action<UndoResult> UndoResolved;

        public static event Action<string> MessageLogged;

        /// <summary>
        /// Raised after restart so Presentation can rebuild from the restored snapshot.
        /// </summary>
        public event Action<GameSessionSnapshot> StateRestored;

        public event Action<BlobSelectionResult> BlobSelected;

        /// <inheritdoc />
        public event Action<DragPreview> DragPreviewChanged;

        /// <inheritdoc />
        public event Action<MoveResult> MoveRejected;

        public string LevelId => _level.Id;
        public int MoveCount => _committedMoveCount;
        public bool CanUndo => _history.Count > 0 && !IsComplete;
        public bool IsComplete { get; private set; }
        public string SelectedBlobId => _selectedBlobId;
        public ObjectiveProgress Objectives => _objectives;

        /// <summary>
        /// Creates a gameplay session from validated level data.
        /// </summary>
        public GameSession(LevelDefinition level, MoveResolver resolver = null)
        {
            _level = level ?? throw new ArgumentNullException(nameof(level));
            _resolver = resolver ?? new MoveResolver();
            _history = new List<UndoActionRecord>();
            _board = LevelFactory.CreateInitialBoard(level);
            _objectives = ObjectiveProgress.Start(level.Objective);
            IsComplete = ObjectiveEvaluator.IsComplete(_board, _objectives);
        }


        /// <inheritdoc />
        public BlobSelectionResult SelectBlobAt(GridPosition position)
        {
            return SelectAt(position, keepSourceOnFailure: true);
        }

        /// <summary>
        /// Tap targeting keeps the source raised after a player-facing rejection so the next tap can
        /// still aim from it; swipe release clears because the gesture already committed.
        /// </summary>
        private BlobSelectionResult SelectAt(GridPosition position, bool keepSourceOnFailure)
        {
            var blob = _board.GetBlobAt(position);
            if (blob == null)
            {
                EndDragPreview();
                _selectedBlobId = null;
                var cleared = BlobSelectionResult.Cleared();
                BlobSelected?.Invoke(cleared);
                return cleared;
            }

            if (string.IsNullOrEmpty(_selectedBlobId))
            {
                MoveFailureReason failure =
                    _resolver.ValidateSourceSelection(_board, blob.Id);
                if (failure == MoveFailureReason.None && !IsAllowedSource(blob.Id))
                    failure = MoveFailureReason.SourceCannotMove;

                if (failure != MoveFailureReason.None)
                {
                    var rejected = BlobSelectionResult.Rejected(failure);
                    BlobSelected?.Invoke(rejected);
                    return rejected;
                }

                _selectedBlobId = blob.Id;
                var selected = BlobSelectionResult.Selected(blob.Id, null);
                BlobSelected?.Invoke(selected);
                return selected;
            }
            if (_selectedBlobId == blob.Id)
            {
                EndDragPreview();
                _selectedBlobId = null;
                var cleared = BlobSelectionResult.Cleared();
                BlobSelected?.Invoke(cleared);
                return cleared;
            }

            var selectedBlob = _board.GetBlob(_selectedBlobId);
            var targetBlob = _board.GetBlob(blob.Id);
            var intent = new MoveIntent(selectedBlob, targetBlob);

            if (keepSourceOnFailure)
            {
                MoveResult preview = _resolver.Preview(_board, intent, _objectives);
                if (!preview.Succeeded)
                {
                    BlobSelected?.Invoke(BlobSelectionResult.Selected(selectedBlob.Id, null));
                    var rejected = BlobSelectionResult.Move(preview);
                    MoveRejected?.Invoke(preview);
                    return rejected;
                }

                ShowTapAim(selectedBlob.Id, selectedBlob.Position, targetBlob);
            }

            EndDragPreview();
            var result = ExecuteMove(intent);
            _selectedBlobId = null;
            var move = BlobSelectionResult.Move(result);
            BlobSelected?.Invoke(move);
            return move;
        }

        private void ShowTapAim(string sourceId, GridPosition origin, BlobState target)
        {
            _dragSourceId = sourceId;
            _dragOrigin = origin;
            _dragTail = target.Position;
            DragPreviewChanged?.Invoke(DragPreview.Active(
                sourceId, origin, target.Position, target.Id, DragVerdict.Valid, MoveFailureReason.None));
        }

        public BlobSelectionResult BeginDragAt(GridPosition position)
        {
            CancelDrag();
            BlobSelectionResult result = SelectBlobAt(position);
            if (!result.HasSelection)
                return result;

            _dragSourceId = result.SourceId;
            _dragOrigin = position;
            _dragTail = position;
            PublishDragPreview(null);
            return result;
        }

        public BlobSelectionResult UpdateDragAt(GridPosition position)
        {
            if (!IsDragActive())
            {
                if (_dragSourceId != null)
                    CancelDrag();
                return BlobSelectionResult.Cleared();
            }

            GridPosition tail = ResolveDragTail(position);
            string targetId = GetDragTargetId(tail);
            DragPreview preview = BuildDragPreview(targetId, tail);
            // Selection highlight stays on the Application result so presenters never need to
            // subscribe to drag-preview verdicts. Aim geometry/tint still uses DragPreview.
            var selected = BlobSelectionResult.Selected(
                _dragSourceId,
                targetId,
                targetSelected: preview.Verdict == DragVerdict.Valid);
            if (tail == _dragTail)
                return selected;

            _dragTail = tail;
            BlobSelected?.Invoke(selected);
            DragPreviewChanged?.Invoke(preview);
            return selected;
        }

        public BlobSelectionResult EndDragAt(GridPosition position)
        {
            if (!IsDragActive())
            {
                CancelDrag();
                return BlobSelectionResult.Cleared();
            }

            GridPosition tail = ResolveDragTail(position);
            if (tail == _dragOrigin)
            {
                // Releasing where the press began is a tap: hold the source and its indicator for a second tap.
                var held = BlobSelectionResult.Selected(_dragSourceId, null);
                BlobSelected?.Invoke(held);
                return held;
            }

            EndDragPreview();
            return SelectAt(tail, keepSourceOnFailure: false);
        }

        public void CancelDrag()
        {
            EndDragPreview();
            _selectedBlobId = null;
            BlobSelected?.Invoke(BlobSelectionResult.Cleared());
        }

        private bool IsDragActive()
        {
            return _dragSourceId != null &&
                _selectedBlobId == _dragSourceId &&
                _board.GetBlob(_dragSourceId) != null;
        }

        private void EndDragPreview()
        {
            if (_dragSourceId == null)
                return;

            _dragSourceId = null;
            DragPreviewChanged?.Invoke(DragPreview.Inactive);
        }

        /// <summary>
        /// Dry-runs the move the drag would commit on release so presentation can warn before the
        /// player lets go. Only player-facing rule failures become <see cref="DragVerdict.Invalid"/>.
        /// </summary>
        private void PublishDragPreview(string targetId)
        {
            DragPreviewChanged?.Invoke(BuildDragPreview(targetId, _dragTail));
        }

        private DragPreview BuildDragPreview(string targetId, GridPosition tail)
        {
            DragVerdict verdict = DragVerdict.Neutral;
            MoveFailureReason failure = MoveFailureReason.None;
            if (targetId != null)
            {
                MoveResult result = _resolver.Preview(
                    _board,
                    new MoveIntent(_board.GetBlob(_dragSourceId), _board.GetBlob(targetId)),
                    _objectives);
                failure = result.FailureReason;
                verdict = result.Succeeded
                    ? DragVerdict.Valid
                    : failure.IsPlayerFacing() ? DragVerdict.Invalid : DragVerdict.Neutral;
            }

            return DragPreview.Active(
                _dragSourceId, _dragOrigin, tail, targetId, verdict, failure);
        }

        /// <summary>
        /// Moves only travel along a row or column, so the pointer cell is projected onto the
        /// dominant drag axis, then clamped to what the source can reach: the board edge, the last
        /// cell before a gap, or a blocking blob such as a rock. Exact diagonals keep the current axis.
        /// </summary>
        private GridPosition ResolveDragTail(GridPosition pointer)
        {
            int dx = Math.Abs(pointer.X - _dragOrigin.X);
            int dy = Math.Abs(pointer.Y - _dragOrigin.Y);
            bool horizontal = dx != dy ? dx > dy : _dragTail.Y == _dragOrigin.Y;
            GridPosition aligned = horizontal
                ? new GridPosition(pointer.X, _dragOrigin.Y)
                : new GridPosition(_dragOrigin.X, pointer.Y);
            return _resolver.ClampToReach(_board, _dragOrigin, aligned);
        }

        private string GetDragTargetId(GridPosition tail)
        {
            return tail == _dragOrigin ? null : _board.GetBlobAt(tail)?.Id;
        }

        /// <summary>
        /// Executes an explicit move intent. This bypasses the selection state and is primarily used by tests
        /// or future input modes that already know source and target ids.
        /// </summary>
        public MoveResult ExecuteMove(MoveIntent intent)
        {
            BoardState previous = _board.Clone();
            ObjectiveProgress previousObjectives = _objectives;
            var result = _resolver.Resolve(_board, intent, _objectives);
            if (!result.Succeeded)
            {
                MoveRejected?.Invoke(result);
                return result;
            }
            _objectives = result.Objectives ?? _objectives;
            IsComplete = result.IsComplete;
            // Valid contact can produce feedback without changing the board.
            if (result.Effects.Count > 0)
            {
                _committedMoveCount++;
                _history.Add(new UndoActionRecord(
                    previous,
                    previousObjectives,
                    result,
                    BuildRestorationBlobs(previous, result)));
            }
            MoveResolved?.Invoke(result);
            SnapshotChanged?.Invoke(CreateSnapshot());
            return result;
        }

        /// <inheritdoc />
        public bool Undo()
        {
            if (!CanUndo)
                return false;

            EndDragPreview();
            UndoActionRecord record = _history[_history.Count - 1];
            _history.RemoveAt(_history.Count - 1);
            _committedMoveCount--;
            _board = record.PreviousBoard.Clone();
            _objectives = record.PreviousObjectives;
            _selectedBlobId = null;
            IsComplete = ObjectiveEvaluator.IsComplete(_board, _objectives);
            var snapshot = CreateSnapshot();
            UndoResolved?.Invoke(new UndoResult(
                record.ForwardResult,
                snapshot,
                record.RestorationBlobs));
            SnapshotChanged?.Invoke(snapshot);
            return true;
        }


        /// <inheritdoc />
        public void Restart()
        {
            RestoreInitialState();
        }

        /// <inheritdoc />
        public IReadOnlyList<string> ApplyStage(
            IReadOnlyList<BlobDefinition> blobs,
            IReadOnlyList<GridPosition> cells,
            IReadOnlyList<string> removeBlobIds = null)
        {
            EndDragPreview();
            _selectedBlobId = null;

            if (removeBlobIds != null)
            {
                foreach (string id in removeBlobIds)
                {
                    if (_board.GetBlob(id) != null)
                        _board.RemoveBlob(id);
                }
            }

            if (cells != null)
            {
                foreach (GridPosition cell in cells)
                    _board.EmptyPositions.Remove(cell);
            }

            var added = new List<string>();
            if (blobs != null)
            {
                foreach (BlobDefinition definition in blobs)
                {
                    if (_board.GetBlob(definition.Id) != null || _board.GetBlobAt(definition.Position) != null)
                    {
                        MessageLogged?.Invoke(
                            $"Tutorial reveal skipped '{definition.Id}': {definition.Position} is occupied.");
                        continue;
                    }

                    _board.AddBlob(LevelFactory.CreateBlob(definition));
                    added.Add(definition.Id);
                }
            }

            // A stage boundary is a fresh starting point: Undo cannot cross it and Restart returns here.
            _history.Clear();
            _committedMoveCount = 0;
            IsComplete = ObjectiveEvaluator.IsComplete(_board, _objectives);
            _restartBoard = _board.Clone();
            _restartObjectives = _objectives;

            BlobSelected?.Invoke(BlobSelectionResult.Cleared());
            SnapshotChanged?.Invoke(CreateSnapshot());
            return added;
        }

        /// <inheritdoc />
        public void SetAllowedSourceBlobIds(IReadOnlyList<string> blobIds)
        {
            if (blobIds == null || blobIds.Count == 0)
            {
                _allowedSourceBlobIds = null;
                return;
            }

            _allowedSourceBlobIds = new HashSet<string>(blobIds);
            // Drop a held selection that the new step no longer allows as a source.
            if (!string.IsNullOrEmpty(_selectedBlobId) && !IsAllowedSource(_selectedBlobId))
            {
                EndDragPreview();
                _selectedBlobId = null;
                BlobSelected?.Invoke(BlobSelectionResult.Cleared());
            }
        }

        private bool IsAllowedSource(string blobId)
        {
            return _allowedSourceBlobIds == null || _allowedSourceBlobIds.Contains(blobId);
        }

        /// <summary>
        /// Rebuilds board, objectives, history, and selection from the restart baseline (the latest
        /// tutorial stage, else the authored level) and announces the result through the same path
        /// Presentation already rebuilds from.
        /// </summary>
        private void RestoreInitialState()
        {
            EndDragPreview();
            _history.Clear();
            _committedMoveCount = 0;
            if (_restartBoard != null)
            {
                _board = _restartBoard.Clone();
                _objectives = _restartObjectives;
            }
            else
            {
                _board = LevelFactory.CreateInitialBoard(_level);
                _objectives = ObjectiveProgress.Start(_level.Objective);
            }
            _selectedBlobId = null;
            IsComplete = ObjectiveEvaluator.IsComplete(_board, _objectives);
            var snapshot = CreateSnapshot();
            StateRestored?.Invoke(snapshot);
            SnapshotChanged?.Invoke(snapshot);
        }

        /// <inheritdoc />
        public GameSessionSnapshot CreateSnapshot()
        {
            return new GameSessionSnapshot(
                _level.Id,
                _board.Clone(),
                MoveCount,
                IsComplete,
                CanUndo,
                _objectives);
        }

        private static IReadOnlyDictionary<string, BlobState> BuildRestorationBlobs(
            BoardState previous,
            MoveResult result)
        {
            var catalog = new Dictionary<string, BlobState>();
            foreach (BlobState blob in previous.Blobs)
                catalog[blob.Id] = blob;

            foreach (IBoardEffect effect in result.Effects)
            {
                switch (effect)
                {
                    case SpawnBlobEffect spawn:
                        catalog[spawn.Blob.Id] = spawn.Blob;
                        break;
                    case MergeEffect merge when merge.ConsumedBlob != null:
                        catalog[merge.ConsumedBlob.Id] = merge.ConsumedBlob;
                        break;
                    case MergeIntoNestEffect nest when nest.SourceBlob != null:
                        catalog[nest.SourceBlob.Id] = nest.SourceBlob;
                        break;
                    case RemoveBlobEffect remove when remove.Blob != null:
                        catalog[remove.Blob.Id] = remove.Blob;
                        break;
                    case GhostHauntEffect haunt when haunt.LandingBlob != null:
                        catalog[haunt.LandingBlob.Id] = haunt.LandingBlob;
                        break;
                    case GhostRestEffect rest:
                        if (rest.GhostBlob != null)
                            catalog[rest.GhostBlob.Id] = rest.GhostBlob;
                        if (rest.LandingBlob != null)
                            catalog[rest.LandingBlob.Id] = rest.LandingBlob;
                        break;
                }
            }

            return catalog;
        }

        private sealed class UndoActionRecord
        {
            public UndoActionRecord(
                BoardState previousBoard,
                ObjectiveProgress previousObjectives,
                MoveResult forwardResult,
                IReadOnlyDictionary<string, BlobState> restorationBlobs)
            {
                PreviousBoard = previousBoard;
                PreviousObjectives = previousObjectives;
                ForwardResult = forwardResult;
                RestorationBlobs = restorationBlobs;
            }

            public BoardState PreviousBoard { get; }
            public ObjectiveProgress PreviousObjectives { get; }
            public MoveResult ForwardResult { get; }
            public IReadOnlyDictionary<string, BlobState> RestorationBlobs { get; }
        }
    }
}
