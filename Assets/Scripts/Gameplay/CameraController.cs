using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [SerializeField] private float _aspectRatio;
    [SerializeField] private float padding;
    [SerializeField] private float pixelsPerUnit;

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
        if (_board.BoardModel == null)
        {
            return;
        }
        RepositionCamera((_board.BoardModel.Width - 1) * BoardPresenter.TileSize, (_board.BoardModel.Height - 1) * BoardPresenter.TileSize);

    }

  
    void RepositionCamera(float x, float y)
    {
        Vector3 tempPosition = new(x / 2, y / 2, -1);
        transform.position = tempPosition;
        if (_board.BoardModel.Width >= _board.BoardModel.Height)
        {
            Camera.main.orthographicSize = (x / 2 + padding) / _aspectRatio;
        }
        else
        {
            Camera.main.orthographicSize = y / 2 + padding;
        }

    }


}
