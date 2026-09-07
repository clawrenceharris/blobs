using System.Collections;
using UnityEngine;
using Blobs.Core;
namespace Blobs.Presentation
{

    /// <summary>
    /// Positions and sizes an orthographic camera so the current board fits in view.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraPresenter : MonoBehaviour
    {
        [SerializeField] private float padding;
        private Camera cam;
        void Awake()
        {
            cam = GetComponent<Camera>();
        }






        /// <summary>
        /// Centers the camera on the board and adjusts orthographic size using board dimensions.
        /// </summary>
        public void FitCameraToBoard(int width, int height, float cellSize)
        {

            float boardWidth = (width - 1) * cellSize;
            float boardHeight = (height - 1) * cellSize;

            float requiredForHeight = boardHeight * cellSize + padding;
            float requiredForWidth = (boardWidth * cellSize + padding) / cam.aspect;

            cam.orthographicSize = Mathf.Max(requiredForHeight, requiredForWidth);

        }


    }
}
