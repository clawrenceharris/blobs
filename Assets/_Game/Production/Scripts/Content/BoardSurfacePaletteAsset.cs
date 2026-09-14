using UnityEngine;
using UnityEngine.Serialization;

namespace Blobs.Content
{
    /// <summary>
    /// Art-directed colors for the occupancy-baked board slab, including the
    /// environment tint on the underside lip.
    /// </summary>
    [CreateAssetMenu(
        fileName = "BoardSurfacePalette_New",
        menuName = "Blobs/Board Surface Palette")]
    public sealed class BoardSurfacePaletteAsset : ScriptableObject
    {
        [Header("Top Surface")]
        [FormerlySerializedAs("surface")]
        [SerializeField] private Color fillA = new(0.980392f, 0.949020f, 0.898039f, 1f);
        [FormerlySerializedAs("surfaceWarm")]
        [SerializeField] private Color fillB = new(0.949020f, 0.921569f, 0.882353f, 1f);
        [FormerlySerializedAs("bevel")]
        [SerializeField] private Color ambientEdge = new(0.952941f, 0.905882f, 0.823529f, 1f);
        [SerializeField] private Color lowerEdge = new(0.760784f, 0.654902f, 0.592157f, 1f);
        [SerializeField] private Color highlight = new(1f, 1f, 0.980392f, 1f);

        [Header("Separation")]
        [SerializeField] private Color shadow = new(0.215686f, 0.019608f, 0.549020f, 1f);

        [Header("Underside")]
        [SerializeField] private Color lipTint = new(0.788235f, 0.635294f, 0.847059f, 1f);
        [SerializeField, Range(0f, 1f)] private float lipTintStrength = 0.60f;

        public Color FillA => fillA;
        public Color FillB => fillB;
        public Color AmbientEdge => ambientEdge;
        public Color LowerEdge => lowerEdge;
        public Color Highlight => highlight;
        public Color Shadow => shadow;
        public Color LipTint => lipTint;
        public float LipTintStrength => lipTintStrength;
    }
}
