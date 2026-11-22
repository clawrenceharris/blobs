using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Blobs.UI
{
    public sealed class WorldCarouselCardView : MonoBehaviour
    {
        [SerializeField] private RectTransform mechanicRoot;
        [SerializeField] private Image lockIcon;
        [SerializeField] private UiThemeAsset theme;

        public RectTransform RectTransform => (RectTransform)transform;
        public UiThemeAsset Theme => theme;

        public void SetTheme(UiThemeAsset value)
        {
            theme = value;
        }

        public void SetContent(int worldNumber, GameObject mechanicPrefab, bool unlocked)
        {
            BuildMechanicPreview(mechanicPrefab, unlocked);

            if (lockIcon != null)
            {
                lockIcon.gameObject.SetActive(!unlocked);
                if (!unlocked && theme != null)
                    lockIcon.color = theme.OnPrimaryText;
            }
        }

        private void BuildMechanicPreview(GameObject prefab, bool unlocked)
        {
            if (mechanicRoot == null)
                return;

            for (int i = mechanicRoot.childCount - 1; i >= 0; i--)
                Destroy(mechanicRoot.GetChild(i).gameObject);

            if (prefab == null)
                return;

            SpriteRenderer[] renderers = prefab.GetComponentsInChildren<SpriteRenderer>(true);
            Array.Sort(renderers, (left, right) => left.sortingOrder.CompareTo(right.sortingOrder));

            bool hasBounds = false;
            Vector2 minimum = Vector2.zero;
            Vector2 maximum = Vector2.zero;
            foreach (SpriteRenderer renderer in renderers)
            {
                if (renderer.sprite == null || !renderer.enabled)
                    continue;

                GetRendererBounds(renderer, out Vector2 rendererMin, out Vector2 rendererMax);
                if (!hasBounds)
                {
                    minimum = rendererMin;
                    maximum = rendererMax;
                    hasBounds = true;
                }
                else
                {
                    minimum = Vector2.Min(minimum, rendererMin);
                    maximum = Vector2.Max(maximum, rendererMax);
                }
            }

            Vector2 totalSize = maximum - minimum;
            if (!hasBounds || totalSize.x <= 0f || totalSize.y <= 0f)
                return;

            float contentMinimumY = maximum.y;
            foreach (SpriteRenderer renderer in renderers)
            {
                if (renderer.sprite == null || !renderer.enabled || IsShadow(renderer))
                    continue;

                GetRendererBounds(renderer, out Vector2 rendererMin, out _);
                contentMinimumY = Mathf.Min(contentMinimumY, rendererMin.y);
            }

            foreach (SpriteRenderer renderer in renderers)
            {
                if (renderer.sprite == null || !renderer.enabled)
                    continue;

                bool isShadow = IsShadow(renderer);
                GetRendererBounds(renderer, out Vector2 rendererMin, out Vector2 rendererMax);
                if (isShadow)
                {
                    float desiredShadowTop = contentMinimumY - totalSize.y * 0.02f;
                    float verticalOffset = desiredShadowTop - rendererMax.y;
                    rendererMin.y += verticalOffset;
                    rendererMax.y += verticalOffset;
                }

                var visual = new GameObject(renderer.gameObject.name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                RectTransform rect = visual.GetComponent<RectTransform>();
                rect.SetParent(mechanicRoot, false);
                rect.anchorMin = new Vector2(
                    Mathf.InverseLerp(minimum.x, maximum.x, rendererMin.x),
                    Mathf.InverseLerp(minimum.y, maximum.y, rendererMin.y));
                rect.anchorMax = new Vector2(
                    Mathf.InverseLerp(minimum.x, maximum.x, rendererMax.x),
                    Mathf.InverseLerp(minimum.y, maximum.y, rendererMax.y));
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                Image image = visual.GetComponent<Image>();
                image.material = renderer.sharedMaterial;
                image.sprite = renderer.sprite;
                image.preserveAspect = true;
                image.raycastTarget = false;
                if (!unlocked && theme != null)
                    image.color = isShadow ? theme.LockedShadow : theme.LockedContent;
            }
        }

        private static bool IsShadow(SpriteRenderer renderer)
        {
            return renderer.gameObject.name.IndexOf(
                "shadow",
                StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void GetRendererBounds(
            SpriteRenderer renderer,
            out Vector2 minimum,
            out Vector2 maximum)
        {
            Bounds bounds = renderer.sprite.bounds;
            Vector3 first = renderer.transform.TransformPoint(bounds.min);
            Vector3 second = renderer.transform.TransformPoint(bounds.max);
            minimum = Vector2.Min(first, second);
            maximum = Vector2.Max(first, second);
        }
    }
}
