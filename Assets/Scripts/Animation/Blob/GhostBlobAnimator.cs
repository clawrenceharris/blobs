
using DG.Tweening;
using UnityEngine;

public class GhostBlobAnimator : BlobAnimator
{



    /// <summary>
    /// Returns a tween that scales the blob to zero and cleans up on complete. Caller can Join/Append into a sequence.
    /// </summary>
    public override Tween CreateRemoveTween(float duration)
    {
        Sequence sequence = DOTween.Sequence();
        Tween fadeTween = blobView.Visuals.SpriteRenderer.DOFade(1, 0.3f);
        sequence.Append(base.CreateRemoveTween(duration)).Join(fadeTween);
        return sequence;

    }
    
    
    public override Tween CreateMoveTween(Vector3 position, float duration)
    {
        Sequence sequence = DOTween.Sequence();
        Tween fadeTween = blobView.Visuals.SpriteRenderer.DOFade(0, 0.3f);
        sequence.Append(base.CreateMoveTween(position, duration)).Join(fadeTween);
        return sequence;


        
    }
}