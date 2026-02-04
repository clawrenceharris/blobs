using Blobs.Commands;
using Blobs.Core.Merge;
using UnityEngine;

namespace Blobs.Input
{

    public class SelectionPresenter : MonoBehaviour
    {
        private FeedbackPresenter _feedback;

        private IMergeService _mergeService;  
        private IBoardPresenter _board;
        private string _selectedId;
        
        private void Awake()
        {
            _feedback = FindFirstObjectByType<FeedbackPresenter>();
            _board = FindFirstObjectByType<BoardPresenter>();
            _mergeService = new MergeService(_board);
        }

        private void OnEnable()
        {
            InputRouter.BlobClicked += OnBlobClicked;
            InputRouter.EmptyClicked += OnEmptyClicked;
            InputRouter.UndoPressed += () => MergeInvoker.UndoMerge();
        }

        private void OnDisable()
        {
            InputRouter.BlobClicked -= OnBlobClicked;
            InputRouter.EmptyClicked -= OnEmptyClicked;
        }

        private void OnBlobClicked(BlobView view)
        {
            var clicked = view.Model;
            if (clicked == null) return;
            if (!clicked.Enabled) return;
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

            // attempt merge
            TryMerge(_selectedId, clicked.ID);
        }

        

        private void TryMerge(string sourceId, string targetId)
        {
            var result = _mergeService.TryCreateMergeCommand(sourceId, targetId);
            if (!result.Ok)
            {
                _feedback.ShowInvalid(result.FailReason, sourceId);
                Deselect();
                return;
            }
            MergeInvoker.ExecuteMerge(result.Plan, _board.Model);
            Deselect();
        }

        private void Select(string id)
        {
            var blob =_board.GetBlobById(id);
            if(blob == null) return;
            
            _selectedId = id;
            blob.Select();
        }

        private void Deselect()
        {
            if(_selectedId == null) return; 
            _board.GetBlobById(_selectedId)?.Deselect();
            _selectedId = null;
        }

        private void OnEmptyClicked() => Deselect();

    }
}