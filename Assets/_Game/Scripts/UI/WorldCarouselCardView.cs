using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Blobs.UI
{
    public sealed class WorldCarouselCardView : MonoBehaviour
    {
        [SerializeField] private RectTransform mechanicRoot;
        [SerializeField] private TMP_Text worldTitleLabel;
        [SerializeField] private Image lockIcon;
        [SerializeField] private Image cardBackground;

        public RectTransform RectTransform => (RectTransform)transform;

        public void SetContent(int worldNumber, GameObject mechanicPrefab, bool unlocked)
        {
            if (worldTitleLabel != null)
                worldTitleLabel.SetText(worldNumber.ToString(), worldNumber);

            if (cardBackground != null)
            {
                cardBackground.enabled = !unlocked;
            }

            BuildMechanicPreview(mechanicPrefab, unlocked);

            if (lockIcon != null)
            {
                lockIcon.gameObject.SetActive(!unlocked);
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

            foreach (SpriteRenderer renderer in renderers)
            {
                if (renderer.sprite == null || !renderer.enabled)
                    continue;

                GetRendererBounds(renderer, out Vector2 rendererMin, out Vector2 rendererMax);
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
            }
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
