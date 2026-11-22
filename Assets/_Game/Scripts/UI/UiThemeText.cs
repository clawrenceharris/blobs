using TMPro;
using UnityEngine;

namespace Blobs.UI
{
    [ExecuteAlways]
    [RequireComponent(typeof(TMP_Text))]
    public sealed class UiThemeText : MonoBehaviour
    {
        [SerializeField] private UiThemeAsset theme;
        [SerializeField] private TextRole role;

        public UiThemeAsset Theme => theme;
        public TextRole Role => role;

        private void OnEnable() => Apply();

#if UNITY_EDITOR
        private void OnValidate() => Apply();
#endif

        public void Configure(UiThemeAsset value, TextRole textRole)
        {
            theme = value;
            role = textRole;
            Apply();
        }

        public void Apply()
        {
            if (theme == null || !TryGetComponent(out TMP_Text text))
                return;

            TMP_FontAsset font = theme.GetFont(role);
            if (font != null)
                text.font = font;

            text.fontSize = theme.GetFontSize(role);
            text.color = theme.GetTextColor(role);
        }
    }
}
