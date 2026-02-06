using System;
using Blobs.Core.Merge;
using DG.Tweening;
using UnityEngine;

namespace Blobs.Merge.Animation
{
    /// <summary>
    /// Animates PlayVfxEvent: spawn VFX at grid position (particles, etc.).
    /// No board API for VFX key lookup; override or extend to spawn by VfxKey. Undo is a no-op.
    /// </summary>
    public class PlayVfxEventAnimator : IEventAnimator
    {
        public Sequence BuildSequence(IMergeEvent e, IBoardPresenter board)
        {
            if (e is not PlayVfxEvent evt || board == null) return null;
            var worldPos = board.Layout.GridToWorldWithBlobOffset(evt.GridPos);
            var blob = board.GetBlob(evt.BlobTriggerId);
            var tile = board.GetTile(evt.TileTriggerId);

            Sequence seq = DOTween.Sequence();
            if (blob != null)
                seq.AppendCallback(() => { blob.PlayMergeEffect(); });
            if (tile != null)
                seq.AppendCallback(() => { tile.PlayTraversalEffect(); });

            return seq;
        }

        public Sequence BuildUndoSequence(IMergeEvent e, IBoardPresenter board)
        {
            return BuildSequence(e, board);
        }
    }
}
