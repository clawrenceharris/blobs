using System;
using System.Collections.Generic;
using Blobs.Content;
using Blobs.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Blobs.UI
{
    /// <summary>
    /// One HUD objective counter: an icon of the target with the remaining amount beneath it.
    /// </summary>
    public sealed class GameplayObjectiveView : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ShadowColorId = Shader.PropertyToID("_ShadowColor");
        private static readonly int HighlightColorId = Shader.PropertyToID("_HighlightColor");

        [SerializeField] private TMP_Text countText;

        [SerializeField, Tooltip("Icon parts shaded with the target color's base, shadow, and highlight tones, such as a monster body.")]
        private Graphic[] shadedGraphics = Array.Empty<Graphic>();

        [SerializeField, Tooltip("Icon parts filled with the target color's base tone only, such as the nest bowl.")]
        private Graphic[] flatGraphics = Array.Empty<Graphic>();

        [SerializeField, Tooltip("Optional. Shown once the item is complete.")]
        private GameObject completedRoot;

        private readonly List<Material> _tintMaterials = new();

        public void Bind(ObjectiveItemProgress progress, LevelColorPaletteAsset palette)
        {
            if (palette != null &&
                progress.Target is BlobObjectiveTarget blob &&
                blob.Color.HasValue)
            {
                BlobShaderColors colors = palette.GetRequired(blob.Color.Value);
                Tint(shadedGraphics, colors.BaseColor, colors.ShadowColor, colors.HighlightColor);
            }

            Render(progress);
        }

        public void Render(ObjectiveItemProgress progress)
        {
            if (countText != null)
                countText.SetText("{0}", progress.Remaining);

            if (completedRoot != null)
                completedRoot.SetActive(progress.IsComplete);
        }

        private void Tint(Graphic[] graphics, Color baseColor, Color shadowColor, Color highlightColor)
        {
            foreach (Graphic graphic in graphics)
            {
                if (graphic == null)
                    continue;

                Material shared = graphic.material;
                if (shared == null || !shared.HasProperty(BaseColorId))
                {
                    graphic.color = baseColor;
                    continue;
                }

                var instance = new Material(shared);
                instance.SetColor(BaseColorId, baseColor);
                instance.SetColor(ShadowColorId, shadowColor);
                instance.SetColor(HighlightColorId, highlightColor);
                graphic.material = instance;
                _tintMaterials.Add(instance);
            }
        }

        private void OnDestroy()
        {
            foreach (Material material in _tintMaterials)
            {
                if (material != null)
                    Destroy(material);
            }

            _tintMaterials.Clear();
        }
    }
}
