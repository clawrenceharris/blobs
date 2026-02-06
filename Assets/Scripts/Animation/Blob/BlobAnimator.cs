using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

namespace Blobs.Animation
{
    [RequireComponent(typeof(BlobView))]
    /// <summary>
    /// Handles all blob animations using DOTween.
    /// Provides smooth state-based animations: idle, selected, moving, merging.
    /// </summary>
    public class BlobAnimator : BaseAnimator
    {
        public enum BlobState
        {
            Idle,
            Selected,
            Moving,
            Merging
        }

        private BlobAnimationRecipe _recipe;

        
        private Sequence _idleSequence;
        private Tween _currentMoveTween;
        private Sequence _selectionSequence;

        public override List<Sequence> Sequences => new() { _idleSequence, _selectionSequence };
       
        private BlobState currentState = BlobState.Idle;
        public BlobState CurrentState => currentState;

        protected BlobView _blobView;

        protected void SetRecipe(BlobType blobType)
        {
            _recipe = AnimationRecipeProvider.Instance.GetRecipe<BlobAnimationRecipe>(blobType);
        }
        public override void Initialize()
        {
            base.Initialize();
            currentState = BlobState.Idle;
            _blobView = GetComponent<BlobView>();
            _renderer = _blobView.Visuals.SpriteRenderer;

             SetRecipe(_blobView.Model.Type);

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
            _idleSequence = DOTween.Sequence();

            // Breathing: scale pulse
            _idleSequence.Append(
                transform.DOScale(_originalScale * (1f + _recipe.idleScaleAmount), _recipe.idleScaleDuration / 2f)
                    .SetEase(Ease.InOutSine)
            );
            _idleSequence.Append(
                transform.DOScale(_originalScale * (1f - _recipe.idleScaleAmount * 0.5f), _recipe.idleScaleDuration / 2f)
                    .SetEase(Ease.InOutSine)
            );

            // Also add subtle float movement
            transform.DOLocalMoveY(_originalPosition.y + _recipe.idleFloatAmount, _recipe.idleFloatDuration / 2f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);

            _idleSequence.SetLoops(-1, LoopType.Restart);
        }

        #endregion

        #region Selection


        public virtual Sequence StartSelectionLoop()
        {
            float selectionSquishDuration = _recipe.selectionSquishDuration;
            float selectionSquishAmount = _recipe.selectionSquishAmount;
            float selectionStretchAmount = _recipe.selectionStretchAmount;


            // Kill any existing loop
            StopSelectionLoop();

            // Store current scale as base (in case it was modified)
            _originalScale = transform.localScale;

            // Create squish in/out loop: squash down (Y smaller, X larger) then return to base
            _selectionSequence = DOTween.Sequence();

            Vector3 squishScale = new Vector3(
                _originalScale.x * selectionStretchAmount,
                _originalScale.y * selectionSquishAmount,
                _originalScale.z
            );

            // Squish in
            _selectionSequence.Append(transform.DOScale(squishScale, selectionSquishDuration)
                .SetEase(Ease.OutQuad));

            // Squish out (back to base)
            _selectionSequence.Append(transform.DOScale(_originalScale, selectionSquishDuration)
                .SetEase(Ease.InQuad));

            // Loop indefinitely
            _selectionSequence.SetLoops(-1, LoopType.Restart);
            return _selectionSequence;
        }
    

    public virtual void StopSelectionLoop()
    {
        if (_selectionSequence != null && _selectionSequence.IsActive())
        {
            // Kill the loop and smoothly blend back to base scale
            _selectionSequence.Kill();
            transform.DOScale(_originalScale, _recipe.selectionSquishDuration * 0.5f).SetEase(Ease.OutQuad);
        }
        _selectionSequence = null;
    }

        public void PlaySelectAnimation()
        {
            if (currentState == BlobState.Selected) return;
            KillAllTweens();
            StartSelectionLoop();
        }

        public void PlayDeselectAnimation()
        {
            StopSelectionLoop();
            StartIdleAnimation();
        }

        #endregion

        #region Movement
        
        /// <summary>
        /// Smoothly move to target position
        /// </summary>
        public Sequence AnimateMoveTo(Vector3 targetPosition)
        {
            KillAllTweens();
            currentState = BlobState.Moving;
            _isAnimating = true;

            _currentMoveTween = transform.DOMove(targetPosition, _recipe.moveDuration).SetEase(Ease.OutQuad);
            return DOTween.Sequence().Append(_currentMoveTween);
        }

        #endregion


        public Sequence PlayResizeAnimation(float targetScale)
        {

            transform.localScale = Vector3.zero;
            transform.localRotation = Quaternion.identity;

            Sequence spawnSeq = DOTween.Sequence();

            spawnSeq.Append(
                transform.DOScale(targetScale, _recipe.spawnDuration * 0.4f)
                    .SetEase(Ease.OutBounce)
            );
            return spawnSeq;
        }

        public override Sequence PlaySpawnAnimation()
        {
            return base.PlaySpawnAnimation().OnComplete(() =>
            {
                StartIdleAnimation();
            });
           
        }

        #region Merge Animation

        /// <summary>
        /// Play merge animation - squish towards target then spawn particles
        /// </summary>
        public Sequence PlayMergeAnimation(Vector3 targetPosition)
        {
            KillAllTweens();
            currentState = BlobState.Merging;
            _isAnimating = true;

            Vector3 startPosition = transform.position;

            Sequence mergeSeq = DOTween.Sequence();

            // Squish towards target (stretch in direction of movement)
            Vector3 direction = (targetPosition - startPosition).normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            // Stretch effect
            mergeSeq.Append(
                transform.DOScale(new Vector3(_originalScale.x * 1.3f, _originalScale.y * 0.7f, _originalScale.z), _recipe.mergeDuration * 0.3f)
                    .SetEase(Ease.OutQuad)
            );

            // Move to target while shrinking
            mergeSeq.Append(
                transform.DOMove(targetPosition, _recipe.mergeDuration * 0.7f)
                    .SetEase(Ease.InQuad)
            );
            mergeSeq.Join(
                transform.DOScale(Vector3.zero, _recipe.mergeDuration * 0.7f)
                    .SetEase(Ease.InQuad)
            );

            return mergeSeq;
        }

        public void SpawnMergeParticles(Color color)
        {
            if (_blobView.Visuals.Particles == null) return;

            ParticleSystem particles = Instantiate(_blobView.Visuals.Particles, transform.position, Quaternion.identity);

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
                        PlaySelectAnimation();
                    else
                        StartIdleAnimation();
                });
        }

        #endregion

       


    }
}