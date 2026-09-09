using System;
using System.Threading;
using Blobs.Content;
using Blobs.Core;
using Cysharp.Threading.Tasks;
using DG.Tweening;

namespace Blobs.Presentation
{
    /// <summary>One fade, one continuous return, and one materialization or Grave departure.</summary>
    internal sealed class GhostRestPresenter
    {
        private readonly BlobPresenter _blobs;
        private readonly GhostReturnSettings _settings;
        public GhostRestPresenter(BlobPresenter blobs, GhostReturnSettings settings)
        {
            _blobs = blobs;
            _settings = settings;

        }

        public bool Present(GhostRestEffect effect, PresentationTimeline timeline, Action onContact)
        {
            if (!_blobs.TryRetireView(effect.GhostId, out BlobView ghost)) return false;

            if (!timeline.IsAnimated)
            {
                ghost.SetGridPosition(effect.RestDestination);
                ghost.BlobRenderer?.FadeableVisual?.Restore();
                _blobs.DestroyRetiringView(ghost);
                onContact?.Invoke();
                return true;
            }

            timeline.AppendAsync(token => ReturnAsync(effect, ghost, onContact, token));
            return true;
        }

        private async UniTask ReturnAsync(GhostRestEffect effect, BlobView ghost, Action onContact, CancellationToken token)
        {
            FadeableVisual fade = ghost.BlobRenderer?.FadeableVisual;
            try
            {
                if (fade != null) await fade.FadeTo(0f, _settings.FadeDuration, token);
                // A straight linear tween preserves constant speed across logical path cells.
                await PresentationTimeline.AwaitTweenAsync(ghost.AnimateMoveTo(
                    effect.RestDestination, _settings.MoveDuration * effect.Path.Count, Ease.Linear), token);

                // Reveal the Ghost at its resting place before despawning it.
                // Destruction must be the final operation on this view.
                if (fade != null)
                    await fade.FadeTo(1f, _settings.FadeDuration, token);
                onContact?.Invoke();
                token.ThrowIfCancellationRequested();
                await PresentationTimeline.AwaitTweenAsync(
                    ghost.PlayDespawn(_settings.DespawnDuration), token);
                _blobs.DestroyRetiringView(ghost);
            }
            finally
            {
                // Restart owns cleanup of retired views; do not touch a destroyed view.
                if (ghost != null && fade != null && token.IsCancellationRequested)
                    fade.Restore();
            }
        }
    }
}