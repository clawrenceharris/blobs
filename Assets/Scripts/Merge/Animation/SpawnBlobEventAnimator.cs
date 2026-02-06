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
        public Sequence BuildSequence(IMergeEvent e, IBoardPresenter board)
        {
            if (e is not SpawnBlobEvent evt) return null;
            var id = evt.BlobToSpawn?.ID;
            if (string.IsNullOrEmpty(id)) return null;
            var presenter = board?.GetBlob(id);
            if (presenter == null) return null;
            return presenter.Spawn();
           
        }

        public Sequence BuildUndoSequence(IMergeEvent e, IBoardPresenter board)
        {
            if (e is not SpawnBlobEvent evt) return null;
            var id = evt.BlobToSpawn?.ID;
            if (string.IsNullOrEmpty(id)) return null;
            var presenter = board?.GetBlob(id);
            if (presenter == null) return null;
            return presenter.Remove();
          
        }
    }
}
