using Blobs.Application;
using Blobs.Core;
using Blobs.Input;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Blobs.Presentation
{
    /// <summary>
    /// Translates gameplay failure codes into player-facing UI feedback.
    /// Gameplay layers remain responsible only for deciding why an action failed.
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public sealed class GameplayFeedbackPresenter : MonoBehaviour
    {
        [Header("View")]
        [SerializeField] private TMP_Text feedbackText;

        [Header("Timing")]
        [SerializeField, Min(0f)] private float fadeInDuration = 0.2f;
        [SerializeField, Min(0f)] private float holdDuration = 1.5f;
        [SerializeField, Min(0f)] private float fadeOutDuration = 0.3f;

        [Header("Motion")]
        [SerializeField, Min(0f)] private float shakeDuration = 0.25f;
        [SerializeField] private Vector2 shakeStrength = new Vector2(12f, 4f);
        [SerializeField, Min(1)] private int shakeVibrato = 20;
        [SerializeField, Range(0f, 180f)] private float shakeRandomness = 45f;
        [SerializeField, Min(0f)] private float moveUpAmount = 30f;
        [SerializeField, Min(1f)] private float popScale = 1.1f;

        private GameplayInputAdapter _inputAdapter;
        private Sequence _feedbackSequence;
        private Vector2 _originalPosition;
        private Vector3 _originalScale;
        private bool _subscribed;

        private void Awake()
        {
            CacheViewState();
            HideImmediately();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
            HideImmediately();
        }

        private void OnDestroy()
        {
            Unsubscribe();
            KillAnimation();
        }

        private void OnValidate()
        {
            if (feedbackText == null)
                feedbackText = GetComponent<TMP_Text>();
        }

        /// <summary>
        /// Connects this view to the input boundary that publishes selection and move outcomes.
        /// </summary>
        public void Initialize(GameplayInputAdapter inputAdapter)
        {
            Unsubscribe();
            HideImmediately();
            _inputAdapter = inputAdapter;
            CacheViewState();
            Subscribe();
        }

        /// <summary>
        /// Provides the concise presentation copy for a domain failure reason.
        /// </summary>
        public static string MessageFor(MoveFailureReason reason)
        {
            return reason switch
            {
                MoveFailureReason.SourceCannotMove => "That blob can't move.",
                MoveFailureReason.TargetMissing => "There's no blob there.",
                MoveFailureReason.BlockedPath => "Path is blocked.",
                MoveFailureReason.NotAligned => "Blobs must share the same column or row to merge",
                MoveFailureReason.SameBlob => "Choose a different blob.",
                MoveFailureReason.UnsupportedInteraction => "Those blobs can't merge.",
                MoveFailureReason.NormalMergeRequiresDifferentColors =>
                    "Can't merge same colors!",
                MoveFailureReason.FlagRequiresMatchingColor => "Match the flag's color.",
                MoveFailureReason.FlagRequiresNoOtherBlobs => "Clear the other blobs first.",
                _ => "That move isn't valid."
            };
        }

        private void HandleSelectionResolved(BlobSelectionResult result)
        {
            MoveResult moveResult = result?.MoveResult;

            if (moveResult == null)
            {
                return;
            }
            if (!result.MoveAttempted)
            {

                return;
            }

            if (moveResult.Succeeded)
            {
                HideImmediately();
                return;
            }

            if (moveResult.FailureReason == MoveFailureReason.None)
                return;

            if (!moveResult.FailureReason.ShouldShowFeedback())
            {
                HideImmediately();
                return;
            }
            ShowFailure(moveResult.FailureReason);
        }

        private void ShowFailure(MoveFailureReason reason)
        {
            if (feedbackText == null)
                return;

            KillAnimation();
            ResetTransform();

            feedbackText.text = MessageFor(reason);
            feedbackText.alpha = 0f;

            RectTransform rectTransform = feedbackText.rectTransform;
            float popUpDuration = fadeInDuration * 0.5f;
            float popDownDuration = fadeInDuration - popUpDuration;

            _feedbackSequence = DOTween.Sequence()
                .Append(DOTween.To(
                        () => feedbackText.alpha,
                        alpha => feedbackText.alpha = alpha,
                        1f,
                        fadeInDuration)
                    .SetEase(Ease.OutBack))
                .Join(rectTransform.DOScale(_originalScale * popScale, popUpDuration)
                    .SetEase(Ease.OutBack))
                .Join(rectTransform.DOShakePosition(
                    shakeDuration,
                    new Vector3(shakeStrength.x, shakeStrength.y, 0f),
                    shakeVibrato,
                    shakeRandomness,
                    false,
                    true))
                .Append(rectTransform.DOScale(_originalScale, popDownDuration)
                    .SetEase(Ease.OutQuad))
                .AppendInterval(holdDuration)
                .Append(DOTween.To(
                        () => feedbackText.alpha,
                        alpha => feedbackText.alpha = alpha,
                        0f,
                        fadeOutDuration)
                    .SetEase(Ease.InQuad))
                .Join(DOTween.To(
                        () => rectTransform.anchoredPosition,
                        position => rectTransform.anchoredPosition = position,
                        new Vector2(_originalPosition.x, _originalPosition.y + moveUpAmount),
                        fadeOutDuration)
                    .SetEase(Ease.InQuad))
                .OnComplete(ResetTransform)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        }

        private void CacheViewState()
        {
            if (feedbackText == null)
                feedbackText = GetComponent<TMP_Text>();

            if (feedbackText == null)
                return;

            _originalPosition = feedbackText.rectTransform.anchoredPosition;
            _originalScale = feedbackText.rectTransform.localScale;
        }

        private void HideImmediately()
        {
            KillAnimation();
            ResetTransform();

            if (feedbackText != null)
            {
                feedbackText.text = string.Empty;
                feedbackText.alpha = 0f;
            }
        }

        private void ResetTransform()
        {
            if (feedbackText == null)
                return;

            feedbackText.rectTransform.anchoredPosition = _originalPosition;
            feedbackText.rectTransform.localScale = _originalScale;
        }

        private void KillAnimation()
        {
            _feedbackSequence?.Kill();
            _feedbackSequence = null;
        }

        private void Subscribe()
        {
            if (_subscribed || _inputAdapter == null || !isActiveAndEnabled)
                return;

            _inputAdapter.BlobSelectionResolved += HandleSelectionResolved;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed || _inputAdapter == null)
                return;

            _inputAdapter.BlobSelectionResolved -= HandleSelectionResolved;
            _subscribed = false;
        }
    }
}
