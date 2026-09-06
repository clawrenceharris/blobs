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
        /// Raised after any session state change that should refresh UI or snapshot-driven presenters.
        /// </summary>
        event Action<GameSessionSnapshot> SnapshotChanged;

        /// <summary>
        /// Raised after a successful move has updated Core state.
        /// </summary>
        event Action<MoveResult> MoveResolved;

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
        private readonly List<ResolvedMoveCommand> _history;
        private readonly LevelDefinition _level;
        private BoardState _board;
        private string _selectedBlobId;
        public BoardState CurrentState => _board;
        /// <summary>
        /// Raised after a successful move or restart changes the session snapshot.
        /// </summary>
        public event Action<GameSessionSnapshot> SnapshotChanged;

        /// <inheritdoc />
        public event Action<MoveResult> MoveResolved;

        public static event Action<string> MessageLogged;

        /// <summary>
        /// Raised after restart so Presentation can rebuild from the restored snapshot.
        /// </summary>
        public event Action<GameSessionSnapshot> StateRestored;

        public event Action<BlobSelectionResult> BlobSelected;

        public string LevelId => _level.Id;
        public int MoveCount => _history.Count;
        public bool IsComplete { get; private set; }
        public string SelectedBlobId => _selectedBlobId;

        /// <summary>
        /// Creates a gameplay session from validated level data.
        /// </summary>
        public GameSession(LevelDefinition level, MoveResolver resolver = null)
        {
            _level = level ?? throw new ArgumentNullException(nameof(level));
            _resolver = resolver ?? new MoveResolver();
            _history = new List<ResolvedMoveCommand>();
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
            var result = _resolver.Resolve(_board, intent, _level.Objective);
            if (!result.Succeeded)
                return result;
            IsComplete = result.IsComplete;
            _history.Add(new ResolvedMoveCommand(intent));
            MoveResolved?.Invoke(result);
            SnapshotChanged?.Invoke(CreateSnapshot());
            return result;
        }


        /// <inheritdoc />
        public void Restart()
        {
            _history.Clear();
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
                IsComplete);
        }

        private sealed class ResolvedMoveCommand
        {
            public ResolvedMoveCommand(MoveIntent intent)
            {
                Intent = intent;
            }

            public MoveIntent Intent { get; }
        }
    }
}
