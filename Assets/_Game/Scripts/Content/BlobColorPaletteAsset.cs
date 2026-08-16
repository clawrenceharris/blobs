using System;
using Blobs.Core;
using UnityEngine;

namespace Blobs.Content
{
    /// <summary>
    /// The three colors consumed by BlobColorShader for one logical blob color.
    /// </summary>
    [Serializable]
    public sealed class BlobShaderColors
    {
        [SerializeField] private Color baseColor = Color.white;
        [SerializeField] private Color shadowColor = Color.gray;
        [SerializeField] private Color highlightColor = Color.white;

        public BlobShaderColors(Color baseColor, Color shadowColor, Color highlightColor)
        {
            this.baseColor = baseColor;
            this.shadowColor = shadowColor;
            this.highlightColor = highlightColor;
        }

        public Color BaseColor => baseColor;
        public Color ShadowColor => shadowColor;
        public Color HighlightColor => highlightColor;
    }

    /// <summary>
    /// Authoritative presentation mapping from Core blob colors to BlobColorShader colors.
    /// </summary>
    [CreateAssetMenu(
        fileName = "BlobColorPalette_New",
        menuName = "Blobs/Presentation/Blob Color Palette")]
    public sealed class BlobColorPaletteAsset : ScriptableObject
    {
        [SerializeField]
        private BlobShaderColors red = new(
            new Color(0.996f, 0.278f, 0.467f),
            new Color(0.710f, 0.105f, 0.275f),
            new Color(1.000f, 0.620f, 0.735f));

        [SerializeField]
        private BlobShaderColors blue = new(
            new Color(0.335f, 0.720f, 1.000f),
            new Color(0.110f, 0.390f, 0.735f),
            new Color(0.665f, 0.900f, 1.000f));

        [SerializeField]
        private BlobShaderColors green = new(
            new Color(0.478f, 0.976f, 0.694f),
            new Color(0.145f, 0.610f, 0.365f),
            new Color(0.755f, 1.000f, 0.860f));

        [SerializeField]
        private BlobShaderColors yellow = new(
            new Color(1.000f, 0.796f, 0.288f),
            new Color(0.765f, 0.475f, 0.095f),
            new Color(1.000f, 0.940f, 0.620f));

        [SerializeField]
        private BlobShaderColors purple = new(
            new Color(0.527f, 0.400f, 1.000f),
            new Color(0.305f, 0.175f, 0.680f),
            new Color(0.760f, 0.680f, 1.000f));
        [SerializeField]
        private BlobShaderColors clear = new(
            Color.clear,
            Color.clear,
            Color.clear);
        public BlobShaderColors GetRequired(BlobColor color)
        {
            BlobShaderColors colors = color switch
            {
                BlobColor.Red => red,
                BlobColor.Blue => blue,
                BlobColor.Green => green,
                BlobColor.Yellow => yellow,
                BlobColor.Purple => purple,
                BlobColor.None => clear,
                _ => throw new ArgumentOutOfRangeException(nameof(color), color, "Unknown blob color."),
            };

            return colors ?? throw new InvalidOperationException(
                $"Blob color palette '{name}' has no colors configured for {color}.");
        }
    }
}
