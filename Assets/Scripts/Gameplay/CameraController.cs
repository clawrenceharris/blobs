using System;
using System.Collections;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    private readonly float _aspectRatio = 0.625f;
    [SerializeField] private float padding;
    private Camera _cam;
    private BoardPresenter _board;
    private void Awake()
    {
        _board = FindFirstObjectByType<BoardPresenter>();
        _cam = GetComponent<Camera>();

    }
    void Start()
    {
        _cam.backgroundColor = ColorSchemeManager.CurrentColorScheme.BackgroundColor;
    }

    private void Update()
    {
        if(_board.BoardModel == null)
        {
            return;
        }
        RepositionCamera(_board.BoardModel);
    }

  
    void RepositionCamera(BoardModel board)
    {
        int x = board.Width - 1;
        int y = board.Height - 1;
        Vector3 tempPosition = new(x / 2, y / 2, -1);
        transform.position = tempPosition;
        if (board.Width >= board.Height)
        {
            Camera.main.orthographicSize = (x / 2 + padding) / _aspectRatio;
        }
        else
        {
            Camera.main.orthographicSize = y / 2 + padding;
        }

    }


}
