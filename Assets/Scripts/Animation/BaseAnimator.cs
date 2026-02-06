using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Blobs.Animation
{
    public abstract class BaseAnimator : MonoBehaviour
    {
        protected SpriteRenderer _renderer;
        protected Vector3 _originalScale;
        protected Vector3 _originalPosition;
        private AnimationRecipe _recipe;

        protected T GetRecipe<T>(BlobType type) where T : AnimationRecipe
        {
            return AnimationRecipeProvider.Instance.GetBlobRecipe<T>(type);
        }
        public virtual List<Sequence> Sequences { get; } = new();
         protected bool _isAnimating;

       
        public bool IsAnimating => _isAnimating;

        public virtual void Initialize()
        {
            _originalScale = transform.localScale;
            _originalPosition = transform.localPosition;
            _recipe = AnimationRecipeProvider.Instance.DefaultRecipe;
        }
        
        protected void KillAllTweens()
        {
            foreach (var sequence in Sequences)
            {
                sequence.Kill();
            }
            
            transform.DOKill();
        }

        public virtual Sequence PlaySpawnAnimation()
        {
            KillAllTweens();

            transform.localScale = Vector3.zero;
            transform.localRotation = Quaternion.identity;

            Sequence spawnSeq = DOTween.Sequence();
        
            // Pop in with overshoot
            spawnSeq.Append(
                transform.DOScale(_originalScale * 1.15f, _recipe.spawnDuration * 0.6f)
                    .SetEase(Ease.OutBack)
            );
            spawnSeq.Append(
                transform.DOScale(_originalScale, _recipe.spawnDuration * 0.4f)
                    .SetEase(Ease.OutBounce)
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
                    .SetEase(Ease.InBack)
            );
            
            despawnSeq.OnComplete(() =>
            {
                _isAnimating = false;
            });
            return despawnSeq;
        }


    }
}