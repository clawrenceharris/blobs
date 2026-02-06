using System;
using System.Collections;
using Blobs.Utilities;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [SerializeField] private float _padding;

    private Camera _cam;
    private BoardPresenter _board;
    private void Awake()
    {
        _board = FindFirstObjectByType<BoardPresenter>();
        _cam = GetComponent<Camera>();
        BoardPresenter.OnBoardInitialized += HandleBoardInitialized;
    }

    private void Start()
    {
        _cam.backgroundColor = ColorSchemeManager.CurrentColorScheme.BackgroundColor;

    }
    private void HandleBoardInitialized(IBoardPresenter board)
    {
        
        RepositionCamera(board.Width, board.Height);

    }



    void RepositionCamera(int width, int height)
    {
        Vector2 topLeft = GridUtility.GridToWorld(0, height - 1);
        Vector2 bottomRight = GridUtility.GridToWorld(width - 1, 0);

        Vector2 center = (topLeft + bottomRight) / 2f;
        transform.position = new Vector3(center.x, center.y, -10);

        float boardPixelWidth = Mathf.Abs(topLeft.x - bottomRight.x);
        float boardPixelHeight = Mathf.Abs(topLeft.y - bottomRight.y);

        float aspect = (float)Screen.width / Screen.height;

        float verticalSize = boardPixelHeight / 2f;
        float horizontalSize = boardPixelWidth / 2f / aspect;
        if (width >= height)
        {
            _cam.orthographicSize = horizontalSize + _padding;
        }
        else
        {
            _cam.orthographicSize = verticalSize + _padding;
        }

    }


}
