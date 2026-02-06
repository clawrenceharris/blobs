using Blobs.Core.Merge;
using UnityEngine;

namespace Blobs.Input
{

    public class SelectionPresenter : MonoBehaviour
    {
        private FeedbackPresenter _feedback;
        private TutorialPresenter _tutorial;
        
        private IMergeService _mergeService;  
        private IBoardPresenter _board;
        private string _selectedId;
        
        private void Awake()
        {
            _feedback = FindFirstObjectByType<FeedbackPresenter>();
            _board = FindFirstObjectByType<BoardPresenter>();
            _mergeService = new MergeService(_board);
            _tutorial = FindFirstObjectByType<TutorialPresenter>();
        }

        private void OnEnable()
        {
            InputService.BlobClicked += OnBlobClicked;
            InputService.EmptyClicked += OnEmptyClicked;
            InputService.UndoPressed += () => MergeInvoker.UndoMerge();
        }

        private void OnDisable()
        {
            InputService.BlobClicked -= OnBlobClicked;
            InputService.EmptyClicked -= OnEmptyClicked;
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
            var source = _board.GetBlob(sourceId)?.Model;
            

            if (!result.Ok)
            {
                _feedback.ShowInvalid(result.FailReason, sourceId);
                Deselect();
                return;
            }
            MergeInvoker.ExecuteMerge(result.Plan, _board);
            Deselect();
        }

        private void Select(string id)
        {
            var blob = _board.GetBlob(id);
            
            if(blob == null) return;
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