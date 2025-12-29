using System.Collections;
using DG.Tweening;
using UnityEngine;

public class GhostBlobPresenter : BlobPresenter
{
    public GhostBlobPresenter(BlobView view) : base(view)
    {
    }

    public override IEnumerator MoveBlob(Vector2Int to, Ease ease = Ease.Linear)
    {
        _view.Visuals.SpriteRenderer.DOFade(0, 0.3f);

        yield return base.MoveBlob(to, ease);

    }   
    
    public override IEnumerator Merge()
    {
        _view.Visuals.SpriteRenderer.DOFade(1, 0.3f);
        yield return base.Merge();
    }

    public override IEnumerator RemoveBlob()
    {
        _view.Visuals.SpriteRenderer.DOFade(1, 0.3f);
        yield return base.RemoveBlob();
    }
}