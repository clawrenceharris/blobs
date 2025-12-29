using System.Collections;
using DG.Tweening;
using UnityEngine;

public class BombBlobPresenter : BlobPresenter
{
    public BombBlobPresenter(BlobView view) : base(view)
    {
    }

    public override IEnumerator RemoveBlob()
    {
       
        BombBlobVisuals visuals = (BombBlobVisuals)_view.Visuals;
        _view.Visuals.SpriteRenderer.sortingOrder += 10;


    yield return _view.transform
  .DOScale(1.5f, 0.7f)
  .SetLoops(3, LoopType.Yoyo)
  .SetEase(Ease.InOutQuad).WaitForCompletion();
    _board.StartCoroutine(base.RemoveBlob());

    GameObject ring = Object.Instantiate(visuals.ExplosionRing,  _board.GridToIso(_view.Model.GridPosition), Quaternion.identity);
    ring.transform.DOScale(3.5f * TilePresenter.TileSize * Vector2.one, 0.8f).SetEase(Ease.InBack);
    SpriteRenderer sr = ring.GetComponent<SpriteRenderer>();
    sr.DOFade(0, 0.8f).OnComplete(()=>
    {
      Object.Destroy(ring);
    });
        
        
    }
}