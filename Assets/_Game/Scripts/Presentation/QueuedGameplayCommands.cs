using Blobs.Application;
using Blobs.Core;
using Cysharp.Threading.Tasks;

namespace Blobs.Presentation
{
    public sealed class QueuedGameplayCommands : IGameplayCommands
    {
        private readonly IGameplayCommands _session;
        private readonly IGameplayState _state;
        private readonly BoardPresenter _boardPresenter;
        private int _pendingUndoCount;
        private bool _draining;

        public QueuedGameplayCommands(IGameplayCommands session, BoardPresenter boardPresenter)
        {
            _session = session ?? throw new System.ArgumentNullException(nameof(session));
            _state = session as IGameplayState;
            _boardPresenter = boardPresenter ?? throw new System.ArgumentNullException(nameof(boardPresenter));
        }

        public bool Undo()
        {
            if (_state != null && !_state.CanUndo)
                return false;

            _pendingUndoCount++;
            DrainUndoQueue();
            return true;
        }

        public BlobSelectionResult SelectBlobAt(GridPosition position)
        {
            if (_boardPresenter.IsPresenting)
                return BlobSelectionResult.Cleared();

            return _session.SelectBlobAt(position);
        }

        public void Restart()
        {
            _pendingUndoCount = 0;
            _session.Restart();
        }

        private async void DrainUndoQueue()
        {
            if (_draining)
                return;

            _draining = true;
            try
            {
                while (_pendingUndoCount > 0)
                {
                    await _boardPresenter.WaitUntilIdleAsync();
                    if (_pendingUndoCount <= 0)
                        break;

                    _pendingUndoCount--;
                    if (!_session.Undo())
                    {
                        _pendingUndoCount = 0;
                        break;
                    }

                    await UniTask.Yield();
                    await _boardPresenter.WaitUntilIdleAsync();
                }
            }
            finally
            {
                _draining = false;
            }
        }
    }
}
