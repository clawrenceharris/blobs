using DG.Tweening;
using UnityEngine;

namespace Blobs.Presentation
{
    /// <summary>
    /// Plays one authored merge-impact prefab. Particle shapes and motion live on the
    /// prefab; this component applies skin colors, sorting, timing, and cleanup.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MergeImpactVfx : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ShadowColorId = Shader.PropertyToID("_ShadowColor");
        private static readonly int HighlightColorId = Shader.PropertyToID("_HighlightColor");
        private static readonly int GlowColorId = Shader.PropertyToID("_GlowColor");
        private static readonly int CoreColorId = Shader.PropertyToID("_CoreColor");

        [Header("Authored Particle Layers")]
        [SerializeField] private ParticleSystem[] _blobColorParticles;
        [SerializeField] private ParticleSystem[] _sourceColorParticles;
        [SerializeField] private ParticleSystem[] _targetColorParticles;
        [SerializeField] private ParticleSystem[] _highlightParticles;
        [SerializeField] private ParticleSystem[] _extraParticles;
        [SerializeField, Range(0f, 1f)] private float _highlightColorBlend = 0.72f;

        [Header("Authored Sprite Layers")]
        [SerializeField] private SpriteRenderer _flashRenderer;
        [SerializeField] private SpriteRenderer _innerGlowRenderer;
        [SerializeField] private SpriteRenderer _ringRenderer;
        [SerializeField] private SpriteRenderer _tileHaloRenderer;

        [Header("Flash")]
        [SerializeField, Min(0.01f)] private float _flashDuration = 0.1f;
        [SerializeField, Min(0.01f)] private float _flashStartSize = 0.12f;
        [SerializeField, Min(0.01f)] private float _flashEndSize = 0.42f;
        [SerializeField, Range(0f, 1f)] private float _flashAlpha = 0.95f;

        [Header("Inner Glow")]
        [SerializeField, Min(0.01f)] private float _innerGlowDuration = 0.18f;
        [SerializeField, Min(0.01f)] private float _innerGlowStartSize = 0.28f;
        [SerializeField, Min(0.01f)] private float _innerGlowEndSize = 0.95f;
        [SerializeField, Range(0f, 1f)] private float _innerGlowAlpha = 0.4f;

        [Header("Shock Wave")]
        [SerializeField, Min(0.01f)] private float _ringDuration = 0.42f;
        [SerializeField, Min(0.01f)] private float _ringStartSize = 0.28f;
        [SerializeField, Min(0.01f)] private float _ringEndSize = 1.7f;
        [SerializeField, Range(0f, 1f)] private float _ringAlpha = 0.68f;
        [SerializeField, Range(0f, 1f)] private float _ringColorBlend = 0.45f;
        [SerializeField] private Vector2 _ringAspect = new(1.4f, 0.72f);

        [Header("Tile Halo")]
        [SerializeField, Min(0.01f)] private float _tileHaloDuration = 0.45f;
        [SerializeField, Min(0.01f)] private float _tileHaloStartSize = 0.85f;
        [SerializeField, Min(0.01f)] private float _tileHaloEndSize = 1.35f;
        [SerializeField, Range(0f, 1f)] private float _tileHaloAlpha = 0.3f;
        [SerializeField] private Vector2 _tileHaloAspect = new(1.45f, 0.8f);

        [Header("Sorting")]
        [SerializeField] private int _tileHaloOrderOffset = -3;
        [SerializeField] private int _ringOrderOffset = 0;
        [SerializeField] private int _particleOrderOffset = 1;
        [SerializeField] private int _innerGlowOrderOffset = 2;
        [SerializeField] private int _flashOrderOffset = 3;

        [Header("Glow")]
        [Tooltip("Fraction of a sprite layer's life spent fading up before it decays.")]
        [SerializeField, Range(0.01f, 0.8f)] private float _fadeInFraction = 0.22f;
        [Tooltip("HDR multiplier on the white core. Values above 1 are what bloom picks up.")]
        [SerializeField, Range(1f, 8f)] private float _coreIntensity = 3.2f;
        [Tooltip("HDR multiplier on the blob-colored halo around each white layer.")]
        [SerializeField, Range(1f, 8f)] private float _glowIntensity = 2.6f;

        [Header("Lifecycle")]
        [SerializeField, Min(0f)] private float _cleanupPadding = 0.08f;

        private Sequence _spriteSequence;
        private Tween _cleanupTween;
        private MaterialPropertyBlock _skinBlock;

        /// <summary>
        /// Starts the authored effect. Sprite layers with no sprite assigned are skipped.
        /// Extra particle systems play as authored so trails and dust can be dropped in later.
        /// </summary>
        public void Play(MergeImpactFeedbackContext context, int sortingLayerId, int sortingOrder)
        {
            PlaceImpactLayers(context);
            OrientDirectionalLayers(context.Direction);

            Skin resultSkin = context.ResultSkin;
            // The halo carries both blobs, so a merge always reads as a mix of the two
            // rather than only the colour of whichever blob moved.
            Color haloColor = Color.Lerp(context.SourceColor, context.TargetColor, 0.5f);
            Skin glintSkin = new(
                Color.Lerp(resultSkin.HighlightColor, Color.white, _highlightColorBlend),
                resultSkin.BaseColor,
                Color.white);

            float particleLifetime = 0f;
            particleLifetime = Mathf.Max(
                particleLifetime,
                PlayParticles(
                    _blobColorParticles,
                    resultSkin,
                    sortingLayerId,
                    sortingOrder + _particleOrderOffset,
                    applySkin: true));
            particleLifetime = Mathf.Max(
                particleLifetime,
                PlayParticles(
                    _sourceColorParticles,
                    context.SourceSkin,
                    sortingLayerId,
                    sortingOrder + _particleOrderOffset,
                    applySkin: true));
            particleLifetime = Mathf.Max(
                particleLifetime,
                PlayParticles(
                    _targetColorParticles,
                    context.TargetSkin,
                    sortingLayerId,
                    sortingOrder + _particleOrderOffset,
                    applySkin: true));
            particleLifetime = Mathf.Max(
                particleLifetime,
                PlayParticles(
                    _highlightParticles,
                    glintSkin,
                    sortingLayerId,
                    sortingOrder + _particleOrderOffset + 1,
                    applySkin: true));
            particleLifetime = Mathf.Max(
                particleLifetime,
                PlayParticles(
                    _extraParticles,
                    resultSkin,
                    sortingLayerId,
                    sortingOrder + _particleOrderOffset,
                    applySkin: false));

            _spriteSequence?.Kill(false);
            _spriteSequence = DOTween.Sequence();
            AnimateFlash(_flashRenderer, sortingLayerId, sortingOrder + _flashOrderOffset,
                 Color.white, Color.white, _flashAlpha, _flashStartSize, _flashEndSize, _flashDuration, Vector2.one);
            AnimateFlash(_innerGlowRenderer, sortingLayerId, sortingOrder + _innerGlowOrderOffset,
               Color.white, haloColor,
                _innerGlowAlpha, _innerGlowStartSize, _innerGlowEndSize, _innerGlowDuration, Vector2.one);
            AnimateFlash(_ringRenderer, sortingLayerId, sortingOrder + _ringOrderOffset,
                Color.Lerp(haloColor, Color.white, _ringColorBlend), haloColor,
                _ringAlpha, _ringStartSize, _ringEndSize, _ringDuration, _ringAspect);
            AnimateFlash(_tileHaloRenderer, sortingLayerId, sortingOrder + _tileHaloOrderOffset,
                haloColor, haloColor, _tileHaloAlpha, _tileHaloStartSize, _tileHaloEndSize,
                _tileHaloDuration, _tileHaloAspect);

            float visualLifetime = Mathf.Max(
                particleLifetime,
                Mathf.Max(_flashDuration, Mathf.Max(_innerGlowDuration, Mathf.Max(_ringDuration, _tileHaloDuration))));
            _cleanupTween?.Kill(false);
            _cleanupTween = DOVirtual.DelayedCall(
                visualLifetime + _cleanupPadding,
                () => Destroy(gameObject));
        }

        /// <summary>
        /// Sharp collision layers live at the silhouette seam. Grounded, longer-lived layers
        /// stay centred on the destination tile so the result reads in board space.
        /// </summary>
        private void PlaceImpactLayers(MergeImpactFeedbackContext context)
        {
            Vector3 contactPosition = context.ContactWorldPosition;
            transform.position = context.DestinationWorldPosition;

            PositionSystems(_blobColorParticles, contactPosition);
            PositionSystems(_sourceColorParticles, contactPosition);
            PositionSystems(_targetColorParticles, contactPosition);
            PositionSystems(_highlightParticles, contactPosition);
            PositionSystems(_extraParticles, contactPosition);

            SetWorldPosition(_flashRenderer, contactPosition);
            SetWorldPosition(_innerGlowRenderer, contactPosition);
            SetWorldPosition(_ringRenderer, context.DestinationWorldPosition);
            SetWorldPosition(_tileHaloRenderer, context.DestinationWorldPosition);
        }

        private static void PositionSystems(ParticleSystem[] systems, Vector3 worldPosition)
        {
            if (systems == null)
                return;

            foreach (ParticleSystem system in systems)
            {
                if (system != null)
                    system.transform.position = worldPosition;
            }
        }

        private static void SetWorldPosition(SpriteRenderer renderer, Vector3 worldPosition)
        {
            if (renderer != null)
                renderer.transform.position = worldPosition;
        }

        private void OrientDirectionalLayers(Vector2 direction)
        {
            if (direction.sqrMagnitude < 0.001f)
                return;

            Vector3 planar = new Vector3(direction.x, direction.y, 0f).normalized;
            // Two opposing, slightly upward jets read as liquid displaced from
            // the seam. A single forward-facing cone reads as collision debris.
            Vector3 splashAxis = Mathf.Abs(planar.x) >= Mathf.Abs(planar.y)
                ? new Vector3(Mathf.Sign(planar.x), 0f, 0f)
                : Vector3.right;
            Vector3 sourceJet = (-splashAxis + Vector3.up * 0.55f).normalized;
            Vector3 targetJet = (splashAxis + Vector3.up * 0.55f).normalized;
            RotateSystems(_blobColorParticles, ParticleFacing(Vector3.up));
            RotateSystems(_sourceColorParticles, ParticleFacing(sourceJet));
            RotateSystems(_targetColorParticles, ParticleFacing(targetJet));
            RotateSystems(_highlightParticles, ParticleFacing(planar));

            // The star and its glow point along the merge axis; the shock wave and tile
            // halo stay ground-aligned so they keep reading as a flat disc.
            Quaternion spriteFacing = Quaternion.Euler(
                0f,
                0f,
                Mathf.Atan2(planar.y, planar.x) * Mathf.Rad2Deg);
            if (_flashRenderer != null)
                _flashRenderer.transform.rotation = spriteFacing;
            if (_innerGlowRenderer != null)
                _innerGlowRenderer.transform.rotation = spriteFacing;
        }

        private static Quaternion ParticleFacing(Vector3 direction)
        {
            return Quaternion.FromToRotation(Vector3.up, direction)
                * Quaternion.Euler(-90f, 0f, 0f);
        }

        private static void RotateSystems(ParticleSystem[] systems, Quaternion rotation)
        {
            if (systems == null)
                return;

            foreach (ParticleSystem system in systems)
            {
                if (system != null)
                    system.transform.rotation = rotation;
            }
        }

        private float PlayParticles(
            ParticleSystem[] systems,
            Skin skin,
            int sortingLayerId,
            int sortingOrder,
            bool applySkin)
        {
            float maxLifetime = 0f;
            if (systems == null)
                return maxLifetime;

            foreach (ParticleSystem system in systems)
            {
                if (system == null)
                    continue;

                ParticleSystem.MainModule main = system.main;
                main.startColor = Color.white;

                if (system.TryGetComponent(out ParticleSystemRenderer renderer))
                {
                    renderer.sortingLayerID = sortingLayerId;
                    renderer.sortingOrder = sortingOrder;
                    if (applySkin)
                        ApplySkin(renderer, skin);
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

        private void ApplySkin(ParticleSystemRenderer renderer, Skin skin)
        {
            Material material = renderer.sharedMaterial;
            if (material == null || !material.HasProperty(BaseColorId))
                return;

            _skinBlock ??= new MaterialPropertyBlock();
            renderer.GetPropertyBlock(_skinBlock);
            _skinBlock.SetColor(BaseColorId, skin.BaseColor);
            if (material.HasProperty(ShadowColorId))
                _skinBlock.SetColor(ShadowColorId, skin.ShadowColor);
            if (material.HasProperty(HighlightColorId))
                _skinBlock.SetColor(HighlightColorId, skin.HighlightColor);
            renderer.SetPropertyBlock(_skinBlock);
        }

        private void AnimateFlash(
            SpriteRenderer renderer,
            int sortingLayerId,
            int sortingOrder,
            Color color,
            Color glowColor,
            float alpha,
            float startSize,
            float endSize,
            float duration,
            Vector2 aspect)
        {
            if (renderer == null || renderer.sprite == null)
                return;

            renderer.enabled = true;
            renderer.sortingLayerID = sortingLayerId;
            renderer.sortingOrder = sortingOrder;
            color.a = 0f;
            renderer.color = color;
            ApplyGlow(renderer, glowColor);
            SetWorldSize(renderer, startSize, aspect);

            float fadeIn = duration * _fadeInFraction;
            _spriteSequence.Join(ScaleToWorldSize(renderer, endSize, duration, aspect)
                .SetEase(Ease.OutCubic));
            _spriteSequence.Join(Fade(renderer, alpha, fadeIn).SetEase(Ease.OutQuad));
            _spriteSequence.Insert(fadeIn, Fade(renderer, 0f, duration - fadeIn).SetEase(Ease.InQuad));
        }

        /// <summary>
        /// Pushes the halo tint and HDR intensities into the glow material. Values above 1
        /// are what the camera's bloom pass keys on, so the brightness lives here.
        /// </summary>
        private void ApplyGlow(SpriteRenderer renderer, Color glowColor)
        {
            Material material = renderer.sharedMaterial;
            if (material == null || !material.HasProperty(GlowColorId))
                return;

            _skinBlock ??= new MaterialPropertyBlock();
            renderer.GetPropertyBlock(_skinBlock);
            _skinBlock.SetColor(GlowColorId, glowColor * _glowIntensity);
            if (material.HasProperty(CoreColorId))
                _skinBlock.SetColor(CoreColorId, Color.white * _coreIntensity);
            renderer.SetPropertyBlock(_skinBlock);
        }

        private static Tween ScaleToWorldSize(
            SpriteRenderer renderer,
            float worldSize,
            float duration,
            Vector2 aspect)
        {
            return renderer.transform.DOScale(
                ScaleForWorldSize(renderer.sprite, worldSize, aspect),
                duration);
        }

        private static void SetWorldSize(SpriteRenderer renderer, float worldSize, Vector2 aspect)
        {
            renderer.transform.localScale = ScaleForWorldSize(
                renderer.sprite,
                worldSize,
                aspect);
        }

        private static Vector3 ScaleForWorldSize(Sprite sprite, float worldSize, Vector2 aspect)
        {
            float authoredSize = Mathf.Max(
                sprite.bounds.size.x,
                sprite.bounds.size.y);
            float scale = authoredSize > 0f ? worldSize / authoredSize : 1f;
            return new Vector3(scale * aspect.x, scale * aspect.y, 1f);
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
            _innerGlowDuration = Mathf.Max(0.01f, _innerGlowDuration);
            _ringDuration = Mathf.Max(0.01f, _ringDuration);
            _tileHaloDuration = Mathf.Max(0.01f, _tileHaloDuration);
            _flashStartSize = Mathf.Max(0.01f, _flashStartSize);
            _flashEndSize = Mathf.Max(0.01f, _flashEndSize);
            _innerGlowStartSize = Mathf.Max(0.01f, _innerGlowStartSize);
            _innerGlowEndSize = Mathf.Max(0.01f, _innerGlowEndSize);
            _ringStartSize = Mathf.Max(0.01f, _ringStartSize);
            _ringEndSize = Mathf.Max(0.01f, _ringEndSize);
            _tileHaloStartSize = Mathf.Max(0.01f, _tileHaloStartSize);
            _tileHaloEndSize = Mathf.Max(0.01f, _tileHaloEndSize);
            _cleanupPadding = Mathf.Max(0f, _cleanupPadding);
        }
    }
}
