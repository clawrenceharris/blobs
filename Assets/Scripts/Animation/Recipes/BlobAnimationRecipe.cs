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
        public float moveArcHeight = 1f;
        public Ease moveEase = Ease.OutQuad;
        public float moveInterval = 0.5f;

        [Header("Merge")]
        public float mergeDuration = 0.25f;
        public float mergeAnticipationDuration = 0.1f;
        public float mergeAnticipationAmount = 0.85f;
        public float mergeAnticipationStretchAmount = 1.08f;
        public float mergeImpactNudge = 0.08f;
        public float mergeImpactInDuration = 0.08f;
        public float mergeImpactOutDuration = 0.12f;
        public float mergeOvershootDuration = 0.2f;
        public float mergeOvershootAmount = 1.08f;
        public float mergeSquashAmount = 0.85f;
        public float mergeStretchAmount = 1.08f;
        public float mergeSettleDuration = 0.2f;
        public float mergeSettleAmount = 1.0f;
        public Ease mergeSettleEase = Ease.OutQuad;

       
        [Header("Resize")]
        public  float resizeDuration = 0.25f;
        public  Ease resizeEase = Ease.OutBack;


       
        [Header("Expressions (e.g. Shocked before Sigil clear)")]
        public  string beforeRemoveExpression;
        public  float beforeRemoveExpressionDuration = 0.2f;
    }
}
