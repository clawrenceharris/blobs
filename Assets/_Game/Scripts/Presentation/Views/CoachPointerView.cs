using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Blobs.Presentation
{
    public enum CoachCaptionSlot
    {
        Top,
        Bottom,
        Center
    }

    /// <summary>
    /// Board-space ghost swipe plus tutorial caption slots (top, bottom, center). Soft-loops a
    /// pointer: fade in at A, travel to B, fade out, pause, then repeat until dismissed. Captions
    /// fade in once and stay visible until explicitly hidden. An optional full-screen blocker
    /// steals taps during reaction slides so the board cannot be selected underneath.
    /// </summary>
    public sealed class CoachPointerView : MonoBehaviour
    {
        [SerializeField] private Transform pointer;
        [SerializeField] private SpriteRenderer pointerRenderer;
        [SerializeField, Tooltip("Legacy / default top caption. Used when topCaptionText is unset.")]
        private TMP_Text captionText;
        [SerializeField] private TMP_Text topCaptionText;
        [SerializeField] private TMP_Text bottomCaptionText;
        [SerializeField] private TMP_Text centerCaptionText;
        [SerializeField, Tooltip("Optional full-screen UI Graphic. Enabled during reaction slides to capture taps.")]
        private Graphic continueBlocker;
        [SerializeField, Min(0.1f)] private float travelDuration = 0.85f;
        [SerializeField, Min(0f)] private float pauseAtEnd = 0.2f;
        [SerializeField, Min(0f)] private float fadeDuration = 0.25f;
        [SerializeField, Min(0f)] private float cooldown = 0.55f;
        [SerializeField] private float offsetY = 0.5f;
        [SerializeField] private float offsetX = 0f;

        private Sequence _loop;
        private Tween _captionTween;
        private float _baseAlpha = 1f;
        private Action _onContinue;
        private bool _continueArmed;

        private void Awake()
        {
            if (pointer == null)
                pointer = transform;
            if (pointerRenderer == null)
                pointerRenderer = GetComponentInChildren<SpriteRenderer>(true);
            if (pointerRenderer != null)
                _baseAlpha = pointerRenderer.color.a;
            if (topCaptionText == null)
                topCaptionText = captionText;
            HideImmediately();
        }

        private void OnDestroy()
        {
            KillLoop();
            KillCaptionTween();
            _onContinue = null;
        }

        /// <summary>Starts a soft looping travel from <paramref name="fromLocal"/> to <paramref name="toLocal"/>.</summary>
        public void Play(Vector3 fromLocal, Vector3 toLocal, string caption)
        {
            KillLoop();
            KillCaptionTween();
            gameObject.SetActive(true);
            Vector3 from = Offset(fromLocal);
            Vector3 to = Offset(toLocal);
            if (pointer != null)
            {
                pointer.gameObject.SetActive(true);
                pointer.localPosition = from;
            }

            SetPointerAlpha(0f);

            _loop = DOTween.Sequence()
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
                .Append(FadePointerTo(1f))
                .Append(pointer.DOLocalMove(to, travelDuration).SetEase(Ease.InOutSine))
                .AppendInterval(pauseAtEnd)
                .Append(FadePointerTo(0f))
                .AppendCallback(() =>
                {
                    if (pointer != null)
                        pointer.localPosition = from;
                })
                .AppendInterval(cooldown)
                .SetLoops(-1, LoopType.Restart);
        }

        public void Hide()
        {
            KillLoop();
            DisarmContinue();
            if (!isActiveAndEnabled)
            {
                HideImmediately();
                return;
            }

            Sequence hide = DOTween.Sequence()
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
                .Append(FadePointerTo(0f));
            JoinCaptionFades(hide, 0f);
            hide.OnComplete(HideImmediately);
        }

        /// <summary>Fades the captions out without stopping the pointer loop.</summary>
        public void HideCaption()
        {
            KillCaptionTween();
            Sequence hide = DOTween.Sequence().SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            JoinCaptionFades(hide, 0f);
            hide.OnComplete(ClearCaptionText);
            _captionTween = hide;
        }

        public void HideImmediately()
        {
            KillLoop();
            KillCaptionTween();
            DisarmContinue();
            SetPointerAlpha(0f);
            ClearCaptionText();
            SetCaptionAlpha(topCaptionText, 0f);
            SetCaptionAlpha(bottomCaptionText, 0f);
            SetCaptionAlpha(centerCaptionText, 0f);

            if (pointer != null)
                pointer.gameObject.SetActive(false);
        }

        /// <summary>
        /// Shows a single top caption. Kept for the per-level coach path that only authors one line.
        /// </summary>
        public void ShowCaption(string caption)
        {
            ShowCaptions(caption, bottom: null, center: null);
        }

        /// <summary>
        /// Shows any combination of top, bottom, and center caption slots. Empty slots hide.
        /// </summary>
        public void ShowCaptions(string top, string bottom, string center)
        {
            KillCaptionTween();
            gameObject.SetActive(true);

            bool any = ApplyCaptionSlot(topCaptionText, top) |
                ApplyCaptionSlot(bottomCaptionText, bottom) |
                ApplyCaptionSlot(centerCaptionText, center);
            if (!any)
                return;

            Sequence show = DOTween.Sequence().SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            JoinCaptionFades(show, 1f);
            _captionTween = show;
        }

        /// <summary>
        /// Returns an unstarted fade-in for one slot. The slot's text is swapped when the tween first
        /// updates, so a caption still fading out in the same slot is not replaced early.
        /// </summary>
        public Tween CaptionIn(CoachCaptionSlot slot, string caption, float duration)
        {
            TMP_Text text = GetSlot(slot);
            if (text == null || string.IsNullOrWhiteSpace(caption))
                return null;

            gameObject.SetActive(true);
            bool prepared = false;
            return DOTween.To(
                    () => 0f,
                    alpha =>
                    {
                        if (!prepared)
                        {
                            prepared = true;
                            ApplyCaptionSlot(text, caption);
                        }
                        SetCaptionAlpha(text, alpha);
                    },
                    1f,
                    Mathf.Max(0.0001f, duration))
                .SetEase(Ease.OutSine);
        }

        /// <summary>Returns an unstarted fade-out of every visible caption slot, clearing them at the end.</summary>
        public Tween CaptionsOut(float duration)
        {
            TMP_Text[] slots = { topCaptionText, bottomCaptionText, centerCaptionText };
            float[] start = null;
            return DOTween.To(
                    () => 1f,
                    progress =>
                    {
                        if (start == null)
                        {
                            start = new float[slots.Length];
                            for (int i = 0; i < slots.Length; i++)
                                start[i] = slots[i] != null && slots[i].gameObject.activeSelf ? slots[i].alpha : 0f;
                        }

                        for (int i = 0; i < slots.Length; i++)
                        {
                            if (slots[i] == null || start[i] <= 0f)
                                continue;
                            if (progress <= 0f)
                                ClearSlot(slots[i]);
                            else
                                SetCaptionAlpha(slots[i], start[i] * progress);
                        }
                    },
                    0f,
                    Mathf.Max(0.0001f, duration))
                .SetEase(Ease.InSine);
        }

        /// <summary>Stops the ghost swipe loop and returns an unstarted fade-out of the pointer.</summary>
        public Tween StopPointer(float duration)
        {
            KillLoop();
            return DOTween.To(
                    () => pointerRenderer != null
                        ? pointerRenderer.color.a / Mathf.Max(_baseAlpha, 0.0001f)
                        : 0f,
                    alpha =>
                    {
                        SetPointerAlpha(alpha);
                        if (alpha <= 0f && pointer != null && pointer != transform)
                            pointer.gameObject.SetActive(false);
                    },
                    0f,
                    Mathf.Max(0.0001f, duration));
        }

        /// <summary>Whether a full-screen tap target is wired for reaction slides.</summary>
        public bool HasContinueBlocker => continueBlocker != null;

        /// <summary>
        /// Arms the full-screen blocker so the next tap calls <paramref name="onContinue"/>.
        /// Waits <paramref name="armDelay"/> (default: the caption fade) so an accidental post-move
        /// tap cannot skip the reaction.
        /// </summary>
        public void ArmContinue(Action onContinue, float? armDelay = null)
        {
            _onContinue = onContinue ?? throw new ArgumentNullException(nameof(onContinue));
            _continueArmed = false;
            if (continueBlocker == null)
            {
                Debug.LogWarning(
                    "CoachPointerView needs a continueBlocker Graphic to capture reaction taps.",
                    this);
                // Still allow Continue via a late arm so tests without UI can call through.
                _continueArmed = true;
                return;
            }

            continueBlocker.gameObject.SetActive(true);
            continueBlocker.raycastTarget = true;
            Button button = continueBlocker.GetComponent<Button>();
            if (button == null)
                button = continueBlocker.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.RemoveListener(HandleContinueClicked);
            button.onClick.AddListener(HandleContinueClicked);
            float delay = armDelay ?? fadeDuration;
            if (delay <= 0f)
            {
                _continueArmed = true;
                return;
            }

            DOVirtual.DelayedCall(delay, () => _continueArmed = true)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
                .SetTarget(this);
        }

        private void HandleContinueClicked()
        {
            if (!_continueArmed || _onContinue == null)
                return;

            Action continueAction = _onContinue;
            DisarmContinue();
            continueAction.Invoke();
        }

        public void DisarmContinue()
        {
            _continueArmed = false;
            _onContinue = null;
            DOTween.Kill(this);
            if (continueBlocker != null)
            {
                Button button = continueBlocker.GetComponent<Button>();
                if (button != null)
                    button.onClick.RemoveListener(HandleContinueClicked);
                continueBlocker.raycastTarget = false;
                continueBlocker.gameObject.SetActive(false);
            }
        }

        private TMP_Text GetSlot(CoachCaptionSlot slot)
        {
            return slot switch
            {
                CoachCaptionSlot.Top => topCaptionText,
                CoachCaptionSlot.Bottom => bottomCaptionText,
                CoachCaptionSlot.Center => centerCaptionText,
                _ => null
            };
        }

        private static bool ApplyCaptionSlot(TMP_Text slot, string caption)
        {
            if (slot == null)
                return false;

            slot.raycastTarget = false;
            bool show = !string.IsNullOrWhiteSpace(caption);
            slot.gameObject.SetActive(show);
            slot.text = show ? caption : string.Empty;
            slot.alpha = 0f;
            return show;
        }

        private void ClearCaptionText()
        {
            ClearSlot(topCaptionText);
            ClearSlot(bottomCaptionText);
            ClearSlot(centerCaptionText);
        }

        private static void ClearSlot(TMP_Text slot)
        {
            if (slot == null)
                return;
            slot.text = string.Empty;
            slot.alpha = 0f;
            slot.gameObject.SetActive(false);
        }

        private void JoinCaptionFades(Sequence sequence, float alpha)
        {
            if (topCaptionText != null && topCaptionText.gameObject.activeSelf)
                sequence.Join(FadeCaptionTo(topCaptionText, alpha));
            if (bottomCaptionText != null && bottomCaptionText.gameObject.activeSelf)
                sequence.Join(FadeCaptionTo(bottomCaptionText, alpha));
            if (centerCaptionText != null && centerCaptionText.gameObject.activeSelf)
                sequence.Join(FadeCaptionTo(centerCaptionText, alpha));
        }

        private Vector3 Offset(Vector3 local) => local + new Vector3(offsetX, offsetY, 0f);

        private Tween FadePointerTo(float alpha)
        {
            return DOTween.To(
                () => pointerRenderer != null
                    ? pointerRenderer.color.a / Mathf.Max(_baseAlpha, 0.0001f)
                    : 0f,
                SetPointerAlpha,
                alpha,
                fadeDuration);
        }

        private Tween FadeCaptionTo(TMP_Text slot, float alpha)
        {
            return DOTween.To(
                () => slot != null ? slot.alpha : 0f,
                a => SetCaptionAlpha(slot, a),
                alpha,
                fadeDuration);
        }

        private static void SetCaptionAlpha(TMP_Text slot, float alpha)
        {
            if (slot != null)
                slot.alpha = alpha;
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

        private void KillCaptionTween()
        {
            _captionTween?.Kill();
            _captionTween = null;
        }
    }
}
