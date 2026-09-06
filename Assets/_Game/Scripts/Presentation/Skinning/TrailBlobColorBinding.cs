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
            LevelColorPaletteAsset palette)
        {
            if (blob == null)
                throw new ArgumentNullException(nameof(blob));

            if (!blob.Components.Trail.HasValue)
            {
                throw new InvalidOperationException(
                    $"Trail color binding on '{name}' requires a TrailBlobModel for blob '{blob.Id}'.");
            }

            Skin skin = _resolver.ResolveSkin(blob.Components.Trail.Value.TrailColor, palette);
            BlobRenderer.ApplyShaderSkin(
                targetRenderer,
                targetRenderer.material,
                skin,
                ref _properties);
        }
    }
}
