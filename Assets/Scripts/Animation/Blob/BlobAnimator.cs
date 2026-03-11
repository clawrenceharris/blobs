using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;
using Blobs.Visuals;
using System;

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
            Merging,
            Winning
        }

        private Sequence _moveSequence;
        private Sequence _mergeSequence;
        private Sequence _selectionSequence;
        public override List<Sequence> Sequences => new() { _selectionSequence, _mergeSequence };
        [SerializeField] private BlobAnimationRecipe _recipe;
        private BlobState _currentState;
        public BlobState CurrentState => _currentState;

        protected BlobView _blobView;

        private void Awake()
        {
            _blobView = GetComponent<BlobView>();
            if (_recipe == null)
            {
                _recipe = Resources.Load<BlobAnimationRecipe>("BlobAnimationRecipe");
                if (_recipe == null)
                {
                    Debug.LogError("BlobAnimationRecipe not found");
                }
            }


        }


        #region Selection


        public virtual Sequence StartSelectionLoop()
        {
            _currentState = BlobState.Selected;
            float selectionSquishDuration = _recipe.selectionSquishDuration;
            float selectionSquishAmount = _recipe.selectionSquishAmount;
            float selectionStretchAmount = _recipe.selectionStretchAmount;


            // Kill any existing loop
            StopSelectionLoop();

            // Store current scale as base (in case it was modified)
            var originalScale = _blobView.Scale;

            // Create squish in/out loop: squash down (Y smaller, X larger) then return to base
            _selectionSequence = DOTween.Sequence();

            Vector3 squishScale = new Vector3(
                originalScale.x * selectionStretchAmount,
                originalScale.y * selectionSquishAmount,
                originalScale.z
            );

            // Squish in
            _selectionSequence.Append(transform.DOScale(squishScale, selectionSquishDuration)
                .SetEase(Ease.OutQuad));

            // Squish out (back to base)
            _selectionSequence.Append(transform.DOScale(originalScale, selectionSquishDuration)
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
                transform.DOScale(_blobView.Scale, _recipe.selectionSquishDuration * 0.5f).SetEase(Ease.OutQuad);
            }

            _currentState = BlobState.Idle;
            _selectionSequence = null;
        }

        public void PlaySelectAnimation()
        {
            KillAllTweens();
            StartSelectionLoop();
        }

        public void PlayDeselectAnimation()
        {
            StopSelectionLoop();
        }

        #endregion

        #region Movement
        public virtual Sequence AnimateMoveTo(Vector3 targetPosition)
        {
            KillAllTweens();
            _currentState = BlobState.Moving;
            _isAnimating = true;
            Sequence moveSeq = DOTween.Sequence();
            moveSeq.Append(transform.DOMove(targetPosition, _recipe.moveDuration)
            .SetEase(_recipe.moveEase));

            moveSeq.OnComplete(() =>
            {
                _isAnimating = false;
            });
            return moveSeq;
        }

        /// <summary>
        /// Smoothly move to target position
        /// </summary>
        public Sequence AnimateMovePath(List<Vector3> path)
        {
            KillAllTweens();

            _currentState = BlobState.Moving;
            _isAnimating = true;


            Sequence moveSeq = DOTween.Sequence();
            var pathArray = path.ToArray();
            _moveSequence = moveSeq.Append(transform.DOPath(pathArray, _recipe.moveDuration).SetEase(Ease.Linear));
            _moveSequence.OnComplete(() =>
            {
                _isAnimating = false;
            });
            return moveSeq;


        }
        public virtual Sequence PlayMoveForMerge(Vector3 targetPosition)
        {
            if (_currentState == BlobState.Merging) return null;
            KillAllTweens();
            _currentState = BlobState.Merging;
            _isAnimating = true;
            _mergeSequence = DOTween.Sequence();


            _mergeSequence.Append(transform.transform.DOMove(targetPosition, _recipe.mergeDuration)
            .SetEase(_recipe.mergeEase))
            .OnComplete(() =>
            {
                _isAnimating = false;
                _currentState = BlobState.Idle;
            });


            return _mergeSequence;
        }


        #endregion


        public Sequence PlayResizeAnimation(float targetScale)
        {
            KillAllTweens();
            Sequence resizeSeq = DOTween.Sequence();
            Debug.Log("[BlobAnimator] Playing resize animation to scale: " + targetScale);
            resizeSeq.Append(
                transform.DOScale(targetScale, _recipe.resizeDuration)
                    .SetEase(_recipe.resizeEase)
            );
            return resizeSeq.OnComplete(() =>
            {
                _isAnimating = false;
                transform.localScale = Vector3.one * targetScale;
            });
        }



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
                });
        }
       
        
        public void SpawnMergeParticles(Color color)
        {

            ParticleSystem particles = Instantiate(_blobView.Visuals.Particles, _blobView.transform.position, Quaternion.identity);
            var main = particles.main;
            main.startColor = color;
            particles.Play();
            Destroy(particles.gameObject, main.duration + main.startLifetime.constantMax);
        }


        public Sequence PlayMergeEffect(IBlobPresenter blobToRemove, IBlobPresenter blobToMove)
        {
            if (_blobView.Visuals.MergeEffect != null)
            {
                var seq = DOTween.Sequence();
                var vfx1 = Instantiate(_blobView.Visuals.MergeEffect, transform.position, Quaternion.identity).GetComponent<IMergeEffectVfx>();
                var vfx2 = Instantiate(_blobView.Visuals.MergeEffect, transform.position, Quaternion.identity).GetComponent<IMergeEffectVfx>();
                seq.Append(vfx1.PlayWithBlobColor(blobToRemove.View.transform.position, blobToRemove.Model.Color));
                seq.Join(vfx2.PlayWithBlobColor(blobToMove.View.transform.position, blobToMove.Model.Color)).AppendInterval(0.2f);
                return seq;
            }
            return null;
        }

        #endregion

        #region Spawn Animation
        public virtual Sequence PlaySpawnAnimation(Vector3 scale)
        {
            KillAllTweens();

            transform.localScale = Vector3.zero;
            transform.localRotation = Quaternion.identity;

            Sequence spawnSeq = DOTween.Sequence();

            // Pop in with overshoot

            spawnSeq.Append(
                transform.DOScale(scale, _recipe.spawnDuration)
                    .SetEase(_recipe.spawnEase)
            );
            spawnSeq.OnComplete(() =>
            {
                _isAnimating = false;
            });
            return spawnSeq;


        }

        public virtual Sequence PlayDespawnAnimation()
        {
            KillAllTweens();
            _isAnimating = true;

            Sequence despawnSeq = DOTween.Sequence();

            // Shrink + spin + fade
            despawnSeq.Append(
                transform.DOScale(Vector3.zero, _recipe.despawnDuration)
                    .SetEase(_recipe.despawnEase)
            );
            
            despawnSeq.OnComplete(() =>
            {
                _isAnimating = false;
            });
            return despawnSeq;
        }

        public Sequence PlayNudgeInDirectionAnimation(Vector3 direction)
        {
            var seq = DOTween.Sequence();
            var originalPosition = transform.position;
            float nudgeAmount = 0.2f; // Small nudge factor
            Vector3 nudgeVector = direction.normalized * nudgeAmount;
            seq.Append(transform.DOMove(originalPosition + nudgeVector, 0.08f)
                   .SetEase(Ease.InOutQuad));
            seq.Append(transform.DOMove(originalPosition, 0.16f)
                   .SetEase(Ease.InOutQuad));
            return seq;
        }
        #endregion

    }
}