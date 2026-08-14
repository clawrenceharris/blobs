using System;
using System.Collections.Generic;
using Blobs.Core;

namespace Blobs.Application
{
    public sealed class GameSession
    {
        private readonly MoveResolver _resolver;
        private readonly List<ResolvedMoveCommand> _history;
        private readonly LevelDefinition _level;
        private BoardState _board;
        private string _selectedBlobId;
        public BoardState CurrentState => _board;

        public GameSession(LevelDefinition level, MoveResolver resolver = null)
        {
            _level = level ?? throw new ArgumentNullException(nameof(level));
            _resolver = resolver ?? new MoveResolver();
            _history = new List<ResolvedMoveCommand>();
            _board = LevelFactory.CreateInitialBoard(level);
            IsComplete = ObjectiveEvaluator.IsComplete(_board);
        }

        public string LevelId => _level.Id;
        public int MoveCount => _history.Count;
        public bool CanUndo => _history.Count > 0;
        public bool IsComplete { get; private set; }
        public string SelectedBlobId => _selectedBlobId;

        public BlobSelectionResult SelectBlob(GridPosition position)
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

            _history.Add(new ResolvedMoveCommand(intent, result.InverseEffects));
            IsComplete = result.IsComplete;
            return result;
        }

        public bool Undo()
        {
            return UndoLastMove().Succeeded;
        }

        public UndoResult UndoLastMove()
        {
            if (_history.Count == 0)
                return UndoResult.Failed(IsComplete);

            var index = _history.Count - 1;
            var command = _history[index];
            _history.RemoveAt(index);
            _resolver.ApplyEffects(_board, command.InverseEffects);
            _selectedBlobId = null;
            IsComplete = ObjectiveEvaluator.IsComplete(_board);
            return new UndoResult(true, command.InverseEffects, IsComplete);
        }

        public void Restart()
        {
            _history.Clear();
            _board = LevelFactory.CreateInitialBoard(_level);
            _selectedBlobId = null;
            IsComplete = ObjectiveEvaluator.IsComplete(_board);
        }

        public GameSessionSnapshot CreateSnapshot()
        {
            return new GameSessionSnapshot(
                _level.Id,
                new List<BlobState>(_board.Blobs),
                new List<TileState>(_board.Tiles),
                MoveCount,
                CanUndo,
                IsComplete);
        }

        private sealed class ResolvedMoveCommand
        {
            public ResolvedMoveCommand(MoveIntent intent, IReadOnlyList<IBoardEffect> inverseEffects)
            {
                Intent = intent;
                InverseEffects = inverseEffects;
            }

            public MoveIntent Intent { get; }
            public IReadOnlyList<IBoardEffect> InverseEffects { get; }
        }
    }
}
