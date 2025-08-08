using System.Collections;
using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(BombBlobVisuals))]
public class BombBlobView : BlobView
{
  public override IEnumerator Remove(float duration)
  {
    BombBlobVisuals visuals = (BombBlobVisuals)Visuals;
    Visuals.SpriteRenderer.sortingOrder += 10;


    yield return transform
  .DOScale(1.5f, 0.7f)
  .SetLoops(3, LoopType.Yoyo)
  .SetEase(Ease.InOutQuad).WaitForCompletion();
    StartCoroutine(base.Remove(duration));

    GameObject ring = Instantiate(visuals.ExplosionRing, (Vector2)Model.GridPosition * BoardPresenter.TileSize, Quaternion.identity);
    ring.transform.DOScale(3.5f * BoardPresenter.TileSize * Vector2.one, 0.8f).SetEase(Ease.InBack);
    SpriteRenderer sr = ring.GetComponent<SpriteRenderer>();
    sr.DOFade(0, 0.8f).OnComplete(()=>
    {
      Destroy(ring);
    });


    
  


  }
}