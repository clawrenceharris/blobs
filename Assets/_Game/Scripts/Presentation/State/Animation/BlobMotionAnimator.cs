using Blobs.Application;
using Blobs.Content;
using UnityEngine;

namespace Blobs.Presentation
{

    public enum BlobAnimationState
    {
        Idle,
        Selected,
        Moving,
        Merging
    }

    [RequireComponent(typeof(BlobView))]
    public class BlobMotionAnimator : MonoBehaviour
    {
        [SerializeField] private BlobAnimationSettingsAsset _blobAnimationSettings;
        private IGameplayState _state;
        private AnimationStateContext _context;

        private BlobView _blobView;
        private bool _isSubscribed;

        private IState<AnimationStateContext> _currentState;
        public BlobAnimationState? CurrentState => _currentState?.State;


        public void Configure(IGameplayState state)
        {
            Unsubscribe();
            _state = state;
            _blobView = GetComponent<BlobView>();
            Transform visualRoot = _blobView.VisualRoot != null
                ? _blobView.VisualRoot
                : transform;
            _context = new AnimationStateContext()
            {
                BlobView = _blobView,
                BlobAnimationSettings = _blobAnimationSettings,
                BlobAnimator = this,
                BaseScale = visualRoot.localScale,
            };
            Subscribe();
            SetIdle();
        }



        private void HandleBlobSelected(BlobSelectionResult result)
        {
            if (result.IsSelected(_blobView.BlobId) && _currentState.State != BlobAnimationState.Selected)
            {
                SetSelected();
            }
            if (!result.IsSelected(_blobView.BlobId) && _currentState.State != BlobAnimationState.Idle)
            {
                SetIdle();
            }
            if (result.IsSelected(_blobView.BlobId) && result.MoveAttempted && _currentState.State != BlobAnimationState.Idle)
            {
                SetIdle();
            }

        }


        private void Subscribe()
        {
            if (_state == null || _isSubscribed)
                return;

            _state.BlobSelected += HandleBlobSelected;
            _isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (_state != null && _isSubscribed)
                _state.BlobSelected -= HandleBlobSelected;
            _isSubscribed = false;
        }

        private void OnEnable()
        {
            Subscribe();

            if (_context != null && _currentState == null)
                SetIdle();
        }

        private void OnDisable()
        {
            Unsubscribe();
            _currentState?.Exit(_context);
            _currentState = null;
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        public virtual void SetIdle()
        {
            SetState(new BlobIdleState());
        }
        public virtual void SetSelected()
        {
            SetState(new BlobSelectedState());
        }

        public virtual void SetMoving()
        {
            SetState(new BlobMovingState());
        }

        public virtual void SetMerging()
        {
            SetState(new BlobMergingState());
        }

        public void SetState(IState<AnimationStateContext> state)
        {
            if (state == null || _context == null)
                return;

            if (_currentState != null && _currentState.State == state.State)
                return;

            _currentState?.Exit(_context);
            _currentState = state;
            _currentState.Enter(_context);
        }


    }


}
