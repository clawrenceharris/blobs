using System.Collections.Generic;
using Blobs.Animation;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class FeedbackPresenter : MonoBehaviour
{
    private IBoardPresenter _board;

    [SerializeField] private float feedbackDuration = 1.5f;
    [SerializeField] private float fadeInDuration = 0.2f;
    [SerializeField] private float fadeOutDuration = 0.3f;
    [SerializeField] private float moveUpAmount = 30f;
    [SerializeField] private Ease fadeInEase = Ease.OutBack;
    [SerializeField] private Ease fadeOutEase = Ease.InQuad;

    private Sequence currentFeedbackSequence;
    private Vector3 feedbackOriginalPosition;
    [Header("Feedback Text")]
    [SerializeField] private TextMeshProUGUI feedbackText;
    public Dictionary<MergeFailReason, string> feedbackMap = new()
    {
        {MergeFailReason.InvalidSource, "This blob can't initiate a merge!"},
        {MergeFailReason.InvalidTarget,"Can't merge with that!" },
        {MergeFailReason.NoTargetInDirection, "No blob there!" },
        {MergeFailReason.TileBlocked, "Path is blocked!" },
        {MergeFailReason.NotAligned, "Blobs must share the same column or row to merge"},
        {MergeFailReason.LaserBlocked, "Laser is blocking the path!"},
        {MergeFailReason.ColorRuleRejected, "Can't merge same colors!"},
        {MergeFailReason.TargetColorRuleRejected, "A Target only accepts blobs that match its color!"},
        {MergeFailReason.TargetMergeRuleRejected, "Only your last blob can merge with a Target!"},
        {MergeFailReason.BlobBlocked, "Path is blocked!"},
        {MergeFailReason.OffBoard, "Blob is off the board!"}

    };

    private void Awake()
    {
        _board = FindFirstObjectByType<BoardPresenter>();
            // Singleton
           
            // Store original position
            if (feedbackText != null)
            {
                feedbackOriginalPosition = feedbackText.rectTransform.anchoredPosition;
                feedbackText.alpha = 0f;
            }
        }

        private void OnDestroy()
        {
            

            currentFeedbackSequence?.Kill();
        }

        /// <summary>
        /// Show animated feedback text
        /// </summary>
        public void ShowFeedback(string message)
        {
            if (feedbackText == null)
            {
                Debug.LogWarning("[UIManager] Feedback text not assigned!");
                return;
            }

            // Kill any existing animation
            currentFeedbackSequence?.Kill();

            // Reset position and set text
            feedbackText.rectTransform.anchoredPosition = feedbackOriginalPosition;
            feedbackText.text = message;
            feedbackText.alpha = 0f;

            // Create animation sequence
            currentFeedbackSequence = DOTween.Sequence();

            // Fade in + scale pop
            currentFeedbackSequence.Append(
                feedbackText.DOFade(1f, fadeInDuration)
                    .SetEase(fadeInEase)
            );
            currentFeedbackSequence.Join(
                feedbackText.rectTransform.DOScale(1.1f, fadeInDuration * 0.5f)
                    .SetEase(Ease.OutBack)
            );
            currentFeedbackSequence.Append(
                feedbackText.rectTransform.DOScale(1f, fadeInDuration * 0.5f)
                    .SetEase(Ease.OutQuad)
            );

            // Hold for duration
            currentFeedbackSequence.AppendInterval(feedbackDuration);

            // Fade out + move up
            currentFeedbackSequence.Append(
                feedbackText.DOFade(0f, fadeOutDuration)
                    .SetEase(fadeOutEase)
            );
            currentFeedbackSequence.Join(
                feedbackText.rectTransform.DOAnchorPosY(
                    feedbackOriginalPosition.y + moveUpAmount, 
                    fadeOutDuration
                ).SetEase(fadeOutEase)
            );

            // Reset position after complete
            currentFeedbackSequence.OnComplete(() =>
            {
                feedbackText.rectTransform.anchoredPosition = feedbackOriginalPosition;
            });
        }

    public void ShowInvalid(MoveFailContext context)
    {
        if (feedbackMap.TryGetValue(context.Reason, out var feedback))
        {
            ShowFeedback(feedback);
            NudgeBlobInDirection(context.Intent.SourceId, context.Intent.Direction);
        }
    }

    private void NudgeBlobInDirection(string blobId, Vector2Int direction)
    {
        var blob = _board.GetBlob(blobId);
        blob.View.ExpressionController.ShowExpression(ExpressionType.Sad);
        blob?.NudgeInDirection(direction).AppendCallback(() => blob.View.ExpressionController.ShowExpression(ExpressionType.Normal));
    }
}