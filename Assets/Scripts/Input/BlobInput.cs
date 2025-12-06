using System;
using UnityEngine;

public class BlobInput : MonoBehaviour
{



    public static bool InputEnabled;
    public static Action<Blob> OnBlobSelected;
    private BoardModel _board;

    public static Action<Blob> OnBlobDeselected;
    
    void Start()
    {
        BoardModel.OnBoardCreated += HandleBoardCreated;
        EnableInput();
    }
    private void HandleBoardCreated(BoardModel board)
    {
        _board = board;
    }
    
    private Blob GetBlobAt(Vector3 worldPoint)
    {
        int x = (int)worldPoint.x;
        int y = (int)worldPoint.y;

        return _board.GetBlobAt(x, y);
    }
    public static void EnableInput()
    {
        InputEnabled = true;
    }

    public static void DisableInput()
    {
        InputEnabled = false;
    }
    private void CheckInput()
    {
        if (!InputEnabled) return;

        if (Input.GetMouseButtonUp(0))
        {

            Vector3 worldPos = (Camera.main.ScreenToWorldPoint(Input.mousePosition) / BoardPresenter.TileSize) + Vector3.one * 0.5f;
            Blob blobAtWorldPos = GetBlobAt(worldPos);

            if (blobAtWorldPos != null && blobAtWorldPos.Enabled)
            {
                OnBlobSelected?.Invoke(blobAtWorldPos);
            }
            

        }

    }

    void Update()
    {
        CheckInput();

    }

   
}
