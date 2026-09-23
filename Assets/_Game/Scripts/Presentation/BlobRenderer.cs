using System;
using Blobs.Content;
using UnityEngine;
using UnityEngine.Serialization;
namespace Blobs.Presentation
{
    /// <summary>
    /// Applies resolved presentation skins to one or more SpriteRenderers that compose a blob prefab.
    /// This keeps blob prefab color binding separate from Core blob state.
    /// </summary>
    public class BlobRenderer : MonoBehaviour
    {
        [Serializable]
        /// <summary>
        /// Maps a color-binding mode to one or more sprite renderers in a blob prefab.
        /// </summary>
        public class Target
        {
            [FormerlySerializedAs("Role")]
            public BlobColorBindingMode ColorBinding = BlobColorBindingMode.Blob;
            public SpriteRenderer[] Renderers;
        }
        [FormerlySerializedAs("_fallbackBaseRenderer")]
        [SerializeField] private SpriteRenderer fallbackBaseRenderer;

        [FormerlySerializedAs("_targets")]
        [SerializeField] private Target[] targets;

        private MaterialPropertyBlock _properties;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ShadowColorId = Shader.PropertyToID("_ShadowColor");
        private static readonly int HighlightColorId = Shader.PropertyToID("_HighlightColor");
        private static readonly int HsvShiftId = Shader.PropertyToID("_HsvShift");
        private static readonly int HsvSaturationId = Shader.PropertyToID("_HsvSaturation");
        private static readonly int HsvBrightnessId = Shader.PropertyToID("_HsvBright");

        [SerializeField] private FadeableVisual fadeableVisual;
        public Target[] Targets => targets;

        public FadeableVisual FadeableVisual => fadeableVisual;
        public SpriteRenderer BaseRenderer => fallbackBaseRenderer;


        private void Awake()
        {
            if (fallbackBaseRenderer == null)
                fallbackBaseRenderer = TryGetComponent(out SpriteRenderer renderer) ? renderer : null;
        }

        /// <summary>
        /// Applies the legacy three-color shader fallback to targets bound to the blob color.
        /// </summary>
        public void ApplySkin(Skin skin)
        {
            bool applied = false;
            if (targets != null)
            {
                for (int i = 0; i < targets.Length; i++)
                {
                    var binding = targets[i];
                    if (binding == null ||
                        binding.ColorBinding != BlobColorBindingMode.Blob ||
                        binding.Renderers == null)
                    {
                        continue;
                    }

                    for (int j = 0; j < binding.Renderers.Length; j++)
                    {
                        if (binding.Renderers[j] == null) continue;
                        ApplyShaderSkin(
                            binding.Renderers[j],
                            skin,
                            ref _properties);
                        applied = true;
                    }
                }
            }

            if (!applied && fallbackBaseRenderer != null)
                ApplyShaderSkin(
                    fallbackBaseRenderer,
                    skin,
                    ref _properties);
        }

        public void ApplySkin(Material material, BlobRampHsv rampHsv)
        {
            if (material == null || rampHsv == null)
                return;
            bool applied = false;
            if (targets != null)
            {
                for (int i = 0; i < targets.Length; i++)
                {
                    var binding = targets[i];
                    if (binding == null ||
                        binding.ColorBinding != BlobColorBindingMode.Blob ||
                        binding.Renderers == null)
                    {
                        continue;
                    }

                    for (int j = 0; j < binding.Renderers.Length; j++)
                    {
                        if (binding.Renderers[j] == null) continue;
                        ApplyRampSkin(
                            binding.Renderers[j],
                            material,
                            rampHsv,
                            ref _properties);
                        applied = true;
                    }
                }
            }

            if (!applied && fallbackBaseRenderer != null)
            {
                ApplyRampSkin(
                    fallbackBaseRenderer,
                    material,
                    rampHsv,
                    ref _properties);
            }
        }

        /// <summary>
        /// Applies one shared All In 1 Sprite Shader material plus per-renderer palette values.
        /// Keeping color variation in a property block avoids cloning materials per blob or level.
        /// </summary>
        internal static void ApplyRampSkin(
            SpriteRenderer renderer,
            Material material,
            BlobRampHsv rampHsv,
            ref MaterialPropertyBlock properties)
        {
            if (renderer == null || material == null || rampHsv == null)
                return;

            renderer.sharedMaterial = material;
            properties ??= new MaterialPropertyBlock();
            renderer.GetPropertyBlock(properties);
            properties.SetFloat(HsvShiftId, rampHsv.HueShift);
            properties.SetFloat(HsvSaturationId, rampHsv.Saturation);
            properties.SetFloat(HsvBrightnessId, rampHsv.Brightness);
            renderer.SetPropertyBlock(properties);

            Color tint = renderer.color;
            renderer.color = new Color(renderer.color.r, renderer.color.g, renderer.color.b, tint.a);
        }
        /// <summary>
        /// Applies per-renderer shader values without cloning or replacing the authored material.
        /// </summary>
        internal static void ApplyShaderSkin(
            SpriteRenderer renderer,
            Skin skin,
            ref MaterialPropertyBlock properties)
        {
            if (renderer == null)
                return;

            properties ??= new MaterialPropertyBlock();
            renderer.GetPropertyBlock(properties);
            properties.SetColor(BaseColorId, skin.BaseColor);
            properties.SetColor(ShadowColorId, skin.ShadowColor);
            properties.SetColor(HighlightColorId, skin.HighlightColor);
            renderer.SetPropertyBlock(properties);

            Color tint = renderer.color;
            renderer.color = new Color(1f, 1f, 1f, tint.a);
        }
        /// <summary>
        /// Applies one color to all configured renderers. Useful for simple fallback visuals.
        /// </summary>
        public void SetColor(Color color)
        {
            ApplySkin(new Skin(color, color, color));
        }

        /// <summary>
        /// Sets alpha across the blob's configured renderers without changing their RGB values.
        /// </summary>
        public void SetAlpha(float alpha)
        {

            SetRendererAlpha(BaseRenderer, alpha);

            if (Targets == null)
                return;

            foreach (Target target in Targets)
            {
                if (target?.Renderers == null)
                    continue;

                foreach (SpriteRenderer renderer in target.Renderers)
                    SetRendererAlpha(renderer, alpha);
            }
        }

        /// <summary>
        /// Sets sorting order across the blob's configured renderers.
        /// </summary>
        public void SetSortingOrder(int sortingOrder)
        {
            SetRendererSortingOrder(BaseRenderer, sortingOrder);

            if (Targets == null)
                return;

            foreach (Target target in Targets)
            {
                if (target?.Renderers == null)
                    continue;

                foreach (SpriteRenderer renderer in target.Renderers)
                    SetRendererSortingOrder(renderer, sortingOrder);
            }
        }

        private static void SetRendererAlpha(SpriteRenderer renderer, float alpha)
        {
            if (renderer == null)
                return;

            Color color = renderer.color;
            renderer.color = new Color(color.r, color.g, color.b, alpha);
        }

        private static void SetRendererSortingOrder(SpriteRenderer renderer, int sortingOrder)
        {
            if (renderer == null)
                return;

            renderer.sortingOrder = sortingOrder;
        }

    }
}
