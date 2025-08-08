using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlobInput : MonoBehaviour
{
   

    private BlobView _blob;

    public static bool InputEnabled;
    public  Action<Blob> OnBlobSelected;
    private static BoardPresenter board;

    public Action<Blob> OnBlobDeselected;
    private void Awake() {

        _blob =  GetComponent<BlobView>();
        board = FindFirstObjectByType<BoardPresenter>();
    }
    private bool IsBlobSelected(Vector3 worldPoint)
    {
        int x = (int)worldPoint.x;
        int y = (int)worldPoint.y;
        return x == _blob.Model.GridPosition.x && y == _blob.Model.GridPosition.y;

        
    }
    private static bool GetBlobAtWorldPoint(Vector3 worldPoint)
    {
        return board.BoardLogic.GetBlobAt((int)worldPoint.x, (int)worldPoint.y) != null;
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
            if (!GetBlobAtWorldPoint(worldPos))
            {
                OnBlobDeselected?.Invoke(_blob.Model);
            }

            else if (IsBlobSelected(worldPos))
            {
                OnBlobSelected?.Invoke(_blob.Model);
            }

        }
        
        // if (Input.GetMouseButtonUp(0))
        // {
        //     worldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        //     worldPoint = new Vector3(worldPoint.x + offsetX, worldPoint.y + offsetY);

        //     if (IsBlobAtPosition(worldPoint))
        //     {

        //         OnBlobDeselected?.Invoke(blob.Model);

        //     }
        // }
    }

    void Update()
    {
        CheckInput();

    }

   
}
