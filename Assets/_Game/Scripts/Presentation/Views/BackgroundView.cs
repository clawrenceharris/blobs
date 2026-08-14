using Blobs.Content;
using UnityEngine;

namespace Blobs.Presentation
{
    /// <summary>
    /// Applies level theme gradient values to the scene background and keeps it sized to the camera.
    /// </summary>
    public sealed class BackgroundView : MonoBehaviour
    {
        private static readonly int TopColorId =
            Shader.PropertyToID("_TopColor");

        private static readonly int MiddleColorId =
            Shader.PropertyToID("_MiddleColor");

        private static readonly int BottomColorId =
            Shader.PropertyToID("_BottomColor");

        private static readonly int MiddlePositionId =
            Shader.PropertyToID("_MiddlePosition");

        private static readonly int BlendSoftnessId =
            Shader.PropertyToID("_BlendSoftness");

        [SerializeField] private Camera targetCamera;
        [SerializeField] private SpriteRenderer spriteRenderer;

        private MaterialPropertyBlock _properties;
        private void Awake()
        {
            _properties = new();
        }
        /// <summary>
        /// Applies background colors and gradient tuning from the current level theme.
        /// </summary>
        public void Apply(LevelVisualThemeAsset theme)
        {
            if (theme == null || spriteRenderer == null)
                return;

            spriteRenderer.GetPropertyBlock(_properties);

            _properties.SetColor(TopColorId, theme.TopColor);
            _properties.SetColor(MiddleColorId, theme.MiddleColor);
            _properties.SetColor(BottomColorId, theme.BottomColor);
            _properties.SetFloat(
                MiddlePositionId,
                theme.MiddlePosition);
            _properties.SetFloat(
                BlendSoftnessId,
                theme.BlendSoftness);

            spriteRenderer.SetPropertyBlock(_properties);
        }

        private void LateUpdate()
        {
            FitToCamera();
        }

        private void FitToCamera()
        {
            if (targetCamera == null || spriteRenderer == null)
                return;

            float height =
                targetCamera.orthographicSize * 2f;

            float width =
                height * targetCamera.aspect;

            transform.position = new Vector3(
                targetCamera.transform.position.x,
                targetCamera.transform.position.y,
                0f);

            transform.localScale =
                new Vector3(width, height, 1f);
        }
    }
}
