using UnityEngine;

namespace Blobs.Content
{
    /// <summary>
    /// Art-directed board-surface palette reserved for future world/theme integration.
    /// It intentionally has no runtime consumer yet.
    /// </summary>
    [CreateAssetMenu(
        fileName = "BoardSurfacePalette_New",
        menuName = "Blobs/Board Surface Palette")]
    public sealed class BoardSurfacePaletteAsset : ScriptableObject
    {
        [Header("Top Surface")]
        [SerializeField] private Color surface = new(0.9804f, 0.9569f, 0.9373f, 1f);
        [SerializeField] private Color surfaceWarm = new(0.9725f, 0.9333f, 0.9137f, 1f);
        [SerializeField] private Color highlight = new(1f, 1f, 0.9882f, 1f);

        [Header("Depth")]
        [SerializeField] private Color bevel = new(0.9176f, 0.8471f, 0.8314f, 1f);
        [SerializeField] private Color thickness = new(0.8039f, 0.6627f, 0.6824f, 1f);
        [SerializeField] private Color shadow = new(0.3882f, 0.2431f, 0.3333f, 1f);

        public Color Surface => surface;
        public Color SurfaceWarm => surfaceWarm;
        public Color Highlight => highlight;
        public Color Bevel => bevel;
        public Color Thickness => thickness;
        public Color Shadow => shadow;
    }
}
