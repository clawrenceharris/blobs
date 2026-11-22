using UnityEngine;
using UnityEngine.UI;

namespace Blobs.UI
{
    [ExecuteAlways]
    [RequireComponent(typeof(Image))]
    public sealed class UiThemeGraphic : MonoBehaviour
    {
        [SerializeField] private UiThemeAsset theme;
        [SerializeField] private SurfaceRole role;

        public UiThemeAsset Theme => theme;
        public SurfaceRole Role => role;

        private void OnEnable() => Apply();

#if UNITY_EDITOR
        private void OnValidate() => Apply();
#endif

        public void Configure(UiThemeAsset value, SurfaceRole surfaceRole)
        {
            theme = value;
            role = surfaceRole;
            Apply();
        }

        public void Apply()
        {
            if (theme != null && TryGetComponent(out Image image))
                image.color = theme.GetSurface(role);
        }
    }
}
