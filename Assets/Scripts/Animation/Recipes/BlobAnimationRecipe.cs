using DG.Tweening;
using UnityEngine;

namespace Blobs.Animation
{
    /// <summary>
    /// Per-blob-type animation tuning: move/remove durations, easing, overshoot, expression and particle keys.
    /// Assign per BlobType in a recipe set or load via AnimationRecipeProvider.
    /// </summary>
    [CreateAssetMenu(fileName = "BlobAnimationRecipe", menuName = "Scriptable Objects/Animation Recipe (Blob)")]
    public class BlobAnimationRecipe : AnimationRecipe
    {
        [Header("Movement")]
        public float moveDuration = 0.3f;
        public Ease moveEase = Ease.InOutQuad;

        [Header("Spawn/Despawn")]
        public float spawnDuration = 0.5f;
        public Ease spawnEase = Ease.OutBack;
        public float despawnDuration = 0.5f;
        public Ease despawnEase = Ease.InBack;

        [Header("Selection State")]

        public float selectionSquishDuration = 0.3f;
        public float selectionSquishAmount = 0.85f;
        public float selectionStretchAmount = 1.08f;

        [Header("Merge")]
        public float mergeDuration = 0.4f;
        public Ease mergeEase = Ease.InBack;

        [Header("Resize")]
        public float resizeDuration = 0.3f;
        public Ease resizeEase = Ease.OutBack;

    }
}
