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
        public Sequence BuildSequence(IMergeEvent e, IBoardPresenter board, object recipe)
        {
            if (e is not PlayVfxEvent evt || board == null) return null;
            var worldPos = board.Layout.GridToWorldWithBlobOffset(evt.GridPos);
            return DOTween.Sequence().AppendCallback(() => { /* Spawn VFX by evt.VfxKey at worldPos when VFX system exists */ });
        }

        public Sequence BuildUndoSequence(IMergeEvent e, IBoardPresenter board, object recipe)
        {
            return null;
        }
    }
}
