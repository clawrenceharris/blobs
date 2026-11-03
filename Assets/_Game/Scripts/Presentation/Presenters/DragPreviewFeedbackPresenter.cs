using System;
using Blobs.Application;
using DG.Tweening;
using UnityEngine;

namespace Blobs.Presentation
{
    /// <summary>
    /// Turns a valid drag preview into readable intent: the source opens its mouth toward the
    /// target, and a nest under the tail gets a soft invite pulse. Clears when the drag ends.
    /// </summary>
    internal sealed class DragPreviewFeedbackPresenter : IDisposable
    {
        private readonly BlobPresenter _blobs;
        private readonly IGameplayState _state;
        private string _sourceId;
        private string _targetId;
        private Tween _nestPulse;
        private Vector3 _nestBaseScale;

        public DragPreviewFeedbackPresenter(BlobPresenter blobs, IGameplayState state)
        {
            _blobs = blobs ?? throw new ArgumentNullException(nameof(blobs));
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _state.DragPreviewChanged += Present;
        }

        public void Present(DragPreview preview)
        {
            if (preview == null || !preview.IsActive || preview.Verdict != DragVerdict.Valid ||
                string.IsNullOrEmpty(preview.TargetId))
            {
                Clear();
                return;
            }

            if (_sourceId == preview.SourceId && _targetId == preview.TargetId)
                return;

            ClearFacesAndPulse();
            _sourceId = preview.SourceId;
            _targetId = preview.TargetId;

            if (_blobs.TryGetView(_sourceId, out BlobView source) &&
                _blobs.TryGetView(_targetId, out BlobView target) &&
                source.Face != null)
            {
                Vector2 toward = new(
                    target.GridPosition.X - source.GridPosition.X,
                    target.GridPosition.Y - source.GridPosition.Y);
                source.Face.ShowEating(toward);
            }

            if (_blobs.TryGetView(_targetId, out BlobView nestHost) &&
                NestBlobView.TryResolve(nestHost, out NestBlobView nest))
            {
                Transform pulseTarget = nest.transform;
                _nestBaseScale = pulseTarget.localScale;
                _nestPulse = DOTween.Sequence()
                    .SetLink(pulseTarget.gameObject, LinkBehaviour.KillOnDestroy)
                    .Append(pulseTarget.DOScale(_nestBaseScale * 1.06f, 0.18f).SetEase(Ease.OutQuad))
                    .Append(pulseTarget.DOScale(_nestBaseScale, 0.18f).SetEase(Ease.InQuad))
                    .SetLoops(-1, LoopType.Restart);
            }
        }

        public void Dispose()
        {
            _state.DragPreviewChanged -= Present;
            Clear();
        }

        private void Clear()
        {
            ClearFacesAndPulse();
            _sourceId = null;
            _targetId = null;
        }

        private void ClearFacesAndPulse()
        {
            if (!string.IsNullOrEmpty(_sourceId) &&
                _blobs.TryGetView(_sourceId, out BlobView source) &&
                source.Face != null)
            {
                source.Face.ShowIdle();
            }

            if (_nestPulse != null)
            {
                _nestPulse.Kill();
                _nestPulse = null;
                if (!string.IsNullOrEmpty(_targetId) &&
                    _blobs.TryGetView(_targetId, out BlobView nestHost) &&
                    NestBlobView.TryResolve(nestHost, out NestBlobView nest))
                {
                    nest.transform.localScale = _nestBaseScale;
                }
            }
        }
    }
}
