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
        
        /// <summary>
        /// Guards against re-entrant merge calls while a merge animation is playing.
        /// Set to true when a merge is dispatched, cleared when animation completes.
        /// </summary>
        private bool _isMerging;
        
        private void Awake()
        {
            _feedback = FindFirstObjectByType<FeedbackPresenter>();
            _board = FindFirstObjectByType<BoardPresenter>();
            _mergeService = new MergeService(_board);
        }

        private void OnEnable()
        {
            InputService.BlobClicked += OnBlobClicked;
            InputService.EmptyClicked += OnEmptyClicked;
            InputService.UndoPressed += OnUndoPressed;
            BoardPresenter.OnMergeAnimationComplete += OnMergeAnimationComplete;
            BoardPresenter.OnMergeUndoComplete += OnMergeAnimationComplete;
        }

        private void OnDisable()
        {
            InputService.BlobClicked -= OnBlobClicked;
            InputService.EmptyClicked -= OnEmptyClicked;
            InputService.UndoPressed -= OnUndoPressed;
            BoardPresenter.OnMergeAnimationComplete -= OnMergeAnimationComplete;
            BoardPresenter.OnMergeUndoComplete -= OnMergeAnimationComplete;
        }

        private void OnMergeAnimationComplete(MergeAction _)
        {
            _isMerging = false;
        }

        private void OnBlobClicked(BlobView view)
        {
            // Block all interaction while a merge animation is in flight
            if (_isMerging) return;

            if (view == null) return;
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

        private void OnUndoPressed()
        {
            if (_isMerging) return;
            _isMerging = true;
            Deselect();
            MergeInvoker.UndoMerge();
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

            // Mark merge in-progress BEFORE executing (prevents re-entry)
            _isMerging = true;
            
            // Clear selection without triggering deselect animation
            // (the merge animation will handle the visual transition)
            ClearSelectionSilent();
            
            var action = new MergeAction(result.Plan, _board);
            MergeInvoker.ExecuteMerge(action);
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

        /// <summary>
        /// Clears the selection ID without triggering the deselect animation.
        /// Used before merge so the deselect animation doesn't fight the merge animation.
        /// </summary>
        private void ClearSelectionSilent()
        {
            _selectedId = null;
        }

        private void OnEmptyClicked()
        {
            if (_isMerging) return;
            Deselect();
        }

    }
}