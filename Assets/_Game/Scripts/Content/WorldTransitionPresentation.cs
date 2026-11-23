using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Blobs.Content
{
    public enum TransitionEase
    {
        Linear,
        InSine,
        OutSine,
        InOutSine,
        InQuad,
        OutQuad,
        InOutQuad,
        InCubic,
        OutCubic,
        InOutCubic,
        InBack,
        OutBack,
        InOutBack
    }

    public static class TransitionEaseUtility
    {
        private const float BackOvershoot = 1.70158f;

        public static float Evaluate(TransitionEase ease, float value)
        {
            float t = Mathf.Clamp01(value);
            return ease switch
            {
                TransitionEase.InSine => 1f - Mathf.Cos(t * Mathf.PI * 0.5f),
                TransitionEase.OutSine => Mathf.Sin(t * Mathf.PI * 0.5f),
                TransitionEase.InOutSine => -(Mathf.Cos(Mathf.PI * t) - 1f) * 0.5f,
                TransitionEase.InQuad => t * t,
                TransitionEase.OutQuad => 1f - (1f - t) * (1f - t),
                TransitionEase.InOutQuad => t < 0.5f
                    ? 2f * t * t
                    : 1f - Mathf.Pow(-2f * t + 2f, 2f) * 0.5f,
                TransitionEase.InCubic => t * t * t,
                TransitionEase.OutCubic => 1f - Mathf.Pow(1f - t, 3f),
                TransitionEase.InOutCubic => t < 0.5f
                    ? 4f * t * t * t
                    : 1f - Mathf.Pow(-2f * t + 2f, 3f) * 0.5f,
                TransitionEase.InBack => (BackOvershoot + 1f) * t * t * t
                    - BackOvershoot * t * t,
                TransitionEase.OutBack => 1f
                    + (BackOvershoot + 1f) * Mathf.Pow(t - 1f, 3f)
                    + BackOvershoot * Mathf.Pow(t - 1f, 2f),
                TransitionEase.InOutBack => EvaluateInOutBack(t),
                _ => t
            };
        }

        private static float EvaluateInOutBack(float t)
        {
            float overshoot = BackOvershoot * 1.525f;
            return t < 0.5f
                ? Mathf.Pow(2f * t, 2f) * ((overshoot + 1f) * 2f * t - overshoot) * 0.5f
                : (Mathf.Pow(2f * t - 2f, 2f)
                    * ((overshoot + 1f) * (t * 2f - 2f) + overshoot) + 2f) * 0.5f;
        }
    }

    [Serializable]
    public sealed class TransitionFeedback
    {
        [SerializeField] private AudioClip audioClip;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;
        [SerializeField, Range(0.1f, 3f)] private float pitch = 1f;

        public AudioClip AudioClip => audioClip;
        public float Volume => Mathf.Clamp01(volume);
        public float Pitch => Mathf.Clamp(pitch, 0.1f, 3f);
    }

    /// <summary>
    /// Complete authored presentation for one blobster transition. Both the default transition
    /// asset and world definitions embed this same type so their fields cannot drift apart.
    /// </summary>
    [Serializable]
    public sealed class WorldTransitionPresentation
    {
        [Header("Look")]
        [FormerlySerializedAs("faceSprite")]
        [SerializeField] private Sprite eyesSprite;
        [SerializeField, HideInInspector] private Sprite mouthSprite;
        [SerializeField] private Color backgroundColor = Color.white;

        [Header("Mouth")]
        [SerializeField, Min(0.0001f)] private float mouthUnitRadius = 0.03f;
        [SerializeField, Tooltip("Examples: (2, 0.2) for a slit, (2, 1) for 2:1, or (2, 2) for a circle.")]
        private Vector2 closedMouthScale = new(2f, 0.2f);
        [SerializeField, HideInInspector, Min(0.01f)] private float lipVisualScale = 1.22f;
        [SerializeField, Min(0f), Tooltip("Constant black lip thickness in screen pixels.")]
        private float lipWidthPixels = 20f;
        [SerializeField, Min(1f), Tooltip("Multiplier applied to the small circular mouth during the short preparation growth.")]
        private float reshapeGrowthMultiplier = 1.35f;
        [SerializeField, Min(0.01f)] private float mouthReshapeDuration = 0.14f;
        [SerializeField] private TransitionEase reshapeEase = TransitionEase.OutBack;

        [Header("Composition Override")]
        [SerializeField] private bool overrideComposition;
        [SerializeField, Tooltip("Normalized mouth center. (0,0) is bottom-left; (1,1) is top-right.")]
        private Vector2 maskCenter = new(0.5f, 0.32f);
        [SerializeField] private bool faceFollowsMouth = true;
        [SerializeField, Tooltip("Eye position around the current lip radius. (-1,1) places it above-left. Eye size remains fixed.")]
        private Vector2 faceMouthOffset = new(-0.65f, 0.8f);
        [SerializeField, Tooltip("Absolute normalized anchor used when Face Follows Mouth is disabled.")]
        private Vector2 faceAnchor = new(0.5f, 0.82f);
        [SerializeField, Tooltip("Additional eye offset in reference-resolution pixels.")]
        private Vector2 facePosition = new(-60f, 220f);
        [SerializeField, Tooltip("Fixed eye dimensions in reference-resolution pixels.")]
        private Vector2 faceSize = new(360f, 360f);
        [SerializeField, Tooltip("Clockwise eye rotation in degrees.")]
        private float faceRotation;

        [Header("Timing Override")]
        [SerializeField] private bool overrideTiming;
        [SerializeField, Min(0.01f)] private float closeDuration = 0.42f;
        [SerializeField, Min(0f)] private float coveredHoldDuration = 1f;
        [SerializeField, Min(0.01f)] private float openDuration = 0.58f;
        [SerializeField] private TransitionEase closeEase = TransitionEase.InOutSine;
        [SerializeField] private TransitionEase openEase = TransitionEase.InOutSine;

        [Header("Game Tips")]
        [SerializeField, TextArea(2, 4)] private string[] gameTips = Array.Empty<string>();
        [SerializeField, Min(0.01f)] private float tipFadeDuration = 0.2f;
        [SerializeField, Min(0f)] private float tipDisplayDuration = 1.2f;
        [SerializeField, Min(1f)] private float tipFontSize = 54f;

        [Header("Closing Feedback")]
        [SerializeField] private TransitionFeedback closingFeedback = new();

        [Header("Opening Feedback")]
        [FormerlySerializedAs("revealFeedback")]
        [SerializeField] private TransitionFeedback openingFeedback = new();

        public Sprite EyesSprite => eyesSprite;
        public Sprite MouthSprite => mouthSprite;
        public Color BackgroundColor => backgroundColor;
        public Vector2 ClosedMouthRadii => new(
            Mathf.Max(0.0001f, mouthUnitRadius * Mathf.Abs(closedMouthScale.x)),
            Mathf.Max(0.0001f, mouthUnitRadius * Mathf.Abs(closedMouthScale.y)));
        public float LipVisualScale => Mathf.Max(0.01f, lipVisualScale);
        public float LipWidthPixels => Mathf.Max(0f, lipWidthPixels);
        public float ReshapeGrowthMultiplier => Mathf.Max(1f, reshapeGrowthMultiplier);
        public float MouthReshapeDuration => Mathf.Max(0.01f, mouthReshapeDuration);
        public TransitionEase ReshapeEase => reshapeEase;
        public bool OverrideComposition => overrideComposition;
        public Vector2 MaskCenter => maskCenter;
        public bool FaceFollowsMouth => faceFollowsMouth;
        public Vector2 FaceMouthOffset => faceMouthOffset;
        public Vector2 FaceAnchor => faceAnchor;
        public Vector2 FacePosition => facePosition;
        public Vector2 FaceSize => faceSize;
        public float FaceRotation => faceRotation;
        public bool OverrideTiming => overrideTiming;
        public float CloseDuration => Mathf.Max(0.01f, closeDuration);
        public float CoveredHoldDuration => Mathf.Max(0f, coveredHoldDuration);
        public float OpenDuration => Mathf.Max(0.01f, openDuration);
        public TransitionEase CloseEase => closeEase;
        public TransitionEase OpenEase => openEase;
        public string[] GameTips => gameTips ?? Array.Empty<string>();
        public float TipFadeDuration => Mathf.Max(0.01f, tipFadeDuration);
        public float TipDisplayDuration => Mathf.Max(0f, tipDisplayDuration);
        public float TipFontSize => Mathf.Max(1f, tipFontSize);
        public TransitionFeedback ClosingFeedback => closingFeedback;
        public TransitionFeedback OpeningFeedback => openingFeedback;
    }
}
