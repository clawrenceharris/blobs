using DG.Tweening;
using UnityEngine;

namespace Blobs.Presentation
{
    /// <summary>
    /// Board-space highlight spanning an active drag from its source cell to its tail cell.
    /// Tweens only drive extent, scale, and alpha values; <see cref="Apply"/> is the single place
    /// those values reach the renderer, so overlapping show/extend/hide tweens cannot fight.
    /// </summary>
    public sealed class SwipeIndicatorView : MonoBehaviour
    {
        [SerializeField, Tooltip("Use a 9-sliced sprite with Draw Mode Sliced so end caps keep their shape while extending.")]
        private SpriteRenderer spriteRenderer;

        [Header("Shape")]
        [SerializeField, Min(0.1f), Tooltip("Indicator thickness and end-cap footprint, as a fraction of one cell.")]
        private float cellFootprint = 1.05f;

        [Header("Show")]
        [SerializeField, Min(0f)] private float showDuration = 0.14f;
        [SerializeField, Range(0f, 1f)] private float showStartScale = 0.7f;
        [SerializeField] private Ease showEase = Ease.OutBack;

        [Header("Extend")]
        [SerializeField, Min(0f)] private float extendDuration = 0.12f;
        [SerializeField] private Ease extendEase = Ease.OutCubic;
        [SerializeField, Min(0f), Tooltip("Retract time when the drag turns or reverses back through its source cell.")]
        private float retractDuration = 0.06f;

        [Header("Tint")]
        [SerializeField, Min(0f), Tooltip("Kept short so sweeping across several cells does not strobe between colors.")]
        private float tintDuration = 0.08f;
        [SerializeField] private Ease tintEase = Ease.OutQuad;

        [Header("Hide")]
        [SerializeField, Min(0f)] private float hideDuration = 0.1f;
        [SerializeField, Range(0f, 1f)] private float hideEndScale = 0.85f;
        [SerializeField] private Ease hideEase = Ease.InQuad;

        private Vector3 _originLocal;
        private float _cellSize = 1f;
        private Vector2 _extent;
        private float _scale = 1f;
        private float _alpha;
        private Color _tint = Color.white;
        private Color _baseColor = Color.white;
        private Tween _extentTween;
        private Tween _visibilityTween;
        private Tween _tintTween;

        public bool IsVisible => spriteRenderer != null && spriteRenderer.enabled;

        private void Awake()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
            if (spriteRenderer != null)
                _baseColor = spriteRenderer.color;
            HideImmediately();
        }

        private void OnValidate()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
        }

        private void OnDestroy()
        {
            KillTweens();
        }

        /// <summary>Sets the board cell size used to convert cell extents into local units.</summary>
        public void Configure(float cellSize)
        {
            _cellSize = Mathf.Max(0.01f, cellSize);
        }

        /// <summary>Pops the indicator in around the source cell with no extent.</summary>
        public void Show(Vector3 originLocal, Color tint)
        {
            if (spriteRenderer == null)
                return;

            KillTweens();
            _originLocal = originLocal;
            _tint = tint;
            _extent = Vector2.zero;
            _scale = showStartScale;
            _alpha = 0f;
            spriteRenderer.enabled = true;
            Apply();

            _visibilityTween = DOTween.Sequence()
                .Join(TweenScale(1f, showDuration).SetEase(showEase))
                .Join(TweenAlpha(1f, showDuration).SetEase(Ease.OutQuad))
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        }

        /// <summary>
        /// Stretches the indicator from the source cell toward an axis-aligned cell offset.
        /// Turning or reversing retracts through the source first so the shape never cuts a diagonal.
        /// </summary>
        public void ExtendTo(Vector2Int cellOffset)
        {
            if (!IsVisible)
                return;

            Vector2 target = cellOffset;
            _extentTween?.Kill();

            if (_extent.sqrMagnitude > 0.0001f &&
                target != Vector2.zero &&
                Vector2.Dot(_extent, target) <= 0f)
            {
                _extentTween = DOTween.Sequence()
                    .Append(TweenExtent(Vector2.zero, retractDuration).SetEase(Ease.InQuad))
                    .Append(TweenExtent(target, extendDuration).SetEase(extendEase))
                    .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
                return;
            }

            _extentTween = TweenExtent(target, extendDuration)
                .SetEase(extendEase)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        }

        /// <summary>Blends the indicator toward a new tint, such as the palette's invalid-move color.</summary>
        public void SetTint(Color tint)
        {
            if (!IsVisible || (_tintTween == null && _tint == tint))
                return;

            _tintTween?.Kill();
            _tintTween = DOTween.To(() => _tint, value => { _tint = value; Apply(); }, tint, tintDuration)
                .SetEase(tintEase)
                .OnComplete(() => _tintTween = null)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        }

        /// <summary>Fades the indicator out where it stands, then disables the renderer.</summary>
        public void Hide()
        {
            if (!IsVisible)
                return;

            KillTweens();
            _visibilityTween = DOTween.Sequence()
                .Join(TweenScale(_scale * hideEndScale, hideDuration))
                .Join(TweenAlpha(0f, hideDuration))
                .SetEase(hideEase)
                .OnComplete(() =>
                {
                    _visibilityTween = null;
                    spriteRenderer.enabled = false;
                })
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        }

        public void HideImmediately()
        {
            KillTweens();
            _extent = Vector2.zero;
            _scale = 1f;
            _alpha = 0f;
            if (spriteRenderer == null)
                return;

            Apply();
            spriteRenderer.enabled = false;
        }

        private Tween TweenExtent(Vector2 target, float duration)
        {
            return DOTween.To(() => _extent, value => { _extent = value; Apply(); }, target, duration);
        }

        private Tween TweenScale(float target, float duration)
        {
            return DOTween.To(() => _scale, value => { _scale = value; Apply(); }, target, duration);
        }

        private Tween TweenAlpha(float target, float duration)
        {
            return DOTween.To(() => _alpha, value => { _alpha = value; Apply(); }, target, duration);
        }

        private void Apply()
        {
            Vector2 span = new(Mathf.Abs(_extent.x), Mathf.Abs(_extent.y));
            float footprint = _cellSize * cellFootprint;
            Vector2 size = (span * _cellSize + new Vector2(footprint, footprint)) * _scale;

            transform.localPosition = _originLocal + (Vector3)(_extent * (_cellSize * 0.5f));

            if (spriteRenderer.drawMode == SpriteDrawMode.Simple)
            {
                Vector2 native = spriteRenderer.sprite != null
                    ? (Vector2)spriteRenderer.sprite.bounds.size
                    : Vector2.one;
                transform.localScale = new Vector3(size.x / native.x, size.y / native.y, 1f);
            }
            else
            {
                spriteRenderer.size = size;
            }

            Color color = _baseColor * _tint;
            color.a *= _alpha;
            spriteRenderer.color = color;
        }

        private void KillTweens()
        {
            _tintTween?.Kill();
            _tintTween = null;
            _extentTween?.Kill();
            _extentTween = null;
            _visibilityTween?.Kill();
            _visibilityTween = null;
        }
    }
}
