using DG.Tweening;
using UnityEngine;

namespace Blobs.Presentation
{
    /// <summary>
    /// Plays one authored merge-impact prefab. Particle shapes and motion live on the
    /// prefab; this component only applies the blob color, sorting, timing, and cleanup.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MergeImpactVfx : MonoBehaviour
    {
        [Header("Authored Particle Layers")]
        [SerializeField] private ParticleSystem[] _blobColorParticles;
        [SerializeField] private ParticleSystem[] _highlightParticles;
        [SerializeField, Range(0f, 1f)] private float _highlightColorBlend = 0.72f;

        [Header("Authored Sprite Layers")]
        [SerializeField] private SpriteRenderer _flashRenderer;
        [SerializeField] private SpriteRenderer _ringRenderer;

        [Header("Flash")]
        [SerializeField, Min(0.01f)] private float _flashDuration = 0.13f;
        [SerializeField, Min(0.01f)] private float _flashStartSize = 0.12f;
        [SerializeField, Min(0.01f)] private float _flashEndSize = 0.9f;
        [SerializeField, Range(0f, 1f)] private float _flashAlpha = 0.92f;

        [Header("Shock Wave")]
        [SerializeField, Min(0.01f)] private float _ringDuration = 0.38f;
        [SerializeField, Min(0.01f)] private float _ringStartSize = 0.22f;
        [SerializeField, Min(0.01f)] private float _ringEndSize = 1.55f;
        [SerializeField, Range(0f, 1f)] private float _ringAlpha = 0.68f;
        [SerializeField, Range(0f, 1f)] private float _ringColorBlend = 0.55f;

        [Header("Sorting")]
        [SerializeField] private int _ringOrderOffset = 0;
        [SerializeField] private int _particleOrderOffset = 1;
        [SerializeField] private int _flashOrderOffset = 2;

        [Header("Lifecycle")]
        [SerializeField, Min(0f)] private float _cleanupPadding = 0.08f;

        private Sequence _spriteSequence;
        private Tween _cleanupTween;

        /// <summary>Starts the authored effect using the merge blob's palette color.</summary>
        public void Play(Color blobColor, int sortingLayerId, int sortingOrder)
        {
            Color highlightColor = Color.Lerp(
                blobColor,
                Color.white,
                _highlightColorBlend);

            float particleLifetime = 0f;
            particleLifetime = Mathf.Max(
                particleLifetime,
                PlayParticles(
                    _blobColorParticles,
                    blobColor,
                    sortingLayerId,
                    sortingOrder + _particleOrderOffset));
            particleLifetime = Mathf.Max(
                particleLifetime,
                PlayParticles(
                    _highlightParticles,
                    highlightColor,
                    sortingLayerId,
                    sortingOrder + _particleOrderOffset + 1));

            _spriteSequence?.Kill(false);
            _spriteSequence = DOTween.Sequence();
            AnimateFlash(sortingLayerId, sortingOrder + _flashOrderOffset);
            AnimateRing(
                Color.Lerp(blobColor, Color.white, _ringColorBlend),
                sortingLayerId,
                sortingOrder + _ringOrderOffset);

            float visualLifetime = Mathf.Max(
                particleLifetime,
                Mathf.Max(_flashDuration, _ringDuration));
            _cleanupTween?.Kill(false);
            _cleanupTween = DOVirtual.DelayedCall(
                visualLifetime + _cleanupPadding,
                () => Destroy(gameObject));
        }

        private static float PlayParticles(
            ParticleSystem[] systems,
            Color color,
            int sortingLayerId,
            int sortingOrder)
        {
            float maxLifetime = 0f;
            if (systems == null)
                return maxLifetime;

            foreach (ParticleSystem system in systems)
            {
                if (system == null)
                    continue;

                ParticleSystem.MainModule main = system.main;
                main.startColor = color;

                if (system.TryGetComponent(out ParticleSystemRenderer renderer))
                {
                    renderer.sortingLayerID = sortingLayerId;
                    renderer.sortingOrder = sortingOrder;
                }

                system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                system.Play(true);
                maxLifetime = Mathf.Max(
                    maxLifetime,
                    main.startDelay.constantMax +
                    main.duration +
                    main.startLifetime.constantMax);
            }

            return maxLifetime;
        }

        private void AnimateFlash(int sortingLayerId, int sortingOrder)
        {
            if (_flashRenderer == null || _flashRenderer.sprite == null)
                return;

            _flashRenderer.enabled = true;
            _flashRenderer.sortingLayerID = sortingLayerId;
            _flashRenderer.sortingOrder = sortingOrder;
            _flashRenderer.color = new Color(1f, 1f, 1f, _flashAlpha);
            SetWorldSize(_flashRenderer, _flashStartSize);

            _spriteSequence.Join(ScaleToWorldSize(
                    _flashRenderer,
                    _flashEndSize,
                    _flashDuration)
                .SetEase(Ease.OutCubic));
            _spriteSequence.Join(Fade(
                    _flashRenderer,
                    0f,
                    _flashDuration)
                .SetEase(Ease.InQuad));
        }

        private void AnimateRing(
            Color color,
            int sortingLayerId,
            int sortingOrder)
        {
            if (_ringRenderer == null || _ringRenderer.sprite == null)
                return;

            _ringRenderer.enabled = true;
            _ringRenderer.sortingLayerID = sortingLayerId;
            _ringRenderer.sortingOrder = sortingOrder;
            color.a = _ringAlpha;
            _ringRenderer.color = color;
            SetWorldSize(_ringRenderer, _ringStartSize);

            _spriteSequence.Join(ScaleToWorldSize(
                    _ringRenderer,
                    _ringEndSize,
                    _ringDuration)
                .SetEase(Ease.OutCubic));
            _spriteSequence.Join(Fade(
                    _ringRenderer,
                    0f,
                    _ringDuration)
                .SetEase(Ease.InQuad));
        }

        private static Tween ScaleToWorldSize(
            SpriteRenderer renderer,
            float worldSize,
            float duration)
        {
            return renderer.transform.DOScale(
                ScaleForWorldSize(renderer.sprite, worldSize),
                duration);
        }

        private static void SetWorldSize(SpriteRenderer renderer, float worldSize)
        {
            renderer.transform.localScale = ScaleForWorldSize(
                renderer.sprite,
                worldSize);
        }

        private static Vector3 ScaleForWorldSize(Sprite sprite, float worldSize)
        {
            float authoredSize = Mathf.Max(
                sprite.bounds.size.x,
                sprite.bounds.size.y);
            float scale = authoredSize > 0f ? worldSize / authoredSize : 1f;
            return Vector3.one * scale;
        }

        private static Tween Fade(
            SpriteRenderer renderer,
            float alpha,
            float duration)
        {
            return DOTween.To(
                () => renderer.color.a,
                value =>
                {
                    Color color = renderer.color;
                    color.a = value;
                    renderer.color = color;
                },
                alpha,
                duration);
        }

        private void OnDestroy()
        {
            _spriteSequence?.Kill(false);
            _cleanupTween?.Kill(false);
        }

        private void OnValidate()
        {
            _flashDuration = Mathf.Max(0.01f, _flashDuration);
            _ringDuration = Mathf.Max(0.01f, _ringDuration);
            _flashStartSize = Mathf.Max(0.01f, _flashStartSize);
            _flashEndSize = Mathf.Max(0.01f, _flashEndSize);
            _ringStartSize = Mathf.Max(0.01f, _ringStartSize);
            _ringEndSize = Mathf.Max(0.01f, _ringEndSize);
            _cleanupPadding = Mathf.Max(0f, _cleanupPadding);
        }
    }
}
