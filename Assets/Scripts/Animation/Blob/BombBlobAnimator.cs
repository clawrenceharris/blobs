using DG.Tweening;
using UnityEngine;

public class BombBlobAnimator : BlobAnimator
{
   

    public override Tween CreateRemoveTween(float duration)
    {
        Sequence seq = DOTween.Sequence();
        Sequence bombSeq = DOTween.Sequence();
        BombBlobVisuals visuals = (BombBlobVisuals)blobView.Visuals;
        blobView.Visuals.SpriteRenderer.sortingOrder += 10;

        bombSeq.Append(blobView.transform
            .DOScale(1.5f, 0.7f)
            .SetLoops(3, LoopType.Yoyo)
            .SetEase(Ease.InOutQuad));
        bombSeq.Append(blobView.transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack));

        // GameObject ring = Object.Instantiate(visuals.ExplosionRing, _board.GridToIso(_view.Model.GridPosition), Quaternion.identity);
        // ring.transform.DOScale(3.5f * TilePresenter.TileSize * Vector2.one, 0.8f).SetEase(Ease.InBack);
        // SpriteRenderer sr = ring.GetComponent<SpriteRenderer>();
        // sr.DOFade(0, 0.8f).OnComplete(() => Object.Destroy(ring));

        return seq.Append(base.CreateRemoveTween(duration)).Append(bombSeq);
    }
}