using System;
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
        public void BuildSequence(IMergeEvent e, IBoardPresenter board,  Action onComplete)
        {
            if (e is not SpawnBlobEvent evt) return;
            var id = evt.BlobToSpawn?.ID;
            if (string.IsNullOrEmpty(id)) return;
            var presenter = board?.GetBlob(id);
            if (presenter == null) return;
            // var rec = recipe as BlobAnimationRecipe;
            presenter.Spawn(onComplete);
            // if (rec != null && rec.spawnDuration > 0)
            //     spawnTween.SetEase(rec.spawnEase);
            // return DOTween.Sequence().Append(spawnTween);
        }

        public void BuildUndoSequence(IMergeEvent e, IBoardPresenter board, Action onComplete)
        {
            if (e is not SpawnBlobEvent evt) return;
            var id = evt.BlobToSpawn?.ID;
            if (string.IsNullOrEmpty(id)) return;
            var presenter = board?.GetBlob(id);
            if (presenter == null) return;
            // var rec = recipe as BlobAnimationRecipe;
            presenter.Remove(onComplete);
            // if (rec != null && rec.removeDuration > 0)
            //     removeTween.SetEase(rec.removeEase);
            // return DOTween.Sequence().Append(removeTween);
        }
    }
}
