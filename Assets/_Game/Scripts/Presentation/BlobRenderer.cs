using System;
using UnityEngine;
namespace Blobs.Presentation
{
    public class BlobRenderer : MonoBehaviour
    {
        [Serializable]
        public class Target
        {
            public SkinColorRole Role = SkinColorRole.Base;
            public SpriteRenderer[] Renderers;
        }
        [SerializeField] private SpriteRenderer _fallbackBaseRenderer;
        [SerializeField] private Material _defaultMaterial;

        [SerializeField] private Target[] _targets;


        public Target[] Targets => _targets;
        public SpriteRenderer BaseRenderer => _fallbackBaseRenderer;

        public Material DefaultMaterial => _defaultMaterial;

        private void Awake()
        {
            if (_fallbackBaseRenderer == null)
                _fallbackBaseRenderer = TryGetComponent(out SpriteRenderer renderer) ? renderer : null;
        }

        public void ApplySkin(Skin skin)
        {
            ApplyRoleColor(SkinColorRole.Base, skin.BaseColor);
            ApplyRoleColor(SkinColorRole.Accent, skin.AccentColor);
            ApplyRoleColor(SkinColorRole.Detail, skin.DetailColor);
        }
        private void ApplyRoleColor(SkinColorRole role, Color color)
        {
            bool applied = false;
            if (_targets != null)
            {
                for (int i = 0; i < _targets.Length; i++)
                {
                    var binding = _targets[i];
                    if (binding == null || binding.Role != role || binding.Renderers == null) continue;

                    for (int j = 0; j < binding.Renderers.Length; j++)
                    {
                        if (binding.Renderers[j] == null) continue;
                        binding.Renderers[j].color = color;
                        applied = true;
                    }
                }
            }

            if (!applied && role == SkinColorRole.Base && _fallbackBaseRenderer != null)
                _fallbackBaseRenderer.color = color;
        }
        public void SetColor(Color color)
        {
            if (_targets != null)
            {
                for (int i = 0; i < _targets.Length; i++)
                {
                    var binding = _targets[i];
                    foreach (var sprite in binding.Renderers)
                    {
                        sprite.color = color;
                    }
                }

            }
            if (_fallbackBaseRenderer != null)
            {
                _fallbackBaseRenderer.color = color;
            }
        }

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
