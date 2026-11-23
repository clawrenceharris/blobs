using System;
using System.Collections;
using Blobs.Content;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Blobs.UI
{
    /// <summary>
    /// Owns the persistent transition overlay used by screen-level scene navigation.
    /// </summary>
    public static class BlobsterSceneTransition
    {
        private const string DefaultStyleResourcePath = "BlobsterTransition/Default";
        private static BlobsterTransitionController controller;

        public static bool IsRunning => controller != null && controller.IsRunning;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => controller = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateRuntimeOverlay() => EnsureController();

        /// <summary>
        /// Closes the stomach mask, loads the destination while covered, then opens it.
        /// Returns false when another transition is already running.
        /// </summary>
        public static bool LoadScene(
            string sceneName,
            BlobsterTransitionStyle style = null,
            WorldTransitionPresentation presentation = null)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
                throw new ArgumentException("A destination scene name is required.", nameof(sceneName));

            return EnsureController().Begin(sceneName, style, presentation);
        }

        /// <summary>
        /// Plays a close-and-open cycle without loading a scene. Useful for authoring previews
        /// and runtime validation of a transition style.
        /// </summary>
        public static bool PlayPreview(
            BlobsterTransitionStyle style = null,
            WorldTransitionPresentation presentation = null)
        {
            return EnsureController().BeginPreview(style, presentation);
        }

        /// <summary>
        /// Closes the mask, runs a synchronous navigation action while covered, then reveals the
        /// updated screen. This is used for level-to-level navigation inside the Game scene.
        /// </summary>
        public static bool PlayCoveredAction(
            Action coveredAction,
            WorldTransitionPresentation presentation = null,
            BlobsterTransitionStyle style = null)
        {
            if (coveredAction == null)
                throw new ArgumentNullException(nameof(coveredAction));

            return EnsureController().BeginCoveredAction(coveredAction, style, presentation);
        }

        private static BlobsterTransitionController EnsureController()
        {
            if (controller != null)
                return controller;

            var root = new GameObject("Blobster Scene Transition", typeof(RectTransform));
            UnityEngine.Object.DontDestroyOnLoad(root);
            controller = root.AddComponent<BlobsterTransitionController>();
            controller.Initialize(Resources.Load<BlobsterTransitionStyle>(DefaultStyleResourcePath));
            return controller;
        }
    }

    [DisallowMultipleComponent]
    internal sealed class BlobsterTransitionController : MonoBehaviour
    {
        private const string ShaderName = "Blobs/UI/Blobster Transition Mask";
        private static readonly int MaskCenterId = Shader.PropertyToID("_MaskCenter");
        private static readonly int SoftnessId = Shader.PropertyToID("_Softness");
        private static readonly int MouthRadiiId =
            Shader.PropertyToID("_MouthRadii");
        private static readonly int LipColorId = Shader.PropertyToID("_LipColor");
        private static readonly int LipWidthPixelsId = Shader.PropertyToID("_LipWidthPixels");

        private Canvas canvas;
        private Image overlay;
        private Image face;
        private RectTransform mouthRoot;
        private TMP_Text tipText;
        private AudioSource audioSource;
        private Material runtimeMaterial;
        private Material faceRuntimeMaterial;
        private Sequence tipSequence;
        private BlobsterTransitionStyle defaultStyle;
        private bool initialized;

        public bool IsRunning { get; private set; }

        public void Initialize(BlobsterTransitionStyle style)
        {
            if (initialized)
                return;

            defaultStyle = style;
            BuildCanvas();
            SetVisible(false);
            initialized = true;
        }

        public bool Begin(
            string sceneName,
            BlobsterTransitionStyle style,
            WorldTransitionPresentation presentation)
        {
            if (IsRunning)
                return false;

            BlobsterTransitionStyle activeStyle = style != null ? style : defaultStyle;
            if (activeStyle == null)
            {
                Debug.LogError("Blobster transition style is missing. Create Resources/BlobsterTransition/Default.asset.");
                SceneManager.LoadScene(sceneName);
                return true;
            }

            StartCoroutine(TransitionRoutine(sceneName, Resolve(activeStyle, presentation)));
            return true;
        }

        public bool BeginPreview(
            BlobsterTransitionStyle style,
            WorldTransitionPresentation presentation)
        {
            if (IsRunning)
                return false;

            BlobsterTransitionStyle activeStyle = style != null ? style : defaultStyle;
            if (activeStyle == null)
            {
                Debug.LogError("Blobster transition style is missing. Create Resources/BlobsterTransition/Default.asset.");
                return false;
            }

            StartCoroutine(PreviewRoutine(Resolve(activeStyle, presentation)));
            return true;
        }

        public bool BeginCoveredAction(
            Action coveredAction,
            BlobsterTransitionStyle style,
            WorldTransitionPresentation presentation)
        {
            if (IsRunning)
                return false;

            BlobsterTransitionStyle activeStyle = style != null ? style : defaultStyle;
            if (activeStyle == null)
            {
                Debug.LogError("Blobster transition style is missing. Create Resources/BlobsterTransition/Default.asset.");
                coveredAction();
                return true;
            }

            StartCoroutine(CoveredActionRoutine(coveredAction, Resolve(activeStyle, presentation)));
            return true;
        }

        private void BuildCanvas()
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            gameObject.AddComponent<GraphicRaycaster>();

            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;

            // 1. Full-screen colored layer containing the shader opening.
            overlay = CreateImage("Stomach Mask", transform);
            StretchToParent(overlay.rectTransform);
            overlay.raycastTarget = true;

            // 2. Container that follows the shader mouth opening.
            var mouthObject = new GameObject(
                "Mouth Root",
                typeof(RectTransform));

            mouthObject.transform.SetParent(transform, false);

            mouthRoot = mouthObject.GetComponent<RectTransform>();
            mouthRoot.anchorMin = new Vector2(0.5f, 0.42f);
            mouthRoot.anchorMax = new Vector2(0.5f, 0.42f);
            mouthRoot.pivot = new Vector2(0.5f, 0.5f);
            mouthRoot.anchoredPosition = Vector2.zero;

            // Temporary starting size. The animation will replace this.
            mouthRoot.sizeDelta = new Vector2(650f, 90f);

            // 3. Eye/face image. Created last so it renders above the shader-drawn lip.
            face = CreateImage("Blobster Eye", transform);
            face.preserveAspect = true;
            face.raycastTarget = false;

            var tipObject = new GameObject(
                "Game Tip",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            tipObject.transform.SetParent(transform, false);
            tipText = tipObject.GetComponent<TextMeshProUGUI>();
            tipText.alignment = TextAlignmentOptions.Center;
            tipText.raycastTarget = false;
            tipText.enableWordWrapping = true;
            tipText.color = Color.white;
            RectTransform tipRect = tipText.rectTransform;
            tipRect.anchorMin = new Vector2(0.1f, 0.5f);
            tipRect.anchorMax = new Vector2(0.9f, 0.5f);
            tipRect.pivot = new Vector2(0.5f, 0.5f);
            tipRect.anchoredPosition = Vector2.zero;
            tipRect.sizeDelta = new Vector2(0f, 320f);
            tipText.enabled = false;
        }


        private Tween TweenMouth(
    Vector2 from,
    Vector2 to,
    float duration,
            TransitionEase ease,
    ResolvedTransition transition)
        {
            Vector2 current = from;

            return DOTween.To(
                    () => current,
                    value =>
                    {
                        current = value;
                        SetMouthRadii(value, transition);
                    },
                    to,
                    duration)
                .SetEase(ToDotweenEase(ease))
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        }

        private IEnumerator TransitionRoutine(string sceneName, ResolvedTransition transition)
        {
            IsRunning = true;
            ApplyStyle(transition);
            SetVisible(true);

            Vector2 openRadii =
                CalculateOpenMouthRadii(transition.MaskCenter, transition.EdgeSoftness);
            Vector2 closedRadii = transition.ClosedMouthRadii;
            Vector2 preparedRadii = BlobsterTransitionAnimation.GetPreparedMouthRadii(
                closedRadii,
                transition.ReshapeGrowthMultiplier);
            SetMouthRadii(openRadii, transition);
            PlayFeedback(transition.ClosingFeedback);

            yield return TweenMouth(
                openRadii,
                preparedRadii,
                transition.CloseDuration,
                transition.CloseEase,
                transition).WaitForCompletion();
            yield return TweenMouth(
                preparedRadii,
                closedRadii,
                transition.MouthReshapeDuration,
                transition.ReshapeEase,
                transition).WaitForCompletion();
            SetMouthRadii(closedRadii, transition);
            StartTips(transition);

            // Always render the fully covered state before beginning a potentially expensive load.
            yield return null;

            if (transition.CoveredHoldDuration > 0f)
                yield return new WaitForSecondsRealtime(transition.CoveredHoldDuration);

            AsyncOperation loadOperation;
            try
            {
                loadOperation = SceneManager.LoadSceneAsync(sceneName);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                FinishImmediately();
                yield break;
            }

            if (loadOperation == null)
            {
                Debug.LogError($"Could not start loading scene '{sceneName}'.");
                FinishImmediately();
                yield break;
            }

            while (!loadOperation.isDone)
                yield return null;

            // Let the destination Canvas complete one layout pass while fully covered.
            yield return null;

            PlayFeedback(transition.OpeningFeedback);
            yield return TweenMouth(
                closedRadii,
                preparedRadii,
                transition.MouthReshapeDuration,
                transition.ReshapeEase,
                transition).WaitForCompletion();
            yield return TweenMouth(
                preparedRadii,
                openRadii,
                transition.OpenDuration,
                transition.OpenEase,
                transition).WaitForCompletion();
            SetMouthRadii(openRadii, transition);
            FinishImmediately();
        }

        private IEnumerator PreviewRoutine(ResolvedTransition transition)
        {
            IsRunning = true;
            ApplyStyle(transition);
            SetVisible(true);

            Vector2 openRadii =
                CalculateOpenMouthRadii(transition.MaskCenter, transition.EdgeSoftness);
            Vector2 closedRadii = transition.ClosedMouthRadii;
            Vector2 preparedRadii = BlobsterTransitionAnimation.GetPreparedMouthRadii(
                closedRadii,
                transition.ReshapeGrowthMultiplier);
            SetMouthRadii(openRadii, transition);
            PlayFeedback(transition.ClosingFeedback);

            yield return TweenMouth(
                openRadii,
                preparedRadii,
                transition.CloseDuration,
                transition.CloseEase,
                transition).WaitForCompletion();
            yield return TweenMouth(
                preparedRadii,
                closedRadii,
                transition.MouthReshapeDuration,
                transition.ReshapeEase,
                transition).WaitForCompletion();
            SetMouthRadii(closedRadii, transition);
            StartTips(transition);
            yield return null;

            if (transition.CoveredHoldDuration > 0f)
                yield return new WaitForSecondsRealtime(transition.CoveredHoldDuration);

            PlayFeedback(transition.OpeningFeedback);
            yield return TweenMouth(
                closedRadii,
                preparedRadii,
                transition.MouthReshapeDuration,
                transition.ReshapeEase,
                transition).WaitForCompletion();
            yield return TweenMouth(
                preparedRadii,
                openRadii,
                transition.OpenDuration,
                transition.OpenEase,
                transition).WaitForCompletion();
            SetMouthRadii(openRadii, transition);
            FinishImmediately();
        }

        private IEnumerator CoveredActionRoutine(Action coveredAction, ResolvedTransition transition)
        {
            IsRunning = true;
            ApplyStyle(transition);
            SetVisible(true);

            Vector2 openRadii = CalculateOpenMouthRadii(transition.MaskCenter, transition.EdgeSoftness);
            Vector2 closedRadii = transition.ClosedMouthRadii;
            Vector2 preparedRadii = BlobsterTransitionAnimation.GetPreparedMouthRadii(
                closedRadii,
                transition.ReshapeGrowthMultiplier);
            SetMouthRadii(openRadii, transition);
            PlayFeedback(transition.ClosingFeedback);

            yield return TweenMouth(
                openRadii,
                preparedRadii,
                transition.CloseDuration,
                transition.CloseEase,
                transition).WaitForCompletion();
            yield return TweenMouth(
                preparedRadii,
                closedRadii,
                transition.MouthReshapeDuration,
                transition.ReshapeEase,
                transition).WaitForCompletion();
            SetMouthRadii(closedRadii, transition);
            StartTips(transition);
            yield return null;

            try
            {
                coveredAction();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }

            // Give in-scene level navigation one rendered frame to rebuild its board and HUD.
            yield return null;
            if (transition.CoveredHoldDuration > 0f)
                yield return new WaitForSecondsRealtime(transition.CoveredHoldDuration);

            PlayFeedback(transition.OpeningFeedback);
            yield return TweenMouth(
                closedRadii,
                preparedRadii,
                transition.MouthReshapeDuration,
                transition.ReshapeEase,
                transition).WaitForCompletion();
            yield return TweenMouth(
                preparedRadii,
                openRadii,
                transition.OpenDuration,
                transition.OpenEase,
                transition).WaitForCompletion();
            SetMouthRadii(openRadii, transition);
            FinishImmediately();
        }

        private void ApplyStyle(ResolvedTransition transition)
        {
            if (runtimeMaterial != null)
                Destroy(runtimeMaterial);
            if (faceRuntimeMaterial != null)
                Destroy(faceRuntimeMaterial);

            Material sourceMaterial = transition.MaskMaterial;
            if (sourceMaterial != null)
                runtimeMaterial = new Material(sourceMaterial);
            else
            {
                Shader shader = Shader.Find(ShaderName);
                if (shader == null)
                    throw new InvalidOperationException($"Required shader '{ShaderName}' was not found.");
                runtimeMaterial = new Material(shader);
            }

            runtimeMaterial.name = "Blobster Transition Mask (Runtime)";
            runtimeMaterial.SetVector(MaskCenterId, transition.MaskCenter);
            runtimeMaterial.SetFloat(SoftnessId, transition.EdgeSoftness);
            runtimeMaterial.SetColor(LipColorId, Color.black);
            runtimeMaterial.SetFloat(LipWidthPixelsId, transition.LipWidthPixels);

            // The eye uses the same opening mask so it is swallowed naturally, but must not
            // render another copy of the lip inside its own UI rectangle.
            faceRuntimeMaterial = new Material(runtimeMaterial)
            {
                name = "Blobster Transition Eye Mask (Runtime)"
            };
            faceRuntimeMaterial.SetFloat(LipWidthPixelsId, 0f);

            overlay.material = runtimeMaterial;
            overlay.color = transition.BackgroundColor;
            face.material = faceRuntimeMaterial;
            face.sprite = transition.EyesSprite;
            face.enabled = transition.EyesSprite != null;

            RectTransform faceRect = face.rectTransform;
            Vector2 faceAnchor = transition.FaceFollowsMouth
                ? transition.MaskCenter
                : transition.FaceAnchor;
            faceRect.anchorMin = faceAnchor;
            faceRect.anchorMax = faceAnchor;
            faceRect.pivot = new Vector2(0.5f, 0.5f);
            faceRect.anchoredPosition = transition.FacePosition;
            faceRect.sizeDelta = transition.FaceSize;
            faceRect.localRotation = Quaternion.Euler(0f, 0f, -transition.FaceRotation);

            mouthRoot.anchorMin = transition.MaskCenter;
            mouthRoot.anchorMax = transition.MaskCenter;
            mouthRoot.anchoredPosition = Vector2.zero;

            tipText.fontSize = transition.TipFontSize;
            tipText.text = string.Empty;
            tipText.enabled = false;
        }

        private void PlayFeedback(TransitionFeedback feedback)
        {
            if (feedback?.AudioClip == null || audioSource == null)
                return;

            audioSource.pitch = feedback.Pitch;
            audioSource.PlayOneShot(feedback.AudioClip, feedback.Volume);
        }

        private static ResolvedTransition Resolve(
            BlobsterTransitionStyle style,
            WorldTransitionPresentation presentation)
        {
            WorldTransitionPresentation defaults = style.Presentation;
            if (defaults == null)
                throw new InvalidOperationException("Default transition presentation is missing.");

            bool overrideTiming = presentation != null && presentation.OverrideTiming;
            bool overrideComposition = presentation != null && presentation.OverrideComposition;
            WorldTransitionPresentation timingSettings = overrideTiming ? presentation : defaults;
            WorldTransitionPresentation compositionSettings = overrideComposition ? presentation : defaults;

            Sprite eyesSprite =
            presentation?.EyesSprite != null
                ? presentation.EyesSprite
                : defaults.EyesSprite;

            TransitionFeedback closingFeedback = presentation?.ClosingFeedback?.AudioClip != null
                ? presentation.ClosingFeedback
                : defaults.ClosingFeedback;
            TransitionFeedback openingFeedback = presentation?.OpeningFeedback?.AudioClip != null
                ? presentation.OpeningFeedback
                : defaults.OpeningFeedback;
            string[] gameTips = presentation != null && presentation.GameTips.Length > 0
                ? presentation.GameTips
                : defaults.GameTips;

            return new ResolvedTransition(
                eyesSprite,
                presentation != null ? presentation.BackgroundColor : defaults.BackgroundColor,
                style.MaskMaterial,
                compositionSettings.MaskCenter,
                compositionSettings.FaceFollowsMouth,
                compositionSettings.FaceMouthOffset,
                compositionSettings.FaceAnchor,
                compositionSettings.FacePosition,
                compositionSettings.FaceSize,
                compositionSettings.FaceRotation,
                style.EdgeSoftness,
                timingSettings.CloseDuration,
                timingSettings.CoveredHoldDuration,
                timingSettings.OpenDuration,
                compositionSettings.MouthReshapeDuration,
                timingSettings.CloseEase,
                compositionSettings.ReshapeEase,
                timingSettings.OpenEase,
                compositionSettings.LipWidthPixels,
                compositionSettings.ClosedMouthRadii,
                compositionSettings.ReshapeGrowthMultiplier,
                gameTips,
                presentation?.TipFadeDuration ?? defaults.TipFadeDuration,
                presentation?.TipDisplayDuration ?? defaults.TipDisplayDuration,
                presentation?.TipFontSize ?? defaults.TipFontSize,
                closingFeedback,
                openingFeedback);
        }

        private void SetMouthRadii(Vector2 radii, ResolvedTransition transition)
        {
            runtimeMaterial.SetVector(MouthRadiiId, radii);

            RectTransform canvasRect = (RectTransform)transform;
            float canvasHeight = Mathf.Max(1f, canvasRect.rect.height);
            mouthRoot.sizeDelta = radii * (canvasHeight * 2f);

            if (transition.FaceFollowsMouth)
            {
                face.rectTransform.anchoredPosition = Vector2.Scale(
                    mouthRoot.sizeDelta * 0.5f,
                    transition.FaceMouthOffset) + transition.FacePosition;
            }
        }

        private void StartTips(ResolvedTransition transition)
        {
            StopTips();
            if (tipText == null || transition.GameTips == null || transition.GameTips.Length == 0)
                return;

            tipText.enabled = true;
            tipText.alpha = 0f;
            tipSequence = DOTween.Sequence()
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);

            int validTipCount = 0;
            foreach (string tip in transition.GameTips)
            {
                if (string.IsNullOrWhiteSpace(tip))
                    continue;

                string capturedTip = tip.Trim();
                tipSequence
                    .AppendCallback(() => tipText.text = capturedTip)
                    .Append(tipText.DOFade(1f, transition.TipFadeDuration))
                    .AppendInterval(transition.TipDisplayDuration)
                    .Append(tipText.DOFade(0f, transition.TipFadeDuration));
                validTipCount++;
            }

            if (validTipCount == 0)
            {
                StopTips();
                return;
            }

            tipSequence.SetLoops(-1, LoopType.Restart);
        }

        private void StopTips()
        {
            tipSequence?.Kill();
            tipSequence = null;
            if (tipText == null)
                return;

            tipText.text = string.Empty;
            tipText.alpha = 0f;
            tipText.enabled = false;
        }

        private static Ease ToDotweenEase(TransitionEase ease)
        {
            return ease switch
            {
                TransitionEase.InSine => Ease.InSine,
                TransitionEase.OutSine => Ease.OutSine,
                TransitionEase.InOutSine => Ease.InOutSine,
                TransitionEase.InQuad => Ease.InQuad,
                TransitionEase.OutQuad => Ease.OutQuad,
                TransitionEase.InOutQuad => Ease.InOutQuad,
                TransitionEase.InCubic => Ease.InCubic,
                TransitionEase.OutCubic => Ease.OutCubic,
                TransitionEase.InOutCubic => Ease.InOutCubic,
                TransitionEase.InBack => Ease.InBack,
                TransitionEase.OutBack => Ease.OutBack,
                TransitionEase.InOutBack => Ease.InOutBack,
                _ => Ease.Linear
            };
        }

        private void SetVisible(bool visible)
        {
            if (canvas != null)
                canvas.enabled = visible;
        }

        private void FinishImmediately()
        {
            StopTips();
            SetVisible(false);
            IsRunning = false;
        }

        private static Vector2 CalculateOpenMouthRadii(Vector2 center, float softness)
        {
            float height = Mathf.Max(1f, Screen.height);
            float aspect = Mathf.Max(1f, Screen.width) / height;
            return BlobsterTransitionAnimation.CalculateOpenMouthRadii(center, softness, aspect);
        }

        private static Image CreateImage(string objectName, Transform parent)
        {
            var child = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            child.transform.SetParent(parent, false);
            return child.GetComponent<Image>();
        }

        private static void StretchToParent(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;
        }

        private void OnDestroy()
        {
            StopTips();
            if (runtimeMaterial != null)
                Destroy(runtimeMaterial);
            if (faceRuntimeMaterial != null)
                Destroy(faceRuntimeMaterial);
        }

        private readonly struct ResolvedTransition
        {
            public ResolvedTransition(
                Sprite eyesSprite,
                Color backgroundColor,
                Material maskMaterial,
                Vector2 maskCenter,
                bool faceFollowsMouth,
                Vector2 faceMouthOffset,
                Vector2 faceAnchor,
                Vector2 facePosition,
                Vector2 faceSize,
                float faceRotation,
                float edgeSoftness,
                float closeDuration,
                float coveredHoldDuration,
                float openDuration,
                float mouthReshapeDuration,
                TransitionEase closeEase,
                TransitionEase reshapeEase,
                TransitionEase openEase,
                float lipWidthPixels,
                Vector2 closedMouthRadii,
                float reshapeGrowthMultiplier,
                string[] gameTips,
                float tipFadeDuration,
                float tipDisplayDuration,
                float tipFontSize,
                TransitionFeedback closingFeedback,
                TransitionFeedback openingFeedback)
            {
                EyesSprite = eyesSprite;
                BackgroundColor = backgroundColor;
                MaskMaterial = maskMaterial;
                MaskCenter = maskCenter;
                FaceFollowsMouth = faceFollowsMouth;
                FaceMouthOffset = faceMouthOffset;
                FaceAnchor = faceAnchor;
                FacePosition = facePosition;
                FaceSize = faceSize;
                FaceRotation = faceRotation;
                EdgeSoftness = edgeSoftness;
                CloseDuration = closeDuration;
                CoveredHoldDuration = coveredHoldDuration;
                OpenDuration = openDuration;
                MouthReshapeDuration = mouthReshapeDuration;
                CloseEase = closeEase;
                ReshapeEase = reshapeEase;
                OpenEase = openEase;
                LipWidthPixels = lipWidthPixels;
                ClosedMouthRadii = closedMouthRadii;
                ReshapeGrowthMultiplier = reshapeGrowthMultiplier;
                GameTips = gameTips;
                TipFadeDuration = tipFadeDuration;
                TipDisplayDuration = tipDisplayDuration;
                TipFontSize = tipFontSize;
                ClosingFeedback = closingFeedback;
                OpeningFeedback = openingFeedback;
            }

            public Sprite EyesSprite { get; }
            public Color BackgroundColor { get; }
            public Material MaskMaterial { get; }
            public Vector2 MaskCenter { get; }
            public bool FaceFollowsMouth { get; }
            public Vector2 FaceMouthOffset { get; }
            public Vector2 FaceAnchor { get; }
            public Vector2 FacePosition { get; }
            public Vector2 FaceSize { get; }
            public float FaceRotation { get; }
            public float EdgeSoftness { get; }
            public float CloseDuration { get; }
            public float CoveredHoldDuration { get; }
            public float OpenDuration { get; }
            public float MouthReshapeDuration { get; }
            public TransitionEase CloseEase { get; }
            public TransitionEase ReshapeEase { get; }
            public TransitionEase OpenEase { get; }
            public float LipWidthPixels { get; }
            public Vector2 ClosedMouthRadii { get; }
            public float ReshapeGrowthMultiplier { get; }
            public string[] GameTips { get; }
            public float TipFadeDuration { get; }
            public float TipDisplayDuration { get; }
            public float TipFontSize { get; }
            public TransitionFeedback ClosingFeedback { get; }
            public TransitionFeedback OpeningFeedback { get; }
        }
    }
}
