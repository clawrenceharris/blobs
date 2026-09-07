using UnityEngine;

namespace Blobs.Presentation
{
    /// <summary>
    /// Positions and sizes an orthographic camera so the current board fits in view.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraPresenter : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float padding;

        private Camera _camera;

        /// <summary>
        /// Centers the camera on the outermost cell centers and selects the larger vertical size
        /// required by the board's horizontal and vertical extents.
        /// </summary>
        public void FitCameraToBoard(int width, int height, float cellSize)
        {
            _camera ??= GetComponent<Camera>();

            float boardWidth = (width - 1) * cellSize;
            float boardHeight = (height - 1) * cellSize;
            transform.position = new Vector3(
                boardWidth * 0.5f,
                boardHeight * 0.5f,
                transform.position.z);

            float requiredForHeight = boardHeight * 0.5f + padding;
            float requiredForWidth = (boardWidth * 0.5f + padding) / _camera.aspect;
            _camera.orthographicSize = Mathf.Max(requiredForHeight, requiredForWidth);
        }

        private void Awake()
        {
            _camera = GetComponent<Camera>();
        }
    }
}
