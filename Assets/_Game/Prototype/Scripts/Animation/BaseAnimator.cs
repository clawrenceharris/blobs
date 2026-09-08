using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Blobs.Animation
{
    public abstract class BaseAnimator : MonoBehaviour
    {

        public virtual List<Sequence> Sequences { get; } = new();
        protected bool _isAnimating;

       
        public bool IsAnimating => _isAnimating;
        protected Vector3 _originalPosition;
        
        public void Initialize()
        {
            _originalPosition = transform.position;
        }
        protected void KillAllTweens()
        {
            foreach (var sequence in Sequences)
            {
                sequence.Kill();
            }
            
            transform.DOKill();
        }

        

    }
}