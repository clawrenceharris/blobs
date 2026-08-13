using UnityEngine;

namespace Blobs.Content
{
    [CreateAssetMenu(
        fileName = "Theme_New",
        menuName = "Blobs/Level Visual Theme")]
    public sealed class LevelVisualThemeAsset : ScriptableObject
    {
        [Header("Board")]
        [SerializeField] private Color cellColor =
            new(0.01f, 0.08f, 0.18f);
        [Header("Game Colors")]
        [SerializeField] private Color red =
            new(0.8f, 0.05f, 0.05f);     // red ≈ (1, 0, 0)
        [SerializeField] private Color yellow =
            new(0.8f, 0.8f, 0.05f);     // yellow ≈ (1, 1, 0)
        [SerializeField] private Color green =
            new(0.05f, 0.8f, 0.05f);    // green ≈ (0, 1, 0)
        [SerializeField] private Color blue =
            new(0.05f, 0.05f, 0.8f);    // blue ≈ (0, 0, 1)
        [SerializeField] private Color purple =
            new(0.8f, 0.05f, 0.8f);     // purple ≈ (1, 0, 1)
    
        [Header("Background Gradient")]
        [SerializeField] private Color topColor =
            new(0.01f, 0.08f, 0.18f);

        [SerializeField] private Color middleColor =
            new(0.01f, 0.35f, 0.36f);

        [SerializeField] private Color bottomColor =
            new(0.01f, 0.85f, 0.66f);

        [SerializeField, Range(0.1f, 0.9f)]
        private float middlePosition = 0.55f;

        [SerializeField, Range(0f, 1f)]
        private float blendSoftness = 0.2f;

        public Color TopColor => topColor;
        public Color MiddleColor => middleColor;
        public Color BottomColor => bottomColor;
        public float MiddlePosition => middlePosition;
        public float BlendSoftness => blendSoftness;
        public Color CellColor => cellColor;
        public Color Red => red;
        public Color Yellow => yellow;
        public Color Green => green;
        public Color Blue => blue;
        public Color Purple => purple;
    }
}