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

    /// <summary>
    /// Owns the animation state machine for one blob view. Selection routing is handled centrally.
    /// </summary>
    [RequireComponent(typeof(BlobView))]
    public class BlobMotionAnimator : MonoBehaviour
    {
        [SerializeField] private BlobAnimationSettingsAsset _blobAnimationSettings;
        private AnimationStateContext _context;
        private IState<AnimationStateContext> _currentState;

        public BlobAnimationState? CurrentState => _currentState?.State;

        /// <summary>Initializes animation state from the composed blob view.</summary>
        public void Configure()
        {
            _currentState?.Exit(_context);
            BlobView blobView = GetComponent<BlobView>();
            Transform visualRoot = blobView.VisualRoot != null
                ? blobView.VisualRoot
                : transform;
            _context = new AnimationStateContext()
            {
                BlobView = blobView,
                BlobAnimationSettings = _blobAnimationSettings,
                BlobAnimator = this,
                BaseScale = visualRoot.localScale,
            };
            _currentState = null;
            SetIdle();
        }

        private void OnEnable()
        {
            if (_context != null && _currentState == null)
                SetIdle();
        }

        private void OnDisable()
        {
            _currentState?.Exit(_context);
            _currentState = null;
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

        /// <summary>
        /// Updates the resting scale used by idle, selection, movement, and merge states.
        /// Size presentation calls this after its one-shot scale tween has settled so later
        /// state transitions cannot restore a stale pre-growth scale.
        /// </summary>
        public void SetBaseScale(Vector3 baseScale)
        {
            if (_context == null)
                return;

            _context.BaseScale = baseScale;
            if (_currentState == null)
                return;

            IState<AnimationStateContext> currentState = _currentState;
            currentState.Exit(_context);
            _currentState = null;
            SetState(currentState);
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
