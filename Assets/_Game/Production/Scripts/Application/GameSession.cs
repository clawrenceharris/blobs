using System;
using System.Collections.Generic;
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
    }

    /// <summary>
    /// Application-level gameplay session. Owns source/target selection, move execution, restart,
    /// move count, and completion state while delegating rules to Core.
    /// </summary>
    public class GameSession : IGameplayCommands, IGameplayState
    {
        private readonly MoveResolver _resolver;
        private readonly List<UndoActionRecord> _history;
        private readonly LevelDefinition _level;
        private BoardState _board;
        private string _selectedBlobId;
        private int _committedMoveCount;
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

        public string LevelId => _level.Id;
        public int MoveCount => _committedMoveCount;
        public bool CanUndo => _history.Count > 0 && !IsComplete;
        public bool IsComplete { get; private set; }
        public string SelectedBlobId => _selectedBlobId;

        /// <summary>
        /// Creates a gameplay session from validated level data.
        /// </summary>
        public GameSession(LevelDefinition level, MoveResolver resolver = null)
        {
            _level = level ?? throw new ArgumentNullException(nameof(level));
            _resolver = resolver ?? new MoveResolver();
            _history = new List<UndoActionRecord>();
            _board = LevelFactory.CreateInitialBoard(level);
            IsComplete = ObjectiveEvaluator.IsComplete(_board, _level.Objective);
        }
        public static void LogMessage(string message)
        {
            MessageLogged?.Invoke(message);
        }

        /// <inheritdoc />
        public BlobSelectionResult SelectBlobAt(GridPosition position)
        {
            var blob = _board.GetBlobAt(position);
            if (blob == null)
            {
                _selectedBlobId = null;
                var cleared = BlobSelectionResult.Cleared();
                BlobSelected?.Invoke(cleared);
                return cleared;
            }

            if (string.IsNullOrEmpty(_selectedBlobId))
            {
                MoveFailureReason failure =
                    _resolver.ValidateSourceSelection(_board, blob.Id);

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
                _selectedBlobId = null;
                var cleared = BlobSelectionResult.Cleared();
                BlobSelected?.Invoke(cleared);
                return cleared;
            }

            var selectedBlob = _board.GetBlob(_selectedBlobId);
            var targetBlob = _board.GetBlob(blob.Id);


            var result = ExecuteMove(new MoveIntent(selectedBlob, targetBlob));
            _selectedBlobId = null;
            var move = BlobSelectionResult.Move(result);
            BlobSelected?.Invoke(move);
            return move;
        }

        /// <summary>
        /// Executes an explicit move intent. This bypasses the selection state and is primarily used by tests
        /// or future input modes that already know source and target ids.
        /// </summary>
        public MoveResult ExecuteMove(MoveIntent intent)
        {
            BoardState previous = _board.Clone();
            var result = _resolver.Resolve(_board, intent, _level.Objective);
            if (!result.Succeeded)
                return result;
            IsComplete = result.IsComplete;
            // Valid contact can produce feedback without changing the board.
            if (result.Effects.Count > 0)
            {
                _committedMoveCount++;
                _history.Add(new UndoActionRecord(
                    previous,
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

            UndoActionRecord record = _history[_history.Count - 1];
            _history.RemoveAt(_history.Count - 1);
            _board = record.PreviousBoard.Clone();
            _selectedBlobId = null;
            IsComplete = ObjectiveEvaluator.IsComplete(_board, _level.Objective);
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
            _history.Clear();
            _committedMoveCount = 0;
            _board = LevelFactory.CreateInitialBoard(_level);
            _selectedBlobId = null;
            IsComplete = ObjectiveEvaluator.IsComplete(_board, _level.Objective);
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
                CanUndo);
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
                MoveResult forwardResult,
                IReadOnlyDictionary<string, BlobState> restorationBlobs)
            {
                PreviousBoard = previousBoard;
                ForwardResult = forwardResult;
                RestorationBlobs = restorationBlobs;
            }

            public BoardState PreviousBoard { get; }
            public MoveResult ForwardResult { get; }
            public IReadOnlyDictionary<string, BlobState> RestorationBlobs { get; }
        }
    }
}
