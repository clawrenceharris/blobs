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


        
        private Sequence _idleSequence;
        private Tween _moveSequence;
        private Sequence _selectionSequence;

        public override List<Sequence> Sequences => new() { _idleSequence, _selectionSequence };
        private BlobAnimationRecipe Recipe => GetRecipe<BlobAnimationRecipe>(_blobView.Model.Type);
        private BlobState _currentState;
        public BlobState CurrentState => _currentState;

        protected BlobView _blobView;

        public override void Initialize()
        {
            base.Initialize();
            _blobView = GetComponent<BlobView>();
            _renderer = _blobView.Visuals.SpriteRenderer;
            StartIdleAnimation();

        }
        #region State Animations

        /// <summary>
        /// Start idle breathing/floating animation
        /// </summary>
        public void StartIdleAnimation()
        {
            if (_currentState == BlobState.Idle) return;

            KillAllTweens();
            _currentState = BlobState.Idle;

            // Reset to original state
            transform.localScale = _originalScale;
            transform.localRotation = Quaternion.identity;

            // Create idle sequence - gentle breathing + floating
            _idleSequence = DOTween.Sequence();

            // Breathing: scale pulse
            _idleSequence.Append(
                transform.DOScale(_originalScale * (1f + Recipe.idleScaleAmount), Recipe.idleScaleDuration / 2f)
                    .SetEase(Ease.InOutSine)
            );
            _idleSequence.Append(
                transform.DOScale(_originalScale * (1f - Recipe.idleScaleAmount * 0.5f), Recipe.idleScaleDuration / 2f)
                    .SetEase(Ease.InOutSine)
            );
            _idleSequence.SetLoops(-1, LoopType.Restart);
        }

        #endregion

        #region Selection


        public virtual Sequence StartSelectionLoop()
        {
            float selectionSquishDuration = Recipe.selectionSquishDuration;
            float selectionSquishAmount = Recipe.selectionSquishAmount;
            float selectionStretchAmount = Recipe.selectionStretchAmount;


            // Kill any existing loop
            StopSelectionLoop();

            // Reset to the true original scale before starting selection loop
            // (don't capture mid-animation scale)
            transform.localScale = _originalScale;

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
            transform.DOScale(_originalScale, Recipe.selectionSquishDuration * 0.5f).SetEase(Ease.OutQuad);
        }
        _selectionSequence = null;
    }

        public void PlaySelectAnimation()
        {
            if (_currentState == BlobState.Selected) return;
            KillAllTweens();
            _currentState = BlobState.Selected;

            // Smoothly transition to selection scale before starting the loop
            transform.DOScale(_originalScale, 0.1f).SetEase(Ease.OutQuad).OnComplete(() =>
            {
                if (_currentState == BlobState.Selected)
                    StartSelectionLoop();
            });
        }

        public void PlayDeselectAnimation()
        {
            _currentState = BlobState.Idle;
            StopSelectionLoop();
            // Don't jump straight to idle — smoothly blend back first, then start idle
            transform.DOScale(_originalScale, 0.12f).SetEase(Ease.OutQuad).OnComplete(() =>
            {
                if (_currentState == BlobState.Idle)
                    StartIdleAnimation();
            });
        }

        #endregion

        #region Movement

        /// <summary>
        /// Smoothly move to target position
        /// </summary>
        public Sequence AnimateMoveTo(Vector3 targetPosition)
        {
            KillAllTweens();

            _currentState = BlobState.Moving;
            _isAnimating = true;

            // Reset scale to original before moving (in case we were mid-selection-squish)
            transform.localScale = _originalScale;

            Sequence moveSeq = DOTween.Sequence();
            moveSeq.Join(transform.DOScale(Recipe.mergeAnticipationStretchAmount, Recipe.mergeAnticipationDuration).SetEase(Ease.OutQuad));
            moveSeq.Join(transform.DOMove(targetPosition, Recipe.moveDuration).SetEase(Recipe.moveEase));
            moveSeq.OnComplete(() =>
            {
                _isAnimating = false;
                transform.localScale = _originalScale;
                _originalPosition = transform.localPosition;
                StartIdleAnimation();
            });
            _moveSequence = moveSeq;
            return moveSeq;

            
        }

        #endregion


        public Sequence PlayResizeAnimation(float targetScale)
        {

            transform.localScale = Vector3.zero;
            transform.localRotation = Quaternion.identity;

            Sequence spawnSeq = DOTween.Sequence();

            spawnSeq.Append(
                transform.DOScale(targetScale, Recipe.spawnDuration * 0.4f)
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
        public Sequence PlayMergeAnimation(IBlobPresenter blobToRemove, IBlobPresenter blobToMove, Vector3 targetPosition)
        {
            KillAllTweens();
            _currentState = BlobState.Merging;
            _isAnimating = true;

            if (blobToRemove == null || blobToMove == null
                || blobToRemove.View == null || blobToMove.View == null)
            {
                _isAnimating = false;
                _currentState = BlobState.Idle;
                return DOTween.Sequence();
            }

            Transform moverTransform = blobToMove.View.transform;
            Transform targetTransform = blobToRemove.View.transform;

            if (moverTransform == null || targetTransform == null)
            {
                _isAnimating = false;
                _currentState = BlobState.Idle;
                return DOTween.Sequence();
            }

            // Reset mover scale to original before starting merge
            moverTransform.localScale = _originalScale;
            Vector3 moverStartPosition = moverTransform.position;
            Vector3 targetStartPosition = targetTransform.position;
            Vector3 moverStartScale = moverTransform.localScale;
            Vector3 targetStartScale = targetTransform.localScale;
            Vector3 direction = (targetPosition - moverStartPosition).normalized;
            bool isHorizontal = Mathf.Abs(direction.x) >= Mathf.Abs(direction.y);
            Vector3 moverAnticipationScale = isHorizontal
                ? new Vector3(moverStartScale.x * Recipe.mergeAnticipationStretchAmount, moverStartScale.y * Recipe.mergeAnticipationAmount, moverStartScale.z)
                : new Vector3(moverStartScale.x * Recipe.mergeAnticipationAmount, moverStartScale.y * Recipe.mergeAnticipationStretchAmount, moverStartScale.z);
            Vector3 targetAnticipationScale = isHorizontal
                ? new Vector3(targetStartScale.x * Recipe.mergeAnticipationAmount, targetStartScale.y * Recipe.mergeAnticipationStretchAmount, targetStartScale.z)
                : new Vector3(targetStartScale.x * Recipe.mergeAnticipationStretchAmount, targetStartScale.y * Recipe.mergeAnticipationAmount, targetStartScale.z);
            Vector3 moverStretchScale = isHorizontal
                ? new Vector3(moverStartScale.x * Recipe.mergeStretchAmount, moverStartScale.y * Recipe.mergeSquashAmount, moverStartScale.z)
                : new Vector3(moverStartScale.x * Recipe.mergeSquashAmount, moverStartScale.y * Recipe.mergeStretchAmount, moverStartScale.z);
            Vector3 targetImpactScale = isHorizontal
                ? new Vector3(targetStartScale.x * Recipe.mergeSquashAmount, targetStartScale.y * Recipe.mergeStretchAmount, targetStartScale.z)
                : new Vector3(targetStartScale.x * Recipe.mergeStretchAmount, targetStartScale.y * Recipe.mergeSquashAmount, targetStartScale.z);
            Vector3 nudgeOffset = direction * Recipe.mergeImpactNudge;

            moverTransform.DOKill(true);
            targetTransform.DOKill(true);

            Sequence mergeSeq = DOTween.Sequence();

            if (Recipe.mergeAnticipationDuration > 0f)
            {
                mergeSeq.Append(moverTransform.DOScale(moverAnticipationScale, Recipe.mergeAnticipationDuration).SetEase(Ease.OutQuad));
                mergeSeq.Join(targetTransform.DOScale(targetAnticipationScale, Recipe.mergeAnticipationDuration).SetEase(Ease.OutQuad));
            }

            // Slide the moving blob into place with a viscous stretch.
            mergeSeq.Append(moverTransform.DOMove(targetPosition, Recipe.moveDuration).SetEase(Recipe.moveEase));
            mergeSeq.Join(moverTransform.DOScale(moverStretchScale, Recipe.moveDuration * 0.7f).SetEase(Ease.OutQuad));

            // Impact: squish + subtle nudge on the target to sell absorption.
            mergeSeq.Append(targetTransform.DOScale(targetImpactScale, Recipe.mergeImpactInDuration).SetEase(Ease.OutQuad));
            mergeSeq.Join(targetTransform.DOMove(targetStartPosition + nudgeOffset, Recipe.mergeImpactInDuration).SetEase(Ease.OutQuad));
            mergeSeq.Append(targetTransform.DOScale(targetStartScale * Recipe.mergeOvershootAmount, Recipe.mergeImpactOutDuration).SetEase(Ease.OutBack));
            mergeSeq.Join(targetTransform.DOMove(targetStartPosition, Recipe.mergeImpactOutDuration).SetEase(Ease.OutQuad));

            // Absorb: target shrinks away while the mover settles back to normal scale.
            mergeSeq.Append(targetTransform.DOScale(Vector3.zero, Recipe.mergeDuration).SetEase(Ease.InQuad));
            mergeSeq.Join(moverTransform.DOScale(moverStartScale * Recipe.mergeSettleAmount, Recipe.mergeSettleDuration).SetEase(Recipe.mergeSettleEase));
            mergeSeq.Append(moverTransform.DOScale(moverStartScale, Recipe.mergeSettleDuration).SetEase(Recipe.mergeSettleEase));

            mergeSeq.OnComplete(() =>
            {
                _isAnimating = false;
                moverTransform.position = targetPosition;
                _originalPosition = moverTransform.localPosition;
                StartIdleAnimation();
            });
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
            if (_currentState == BlobState.Moving || _currentState == BlobState.Merging) return;

            transform.DOShakePosition(0.3f, 0.1f, 20, 90, false, true)
                .OnComplete(() =>
                {
                    // Return to current state animation
                    if (_currentState == BlobState.Selected)
                        PlaySelectAnimation();
                    else
                        StartIdleAnimation();
                });
        }

        #endregion

       


    }
}