using System;
using Blobs.Application;
using Blobs.Core;
using UnityEngine;

namespace Blobs.Presentation
{
    /// <summary>
    /// Plays the source blob's refusal lean when a swipe is released onto a move the rules reject,
    /// so the attempted direction reads before the blob settles back into its cell.
    /// </summary>
    internal sealed class DragRejectionFeedbackPresenter : IDisposable
    {
        private readonly BlobPresenter _blobs;
        private readonly IGameplayState _state;

        public DragRejectionFeedbackPresenter(BlobPresenter blobs, IGameplayState state)
        {
            _blobs = blobs ?? throw new ArgumentNullException(nameof(blobs));
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _state.DragRejected += Present;
        }

        public void Present(DragPreview rejected)
        {
            if (rejected == null ||
                !_blobs.TryGetView(rejected.SourceId, out BlobView source) ||
                source.BlobMotionAnimator == null)
            {
                return;
            }

            GridPosition swipe = rejected.Tail - rejected.Origin;
            source.BlobMotionAnimator.SetRejected(new Vector2(swipe.X, swipe.Y));
        }

        public void Dispose()
        {
            _state.DragRejected -= Present;
        }
    }
}
