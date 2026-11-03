using System;
using Blobs.Application;

namespace Blobs.Presentation
{
    /// <summary>
    /// Owns the single gameplay-selection subscription and updates only views whose selection changed.
    /// </summary>
    internal sealed class BlobSelectionPresenter : IDisposable
    {
        private readonly BlobPresenter _blobs;
        private readonly IGameplayState _state;
        private string _selectedSourceId;
        private string _selectedTargetId;

        public BlobSelectionPresenter(BlobPresenter blobs, IGameplayState state)
        {
            _blobs = blobs ?? throw new ArgumentNullException(nameof(blobs));
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _state.BlobSelected += Present;
        }

        public void Present(BlobSelectionResult result)
        {
            string nextSourceId = result != null && !result.MoveAttempted
                ? result.SourceId
                : null;
            // Only TargetSelected aims pulse. Aimed-but-illegal targets stay idle; Application
            // already decided that on the selection result so this presenter stays verdict-free.
            string nextTargetId = result != null &&
                !result.MoveAttempted &&
                result.TargetSelected
                ? result.TargetId
                : null;

            SetIdleIfNoLongerSelected(_selectedSourceId, nextSourceId, nextTargetId);
            SetIdleIfNoLongerSelected(_selectedTargetId, nextSourceId, nextTargetId);
            SetPickedUp(nextSourceId);
            if (nextTargetId != nextSourceId)
                SetSelected(nextTargetId);

            _selectedSourceId = nextSourceId;
            _selectedTargetId = nextTargetId;
        }

        /// <summary>
        /// Clears tracked selection when authoritative views are rebuilt without a selection snapshot.
        /// </summary>
        public void ClearSelection()
        {
            SetIdle(_selectedSourceId);
            if (_selectedTargetId != _selectedSourceId)
                SetIdle(_selectedTargetId);

            _selectedSourceId = null;
            _selectedTargetId = null;
        }

        public void Dispose()
        {
            _state.BlobSelected -= Present;
            ClearSelection();
        }

        private void SetIdleIfNoLongerSelected(
            string blobId,
            string nextSourceId,
            string nextTargetId)
        {
            if (!string.IsNullOrEmpty(blobId) &&
                blobId != nextSourceId &&
                blobId != nextTargetId)
            {
                SetIdle(blobId);
            }
        }

        private void SetIdle(string blobId)
        {
            if (_blobs.TryGetView(blobId, out BlobView view) &&
                view.BlobMotionAnimator != null &&
                view.BlobMotionAnimator.CurrentState != BlobAnimationState.Idle)
            {
                view.BlobMotionAnimator.SetIdle();
            }
        }

        private void SetPickedUp(string blobId)
        {
            if (_blobs.TryGetView(blobId, out BlobView view) &&
                view.BlobMotionAnimator != null &&
                view.BlobMotionAnimator.CurrentState != BlobAnimationState.PickedUp)
            {
                view.BlobMotionAnimator.SetPickedUp();
            }
        }

        private void SetSelected(string blobId)
        {
            if (_blobs.TryGetView(blobId, out BlobView view) &&
                view.BlobMotionAnimator != null &&
                view.BlobMotionAnimator.CurrentState != BlobAnimationState.Selected)
            {
                view.BlobMotionAnimator.SetSelected();
            }
        }
    }
}
