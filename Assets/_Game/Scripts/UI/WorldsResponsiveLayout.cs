using UnityEngine;

namespace Blobs.UI
{
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public sealed class WorldsResponsiveLayout : MonoBehaviour
    {
        public const float HeroWidthRatio = 0.56f;
        public const float ProgressWidthRatio = 0.60f;

        [SerializeField] private UiThemeAsset theme;
        [SerializeField] private RectTransform worldInfo;
        [SerializeField] private RectTransform carouselViewport;
        [SerializeField] private RectTransform worldCardTemplate;
        [SerializeField] private RectTransform mutedBackground;
        [SerializeField] private RectTransform enterButton;

        private RectTransform _rectTransform;
        private Vector2 _lastSize = new(-1f, -1f);

        public UiThemeAsset Theme => theme;

        private void OnEnable() => RefreshLayout();
        private void OnRectTransformDimensionsChange() => RefreshLayout();

#if UNITY_EDITOR
        private void OnValidate() => RefreshLayout();
#endif

        public void Configure(
            UiThemeAsset value,
            RectTransform info,
            RectTransform viewport,
            RectTransform cardTemplate,
            RectTransform progress,
            RectTransform button)
        {
            theme = value;
            worldInfo = info;
            carouselViewport = viewport;
            worldCardTemplate = cardTemplate;
            mutedBackground = progress;
            enterButton = button;
            RefreshLayout(true);
        }

        public void RefreshLayout() => RefreshLayout(true);

        private void RefreshLayout(bool force)
        {
            _rectTransform ??= (RectTransform)transform;
            Vector2 safeSize = _rectTransform.rect.size;
            if (safeSize.x <= 0f || safeSize.y <= 0f || (!force && safeSize == _lastSize))
                return;

            _lastSize = safeSize;

            SetStretch(worldInfo, new Vector2(0.08f, 0.70f), new Vector2(0.92f, 0.82f));
            SetStretch(carouselViewport, new Vector2(0f, 0.31f), new Vector2(1f, 0.70f));

            if (worldCardTemplate != null)
            {
                float mechanicWidth = safeSize.x * HeroWidthRatio;
                float cardWidth = mechanicWidth / 0.76f;
                float cardSize = Mathf.Min(cardWidth, safeSize.y * 0.42f);
                worldCardTemplate.sizeDelta = new Vector2(cardSize, cardSize);
            }

            if (mutedBackground != null)
            {
                mutedBackground.localScale = Vector3.one;
                float width = safeSize.x * ProgressWidthRatio;
                float height = Mathf.Clamp(safeSize.x * 0.06f, 56f, 72f);
                mutedBackground.anchorMin = new Vector2(0.5f, 0f);
                mutedBackground.anchorMax = new Vector2(0.5f, 0f);
                mutedBackground.pivot = new Vector2(0.5f, 0.5f);
                mutedBackground.sizeDelta = new Vector2(width, height);

                if (enterButton != null)
                {
                    float gap = theme != null ? theme.Cluster : 20f;
                    float buttonTop = enterButton.anchoredPosition.y + enterButton.rect.height * 0.5f;
                    mutedBackground.anchoredPosition = new Vector2(
                        0f,
                        buttonTop + gap + height * 0.5f);
                }
            }
        }

        private static void SetStretch(RectTransform target, Vector2 anchorMin, Vector2 anchorMax)
        {
            if (target == null)
                return;

            target.localScale = Vector3.one;
            target.anchorMin = anchorMin;
            target.anchorMax = anchorMax;
            target.anchoredPosition = Vector2.zero;
            target.sizeDelta = Vector2.zero;
        }
    }
}
