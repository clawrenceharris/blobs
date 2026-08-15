using UnityEngine;
namespace Blobs.Presentation
{
    /// <summary>
    /// Resolved BlobColorShader color set for a blob view.
    /// </summary>
    public readonly struct Skin
    {
        public Color BaseColor { get; }
        public Color ShadowColor { get; }
        public Color HighlightColor { get; }

        /// <summary>
        /// Creates a skin from the three colors consumed by BlobColorShader.
        /// </summary>
        public Skin(Color baseColor, Color shadowColor, Color highlightColor)
        {
            BaseColor = baseColor;
            ShadowColor = shadowColor;
            HighlightColor = highlightColor;
        }
    }
}
