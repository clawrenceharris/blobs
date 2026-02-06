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
        [Header("Idle State")]
        public float idleScaleAmount = 0.02f;
        public float idleScaleDuration = 1.5f;
        public float idleFloatAmount = 0.03f;
        public float idleFloatDuration = 2f;

        [Header("Selection State")]

        public float selectionSquishDuration = 0.3f;
        public float selectionSquishAmount = 0.85f;
        public float selectionStretchAmount = 1.08f;

        [Header("Movement")]
        public float moveDuration = 0.3f;
        public float moveArcHeight = 0.3f;
        public Ease moveEase = Ease.OutQuad;

        [Header("Merge")]
        public float mergeDuration = 0.25f;
        public float mergeAnticipationDuration = 0.1f;
        public float mergeAnticipationAmount = 0.85f;
        public float mergeAnticipationStretchAmount = 1.08f;

       
        [Header("Resize")]
        public  float resizeDuration = 0.25f;
        public  Ease resizeEase = Ease.OutBack;


       
        [Header("Expressions (e.g. Shocked before Sigil clear)")]
        public  string beforeRemoveExpression;
        public  float beforeRemoveExpressionDuration = 0.2f;
        public float mergeOvershootDuration = 0.2f;
        internal int mergeSquashAmount;
    }
}
