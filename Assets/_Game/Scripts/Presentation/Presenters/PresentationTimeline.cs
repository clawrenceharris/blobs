using System;
using DG.Tweening;

namespace Blobs.Presentation
{
    /// <summary>
    /// Makes immediate versus animated presentation explicit while keeping DOTween sequencing centralized.
    /// </summary>
    public sealed class PresentationTimeline
    {
        private readonly Sequence _sequence;

        private PresentationTimeline(bool animated)
        {
            if (animated)
                _sequence = DOTween.Sequence();
        }

        public bool IsAnimated => _sequence != null;
        public bool IsActive => _sequence != null && _sequence.active;
        public float Duration => _sequence != null ? _sequence.Duration() : 0f;

        /// <summary>Creates a timeline that either composes tweens or applies callbacks immediately.</summary>
        public static PresentationTimeline Create(bool animated)
        {
            return new PresentationTimeline(animated);
        }

        /// <summary>Creates an independent beat with the same playback mode as this timeline.</summary>
        public PresentationTimeline CreateBeat()
        {
            return new PresentationTimeline(IsAnimated);
        }

        public void Append(Tween tween)
        {
            if (_sequence != null && tween != null)
                _sequence.Append(tween);
        }

        public void Join(Tween tween)
        {
            if (_sequence != null && tween != null)
                _sequence.Join(tween);
        }

        public void Append(PresentationTimeline beat)
        {
            if (_sequence != null && beat?._sequence != null)
                _sequence.Append(beat._sequence);
        }

        public void AppendCallback(Action callback)
        {
            if (callback == null)
                return;

            if (_sequence != null)
                _sequence.AppendCallback(() => callback());
            else
                callback();
        }

        public void InsertCallback(float atPosition, Action callback)
        {
            if (callback == null)
                return;

            if (_sequence != null)
                _sequence.InsertCallback(atPosition, () => callback());
            else
                callback();
        }

        public void OnComplete(Action callback)
        {
            if (_sequence != null)
                _sequence.OnComplete(() => callback?.Invoke());
            else
                callback?.Invoke();
        }

        public void Kill(bool complete = false)
        {
            _sequence?.Kill(complete);
        }
    }
}
