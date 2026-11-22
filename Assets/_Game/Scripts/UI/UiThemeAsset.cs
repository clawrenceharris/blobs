using System;
using TMPro;
using UnityEngine;

namespace Blobs.UI
{
    public enum TextRole
    {
        Heading,
        Title,
        Body,
        Informational,
        StrongInformational,
        OnPrimary,

    }

    public enum SurfaceRole
    {
        Background,
        MutedBackground,
        Locked,
        LockedShadow,
        WorldsHeader,
    }

    public enum ButtonRole
    {
        Primary,
        Secondary,
        Tertiary,
        Icon,
    }

    public enum ButtonShape
    {
        RoundedRect,
        Circle,
        Squircle,
    }

    [Serializable]
    public sealed class UiButtonSprite
    {
        [SerializeField] private Sprite normalSprite;
        [SerializeField] private Sprite pressedSprite;

        public Sprite NormalSprite => normalSprite;
        public Sprite PressedSprite => pressedSprite;
    }

    [Serializable]
    public sealed class UiButtonTheme
    {
        [SerializeField] private Color normalFill = Color.white;
        [SerializeField] private Color pressedFill = Color.gray;
        [SerializeField] private Color disabledFill = Color.gray;
        [SerializeField] private Color normalLabel = Color.white;
        [SerializeField] private Color pressedLabel = Color.white;
        [SerializeField] private Color disabledLabel = Color.gray;
        [SerializeField] private Color normalIcon = Color.black;
        [SerializeField] private Color disabledIcon = Color.gray;

        public Color NormalFill => normalFill;
        public Color PressedFill => pressedFill;
        public Color DisabledFill => disabledFill;
        public Color NormalLabel => normalLabel;
        public Color PressedLabel => pressedLabel;
        public Color DisabledLabel => disabledLabel;
        public Color NormalIcon => normalIcon;
        public Color DisabledIcon => disabledIcon;
    }

    [CreateAssetMenu(fileName = "UiTheme_New", menuName = "Blobs/UI/Theme")]
    public sealed class UiThemeAsset : ScriptableObject
    {
        [Header("Surfaces")]
        [SerializeField] private Color background = new(0.992f, 0.984f, 0.969f, 1f);
        [SerializeField] private Color mutedBackground = new(0.894f, 0.902f, 0.949f, 1f);
        [SerializeField] private Color lockedContent = new(0.431f, 0.451f, 0.565f, 1f);
        [SerializeField] private Color lockedShadow = new(0.431f, 0.451f, 0.565f, 0.35f);
        [SerializeField] private Color worldsHeader = new(0.992f, 0.984f, 0.969f, 1f);

        [Header("Text")]
        [SerializeField] private Color primaryText = new(0.202f, 0.194f, 0.283f, 1f);
        [SerializeField] private Color secondaryText = new(0.333f, 0.322f, 0.427f, 1f);
        [SerializeField] private Color informationalText = new(0.361f, 0.341f, 0.431f, 1f);
        [SerializeField] private Color strongInformationalText = new(0.361f, 0.341f, 0.431f, 1f);
        [SerializeField] private Color onPrimaryText = Color.white;

        [Header("Buttons")]
        [SerializeField] private UiButtonTheme primaryButton = new();
        [SerializeField] private UiButtonTheme secondaryButton = new();
        [SerializeField] private UiButtonTheme tertiaryButton = new();
        [SerializeField] private UiButtonTheme iconButton = new();

        [SerializeField] private UiButtonSprite circleSprite = new();
        [SerializeField] private UiButtonSprite squircleSprite = new();
        [SerializeField] private UiButtonSprite roundedRectSprite = new();

        [Header("Typography")]
        [SerializeField] private TMP_FontAsset primaryHeadingFont;
        [SerializeField] private TMP_FontAsset secondaryBodyFont;
        [SerializeField, Min(1f)] private float headingSize = 48f;
        [SerializeField, Min(1f)] private float titleSize = 42f;
        [SerializeField, Min(1f)] private float bodySize = 28f;
        [SerializeField, Min(1f)] private float metaSize = 24f;
        [SerializeField, Min(1f)] private float buttonSize = 72f;

        [Header("Spacing")]
        [SerializeField, Min(0f)] private float xs = 16f;
        [SerializeField, Min(0f)] private float sm = 24f;
        [SerializeField, Min(0f)] private float md = 40f;
        [SerializeField, Min(0f)] private float lg = 64f;
        [SerializeField, Min(0f)] private float cluster = 20f;

        public Color Background => background;
        public Color MutedBackground => mutedBackground;
        public Color LockedContent => lockedContent;
        public Color LockedShadow => lockedShadow;
        public Color PrimaryText => primaryText;
        public Color SecondaryText => secondaryText;
        public Color InformationalText => informationalText;
        public Color StrongInformationalText => strongInformationalText;
        public Color OnPrimaryText => onPrimaryText;
        public TMP_FontAsset PrimaryHeadingFont => primaryHeadingFont;
        public TMP_FontAsset SecondaryBodyFont => secondaryBodyFont;
        public float Xs => xs;
        public float Sm => sm;
        public float Md => md;
        public float Lg => lg;
        public float Cluster => cluster;

        public UiButtonTheme GetButton(ButtonRole role) => role switch
        {
            ButtonRole.Primary => primaryButton,
            ButtonRole.Secondary => secondaryButton,
            ButtonRole.Tertiary => tertiaryButton,
            ButtonRole.Icon => iconButton,
            _ => primaryButton,
        };

        public UiButtonSprite GetButtonSprite(ButtonShape shape) => shape switch
        {
            ButtonShape.Circle => circleSprite,
            ButtonShape.Squircle => squircleSprite,
            ButtonShape.RoundedRect => roundedRectSprite,
            _ => circleSprite,
        };

        public Color GetSurface(SurfaceRole role) => role switch
        {
            SurfaceRole.Background => background,
            SurfaceRole.MutedBackground => mutedBackground,
            SurfaceRole.Locked => lockedContent,
            SurfaceRole.LockedShadow => lockedShadow,
            SurfaceRole.WorldsHeader => worldsHeader,
            _ => Color.white,
        };

        public Color GetTextColor(TextRole role) => role switch
        {
            TextRole.Heading => primaryText,
            TextRole.Title => primaryText,
            TextRole.Body => secondaryText,
            TextRole.Informational => informationalText,
            TextRole.OnPrimary => onPrimaryText,
            TextRole.StrongInformational => strongInformationalText,
            _ => primaryText,
        };

        public TMP_FontAsset GetFont(TextRole role) => role switch
        {
            TextRole.Heading => primaryHeadingFont,
            TextRole.Title => primaryHeadingFont,
            TextRole.OnPrimary => primaryHeadingFont,
            _ => secondaryBodyFont,
        };

        public float GetFontSize(TextRole role) => role switch
        {
            TextRole.Heading => headingSize,
            TextRole.Title => titleSize,
            TextRole.Body => bodySize,
            TextRole.Informational => metaSize,
            TextRole.StrongInformational => metaSize,
            TextRole.OnPrimary => buttonSize,
            _ => bodySize,
        };
    }
}
