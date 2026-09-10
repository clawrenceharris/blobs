using System;
using UnityEngine;

public class BlobInput : MonoBehaviour
{
    private BlobView _blob;
    private BoardPresenter _board;
    private bool _tracking;
    private int _fingerId;
    private Vector2 _pressPosition;
    private int _pressGeneration;
    private static int _inputGeneration;

    public static bool InputEnabled;
    public Action<Blob> OnBlobSelected;
    public Action<Blob> OnBlobDeselected;
    public Action<Blob, Blob> OnBlobSwiped;

    private void Awake()
    {
        _blob = GetComponent<BlobView>();
        _board = FindFirstObjectByType<BoardPresenter>();
    }

    public static void EnableInput() => InputEnabled = true;

    public static void DisableInput()
    {
        InputEnabled = false;
        // A press begun before an animation/level change must not finish afterward.
        _inputGeneration++;
    }

    private Blob BlobAtScreenPosition(Vector2 position)
    {
        if (_board.BoardLogic == null || Camera.main == null) return null;
        Vector3 world = Camera.main.ScreenToWorldPoint(position) / BoardPresenter.TileSize;
        int x = Mathf.FloorToInt(world.x + 0.5f);
        int y = Mathf.FloorToInt(world.y + 0.5f);
        if (x < 0 || y < 0 || x >= _board.BoardLogic.Width || y >= _board.BoardLogic.Height)
            return null;
        return _board.BoardLogic.GetBlobAt(x, y);
    }

    private void BeginPointer(Vector2 position, int fingerId)
    {
        if (!InputEnabled || !isActiveAndEnabled) return;
        Blob hit = BlobAtScreenPosition(position);
        if (hit == null)
        {
            OnBlobDeselected?.Invoke(_blob.Model);
            return;
        }
        if (hit != _blob.Model) return;
        _tracking = true;
        _fingerId = fingerId;
        _pressPosition = position;
        _pressGeneration = _inputGeneration;
    }

    private void EndPointer(Vector2 position)
    {
        if (!_tracking) return;
        _tracking = false;
        if (!InputEnabled || !isActiveAndEnabled || _pressGeneration != _inputGeneration) return;
        Vector2 delta = position - _pressPosition;
        float threshold = Mathf.Max(16f, Mathf.Min(Screen.width, Screen.height) * 0.025f);
        if (delta.sqrMagnitude < threshold * threshold)
        {
            OnBlobSelected?.Invoke(_blob.Model);
            return;
        }
        OnBlobSwiped?.Invoke(_blob.Model, BlobAtScreenPosition(position));
    }

    private void Update()
    {
        if (!InputEnabled)
        {
            _tracking = false;
            return;
        }
        // Read touches directly; do not also consume their emulated mouse events.
        if (Input.touchCount > 0)
        {
            if (!_tracking)
            {
                Touch first = Input.GetTouch(0);
                if (first.phase == TouchPhase.Began) BeginPointer(first.position, first.fingerId);
            }
            else
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    Touch touch = Input.GetTouch(i);
                    if (touch.fingerId != _fingerId) continue;
                    if (touch.phase == TouchPhase.Canceled) _tracking = false;
                    else if (touch.phase == TouchPhase.Ended) EndPointer(touch.position);
                    break;
                }
            }
            return;
        }
        // A vanished/canceled touch must not turn into a mouse gesture.
        if (_tracking && _fingerId != -1) _tracking = false;
        if (Input.GetMouseButtonDown(0)) BeginPointer(Input.mousePosition, -1);
        if (Input.GetMouseButtonUp(0)) EndPointer(Input.mousePosition);
    }

    private void OnDisable() => _tracking = false;

    private void OnApplicationFocus(bool focused)
    {
        if (!focused) _tracking = false;
    }
}
