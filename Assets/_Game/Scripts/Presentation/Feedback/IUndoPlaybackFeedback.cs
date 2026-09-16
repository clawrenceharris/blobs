using UnityEngine;

namespace Blobs.Presentation
{
    /// <summary>
    /// Session-level undo feedback, distinct from forward merge impact and contact juice.
    /// </summary>
    public interface IUndoPlaybackFeedback
    {
        void PlayUndo();
    }
}
