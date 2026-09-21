using Blobs.Core;
using Blobs.Content;
using System;
using UnityEngine;
namespace Blobs.Presentation
{
    /// <summary>
    /// Resolves a logical blob color through the authored presentation palette.
    /// </summary>
    public interface IBlobSkinResolver
    {
        Skin ResolveSkin(BlobColor color, LevelColorPaletteAsset palette);
        Material ResolveMaterial(BlobColor color, LevelColorPaletteAsset palette);
    }

    /// <summary>
    /// Converts palette content into a concrete BlobColorShader skin.
    /// Blob-type-specific color sources are deliberately handled by prefab bindings.
    /// </summary>
    public sealed class BlobSkinResolver : IBlobSkinResolver
    {
        public Skin ResolveSkin(BlobColor color, LevelColorPaletteAsset palette)
        {
            if (palette == null)
                throw new ArgumentNullException(nameof(palette));

            BlobShaderColors colors = palette.GetRequired(color);
            return new Skin(
                colors.BaseColor,
                colors.ShadowColor,
                colors.HighlightColor);
        }
        public Material ResolveMaterial(BlobColor color, LevelColorPaletteAsset palette)
        {
            if (palette == null)
                throw new ArgumentNullException(nameof(palette));

            return palette.GetMaterial(color);
        }
    }
    public sealed class FlagBlobSkinResolver : IBlobSkinResolver
    {
        public Skin ResolveSkin(BlobColor color, LevelColorPaletteAsset palette)
        {
            if (palette == null)
                throw new ArgumentNullException(nameof(palette));

            BlobShaderColors colors = palette.GetRequired(color);
            return new Skin(
                colors.BaseColor,
                colors.BaseColor,
                colors.BaseColor);
        }
        public Material ResolveMaterial(BlobColor color, LevelColorPaletteAsset palette)
        {
            if (palette == null)
                throw new ArgumentNullException(nameof(palette));

            return palette.GetMaterial(color);
        }
    }
}
