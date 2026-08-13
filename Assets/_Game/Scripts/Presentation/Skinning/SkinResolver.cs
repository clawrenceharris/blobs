using Blobs.Core;
using Blobs.Content;
using System;
namespace Blobs.Presentation
{
    public interface ISkinResolver<T>
    {
        Skin? ResolveSkin(T entity, LevelVisualThemeAsset theme);
    }
    public class BlobSkinResolver : ISkinResolver<BlobState>
    {
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