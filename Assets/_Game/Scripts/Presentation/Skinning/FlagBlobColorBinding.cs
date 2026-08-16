using System;
using Blobs.Content;
using Blobs.Core;
using UnityEngine;

namespace Blobs.Presentation
{
    /// <summary>
    /// Flag-only binding that colors its checkers and final from BlobState.Color.
    /// </summary>
    public sealed class FlagBlobColorBinding : BlobColorBinding
    {
        [SerializeField] private SpriteRenderer targetRenderer;

        private MaterialPropertyBlock _properties;
        private readonly FlagBlobSkinResolver _resolver = new();

        public SpriteRenderer TargetRenderer => targetRenderer;

        public override void Apply(
            BlobState blob,
            BlobColorPaletteAsset palette)
        {
            if (blob == null)
                throw new ArgumentNullException(nameof(blob));

            if (!blob.TryGetModel(out ColorBlobModel colorBlob))
            {
                throw new InvalidOperationException(
                    $"Flag color binding on '{name}' requires a ColorBlobModel for blob '{blob.Id}'.");
            }

            Skin skin = _resolver.ResolveSkin(colorBlob.Color, palette);
            BlobRenderer.ApplyShaderSkin(
                targetRenderer,
                targetRenderer.material,
                skin,
                ref _properties);
        }
    }
}
