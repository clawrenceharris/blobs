using System;
using System.Linq;
using Blobs.Core;
using UnityEngine;

namespace Blobs.Content
{
    /// <summary>
    /// The semantic colors retained for feedback and the legacy three-color shader fallback.
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

    [Serializable]
    public sealed class BlobMaterial
    {
        [SerializeField] private Material material;
        [SerializeField] private BlobColor color;
        public Material Material => material;
        public BlobColor Color => color;
    }

    /// <summary>
    /// Per-color controls consumed by the All In 1 Sprite Shader HSV effect.
    /// The shared color-ramp material owns the ramp texture and enabled shader features;
    /// palettes only vary these inexpensive per-renderer values.
    /// </summary>
    [Serializable]
    public sealed class BlobRampHsv
    {
        [SerializeField, Range(0f, 360f)] private float hueShift;
        [SerializeField, Min(0f)] private float saturation = 1f;
        [SerializeField, Min(0f)] private float brightness = 1f;

        public BlobRampHsv(float hueShift, float saturation = 1f, float brightness = 1f)
        {
            this.hueShift = hueShift;
            this.saturation = saturation;
            this.brightness = brightness;
        }

        public float HueShift => hueShift;
        public float Saturation => saturation;
        public float Brightness => brightness;
    }

    /// <summary>
    /// Authoritative presentation mapping from Core blob colors to shared-material ramp settings.
    /// </summary>
    [CreateAssetMenu(
        fileName = "LevelColorPalette_New",
        menuName = "Blobs/Presentation/Level Color Palette")]
    public sealed class LevelColorPaletteAsset : ScriptableObject
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

        [SerializeField] private Color cellColor = Color.white;

        public Color CellColor => cellColor;

        public BlobShaderColors GetRequired(BlobColor color)
        {
            BlobShaderColors colors = color switch
            {
                BlobColor.Red => red,
                BlobColor.Blue => blue,
                BlobColor.Green => green,
                BlobColor.Yellow => yellow,
                BlobColor.Purple => purple,
                _ => throw new ArgumentOutOfRangeException(nameof(color), color, "Unknown blob color."),
            };

            return colors ?? throw new InvalidOperationException(
                $"Blob color palette '{name}' has no colors configured for {color}.");
        }
    }
}
