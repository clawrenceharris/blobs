using System.Collections.Generic;
using Blobs.Animation;
using Blobs.Core.Merge;
using Blobs.Input;
using Blobs.Services;
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
        {MergeFailReason.ColorRuleRejected, "Can't merge same colors!"},
        {MergeFailReason.InvalidSource, "This blob can't initiate a merge!"},
        {MergeFailReason.InvalidTarget,"Can't merge with that!" },
        {MergeFailReason.NoTargetInDirection, "No blob there!" },
        {MergeFailReason.TileBlocked, "Path is blocked!" },
        {MergeFailReason.NotAligned, "Blobs must share the same column or row to merge"},
        {MergeFailReason.FlagRejected, "Flags are only mergable with a single remaining blob of the same color!"}


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

    public void ShowInvalid( MergeFailReason failReason, string blobId)
    {
        if (feedbackMap.TryGetValue(failReason, out var feedback))
        {
            ShowFeedback(feedback);
            _board.GetBlob(blobId)?.View.GetComponent<BlobAnimator>().PlayShakeAnimation();
        }

    }
       
}