using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;

namespace Blobs.Presentation
{
    /// <summary>
    /// Composes simultaneous DOTween beats and awaits sequential asynchronous operations.
    /// Construction never starts animated playback; the owner awaits PlayAsync.
    /// </summary>
    public sealed class PresentationTimeline
    {
        private Sequence _sequence;
        private readonly List<Func<CancellationToken, UniTask>> _operations = new();
        private readonly List<PresentationTimeline> _children = new();
        private readonly List<Tween> _tweens = new();
        private bool _killed;
        private bool _started;

        private PresentationTimeline(bool animated) { IsAnimated = animated; }
        public bool IsAnimated { get; }
        public bool IsActive => !_killed;
        public float Duration { get; private set; }
        public static PresentationTimeline Create(bool animated) => new(animated);
        public PresentationTimeline CreateBeat() => Create(IsAnimated);

        private Sequence Sequence
        {
            get
            {
                if (_sequence == null)
                {
                    _sequence = DOTween.Sequence().Pause();
                    _tweens.Add(_sequence);
                }
                return _sequence;
            }
        }

        public void Append(Tween tween)
        {
            if (IsAnimated && tween != null) Sequence.Append(tween);
        }

        public void Join(Tween tween)
        {
            if (IsAnimated && tween != null) Sequence.Join(tween);
        }

        public void Append(PresentationTimeline beat)
        {
            if (beat == null) return;
            Flush();
            _children.Add(beat);
            _operations.Add(beat.PlayAsync);
        }

        public void AppendAsync(Func<CancellationToken, UniTask> operation)
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));
            Flush();
            _operations.Add(operation);
        }

        public void AppendCallback(Action callback)
        {
            if (callback == null) return;
            if (IsAnimated) Sequence.AppendCallback(() => callback());
            else callback();
        }

        public void InsertCallback(float atPosition, Action callback)
        {
            if (callback == null) return;
            if (IsAnimated) Sequence.InsertCallback(atPosition, () => callback());
            else callback();
        }

        private void Flush()
        {
            if (_sequence == null) return;
            Sequence sequence = _sequence;
            _sequence = null;
            Duration += sequence.Duration();
            _operations.Add(token => AwaitTweenAsync(sequence, token));
        }

        public async UniTask PlayAsync(CancellationToken cancellationToken = default)
        {
            if (_started) throw new InvalidOperationException("A timeline can only play once.");
            _started = true;
            Flush();
            foreach (var operation in _operations)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (_killed) throw new OperationCanceledException();
                await operation(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                if (_killed) throw new OperationCanceledException();
            }
        }

        /// <summary>Await completion, kill on cancellation, and never overwrite tween callbacks.</summary>
        public static async UniTask AwaitTweenAsync(Tween tween, CancellationToken token)
        {
            if (tween == null)
            {
                token.ThrowIfCancellationRequested();
                return;
            }
            bool completed = tween.IsComplete();
            TweenCallback markComplete = () => completed = true;
            tween.onComplete += markComplete;
            try
            {
                token.ThrowIfCancellationRequested();
                tween.Play();
                await UniTask.WaitUntil(() => !tween.active || tween.IsComplete(),
                    cancellationToken: token);
                token.ThrowIfCancellationRequested();
                if (!completed) throw new OperationCanceledException("The presentation tween was interrupted.");
            }
            finally
            {
                tween.onComplete -= markComplete;
                if (tween.active) tween.Kill();
            }
        }

        public void Kill(bool complete = false)
        {
            _killed = true;
            foreach (var child in _children) child.Kill(complete);
            foreach (var tween in _tweens)
                if (tween.active) tween.Kill(complete);
        }
    }
}
