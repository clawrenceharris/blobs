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
            var color = blob.Color switch
            {
                BlobColor.Red => theme.Red,
                BlobColor.Green => theme.Green,
                BlobColor.Blue => theme.Blue,
                BlobColor.Yellow => theme.Yellow,
                BlobColor.Purple => theme.Purple,
                _ => throw new ArgumentException("Invalid blob color"),
            };
            return new Skin(color, color, color);
        }

    }
}
