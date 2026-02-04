using Blobs.Core.Merge;
using DG.Tweening;
using UnityEngine;

namespace Blobs.Merge.Animation
{
    /// <summary>
    /// Animates SpawnBlobEvent: scale-in + optional blink. Uses recipe for duration/easing.
    /// </summary>
    public class SpawnBlobEventAnimator : IEventAnimator
    {
        public Sequence BuildSequence(IMergeEvent e, IBoardPresenter board, object recipe)
        {
            if (e is not SpawnBlobEvent evt) return null;
            var id = evt.BlobToSpawn?.ID;
            if (string.IsNullOrEmpty(id)) return null;
            var presenter = board?.GetBlobById(id);
            if (presenter == null) return null;
            var rec = recipe as BlobAnimationRecipeSO;
            var spawnTween = presenter.Spawn(rec.spawnDuration);
            if (rec != null && rec.spawnDuration > 0)
                spawnTween.SetEase(rec.spawnEase);
            return DOTween.Sequence().Append(spawnTween);
        }

        public Sequence BuildUndoSequence(IMergeEvent e, IBoardPresenter board, object recipe)
        {
            if (e is not SpawnBlobEvent evt) return null;
            var id = evt.BlobToSpawn?.ID;
            if (string.IsNullOrEmpty(id)) return null;
            var presenter = board?.GetBlobById(id);
            if (presenter == null) return null;
            var rec = recipe as BlobAnimationRecipeSO;
            var removeTween = presenter.Remove(rec.removeDuration);
            if (rec != null && rec.removeDuration > 0)
                removeTween.SetEase(rec.removeEase);
            return DOTween.Sequence().Append(removeTween);
        }
    }
}
