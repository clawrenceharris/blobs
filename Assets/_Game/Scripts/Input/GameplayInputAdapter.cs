using Blobs.Application;
using UnityEngine;
using UnityEngine.InputSystem;
using Blobs.Core;
namespace Blobs.Input
{
    public sealed class GameplayInputAdapter : MonoBehaviour
    {
        private GameSession _session;
        [SerializeField] private InputActionReference pointAction;
        [SerializeField] private InputActionReference timelineSwipeAction;
        [SerializeField] private Camera boardCamera;
        [SerializeField, Min(0.01f)] private float cellSize = 1f;
        [SerializeField] private Vector2 boardOrigin;

        [Header("Timeline swipe tuning")]
        [SerializeField, Min(1f)] private float minimumSwipeDistancePixels = 48f;
        [SerializeField, Min(1f)] private float pixelsPerBeat = 96f;
        [SerializeField, Min(1f)] private float horizontalDominanceRatio = 1.5f;
        [SerializeField, Min(0f)] private float maximumSwipeDurationSeconds;
        [SerializeField, Min(0f)] private float tapMovementTolerancePixels = 16f;

        private InputAction _runtimePointAction;
        private InputAction _runtimeTimelineSwipeAction;
        private InputAction PointAction =>
            pointAction != null ? pointAction.action : _runtimePointAction;

        private InputAction TimelineSwipeAction =>
            timelineSwipeAction != null
                ? timelineSwipeAction.action
                : _runtimeTimelineSwipeAction;

        public void Initialize(
            GameSession session,
            float boardCellSize
)
        {
            Unsubscribe();
            _session = session;
            cellSize = boardCellSize;

        }

       

        private void OnDestroy()
        {
            Unsubscribe();
            _runtimePointAction?.Dispose();
            _runtimeTimelineSwipeAction?.Dispose();
        }

        private void Unsubscribe()
        {
           
            _session = null;
        }
    }

}
