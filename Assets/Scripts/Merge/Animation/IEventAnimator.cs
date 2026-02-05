using System;
using System.Collections;
using Blobs.Core.Merge;
using DG.Tweening;
using UnityEngine;

namespace Blobs.Merge.Animation
{
    /// <summary>
    /// Builds a DOTween sequence or coroutine for a merge event (forward or undo).
    /// Event animators are registered by IMergeEvent type and can use blob recipes for tuning.
    /// </summary>
    public interface IEventAnimator
    {
        /// <summary>Builds the forward animation for this event. Returns a Sequence to play or null to skip.</summary>
        void BuildSequence(IMergeEvent e, IBoardPresenter board, Action onComplete = null);

        /// <summary>Builds the undo animation for this event. Returns a Sequence to play or null to skip.</summary>
        void BuildUndoSequence(IMergeEvent e, IBoardPresenter board, Action onComplete = null);
    }
}
