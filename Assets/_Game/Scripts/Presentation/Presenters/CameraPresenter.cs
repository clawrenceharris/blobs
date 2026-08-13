using System.Collections;
using UnityEngine;
using Blobs.Core;
namespace Blobs.Presentation
{
    
    [RequireComponent(typeof(Camera))]
    public class CameraPresenter : MonoBehaviour
    {
        [SerializeField] private float aspectRatio;
        [SerializeField] private float padding;
        private Camera cam;
        void Awake()
        {
            cam = GetComponent<Camera>();
        }
        

    
    


        public void FitCameraToBoard(BoardState boardState, float cellSize)
        {
        
            float x = (boardState.Width - 1) * cellSize;
            float y = (boardState.Height - 1) * cellSize;
            Vector3 tempPosition = new(x / 2, y / 2, -1);

            transform.position = tempPosition;
            if (boardState.Width >= boardState.Height)
            {
                cam.orthographicSize = (x / 2 + padding) / aspectRatio;
            }
            else
            {
                cam.orthographicSize = y / 2 + padding;
            }

        }


    }
}