using Blobs.Content;
using Blobs.UI;
using UnityEditor;
using UnityEngine;

namespace Blobs.Editor
{
    /// <summary>
    /// Provides an edit-mode, scrub-able preview of the world transition without entering Play Mode.
    /// The preview samples the same timing curves and mouth-radius calculations as the runtime.
    /// </summary>
    [CustomEditor(typeof(WorldDefinitionAsset))]
    public sealed class WorldDefinitionAssetEditor : UnityEditor.Editor
    {
        private const string DefaultStyleAssetPath =
            "Assets/_Game/Resources/BlobsterTransition/Default.asset";
        private const float ReferenceWidth = 1080f;
        private const float ReferenceHeight = 1920f;
        private const float PaddingGuide = 60f;
        private const float PreviewHeight = 430f;
        private const float PreviewAspect = ReferenceWidth / ReferenceHeight;

        private BlobsterTransitionStyle defaultStyle;
        private bool isPlaying;
        private bool loopPlayback;
        private bool initializedPreviewTime;
        private bool hasStartedPlayback;
        private float previewTime;
        private double previousEditorTime;

        private void OnEnable()
        {
            defaultStyle = AssetDatabase.LoadAssetAtPath<BlobsterTransitionStyle>(
                DefaultStyleAssetPath);
            previousEditorTime = EditorApplication.timeSinceStartup;
            EditorApplication.update += UpdatePlayback;
        }

        private void OnDisable()
        {
            EditorApplication.update -= UpdatePlayback;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var world = (WorldDefinitionAsset)target;
            if (world.Transition == null)
                return;

            PreviewSettings settings = ResolveSettings(world.Transition, defaultStyle);
            float duration = settings.Duration;
            if (!initializedPreviewTime)
            {
                previewTime = settings.CloseDuration;
                initializedPreviewTime = true;
            }

            previewTime = Mathf.Clamp(previewTime, 0f, duration);

            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField("Mouth Transition Preview", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Play or scrub the transition without entering Play Mode. The mouth closes, " +
                "holds while covered, reshapes Closed Mouth Scale (X,Y) into the small circle " +
                "(X,X), then grows uniformly. Cyan marks Mask Center and yellow marks the eye " +
                "bounds.",
                MessageType.Info);

            DrawPlaybackControls(settings);
            DrawCompositionPreview(settings, previewTime);

            if (!world.Transition.OverrideComposition)
            {
                EditorGUILayout.HelpBox(
                    "Override Composition is disabled. The preview and runtime use the default " +
                    "style's mask and eye placement, while retaining this world's color and art.",
                    MessageType.Warning);
            }

        }

        private void DrawPlaybackControls(PreviewSettings settings)
        {
            float duration = settings.Duration;
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            string playLabel = isPlaying ? "Pause" : "Play";
            if (GUILayout.Button(playLabel, EditorStyles.toolbarButton, GUILayout.Width(52f)))
            {
                if (isPlaying)
                {
                    isPlaying = false;
                }
                else
                {
                    if (!hasStartedPlayback || previewTime >= duration)
                        previewTime = 0f;

                    isPlaying = true;
                    hasStartedPlayback = true;
                    previousEditorTime = EditorApplication.timeSinceStartup;
                }
            }

            if (GUILayout.Button("Restart", EditorStyles.toolbarButton, GUILayout.Width(58f)))
            {
                previewTime = 0f;
                hasStartedPlayback = true;
                previousEditorTime = EditorApplication.timeSinceStartup;
                Repaint();
            }

            loopPlayback = GUILayout.Toggle(
                loopPlayback,
                "Loop",
                EditorStyles.toolbarButton,
                GUILayout.Width(42f));

            GUILayout.FlexibleSpace();
            GUILayout.Label(GetPhaseLabel(settings, previewTime), EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUI.BeginChangeCheck();
            float scrubbedTime = EditorGUILayout.Slider(previewTime, 0f, duration);
            if (EditorGUI.EndChangeCheck())
            {
                previewTime = scrubbedTime;
                isPlaying = false;
                hasStartedPlayback = true;
                Repaint();
            }

            EditorGUILayout.LabelField(
                $"{previewTime:0.00}s / {duration:0.00}s",
                EditorStyles.centeredGreyMiniLabel);
        }

        private void UpdatePlayback()
        {
            double now = EditorApplication.timeSinceStartup;
            double delta = now - previousEditorTime;
            previousEditorTime = now;

            if (!isPlaying || target == null)
                return;

            var world = (WorldDefinitionAsset)target;
            if (world.Transition == null)
                return;

            PreviewSettings settings = ResolveSettings(world.Transition, defaultStyle);
            float duration = settings.Duration;
            previewTime += Mathf.Max(0f, (float)delta);

            if (previewTime >= duration)
            {
                if (loopPlayback)
                    previewTime = Mathf.Repeat(previewTime, duration);
                else
                {
                    previewTime = duration;
                    isPlaying = false;
                }
            }

            Repaint();
        }

        private static void DrawCompositionPreview(PreviewSettings settings, float elapsed)
        {
            Rect available = GUILayoutUtility.GetRect(
                180f,
                PreviewHeight,
                GUILayout.ExpandWidth(true));
            Rect screen = FitPortraitRect(available);
            float referenceScale = screen.width / ReferenceWidth;

            Vector2 openRadii = BlobsterTransitionAnimation.CalculateOpenMouthRadii(
                settings.MaskCenter,
                settings.EdgeSoftness,
                PreviewAspect);
            Vector2 mouthRadii = BlobsterTransitionAnimation.EvaluateMouthRadii(
                elapsed,
                openRadii,
                settings.ClosedMouthRadii,
                settings.ReshapeGrowthMultiplier,
                settings.CloseDuration,
                settings.ReshapeDuration,
                settings.HoldDuration,
                settings.OpenDuration,
                settings.CloseEase,
                settings.ReshapeEase,
                settings.OpenEase);

            GUI.BeginClip(screen);
            Rect localScreen = new(Vector2.zero, screen.size);
            EditorGUI.DrawRect(localScreen, settings.BackgroundColor);
            DrawFace(localScreen, referenceScale, settings, mouthRadii, false);
            DrawDestinationInsideMouth(localScreen, settings.MaskCenter, mouthRadii);
            DrawMouth(localScreen, settings, mouthRadii);
            DrawTip(localScreen, settings, elapsed);
            GUI.EndClip();

            DrawScreenOutline(screen);
            DrawPaddingGuide(screen, PaddingGuide * referenceScale);
            DrawMaskCenter(screen, settings.MaskCenter);
            DrawFace(screen, referenceScale, settings, mouthRadii, true);

            GUI.Label(
                new Rect(screen.x + 6f, screen.y + 5f, screen.width - 12f, 18f),
                "1080 × 1920 authoring reference",
                EditorStyles.miniLabel);
        }

        private static void DrawDestinationInsideMouth(
            Rect screen,
            Vector2 normalizedCenter,
            Vector2 radii)
        {
            Vector2 center = new(
                screen.x + normalizedCenter.x * screen.width,
                screen.yMax - normalizedCenter.y * screen.height);
            float radiusY = Mathf.Max(0.0001f, radii.y);
            const float rowHeight = 2f;

            for (float y = screen.yMin; y < screen.yMax; y += rowHeight)
            {
                float normalizedY = ((y + rowHeight * 0.5f) - center.y) / screen.height;
                float ellipseY = normalizedY / radiusY;
                if (Mathf.Abs(ellipseY) >= 1f)
                    continue;

                float halfWidth = radii.x
                    * Mathf.Sqrt(Mathf.Max(0f, 1f - ellipseY * ellipseY))
                    * screen.height;
                float xMin = Mathf.Max(screen.xMin, center.x - halfWidth);
                float xMax = Mathf.Min(screen.xMax, center.x + halfWidth);
                if (xMax <= xMin)
                    continue;

                int band = Mathf.FloorToInt((y - screen.yMin) / 46f);
                Color destinationColor = band % 2 == 0
                    ? new Color(0.95f, 0.79f, 0.68f, 1f)
                    : new Color(0.92f, 0.74f, 0.63f, 1f);
                EditorGUI.DrawRect(new Rect(xMin, y, xMax - xMin, rowHeight), destinationColor);
            }
        }

        private static void DrawMouth(
            Rect screen,
            PreviewSettings settings,
            Vector2 radii)
        {
            Vector2 center = new(
                screen.x + settings.MaskCenter.x * screen.width,
                screen.yMax - settings.MaskCenter.y * screen.height);
            Vector2 size = radii * (screen.height * 2f);
            float previewLipWidth = settings.LipWidthPixels * screen.width / ReferenceWidth;
            DrawEllipseOutline(center, size * 0.5f, Color.black, previewLipWidth);
        }

        private static void DrawFace(
            Rect screen,
            float referenceScale,
            PreviewSettings settings,
            Vector2 mouthRadii,
            bool boundsOnly)
        {
            Vector2 center;
            if (settings.FaceFollowsMouth)
            {
                Vector2 mouthCenter = new(
                    screen.x + settings.MaskCenter.x * screen.width,
                    screen.yMax - settings.MaskCenter.y * screen.height);
                Vector2 lipSize = mouthRadii
                    * (screen.height * 2f);
                Vector2 offset = Vector2.Scale(lipSize * 0.5f, settings.FaceMouthOffset);
                center = mouthCenter + new Vector2(offset.x, -offset.y)
                    + new Vector2(
                        settings.FacePosition.x * referenceScale,
                        -settings.FacePosition.y * referenceScale);
            }
            else
            {
                center = new Vector2(
                    screen.x + settings.FaceAnchor.x * screen.width
                        + settings.FacePosition.x * referenceScale,
                    screen.yMax - settings.FaceAnchor.y * screen.height
                        - settings.FacePosition.y * referenceScale);
            }
            Vector2 size = settings.FaceSize * referenceScale;
            Rect bounds = new(center - size * 0.5f, size);

            if (!boundsOnly)
            {
                Matrix4x4 previousMatrix = GUI.matrix;
                GUIUtility.RotateAroundPivot(settings.FaceRotation, center);
                if (settings.EyesSprite != null && settings.EyesSprite.texture != null)
                    DrawSprite(bounds, settings.EyesSprite);
                GUI.matrix = previousMatrix;
                return;
            }

            Vector2[] corners =
            {
                RotateAround(bounds.min, center, settings.FaceRotation),
                RotateAround(new Vector2(bounds.xMax, bounds.yMin), center, settings.FaceRotation),
                RotateAround(bounds.max, center, settings.FaceRotation),
                RotateAround(new Vector2(bounds.xMin, bounds.yMax), center, settings.FaceRotation),
            };

            Handles.BeginGUI();
            Handles.color = Color.yellow;
            Handles.DrawAAPolyLine(
                2f,
                corners[0],
                corners[1],
                corners[2],
                corners[3],
                corners[0]);
            Handles.DrawWireDisc(center, Vector3.forward, 3f);
            Handles.EndGUI();
        }

        private static void DrawSprite(Rect bounds, Sprite sprite)
        {
            Texture2D texture = sprite.texture;
            Rect textureRect = sprite.textureRect;
            Rect uv = new(
                textureRect.x / texture.width,
                textureRect.y / texture.height,
                textureRect.width / texture.width,
                textureRect.height / texture.height);
            GUI.DrawTextureWithTexCoords(bounds, texture, uv, true);
        }

        private static void DrawTip(Rect screen, PreviewSettings settings, float elapsed)
        {
            if (settings.GameTips == null || settings.GameTips.Length == 0)
                return;

            float tipElapsed = elapsed - settings.CloseDuration - settings.ReshapeDuration;
            if (tipElapsed < 0f)
                return;

            float cycleDuration = settings.TipFadeDuration * 2f + settings.TipDisplayDuration;
            if (cycleDuration <= 0f)
                return;

            int index = Mathf.FloorToInt(tipElapsed / cycleDuration) % settings.GameTips.Length;
            string tip = settings.GameTips[index];
            if (string.IsNullOrWhiteSpace(tip))
                return;

            float cycleTime = Mathf.Repeat(tipElapsed, cycleDuration);
            float alpha = cycleTime < settings.TipFadeDuration
                ? cycleTime / settings.TipFadeDuration
                : cycleTime > settings.TipFadeDuration + settings.TipDisplayDuration
                    ? 1f - (cycleTime - settings.TipFadeDuration - settings.TipDisplayDuration)
                        / settings.TipFadeDuration
                    : 1f;

            var style = new GUIStyle(EditorStyles.wordWrappedLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.Max(10, Mathf.RoundToInt(settings.TipFontSize * screen.width / ReferenceWidth))
            };
            style.normal.textColor = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha));
            GUI.Label(
                new Rect(screen.x + screen.width * 0.1f, screen.center.y - 90f,
                    screen.width * 0.8f, 180f),
                tip,
                style);
        }

        private static void DrawEllipseOutline(
            Vector2 center,
            Vector2 radii,
            Color color,
            float width)
        {
            const int segmentCount = 64;
            var points = new Vector3[segmentCount + 1];
            for (int index = 0; index <= segmentCount; index++)
            {
                float angle = index / (float)segmentCount * Mathf.PI * 2f;
                points[index] = center + new Vector2(
                    Mathf.Cos(angle) * radii.x,
                    Mathf.Sin(angle) * radii.y);
            }

            Handles.BeginGUI();
            Handles.color = color;
            Handles.DrawAAPolyLine(width, points);
            Handles.EndGUI();
        }

        private static Rect FitPortraitRect(Rect available)
        {
            float width = Mathf.Min(available.width, available.height * PreviewAspect);
            float height = width / PreviewAspect;
            if (height > available.height)
            {
                height = available.height;
                width = height * PreviewAspect;
            }

            return new Rect(
                available.x + (available.width - width) * 0.5f,
                available.y + (available.height - height) * 0.5f,
                width,
                height);
        }

        private static void DrawScreenOutline(Rect screen)
        {
            Handles.BeginGUI();
            Handles.color = new Color(0f, 0f, 0f, 0.65f);
            Handles.DrawAAPolyLine(
                2f,
                new Vector3(screen.xMin, screen.yMin),
                new Vector3(screen.xMax, screen.yMin),
                new Vector3(screen.xMax, screen.yMax),
                new Vector3(screen.xMin, screen.yMax),
                new Vector3(screen.xMin, screen.yMin));
            Handles.EndGUI();
        }

        private static void DrawPaddingGuide(Rect screen, float padding)
        {
            Rect guide = new(
                screen.x + padding,
                screen.y + padding,
                screen.width - padding * 2f,
                screen.height - padding * 2f);

            Handles.BeginGUI();
            Handles.color = new Color(0.25f, 0.25f, 0.25f, 0.65f);
            Handles.DrawDottedLine(new Vector3(guide.xMin, guide.yMin), new Vector3(guide.xMax, guide.yMin), 4f);
            Handles.DrawDottedLine(new Vector3(guide.xMax, guide.yMin), new Vector3(guide.xMax, guide.yMax), 4f);
            Handles.DrawDottedLine(new Vector3(guide.xMax, guide.yMax), new Vector3(guide.xMin, guide.yMax), 4f);
            Handles.DrawDottedLine(new Vector3(guide.xMin, guide.yMax), new Vector3(guide.xMin, guide.yMin), 4f);
            Handles.EndGUI();
        }

        private static void DrawMaskCenter(Rect screen, Vector2 normalizedCenter)
        {
            Vector2 center = new(
                screen.x + normalizedCenter.x * screen.width,
                screen.yMax - normalizedCenter.y * screen.height);

            Handles.BeginGUI();
            Handles.color = Color.cyan;
            Handles.DrawWireDisc(center, Vector3.forward, 6f);
            Handles.DrawLine(center + Vector2.left * 10f, center + Vector2.right * 10f);
            Handles.DrawLine(center + Vector2.up * 10f, center + Vector2.down * 10f);
            Handles.EndGUI();
        }

        private static Vector2 RotateAround(Vector2 point, Vector2 center, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float cosine = Mathf.Cos(radians);
            float sine = Mathf.Sin(radians);
            Vector2 offset = point - center;
            return center + new Vector2(
                offset.x * cosine - offset.y * sine,
                offset.x * sine + offset.y * cosine);
        }

        private static string GetPhaseLabel(PreviewSettings settings, float elapsed)
        {
            if (elapsed < settings.CloseDuration)
                return "Shrinking";

            elapsed -= settings.CloseDuration;
            if (elapsed < settings.ReshapeDuration)
                return "Closing Mouth";

            elapsed -= settings.ReshapeDuration;
            if (elapsed < settings.HoldDuration)
                return "Covered";

            elapsed -= settings.HoldDuration;
            if (elapsed < settings.ReshapeDuration)
                return "Preparing";

            return elapsed < settings.ReshapeDuration + settings.OpenDuration
                ? "Opening"
                : "Open";
        }

        private static PreviewSettings ResolveSettings(
            WorldTransitionPresentation transition,
            BlobsterTransitionStyle style)
        {
            WorldTransitionPresentation defaults = style?.Presentation;
            WorldTransitionPresentation composition = transition.OverrideComposition || defaults == null
                ? transition
                : defaults;
            WorldTransitionPresentation timing = transition.OverrideTiming || defaults == null
                ? transition
                : defaults;

            return new PreviewSettings(
                transition.EyesSprite != null ? transition.EyesSprite : defaults?.EyesSprite,
                transition.BackgroundColor,
                composition.MaskCenter,
                composition.FaceFollowsMouth,
                composition.FaceMouthOffset,
                composition.FaceAnchor,
                composition.FacePosition,
                composition.FaceSize,
                composition.FaceRotation,
                composition.ClosedMouthRadii,
                composition.LipWidthPixels,
                composition.ReshapeGrowthMultiplier,
                style != null ? style.EdgeSoftness : 0.008f,
                timing.CloseDuration,
                composition.MouthReshapeDuration,
                timing.CoveredHoldDuration,
                timing.OpenDuration,
                timing.CloseEase,
                composition.ReshapeEase,
                timing.OpenEase,
                transition.GameTips.Length > 0 ? transition.GameTips : defaults?.GameTips,
                transition.TipFadeDuration,
                transition.TipDisplayDuration,
                transition.TipFontSize);
        }

        private readonly struct PreviewSettings
        {
            public PreviewSettings(
                Sprite eyesSprite,
                Color backgroundColor,
                Vector2 maskCenter,
                bool faceFollowsMouth,
                Vector2 faceMouthOffset,
                Vector2 faceAnchor,
                Vector2 facePosition,
                Vector2 faceSize,
                float faceRotation,
                Vector2 closedMouthRadii,
                float lipWidthPixels,
                float reshapeGrowthMultiplier,
                float edgeSoftness,
                float closeDuration,
                float reshapeDuration,
                float holdDuration,
                float openDuration,
                TransitionEase closeEase,
                TransitionEase reshapeEase,
                TransitionEase openEase,
                string[] gameTips,
                float tipFadeDuration,
                float tipDisplayDuration,
                float tipFontSize)
            {
                EyesSprite = eyesSprite;
                BackgroundColor = backgroundColor;
                MaskCenter = maskCenter;
                FaceFollowsMouth = faceFollowsMouth;
                FaceMouthOffset = faceMouthOffset;
                FaceAnchor = faceAnchor;
                FacePosition = facePosition;
                FaceSize = faceSize;
                FaceRotation = faceRotation;
                ClosedMouthRadii = closedMouthRadii;
                LipWidthPixels = lipWidthPixels;
                ReshapeGrowthMultiplier = reshapeGrowthMultiplier;
                EdgeSoftness = edgeSoftness;
                CloseDuration = closeDuration;
                ReshapeDuration = reshapeDuration;
                HoldDuration = holdDuration;
                OpenDuration = openDuration;
                CloseEase = closeEase;
                ReshapeEase = reshapeEase;
                OpenEase = openEase;
                GameTips = gameTips;
                TipFadeDuration = tipFadeDuration;
                TipDisplayDuration = tipDisplayDuration;
                TipFontSize = tipFontSize;
            }

            public Sprite EyesSprite { get; }
            public Color BackgroundColor { get; }
            public Vector2 MaskCenter { get; }
            public bool FaceFollowsMouth { get; }
            public Vector2 FaceMouthOffset { get; }
            public Vector2 FaceAnchor { get; }
            public Vector2 FacePosition { get; }
            public Vector2 FaceSize { get; }
            public float FaceRotation { get; }
            public Vector2 ClosedMouthRadii { get; }
            public float LipWidthPixels { get; }
            public float ReshapeGrowthMultiplier { get; }
            public float EdgeSoftness { get; }
            public float CloseDuration { get; }
            public float ReshapeDuration { get; }
            public float HoldDuration { get; }
            public float OpenDuration { get; }
            public TransitionEase CloseEase { get; }
            public TransitionEase ReshapeEase { get; }
            public TransitionEase OpenEase { get; }
            public string[] GameTips { get; }
            public float TipFadeDuration { get; }
            public float TipDisplayDuration { get; }
            public float TipFontSize { get; }
            public float Duration => BlobsterTransitionAnimation.GetDuration(
                CloseDuration,
                ReshapeDuration,
                HoldDuration,
                OpenDuration);
        }
    }
}
