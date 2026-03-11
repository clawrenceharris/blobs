using System;
using Blobs.Utilities;
using DG.Tweening;
using UnityEngine;

namespace Blobs.Animation
{
    [RequireComponent(typeof(TileView))]
    public class TileAnimator : BaseAnimator
    {
        [SerializeField] private float _enterDuration = 0.3f;
        [SerializeField] private float _exitDuration = 0.3f;
        [SerializeField] private Ease _enterEase = Ease.Linear;
        [SerializeField] private Ease _exitEase = Ease.Linear;
        private SpriteRenderer _trailSplatter;
        protected TileView _view;

        private void Awake()
        {
            _view = GetComponent<TileView>();

        }
       
        public Sequence PlayEnterAnimation(Vector3 targetPosition)
        {
            var seq = DOTween.Sequence();
            return seq.Append(transform.DOMoveY(targetPosition.y, _enterDuration).SetEase(_enterEase));
        }

        public Sequence PlayExitAnimation()
        {
            var seq = DOTween.Sequence();
            return seq.Append(transform.DOMoveY(Camera.main.orthographicSize * 2f, _exitDuration).SetEase(_exitEase));
        }

        public Sequence Exit()
        {
            var seq = DOTween.Sequence();
            return seq.Append(transform.DOMoveY(Camera.main.orthographicSize * 2f, _exitDuration).SetEase(_exitEase));
        }

        public Sequence PlayDespawnAnimation()
        {
            var seq = DOTween.Sequence();
            return seq.Append(transform.DOMoveY(Camera.main.orthographicSize * 2f, _exitDuration).SetEase(_exitEase));
        }

        public Sequence PlayLeaveTrailAnimation(Color color, Vector3 position)
        {
            var seq = DOTween.Sequence();
            Debug.Log("Playing leave trail animation");
            _trailSplatter = Instantiate(PrefabLibrary.Instance.TrailSplatter, position, Quaternion.identity);
            _trailSplatter.color = Color.clear;
            return seq.Append(_trailSplatter.DOColor(color, 0.2f));
        }

       
        public Sequence PlayRemoveTrailAnimation()
        {
            var seq = DOTween.Sequence();
            Debug.Log("Playing remove trail animation");
            return seq.Append(_trailSplatter.DOColor(Color.clear, 0.4f))
            .OnComplete(() => Destroy(_trailSplatter.gameObject));
        

        }
    }
}