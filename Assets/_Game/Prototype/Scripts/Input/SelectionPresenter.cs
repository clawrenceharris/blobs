using UnityEngine;

namespace Blobs.Input
{
    public class SelectionPresenter : MonoBehaviour
    {
        private FeedbackPresenter _feedback;
        private IBoardPresenter _board;
        private MoveResolver _moveResolver;
        private string _selectedId;

        private void Awake()
        {
            _feedback = FindFirstObjectByType<FeedbackPresenter>();
            _board = FindFirstObjectByType<BoardPresenter>();
            _moveResolver = new MoveResolver(null);
        }

        private void OnEnable()
        {
            InputService.BlobClicked += OnBlobClicked;
            InputService.EmptyClicked += OnEmptyClicked;
            InputService.UndoPressed += OnUndoPressed;
        }

        private void OnDisable()
        {
            InputService.BlobClicked -= OnBlobClicked;
            InputService.EmptyClicked -= OnEmptyClicked;
            InputService.UndoPressed -= OnUndoPressed;
        }

        private void OnBlobClicked(BlobView clicked)
        {

            if (clicked == null) return;
            if (!clicked.Enabled) return;
            if(!_board.BlobExists(clicked.ID)) return;

            if (_selectedId == null)
            {
                Select(clicked.ID);
                return;
            }

            if (_selectedId == clicked.ID)
            {
                Deselect();
                return;
            }

            TryMerge(_selectedId, clicked.ID);
        }

        private void OnUndoPressed()
        {
            MergeInvoker.UndoMerge();
            Deselect();
        }

        private void TryMerge(string sourceId, string targetId)
        {
            if (_board is not BoardPresenter board)
                return;
            var source = _board.GetBlob(sourceId);
            var target = _board.GetBlob(targetId);
            var direction = target.Model.GridPosition - source.Model.GridPosition;
            var intent = MoveIntent.Merge(sourceId, targetId, direction);
            var result = _moveResolver.Resolve(intent, board);

            if (!result.IsValid)
            {
                var failContext = new MoveFailContext(result.MoveFailReason, intent);
                _feedback.ShowInvalid(failContext);
                Deselect();
                return;
            }

            var command = new MergeCommand(result, board);
            MergeInvoker.Execute(command);
            Deselect();
        }

        private void Select(string id)
        {
            var blob = _board.GetBlob(id);
            if (blob == null) return;
            if (!blob.Model.Type.CanInitiateMerge()) return;

            _selectedId = id;
            blob.Select();
        }

        private void Deselect()
        {
            if (_selectedId == null) return;
            _board.GetBlob(_selectedId)?.Deselect();
            _selectedId = null;
        }

        private void OnEmptyClicked() => Deselect();
    }
}
