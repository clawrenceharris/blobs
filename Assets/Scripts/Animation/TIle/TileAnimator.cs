using System;
using DG.Tweening;
using UnityEngine;

namespace Blobs.Animation
{
    [RequireComponent(typeof(TileView))]
    public class TileAnimator : BaseAnimator
    {
        protected TileView _view;

        public override void Initialize()
        {
            base.Initialize();
            _view = GetComponent<TileView>();
            
        }
        public void PlayTraversalEffect()
        {
            return;
        }
    }
}