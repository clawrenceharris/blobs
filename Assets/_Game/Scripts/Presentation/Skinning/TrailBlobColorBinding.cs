using System;
using Blobs.Content;
using Blobs.Core;
using UnityEngine;

namespace Blobs.Presentation
{
    /// <summary>
    /// Trail-only binding that colors its puddle from BlobState.TrailColor.
    /// </summary>
    public sealed class TrailBlobColorBinding : BlobColorBinding
    {
        [SerializeField] private SpriteRenderer targetRenderer;

        private MaterialPropertyBlock _properties;
        private readonly BlobSkinResolver _resolver = new();

        public SpriteRenderer TargetRenderer => targetRenderer;

        public override void Apply(
            BlobState blob,
            BlobColorPaletteAsset palette)
        {
            if (blob == null)
                throw new ArgumentNullException(nameof(blob));

            if (!blob.TrailColor.HasValue)
            {
                throw new InvalidOperationException(
                    $"Trail color binding on '{name}' requires a TrailColor for blob '{blob.Id}'.");
            }

            Skin skin = _resolver.ResolveSkin(blob.TrailColor.Value, palette);
            BlobRenderer.ApplyShaderSkin(
                targetRenderer,
                targetRenderer.material,
                skin,
                ref _properties);
        }
    }
}
