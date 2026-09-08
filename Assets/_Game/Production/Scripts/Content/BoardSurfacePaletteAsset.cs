using UnityEngine;
using UnityEngine.Serialization;

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
        [FormerlySerializedAs("surface")]
        [SerializeField] private Color fillA = new(0.9725f, 0.9647f, 0.9373f, 1f);
        [FormerlySerializedAs("surfaceWarm")]
        [SerializeField] private Color fillB = new(0.9451f, 0.9412f, 0.9176f, 1f);
        [FormerlySerializedAs("bevel")]
        [SerializeField] private Color ambientEdge = new(0.8471f, 0.8431f, 0.8196f, 1f);
        [SerializeField] private Color lowerEdge = new(0.8157f, 0.8078f, 0.7804f, 1f);
        [SerializeField] private Color highlight = Color.white;

        [Header("Separation")]
        [SerializeField] private Color shadow = new(0.1882f, 0.1490f, 0.2902f, 1f);

        public Color FillA => fillA;
        public Color FillB => fillB;
        public Color AmbientEdge => ambientEdge;
        public Color LowerEdge => lowerEdge;
        public Color Highlight => highlight;
        public Color Shadow => shadow;
    }
}
