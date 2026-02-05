
// using Blobs.Merge.Animation;
// using DG.Tweening;
// using UnityEngine;

// [RequireComponent(typeof(BlobView))]
// [RequireComponent(typeof(Animator))]
// public class BlobAnimator : MonoBehaviour, IBlobAnimator
// {

//     protected BlobView blobView;
//     protected SpriteRenderer _renderer;
//     private Animator _animator;
//     [SerializeField] private BlobAnimationRecipe _recipe;
//     public BlobAnimationRecipe Recipe => _recipe;
//     private Sequence _selectionLoop;
//     private Vector3 _baseScale;


//     private void Awake()
//     {
//         blobView = GetComponent<BlobView>();
//         _animator = GetComponent<Animator>();
//     }
    
//     private void Start()
//     {
//         _recipe = AnimationRecipeProvider.Instance.GetRecipe(blobView.Model.Type);
//         _renderer = blobView.Visuals.SpriteRenderer;
//         _baseScale = transform.localScale;
//     }
//     public void PlaySpawnAnimation()
//         {
//             _animator.PlaySpawnAnimation();
//         }

//         public void PlaySelectAnimation()
//         {
//             _animator?.PlaySelectAnimation();
//         }

//         public void PlayDeselectAnimation()
//         {
//             _animator?.PlayDeselectAnimation();
//         }

//         public void PlayIdleAnimation()
//         {
//             _animator?.StartIdleAnimation();
//         }

//         public void PlayMoveAnimation(Vector3 target, System.Action onComplete)
//         {
//             if (_animator != null)
//                 _animator.AnimateMoveTo(target, onComplete);
//             else
//             {
//                 transform.position = target;
//                 onComplete?.Invoke();
//             }
//         }

//         public void PlayMergeAnimation(Color color, System.Action onComplete)
//         {
//             // BlobAnimator expects (Vector3, Action, Color) - use current position as target
//             _animator?.PlayMergeAnimation(transform.position, onComplete, color);
//         }

//         public void PlayDespawnAnimation(System.Action onComplete)
//         {
//             _animator?.PlayDespawnAnimation(onComplete);
//         }

//         public void PlayShakeAnimation()
//         {
//             _animator?.PlayShakeAnimation();
//         }
    
//     public virtual Tween CreateMoveTween(Vector3 worldTarget, float duration) =>
//         transform.DOMove(worldTarget, duration).SetEase(Ease.OutQuad);

//     public virtual Tween CreateScaleTween(float targetScale, float duration) =>
//         transform.DOScale(targetScale, duration).SetEase(Ease.OutBack);

//     public virtual Tween CreateRemoveTween(float duration) =>
//         transform.DOScale(0f, duration).SetEase(Ease.InBack);

//     public virtual Tween CreateSpawnTween(float duration)
//     {
//         transform.localScale = Vector3.zero;
//         return transform.DOScale(1f, duration).SetEase(Ease.OutBack);
//     }

//     public virtual void StartSelectionLoop()
//     {
//         float selectionSquishDuration = _recipe.selectionSquishDuration;
//         float selectionSquishAmount = _recipe.selectionSquishAmount;
//         float selectionStretchAmount = _recipe.selectionStretchAmount;

       
//         // Kill any existing loop
//         StopSelectionLoop();
        
//         // Store current scale as base (in case it was modified)
//         _baseScale = transform.localScale;
        
//         // Create squish in/out loop: squash down (Y smaller, X larger) then return to base
//         _selectionLoop = DOTween.Sequence();
        
//         Vector3 squishScale = new Vector3(
//             _baseScale.x * selectionStretchAmount,
//             _baseScale.y * selectionSquishAmount,
//             _baseScale.z
//         );
        
//         // Squish in
//         _selectionLoop.Append(transform.DOScale(squishScale, selectionSquishDuration)
//             .SetEase(Ease.OutQuad));
        
//         // Squish out (back to base)
//         _selectionLoop.Append(transform.DOScale(_baseScale, selectionSquishDuration)
//             .SetEase(Ease.InQuad));
        
//         // Loop indefinitely
//         _selectionLoop.SetLoops(-1, LoopType.Restart);
//     }

//     public virtual void StopSelectionLoop()
//     {
//         if (_selectionLoop != null && _selectionLoop.IsActive())
//         {
//             // Kill the loop and smoothly blend back to base scale
//             _selectionLoop.Kill();
//             transform.DOScale(_baseScale, _recipe.selectionSquishDuration * 0.5f).SetEase(Ease.OutQuad);
//         }
//         _selectionLoop = null;
//     }
    
    
// }


using UnityEngine;
using DG.Tweening;
using System;

namespace Blobs.Animation
{
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(BlobView))]
    /// <summary>
    /// Handles all blob animations using DOTween.
    /// Provides smooth state-based animations: idle, selected, moving, merging.
    /// </summary>
    public class BlobAnimator : MonoBehaviour
    {
        public enum BlobState
        {
            Idle,
            Selected,
            Moving,
            Merging
        }

        [Header("Idle Animation")]
        [SerializeField] private float idleScaleAmount = 0.02f;
        [SerializeField] private float idleScaleDuration = 1.5f;
        [SerializeField] private float idleFloatAmount = 0.03f;
        [SerializeField] private float idleFloatDuration = 2f;

        [Header("Selected Animation")]

        [SerializeField] private float selectionSquishDuration = 0.3f;
        [SerializeField] private float selectionSquishAmount = 0.85f;
        [SerializeField] private float selectionStretchAmount = 1.08f;

        [Header("Movement")]
        [SerializeField] private float moveDuration = 0.3f;
        [SerializeField] private float moveArcHeight = 0.3f;
        [SerializeField] private Ease moveEase = Ease.OutQuad;

        [Header("Spawn/Despawn")]
        [SerializeField] private float spawnDuration = 0.25f;
        [SerializeField] private float despawnDuration = 0.2f;

        [Header("Merge")]
        [SerializeField] private float mergeDuration = 0.25f;

        [Header("Particles")]
        [SerializeField] private ParticleSystem mergeParticlePrefab;

        private Vector3 _originalScale;
        private Vector3 _originalPosition;
        private BlobState currentState = BlobState.Idle;
        private bool isAnimating;

        // DOTween sequences
        private Sequence idleSequence;
        private Sequence selectedSequence;
        private Tween currentMoveTween;

        public bool IsAnimating => isAnimating;
        public BlobState CurrentState => currentState;

        public System.Action OnMoveComplete;
        public System.Action OnDespawnComplete;
        protected BlobView blobView;
        protected SpriteRenderer _renderer;
        private Sequence _selectionLoop;
        private Vector3 _baseScale;


        private void Awake()
        {
            blobView = GetComponent<BlobView>();

        }


        private void Start()
        {
            // Start idle animation by default
            StartIdleAnimation();
            _originalScale = transform.localScale;
            _originalPosition = transform.localPosition;
        }

        private void OnDestroy()
        {
            KillAllTweens();
        }

        private void KillAllTweens()
        {
            idleSequence?.Kill();
            selectedSequence?.Kill();
            currentMoveTween?.Kill();
            transform.DOKill();
        }

        #region State Animations

        /// <summary>
        /// Start idle breathing/floating animation
        /// </summary>
        public void StartIdleAnimation()
        {
            if (currentState == BlobState.Idle) return;

            KillAllTweens();
            currentState = BlobState.Idle;

            // Reset to original state
            transform.localScale = _originalScale;
            transform.localRotation = Quaternion.identity;

            // Create idle sequence - gentle breathing + floating
            idleSequence = DOTween.Sequence();

            // Breathing: scale pulse
            idleSequence.Append(
                transform.DOScale(_originalScale * (1f + idleScaleAmount), idleScaleDuration / 2f)
                    .SetEase(Ease.InOutSine)
            );
            idleSequence.Append(
                transform.DOScale(_originalScale * (1f - idleScaleAmount * 0.5f), idleScaleDuration / 2f)
                    .SetEase(Ease.InOutSine)
            );

            // Also add subtle float movement
            transform.DOLocalMoveY(_originalPosition.y + idleFloatAmount, idleFloatDuration / 2f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);

            idleSequence.SetLoops(-1, LoopType.Restart);
        }

        #endregion

        #region Selection


        private void StartSelectedAnimation()
        {

            if (currentState == BlobState.Selected) return;

            KillAllTweens();
            currentState = BlobState.Selected;
            // Kill any existing loop
            StopSelectionLoop();

            // Store current scale as base (in case it was modified)
            _originalScale = transform.localScale;

            // Create squish in/out loop: squash down (Y smaller, X larger) then return to base
            _selectionLoop = DOTween.Sequence();

            Vector3 squishScale = new Vector3(
                _baseScale.x * selectionStretchAmount,
                _baseScale.y * selectionSquishAmount,
                _baseScale.z
            );

            // Squish in
            _selectionLoop.Append(transform.DOScale(squishScale, selectionSquishDuration)
                .SetEase(Ease.OutQuad));

            // Squish out (back to base)
            _selectionLoop.Append(transform.DOScale(_baseScale, selectionSquishDuration)
                .SetEase(Ease.InQuad));

            // Loop indefinitely
            _selectionLoop.SetLoops(-1, LoopType.Restart);
        }

        private void StopSelectionLoop()
        {
            if (_selectionLoop != null && _selectionLoop.IsActive())
            {
                // Kill the loop and smoothly blend back to base scale
                _selectionLoop.Kill();
                transform.DOScale(_baseScale, selectionSquishDuration * 0.5f).SetEase(Ease.OutQuad);
            }
            _selectionLoop = null;
        }

        public void PlaySelectAnimation()
        {
            StartSelectedAnimation();
        }

        public void PlayDeselectAnimation()
        {
            // Reset rotation
            transform.DORotate(Vector3.zero, 0.1f).SetEase(Ease.OutQuad);
            StartIdleAnimation();
        }

        #endregion

        #region Movement

        /// <summary>
        /// Smoothly move to target position with arc
        /// </summary>
        public void AnimateMoveTo(Vector3 targetPosition, Action onComplete = null)
        {
            KillAllTweens();
            currentState = BlobState.Moving;
            isAnimating = true;

            Vector3 startPosition = transform.position;

            // Create arc path
            Vector3[] path = new Vector3[3];
            path[0] = startPosition;
            path[1] = (startPosition + targetPosition) / 2f + Vector3.up * moveArcHeight;
            path[2] = targetPosition;

            currentMoveTween = transform.DOPath(path, moveDuration, PathType.CatmullRom)
                .SetEase(moveEase)
                .OnComplete(() =>
                {
                    isAnimating = false;
                    _originalPosition = transform.localPosition;
                    StartIdleAnimation();
                    onComplete?.Invoke();
                    OnMoveComplete?.Invoke();
                });
        }

        #endregion

        #region Spawn/Despawn/Scale

        public void PlaySpawnAnimation()
        {
            KillAllTweens();

            transform.localScale = Vector3.zero;
            transform.localRotation = Quaternion.identity;

            Sequence spawnSeq = DOTween.Sequence();

            // Pop in with overshoot
            spawnSeq.Append(
                transform.DOScale(_originalScale * 1.15f, spawnDuration * 0.6f)
                    .SetEase(Ease.OutBack)
            );
            spawnSeq.Append(
                transform.DOScale(_originalScale, spawnDuration * 0.4f)
                    .SetEase(Ease.OutBounce)
            );

            spawnSeq.OnComplete(() =>
            {
                StartIdleAnimation();
            });
        }

        public void PlayDespawnAnimation(System.Action onComplete = null)
        {
            KillAllTweens();
            currentState = BlobState.Merging;
            isAnimating = true;

            Sequence despawnSeq = DOTween.Sequence();

            // Shrink + spin + fade
            despawnSeq.Append(
                transform.DOScale(Vector3.zero, despawnDuration)
                    .SetEase(Ease.InBack)
            );
            despawnSeq.Join(
                transform.DORotate(new Vector3(0, 0, 180f), despawnDuration, RotateMode.FastBeyond360)
                    .SetEase(Ease.InQuad)
            );

            // Fade out sprite
            SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                despawnSeq.Join(
                    sr.DOFade(0f, despawnDuration)
                        .SetEase(Ease.InQuad)
                );
            }

            despawnSeq.OnComplete(() =>
            {
                isAnimating = false;
                onComplete?.Invoke();
                OnDespawnComplete?.Invoke();
            });
        }

        public void PlayResizeAnimation(float targetScale)
        {

            transform.localScale = Vector3.zero;
            transform.localRotation = Quaternion.identity;

            Sequence spawnSeq = DOTween.Sequence();

            spawnSeq.Append(
                transform.DOScale(targetScale, spawnDuration * 0.4f)
                    .SetEase(Ease.OutBounce)
            );
        }


        #endregion

        #region Merge Animation

        /// <summary>
        /// Play merge animation - squish towards target then spawn particles
        /// </summary>
        public void PlayMergeAnimation(Vector3 targetPosition, System.Action onComplete = null)
        {
            KillAllTweens();
            currentState = BlobState.Merging;
            isAnimating = true;

            Vector3 startPosition = transform.position;

            Sequence mergeSeq = DOTween.Sequence();

            // Squish towards target (stretch in direction of movement)
            Vector3 direction = (targetPosition - startPosition).normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            // Stretch effect
            mergeSeq.Append(
                transform.DOScale(new Vector3(_originalScale.x * 1.3f, _originalScale.y * 0.7f, _originalScale.z), mergeDuration * 0.3f)
                    .SetEase(Ease.OutQuad)
            );

            // Move to target while shrinking
            mergeSeq.Append(
                transform.DOMove(targetPosition, mergeDuration * 0.7f)
                    .SetEase(Ease.InQuad)
            );
            mergeSeq.Join(
                transform.DOScale(Vector3.zero, mergeDuration * 0.7f)
                    .SetEase(Ease.InQuad)
            );

            mergeSeq.OnComplete(() =>
            {
                // Spawn merge particles
                SpawnMergeParticles(targetPosition, ColorSchemeManager.FromBlobColor(blobView.Model.Color));

                isAnimating = false;
                onComplete?.Invoke();
            });
        }

        private void SpawnMergeParticles(Vector3 position, Color color)
        {
            if (mergeParticlePrefab == null) return;

            ParticleSystem particles = Instantiate(mergeParticlePrefab, position, Quaternion.identity);

            // Set particle color to match blob
            var main = particles.main;
            main.startColor = color;

            particles.Play();

            // Auto-destroy after particles finish
            Destroy(particles.gameObject, main.duration + main.startLifetime.constantMax);
        }

        #endregion

        #region Effects

        /// <summary>
        /// Quick shake effect for invalid moves
        /// </summary>
        public void PlayShakeAnimation()
        {
            // Don't interrupt important animations
            if (currentState == BlobState.Moving || currentState == BlobState.Merging) return;

            transform.DOShakePosition(0.3f, 0.1f, 20, 90, false, true)
                .OnComplete(() =>
                {
                    // Return to current state animation
                    if (currentState == BlobState.Selected)
                        StartSelectedAnimation();
                    else
                        StartIdleAnimation();
                });
        }

        #endregion

        #region Utility

        public void SetOriginalScale(Vector3 scale)
        {
            _originalScale = scale;
        }

        public void SetMergeParticlePrefab(ParticleSystem prefab)
        {
            mergeParticlePrefab = prefab;
        }

        /// <summary>
        /// Force reset to idle state
        /// </summary>
        public void ResetToIdle()
        {
            KillAllTweens();
            transform.localScale = _originalScale;
            transform.localRotation = Quaternion.identity;
            transform.localPosition = _originalPosition;
            isAnimating = false;
            currentState = BlobState.Idle;
            StartIdleAnimation();
        }

       
        #endregion




    }
}