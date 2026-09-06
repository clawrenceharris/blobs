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
            LevelColorPaletteAsset palette)
        {
            if (blob == null)
                throw new ArgumentNullException(nameof(blob));

            if (!blob.Components.Color.HasValue)
            {
                throw new InvalidOperationException(
                    $"Flag color binding on '{name}' requires a Color for blob '{blob.Id}'.");
            }

            Skin skin = _resolver.ResolveSkin(blob.Components.Color.Value.Color, palette);
            BlobRenderer.ApplyShaderSkin(
                targetRenderer,
                targetRenderer.material,
                skin,
                ref _properties);
        }
    }
}
