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
        [SerializeField, Tooltip("Top HUD bounds. The board is framed below this rectangle, including any gap above it.")]
        private RectTransform topHud;


        private Camera _camera;
        private RectTransform _cachedHud;
        private Canvas _hudCanvas;
        private readonly Vector3[] _hudCorners = new Vector3[4];
        private Vector2 _boardSize;
        private bool _hasBoard;
        private float _lastAspect;
        private float _lastAvailableHeight;

        /// <summary>
        /// Fits the padded board below the optional top HUD, preserving camera depth.
        /// </summary>
        public void FitCameraToBoard(int width, int height, float cellSize)
        {
            _camera ??= GetComponent<Camera>();

            _boardSize = new Vector2((width - 1) * cellSize, (height - 1) * cellSize);
            _hasBoard = true;
            // Startup can precede the first canvas layout pass.
            if (topHud != null)
                Canvas.ForceUpdateCanvases();
            ApplyFraming(GetAvailableHeight());
        }

        private void ApplyFraming(float availableHeight)
        {
            float requiredForHeight = (_boardSize.y * 0.5f + padding) / availableHeight;
            float requiredForWidth = (_boardSize.x * 0.5f + padding) / _camera.aspect;
            _camera.orthographicSize = Mathf.Max(requiredForHeight, requiredForWidth);
            // Move the camera up so the board is centered in the unobstructed lower area.
            transform.position = new Vector3(
                _boardSize.x * 0.5f,
                _boardSize.y * 0.5f + _camera.orthographicSize * (1f - availableHeight),
                transform.position.z);

            _lastAspect = _camera.aspect;
            _lastAvailableHeight = availableHeight;
        }

        private float GetAvailableHeight()
        {
            if (topHud == null || !topHud.gameObject.activeInHierarchy)
                return 1f;

            if (_cachedHud != topHud || _hudCanvas == null)
            {
                _cachedHud = topHud;
                _hudCanvas = topHud.GetComponentInParent<Canvas>();
            }
            if (_hudCanvas == null || !_hudCanvas.isActiveAndEnabled)
                return 1f;

            Canvas rootCanvas = _hudCanvas.rootCanvas;
            Camera uiCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : rootCanvas.worldCamera;
            topHud.GetWorldCorners(_hudCorners);
            float bottom = float.PositiveInfinity;
            float top = float.NegativeInfinity;
            float left = float.PositiveInfinity;
            float right = float.NegativeInfinity;
            foreach (Vector3 corner in _hudCorners)
            {
                Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, corner);
                bottom = Mathf.Min(bottom, screenPoint.y);
                top = Mathf.Max(top, screenPoint.y);
                left = Mathf.Min(left, screenPoint.x);
                right = Mathf.Max(right, screenPoint.x);
            }

            Rect viewport = _camera.pixelRect;
            if (viewport.height <= 0f || top <= viewport.yMin || bottom >= viewport.yMax ||
                right <= viewport.xMin || left >= viewport.xMax)
                return 1f;

            // A HUD covering the entire viewport cannot leave usable board space; keep the fit finite.
            return Mathf.Clamp((bottom - viewport.yMin) / viewport.height, 0.01f, 1f);
        }

        private void LateUpdate()
        {
            if (!_hasBoard)
                return;

            float availableHeight = GetAvailableHeight();
            if (!Mathf.Approximately(_lastAspect, _camera.aspect) ||
                !Mathf.Approximately(_lastAvailableHeight, availableHeight))
                ApplyFraming(availableHeight);
        }

        private void Awake()
        {
            _camera = GetComponent<Camera>();
        }
    }
}
