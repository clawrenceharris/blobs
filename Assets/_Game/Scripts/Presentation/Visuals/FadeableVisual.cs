using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace Blobs.Presentation
{
    /// <summary>Fades an explicitly authored group, preserving each renderer's original opacity.</summary>
    public sealed class FadeableVisual : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer[] renderers;
        private float[] _authoredAlpha;
        private Tween _fade;

        private void Cache()
        {
            if (_authoredAlpha != null) return;
            renderers ??= System.Array.Empty<SpriteRenderer>();
            _authoredAlpha = new float[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
                _authoredAlpha[i] = renderers[i] != null ? renderers[i].color.a : 1f;
        }

        public async UniTask FadeTo(float opacity, float duration,
            CancellationToken cancellationToken = default)
        {
            Cache();
            cancellationToken.ThrowIfCancellationRequested();
            _fade?.Kill();
            if (duration <= 0f)
            {
                SetOpacity(opacity);
                return;
            }
            Sequence sequence = DOTween.Sequence().Pause();
            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i] != null)
                    sequence.Join(renderers[i].DOFade(_authoredAlpha[i] * opacity, duration));
            _fade = sequence;
            try { await PresentationTimeline.AwaitTweenAsync(sequence, cancellationToken); }
            finally { if (_fade == sequence) _fade = null; }
        }

        private void SetOpacity(float opacity)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                Color color = renderers[i].color;
                color.a = _authoredAlpha[i] * opacity;
                renderers[i].color = color;
            }
        }

        public void Restore()
        {
            Cache();
            _fade?.Kill();
            _fade = null;
            SetOpacity(1f);
        }

        private void OnDisable() => Restore();
    }
}
