using DG.Tweening;
using UnityEngine;

public class TileAnimator : ITileAnimator
{

    protected readonly TileView blobView;
    protected readonly Transform _transform;
    protected readonly SpriteRenderer _renderer;

    public Animator Animator { get; private set; }
    
    public TileAnimator(TileView view)
    {
        _transform = view.transform;
        _renderer = view.Visuals.SpriteRenderer;
        Animator = view.GetComponent<Animator>();
    }

    

    public virtual Tween CreateMoveTween(Vector3 worldTarget, float duration) =>
        _transform.DOMove(worldTarget, duration).SetEase(Ease.OutQuad);

    public virtual Tween CreateScaleTween(float targetScale, float duration) =>
        _transform.DOScale(targetScale, duration).SetEase(Ease.OutBack);

    public virtual Tween CreateRemoveTween(float duration) =>
        _transform.DOScale(0f, duration).SetEase(Ease.InBack).OnComplete(() => Object.Destroy(_transform.gameObject));

    public virtual Tween CreateSpawnTween(float duration)
    {
        _transform.localScale = Vector3.zero;
        return _transform.DOScale(1f, duration).SetEase(Ease.OutBack);
    }

    
}