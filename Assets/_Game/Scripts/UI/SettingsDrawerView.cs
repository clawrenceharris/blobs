using UnityEngine;
using UnityEngine.UI;

namespace Blobs.UI
{
    /// <summary>
    /// Minimal settings side drawer for the Home screen skeleton. Opens from the right;
    /// content beyond a close affordance is deferred.
    /// </summary>
    public sealed class SettingsDrawerView : MonoBehaviour
    {
        [SerializeField] private RectTransform drawerRoot;
        [SerializeField] private GameObject dimmer;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button dimmerButton;
        [SerializeField, Min(0f)] private float width = 420f;
        [SerializeField, Min(0f)] private float openDuration = 0.2f;

        private bool _isOpen;
        private float _animTime = 1f;
        private float _fromX;
        private float _toX;

        public bool IsOpen => _isOpen;

        private void Awake()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(Close);
            if (dimmerButton != null)
                dimmerButton.onClick.AddListener(Close);

            SetOpenImmediate(false);
        }

        private void OnDestroy()
        {
            if (closeButton != null)
                closeButton.onClick.RemoveListener(Close);
            if (dimmerButton != null)
                dimmerButton.onClick.RemoveListener(Close);
        }

        private void Update()
        {
            if (_animTime >= 1f || drawerRoot == null)
                return;

            _animTime = openDuration <= 0f
                ? 1f
                : Mathf.Min(1f, _animTime + Time.unscaledDeltaTime / openDuration);
            float t = Smooth(_animTime);
            SetDrawerX(Mathf.LerpUnclamped(_fromX, _toX, t));
            if (_animTime >= 1f && !_isOpen)
                HideClosedVisuals();
        }

        public void Open()
        {
            if (drawerRoot == null)
                return;

            if (dimmer != null)
                dimmer.SetActive(true);
            drawerRoot.gameObject.SetActive(true);
            _isOpen = true;
            // Start from the current position so reopening mid-close does not snap off-screen.
            BeginAnim(drawerRoot.anchoredPosition.x, 0f);
        }

        public void Close()
        {
            if (drawerRoot == null || !_isOpen)
                return;

            _isOpen = false;
            BeginAnim(drawerRoot.anchoredPosition.x, ClosedX());
            if (openDuration <= 0f)
                HideClosedVisuals();
        }

        public void Toggle()
        {
            if (_isOpen)
                Close();
            else
                Open();
        }

        private void SetOpenImmediate(bool open)
        {
            _isOpen = open;
            _animTime = 1f;
            if (drawerRoot != null)
            {
                drawerRoot.gameObject.SetActive(open);
                SetDrawerX(open ? 0f : ClosedX());
            }

            if (dimmer != null)
                dimmer.SetActive(open);
        }

        private void BeginAnim(float fromX, float toX)
        {
            _fromX = fromX;
            _toX = toX;
            _animTime = 0f;
            SetDrawerX(fromX);
        }

        private void HideClosedVisuals()
        {
            if (dimmer != null)
                dimmer.SetActive(false);
            if (drawerRoot != null)
                drawerRoot.gameObject.SetActive(false);
        }

        private float ClosedX() => width;

        private void SetDrawerX(float x)
        {
            Vector2 pos = drawerRoot.anchoredPosition;
            pos.x = x;
            drawerRoot.anchoredPosition = pos;
        }

        private static float Smooth(float t) => t * t * (3f - 2f * t);
    }
}
