using System;
using Blobs.Core;
using UnityEngine;

namespace Blobs.Presentation
{
    /// <summary>
    /// Owns normal-merge choreography while the blob presenter owns view tracking and retirement.
    /// </summary>
    internal sealed class NormalMergePresenter
    {
        private readonly BlobPresenter _blobs;
        private readonly MergeAnimationOrchestrator _orchestrator;

        public NormalMergePresenter(
            BlobPresenter blobs,
            MergeAnimationOrchestrator orchestrator)
        {
            _blobs = blobs ?? throw new ArgumentNullException(nameof(blobs));
            _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        }

        public bool Present(
            MoveBlobEffect move,
            RemoveBlobEffect remove,
            PresentationTimeline timeline,
            Action onContact = null)
        {
            if (!_blobs.TryGetView(move.BlobId, out BlobView sourceView) ||
                !_blobs.TryRetireView(remove.BlobId, out BlobView targetView))
            {
                return false;
            }

            if (!timeline.IsAnimated)
            {
                sourceView.SetGridPosition(move.To);
                sourceView.BlobMotionAnimator?.SetIdle();
                onContact?.Invoke();
                _blobs.DestroyRetiringView(targetView);
                return true;
            }

            Vector2Int direction = new(
                move.To.X - move.From.X,
                move.To.Y - move.From.Y);
            timeline.Append(_orchestrator.CreateMergeBeat(
                sourceView,
                targetView,
                direction,
                onContact,
                () => _blobs.DestroyRetiringView(targetView)));
            return true;
        }
    }
}
