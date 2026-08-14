using UnityEngine;
namespace Blobs.Presentation
{
    /// <summary>
    /// Resolved color set for a blob or other skinnable view.
    /// </summary>
    public readonly struct Skin
    {
        public Color BaseColor { get; }
        public Color AccentColor { get; }
        public Color DetailColor { get; }

        /// <summary>
        /// Creates a skin from base, accent, and detail colors.
        /// </summary>
        public Skin(Color baseColor, Color accentColor, Color detailColor)
        {
            BaseColor = baseColor;
            AccentColor = accentColor;
            DetailColor = detailColor;
        }
    }
}
