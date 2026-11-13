using UnityEngine;

namespace Blobs.UI
{
    /// <summary>
    /// Keeps a stretched UI root inside the device safe area while preserving authored padding.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaLayout : MonoBehaviour
    {
        [SerializeField, Tooltip("Left, bottom, right, and top padding inside the safe area.")]
        private Vector4 padding = new(25f, 25f, 25f, 25f);
        [SerializeField] private bool constrainHorizontal = true;
        [SerializeField] private bool constrainVertical = true;

        private RectTransform _rectTransform;
        private Canvas _rootCanvas;
        private Rect _lastSafeArea;
        private Rect _lastCanvasPixels;
        private Vector4 _lastPadding;
        private bool _lastConstrainHorizontal;
        private bool _lastConstrainVertical;

        private void OnEnable()
        {
            _rectTransform = GetComponent<RectTransform>();
            _rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;
            Apply();
        }

        private void LateUpdate()
        {
            Rect canvasPixels = GetCanvasPixels();
            if (_lastSafeArea != Screen.safeArea ||
                _lastCanvasPixels != canvasPixels ||
                _lastPadding != padding ||
                _lastConstrainHorizontal != constrainHorizontal ||
                _lastConstrainVertical != constrainVertical)
                Apply();
        }

        private void Apply()
        {
            if (_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();
            if (_rootCanvas == null)
                _rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;

            Rect canvasPixels = GetCanvasPixels();
            if (canvasPixels.width <= 0f || canvasPixels.height <= 0f)
                return;

            Rect safePixels = Intersect(canvasPixels, Screen.safeArea);
            Vector2 anchorMin = new(
                constrainHorizontal
                    ? Mathf.InverseLerp(canvasPixels.xMin, canvasPixels.xMax, safePixels.xMin)
                    : 0f,
                constrainVertical
                    ? Mathf.InverseLerp(canvasPixels.yMin, canvasPixels.yMax, safePixels.yMin)
                    : 0f);
            Vector2 anchorMax = new(
                constrainHorizontal
                    ? Mathf.InverseLerp(canvasPixels.xMin, canvasPixels.xMax, safePixels.xMax)
                    : 1f,
                constrainVertical
                    ? Mathf.InverseLerp(canvasPixels.yMin, canvasPixels.yMax, safePixels.yMax)
                    : 1f);

            _rectTransform.anchorMin = anchorMin;
            _rectTransform.anchorMax = anchorMax;
            _rectTransform.offsetMin = new Vector2(padding.x, padding.y);
            _rectTransform.offsetMax = new Vector2(-padding.z, -padding.w);

            _lastSafeArea = Screen.safeArea;
            _lastCanvasPixels = canvasPixels;
            _lastPadding = padding;
            _lastConstrainHorizontal = constrainHorizontal;
            _lastConstrainVertical = constrainVertical;
        }

        private Rect GetCanvasPixels()
        {
            return _rootCanvas != null
                ? _rootCanvas.pixelRect
                : new Rect(0f, 0f, Screen.width, Screen.height);
        }

        private static Rect Intersect(Rect a, Rect b)
        {
            float xMin = Mathf.Max(a.xMin, b.xMin);
            float yMin = Mathf.Max(a.yMin, b.yMin);
            float xMax = Mathf.Min(a.xMax, b.xMax);
            float yMax = Mathf.Min(a.yMax, b.yMax);
            return xMax > xMin && yMax > yMin
                ? Rect.MinMaxRect(xMin, yMin, xMax, yMax)
                : a;
        }
    }
}
