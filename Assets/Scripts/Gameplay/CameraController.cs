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
        if(_board.BoardLogic == null)
        {
            return;
        }
        RepositionCamera(_board.BoardLogic);
    }

  
    void RepositionCamera(BoardLogic board)
    {
        float width = (board.Width - 1) * BoardPresenter.TileSize;
        float height = (board.Height - 1) * BoardPresenter.TileSize;
        transform.position = new Vector3(width / 2f, height / 2f, transform.position.z);

        // Reserve the existing narrow board area for tutorial text, but fit tall
        // boards too. Board positions are in world units, not integer grid cells.
        float aspect = Mathf.Min(_aspectRatio, _cam.aspect);
        float margin = Mathf.Max(padding, BoardPresenter.TileSize);
        _cam.orthographicSize = Mathf.Max((width / 2f + margin) / aspect,
            height / 2f + margin);

    }


}
