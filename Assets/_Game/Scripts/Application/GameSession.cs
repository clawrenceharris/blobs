using System;
using System.Collections.Generic;
using Blobs.Core;

namespace Blobs.Application
{
    public interface IGameplayCommands
    {
        BlobSelectionResult SelectBlobAt(GridPosition position);
        void Restart();
    }

    public interface IGameplayState
    {
        GameSessionSnapshot CreateSnapshot();
        event Action<MoveResult> MoveResolved;
        event Action<GameSessionSnapshot> StateRestored;
    }
    public sealed class GameSession : IGameplayCommands, IGameplayState
    {
        private readonly MoveResolver _resolver;
        private readonly List<ResolvedMoveCommand> _history;
        private readonly LevelDefinition _level;
        private BoardState _board;
        private string _selectedBlobId;
        public BoardState CurrentState => _board;
        public event Action<MoveResult> MoveResolved;
        public event Action<GameSessionSnapshot> StateRestored;

        public string LevelId => _level.Id;
        public int MoveCount => _history.Count;
        public bool IsComplete { get; private set; }
        public string SelectedBlobId => _selectedBlobId;

        public GameSession(LevelDefinition level, MoveResolver resolver = null)
        {
            _level = level ?? throw new ArgumentNullException(nameof(level));
            _resolver = resolver ?? new MoveResolver();
            _history = new List<ResolvedMoveCommand>();
            _board = LevelFactory.CreateInitialBoard(level);
            IsComplete = ObjectiveEvaluator.IsComplete(_board);
        }

        public BlobSelectionResult SelectBlobAt(GridPosition position)
        {
            var blob = _board.GetBlobAt(position);
            if (blob == null)
            {
                _selectedBlobId = null;
                return BlobSelectionResult.Cleared();
            }

            if (string.IsNullOrEmpty(_selectedBlobId))
            {
                _selectedBlobId = blob.Id;
                return BlobSelectionResult.Selected(blob.Id);
            }

            if (_selectedBlobId == blob.Id)
            {
                _selectedBlobId = null;
                return BlobSelectionResult.Cleared();
            }


            var result = ExecuteMove(new MoveIntent(_selectedBlobId, blob.Id));
            _selectedBlobId = null;
            return BlobSelectionResult.Move(result);
        }

        public MoveResult ExecuteMove(MoveIntent intent)
        {
            var result = _resolver.Resolve(_board, intent);
            if (!result.Succeeded)
                return result;
            IsComplete = result.IsComplete;
            _history.Add(new ResolvedMoveCommand(intent));
            MoveResolved?.Invoke(result);
            return result;
        }


        public void Restart()
        {
            _history.Clear();
            _board = LevelFactory.CreateInitialBoard(_level);
            _selectedBlobId = null;
            IsComplete = ObjectiveEvaluator.IsComplete(_board);
            StateRestored?.Invoke(CreateSnapshot());
        }

        public GameSessionSnapshot CreateSnapshot()
        {
            return new GameSessionSnapshot(
                _level.Id,
                new List<BlobState>(_board.Blobs),
                new List<TileState>(_board.Tiles),
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
