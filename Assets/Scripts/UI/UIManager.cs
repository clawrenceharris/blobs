using UnityEngine;
using TMPro;
using DG.Tweening;
using System;

namespace Blobs.UI
{
    /// <summary>
    /// Simple UI Manager for gameplay feedback.
    /// Shows animated text feedback for invalid actions.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Feedback Text")]
        [SerializeField] private TextMeshProUGUI feedbackText;
        
        [Header("Animation Settings")]
        [SerializeField] private float feedbackDuration = 1.5f;
        [SerializeField] private float fadeInDuration = 0.2f;
        [SerializeField] private float fadeOutDuration = 0.3f;
        [SerializeField] private float moveUpAmount = 30f;
        [SerializeField] private Ease fadeInEase = Ease.OutBack;
        [SerializeField] private Ease fadeOutEase = Ease.InQuad;

        private Sequence currentFeedbackSequence;
        private Vector3 feedbackOriginalPosition;

        private void Awake()
        {
            // Singleton
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Store original position
            if (feedbackText != null)
            {
                feedbackOriginalPosition = feedbackText.rectTransform.anchoredPosition;
                feedbackText.alpha = 0f;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

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

        #region Predefined Messages

        public void ShowCannotSelectFeedback()
        {
            ShowFeedback("This blob can't initiate a merge!");
        }

        public void ShowSameColorFeedback()
        {
            ShowFeedback("Can't merge same colors!");
        }

        public void ShowCannotMergeFeedback()
        {
            ShowFeedback("Can't merge with that!");
        }

        public void ShowNoMoveFeedback()
        {
            ShowFeedback("No blob there!");
        }

        public void ShowBlockedFeedback()
        {
            ShowFeedback("Path is blocked!");
        }

        public void ShowFlagRejectedFeedback()
        {
            ShowFeedback("Flags are only mergable with a single remaining blob of the same color!");
        }

        #endregion
    }
}