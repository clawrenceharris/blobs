using UnityEngine;
namespace Blobs.Presentation
{
    public readonly struct Skin
    {
        public Color BaseColor { get; }
        public Color AccentColor { get; }
        public Color DetailColor { get; }

        public Skin(Color baseColor, Color accentColor, Color detailColor)
        {
            BaseColor = baseColor;
            AccentColor = accentColor;
            DetailColor = detailColor;
        }
    }
}