using DG.Tweening;
using UnityEngine;

namespace Blobs.Visuals
{
    /// <summary>
    /// Shock wave ring that scales out and fades. Attach to a child of MergeEffect.
    /// Requires SpriteRenderer (e.g. circle sprite) for the ring.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class ShockWaveEffect : MonoBehaviour
    {
        private SpriteRenderer _renderer;
        [SerializeField] private float _startScale = 0.2f;
        [SerializeField] private float _startAlpha = 0.9f;

        private void Awake()
        {
            if (_renderer == null)
                _renderer = GetComponent<SpriteRenderer>();
        }

        /// <summary>
        /// Play the shock wave: scale from start to endScale over duration, fade out.
        /// </summary>
        public Sequence Play(Color color, float duration, float endScale, AnimationCurve scaleCurve = null)
        {
            if (_renderer == null) return null;

            transform.localScale = Vector3.one * _startScale;

            Color startColor = color;
            startColor.a = _startAlpha;
            _renderer.color = startColor;

            scaleCurve ??= AnimationCurve.EaseInOut(0, 0, 1, 1);
            var sequence = DOTween.Sequence();
            sequence.Append(transform.DOScale(Vector3.one * endScale, duration)
                .SetEase(scaleCurve));
            sequence.Join(_renderer.DOFade(0f, duration)).AppendInterval(duration - 0.2f);
            return sequence;
        }
    }
}
