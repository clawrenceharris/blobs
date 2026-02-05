using DG.Tweening;
using UnityEngine;

namespace Blobs.Merge.Animation
{
    /// <summary>
    /// Per-blob-type animation tuning: move/remove/spawn durations, easing, overshoot, expression and particle keys.
    /// Assign per BlobType in a recipe set or load via AnimationRecipeProvider.
    /// </summary>
    [CreateAssetMenu(fileName = "BlobAnimationRecipe", menuName = "Scriptable Objects/Animation Recipe (Blob)")]
    public class BlobAnimationRecipe : ScriptableObject
    {
        [Header("Move")]
        public float moveDuration = 0.35f;
        public Ease moveEase = Ease.OutQuad;
        public float anticipationDuration = 0.08f;
        public float overshootDuration = 0.1f;
        public float squashAmount = 0.85f;

        [Header("Remove")]
        public float removeDuration = 0.3f;
        public Ease removeEase = Ease.InBack;
        public string removeParticleKey;

        [Header("Spawn")]
        public float spawnDuration = 0.35f;
        public Ease spawnEase = Ease.OutBack;

        [Header("Resize")]
        public float resizeDuration = 0.25f;
        public Ease resizeEase = Ease.OutBack;


        [Header("Selection")]
        public float selectionSquishDuration = 0.3f;
        public float selectionSquishAmount = 0.85f;
        public float selectionStretchAmount = 1.08f;

        [Header("Expressions (e.g. Shocked before Sigil clear)")]
        public string beforeRemoveExpression;
        public float beforeRemoveExpressionDuration = 0.2f;

        
    }
}
