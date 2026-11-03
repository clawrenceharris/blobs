using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Blobs.Presentation
{
    /// <summary>
    /// Board-space ghost swipe used by level coaching. Loops a pointer from a start cell to an end
    /// cell until dismissed; optional caption is presentation-only and never required for play.
    /// </summary>
    public sealed class CoachPointerView : MonoBehaviour
    {
        [SerializeField] private Transform pointer;
        [SerializeField] private SpriteRenderer pointerRenderer;
        [SerializeField] private TMP_Text captionText;
        [SerializeField, Min(0.1f)] private float travelDuration = 0.85f;
        [SerializeField, Min(0f)] private float pauseAtEnd = 0.35f;
        [SerializeField, Min(0f)] private float fadeDuration = 0.2f;

        private Sequence _loop;
        private float _baseAlpha = 1f;

        private void Awake()
        {
            if (pointer == null)
                pointer = transform;
            if (pointerRenderer == null)
                pointerRenderer = GetComponentInChildren<SpriteRenderer>(true);
            if (pointerRenderer != null)
                _baseAlpha = pointerRenderer.color.a;
            HideImmediately();
        }

        private void OnDestroy() => KillLoop();

        /// <summary>Starts a looping travel from <paramref name="fromLocal"/> to <paramref name="toLocal"/>.</summary>
        public void Play(Vector3 fromLocal, Vector3 toLocal, string caption)
        {
            KillLoop();
            gameObject.SetActive(true);
            if (pointer != null)
            {
                pointer.gameObject.SetActive(true);
                pointer.localPosition = fromLocal;
            }

            SetPointerAlpha(1f);
            if (captionText != null)
            {
                captionText.gameObject.SetActive(!string.IsNullOrWhiteSpace(caption));
                captionText.text = caption ?? string.Empty;
                captionText.alpha = string.IsNullOrWhiteSpace(caption) ? 0f : 1f;
            }

            _loop = DOTween.Sequence()
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
                .Append(pointer.DOLocalMove(toLocal, travelDuration).SetEase(Ease.InOutSine))
                .AppendInterval(pauseAtEnd)
                .AppendCallback(() =>
                {
                    if (pointer != null)
                        pointer.localPosition = fromLocal;
                })
                .SetLoops(-1, LoopType.Restart);
        }

        public void Hide()
        {
            KillLoop();
            if (!isActiveAndEnabled || pointerRenderer == null)
            {
                HideImmediately();
                return;
            }

            DOTween.Sequence()
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
                .Append(DOTween.To(() => pointerRenderer.color.a, SetPointerAlpha, 0f, fadeDuration))
                .Join(captionText != null
                    ? DOTween.To(() => captionText.alpha, a => captionText.alpha = a, 0f, fadeDuration)
                    : DOTween.Sequence())
                .OnComplete(HideImmediately);
        }

        public void HideImmediately()
        {
            KillLoop();
            SetPointerAlpha(0f);
            if (captionText != null)
            {
                captionText.text = string.Empty;
                captionText.alpha = 0f;
                captionText.gameObject.SetActive(false);
            }

            if (pointer != null)
                pointer.gameObject.SetActive(false);
        }

        private void SetPointerAlpha(float alpha)
        {
            if (pointerRenderer == null)
                return;
            Color color = pointerRenderer.color;
            color.a = _baseAlpha * alpha;
            pointerRenderer.color = color;
        }

        private void KillLoop()
        {
            _loop?.Kill();
            _loop = null;
        }
    }
}
