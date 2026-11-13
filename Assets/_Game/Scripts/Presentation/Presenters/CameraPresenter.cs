using UnityEngine;

namespace Blobs.Presentation
{
    /// <summary>
    /// Positions and sizes an orthographic camera so the current board fits inside the screen
    /// space left by safe-area insets and persistent HUD chrome.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraPresenter : MonoBehaviour
    {
        [SerializeField, Min(0f), Tooltip("Distance from each outer cell center to the usable viewport edge. A half-cell minimum is always preserved.")]
        private float padding;
        [SerializeField, Tooltip("Top HUD bounds. The board is framed below this rectangle, including any gap above it.")]
        private RectTransform topHud;

        [SerializeField, Tooltip("Optional bottom HUD bounds. The board is framed above this rectangle.")]
        private RectTransform bottomHud;
        [SerializeField, Tooltip("Keep the board inside the device safe area before reserving HUD space.")]
        private bool respectSafeArea = true;

        private Camera _camera;
        private RectTransform _cachedTopHud;
        private RectTransform _cachedBottomHud;
        private Canvas _topHudCanvas;
        private Canvas _bottomHudCanvas;
        private readonly Vector3[] _hudCorners = new Vector3[4];
        private Vector2 _boardCenter;
        private Vector2 _boardHalfExtents;
        private bool _hasBoard;
        private float _lastAspect;
        private Rect _lastAvailableViewport;

        /// <summary>
        /// Fits the complete padded board inside the usable viewport, preserving camera depth.
        /// </summary>
        public void FitCameraToBoard(int width, int height, float cellSize)
        {
            _camera ??= GetComponent<Camera>();

            float centerSpanX = Mathf.Max(0, width - 1) * cellSize;
            float centerSpanY = Mathf.Max(0, height - 1) * cellSize;
            float edgePadding = Mathf.Max(padding, cellSize * 0.5f);
            _boardCenter = new Vector2(centerSpanX * 0.5f, centerSpanY * 0.5f);
            _boardHalfExtents = new Vector2(
                centerSpanX * 0.5f + edgePadding,
                centerSpanY * 0.5f + edgePadding);
            _hasBoard = true;
            // Startup can precede the first canvas layout pass.
            if (topHud != null || bottomHud != null)
                Canvas.ForceUpdateCanvases();
            ApplyFraming(GetAvailableViewport());
        }

        private void ApplyFraming(Rect availableViewport)
        {
            float requiredForHeight = _boardHalfExtents.y / availableViewport.height;
            float requiredForWidth = _boardHalfExtents.x /
                (_camera.aspect * availableViewport.width);
            _camera.orthographicSize = Mathf.Max(requiredForHeight, requiredForWidth);

            // Align the board center with the center of the usable viewport rather than the screen.
            Vector2 viewportOffset = availableViewport.center - new Vector2(0.5f, 0.5f);
            transform.position = new Vector3(
                _boardCenter.x - viewportOffset.x * 2f * _camera.orthographicSize * _camera.aspect,
                _boardCenter.y - viewportOffset.y * 2f * _camera.orthographicSize,
                transform.position.z);

            _lastAspect = _camera.aspect;
            _lastAvailableViewport = availableViewport;
        }

        private Rect GetAvailableViewport()
        {
            Rect viewport = _camera.pixelRect;
            if (viewport.width <= 0f || viewport.height <= 0f)
                return new Rect(0f, 0f, 1f, 1f);

            Rect usablePixels = respectSafeArea
                ? Intersect(viewport, Screen.safeArea)
                : viewport;
            Rect available = ToNormalizedViewport(usablePixels, viewport);

            if (TryGetScreenBounds(topHud, ref _cachedTopHud, ref _topHudCanvas, out Rect topBounds) &&
                topBounds.Overlaps(viewport))
            {
                float topHudBottom = Mathf.InverseLerp(viewport.yMin, viewport.yMax, topBounds.yMin);
                available.yMax = Mathf.Min(available.yMax, topHudBottom);
            }

            if (TryGetScreenBounds(bottomHud, ref _cachedBottomHud, ref _bottomHudCanvas, out Rect bottomBounds) &&
                bottomBounds.Overlaps(viewport))
            {
                float bottomHudTop = Mathf.InverseLerp(viewport.yMin, viewport.yMax, bottomBounds.yMax);
                available.yMin = Mathf.Max(available.yMin, bottomHudTop);
            }

            const float minimumSize = 0.01f;
            available.width = Mathf.Max(minimumSize, available.width);
            available.height = Mathf.Max(minimumSize, available.height);
            return available;
        }

        private bool TryGetScreenBounds(
            RectTransform target,
            ref RectTransform cachedTarget,
            ref Canvas cachedCanvas,
            out Rect bounds)
        {
            bounds = default;
            if (target == null || !target.gameObject.activeInHierarchy)
                return false;

            if (cachedTarget != target || cachedCanvas == null)
            {
                cachedTarget = target;
                cachedCanvas = target.GetComponentInParent<Canvas>();
            }
            if (cachedCanvas == null || !cachedCanvas.isActiveAndEnabled)
                return false;

            Canvas rootCanvas = cachedCanvas.rootCanvas;
            Camera uiCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : rootCanvas.worldCamera;
            target.GetWorldCorners(_hudCorners);
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

            bounds = Rect.MinMaxRect(left, bottom, right, top);
            return true;
        }

        private static Rect Intersect(Rect a, Rect b)
        {
            float xMin = Mathf.Max(a.xMin, b.xMin);
            float yMin = Mathf.Max(a.yMin, b.yMin);
            float xMax = Mathf.Min(a.xMax, b.xMax);
            float yMax = Mathf.Min(a.yMax, b.yMax);
            if (xMax <= xMin || yMax <= yMin)
                return a;
            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        private static Rect ToNormalizedViewport(Rect pixels, Rect viewport)
        {
            return Rect.MinMaxRect(
                Mathf.InverseLerp(viewport.xMin, viewport.xMax, pixels.xMin),
                Mathf.InverseLerp(viewport.yMin, viewport.yMax, pixels.yMin),
                Mathf.InverseLerp(viewport.xMin, viewport.xMax, pixels.xMax),
                Mathf.InverseLerp(viewport.yMin, viewport.yMax, pixels.yMax));
        }

        private void LateUpdate()
        {
            if (!_hasBoard)
                return;

            Rect availableViewport = GetAvailableViewport();
            if (!Mathf.Approximately(_lastAspect, _camera.aspect) ||
                !Approximately(_lastAvailableViewport, availableViewport))
                ApplyFraming(availableViewport);
        }

        private static bool Approximately(Rect a, Rect b)
        {
            return Mathf.Approximately(a.x, b.x) &&
                Mathf.Approximately(a.y, b.y) &&
                Mathf.Approximately(a.width, b.width) &&
                Mathf.Approximately(a.height, b.height);
        }

        private void Awake()
        {
            _camera = GetComponent<Camera>();
        }
    }
}
