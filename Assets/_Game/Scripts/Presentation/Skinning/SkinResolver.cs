using Blobs.Core;
using Blobs.Content;
using System;
namespace Blobs.Presentation
{
    /// <summary>
    /// Converts domain/content data into a concrete presentation skin.
    /// </summary>
    public interface ISkinResolver<T>
    {
        /// <summary>
        /// Resolves a skin for an entity using the current level theme.
        /// </summary>
        Skin? ResolveSkin(T entity, LevelVisualThemeAsset theme);
    }

    /// <summary>
    /// Resolves normal blob colors from the current level visual theme.
    /// </summary>
    public class BlobSkinResolver : ISkinResolver<BlobState>
    {
        /// <inheritdoc />
        public Skin? ResolveSkin(BlobState blob, LevelVisualThemeAsset theme)
        {
            var color = ResolveThemeColor(blob.Color, theme);

            // Trail blobs indicate their trail color through the Detail role,
            // which the trail prefab binds to its puddle renderer.
            var detailColor = blob.TrailColor.HasValue
                ? ResolveThemeColor(blob.TrailColor.Value, theme)
                : color;

            return new Skin(color, color, detailColor);
        }

        private static UnityEngine.Color ResolveThemeColor(
            BlobColor color,
            LevelVisualThemeAsset theme)
        {
            return color switch
            {
                BlobColor.Red => theme.Red,
                BlobColor.Green => theme.Green,
                BlobColor.Blue => theme.Blue,
                BlobColor.Yellow => theme.Yellow,
                BlobColor.Purple => theme.Purple,
                _ => throw new ArgumentException("Invalid blob color"),
            };
        }

    }
}
