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
        public void BuildSequence(IMergeEvent e, IBoardPresenter board, Action onComplete)
        {
            if (e is not PlayVfxEvent evt || board == null) return;
            var worldPos = board.Layout.GridToWorldWithBlobOffset(evt.GridPos);
            // return DOTween.Sequence().AppendCallback(() => { /* Spawn VFX by evt.VfxKey at worldPos when VFX system exists */ });
        }

        public void BuildUndoSequence(IMergeEvent e, IBoardPresenter board, Action onComplete)
        {
        }
    }
}
