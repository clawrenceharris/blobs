using Blobs.Content;
using UnityEngine;

namespace Blobs.UI
{
    /// <summary>Resource-backed default for transitions without a selected world.</summary>
    [CreateAssetMenu(fileName = "BlobsterTransitionStyle", menuName = "Blobs/UI/Blobster Transition Style")]
    public sealed class BlobsterTransitionStyle : ScriptableObject
    {
        [SerializeField] private Material maskMaterial;
        [SerializeField, Min(0f)] private float edgeSoftness = 0.008f;
        [SerializeField] private WorldTransitionPresentation presentation = new();

        public Material MaskMaterial => maskMaterial;
        public float EdgeSoftness => Mathf.Max(0f, edgeSoftness);
        public WorldTransitionPresentation Presentation => presentation;
    }

    public static class BlobsterTransitionAnimation
    {
        private const float CornerCoverage = 1.45f;

        public static float GetDuration(
            float closeDuration,
            float reshapeDuration,
            float holdDuration,
            float openDuration)
        {
            return Mathf.Max(0.01f, closeDuration)
                + Mathf.Max(0.01f, reshapeDuration) * 2f
                + Mathf.Max(0f, holdDuration)
                + Mathf.Max(0.01f, openDuration);
        }

        public static Vector2 CalculateOpenMouthRadii(
            Vector2 center,
            float softness,
            float screenAspect)
        {
            float aspect = Mathf.Max(0.0001f, screenAspect);
            float horizontal = Mathf.Max(center.x, 1f - center.x) * aspect;
            float vertical = Mathf.Max(center.y, 1f - center.y);
            float radius = Mathf.Sqrt(horizontal * horizontal + vertical * vertical)
                * CornerCoverage
                + Mathf.Max(0f, softness);
            return Vector2.one * radius;
        }

        public static Vector2 GetPreparedMouthRadii(
            Vector2 closedRadii,
            float growthMultiplier)
        {
            float radius = Mathf.Max(0.0001f, closedRadii.x)
                * Mathf.Max(1f, growthMultiplier);
            return Vector2.one * radius;
        }

        public static Vector2 EvaluateMouthRadii(
            float elapsed,
            Vector2 openRadii,
            Vector2 closedRadii,
            float growthMultiplier,
            float closeDuration,
            float reshapeDuration,
            float holdDuration,
            float openDuration,
            TransitionEase closeEase,
            TransitionEase reshapeEase,
            TransitionEase openEase)
        {
            closeDuration = Mathf.Max(0.01f, closeDuration);
            reshapeDuration = Mathf.Max(0.01f, reshapeDuration);
            holdDuration = Mathf.Max(0f, holdDuration);
            openDuration = Mathf.Max(0.01f, openDuration);
            elapsed = Mathf.Clamp(
                elapsed,
                0f,
                closeDuration + reshapeDuration * 2f + holdDuration + openDuration);
            Vector2 preparedRadii = GetPreparedMouthRadii(closedRadii, growthMultiplier);

            if (elapsed < closeDuration)
                return Lerp(openRadii, preparedRadii, elapsed / closeDuration, closeEase);

            elapsed -= closeDuration;
            if (elapsed < reshapeDuration)
                return Lerp(preparedRadii, closedRadii, elapsed / reshapeDuration, reshapeEase);

            elapsed -= reshapeDuration;
            if (elapsed < holdDuration)
                return closedRadii;

            elapsed -= holdDuration;
            if (elapsed < reshapeDuration)
                return Lerp(closedRadii, preparedRadii, elapsed / reshapeDuration, reshapeEase);

            elapsed -= reshapeDuration;
            return Lerp(preparedRadii, openRadii, elapsed / openDuration, openEase);
        }

        private static Vector2 Lerp(
            Vector2 from,
            Vector2 to,
            float normalized,
            TransitionEase ease)
        {
            return Vector2.LerpUnclamped(
                from,
                to,
                TransitionEaseUtility.Evaluate(ease, normalized));
        }
    }
}
