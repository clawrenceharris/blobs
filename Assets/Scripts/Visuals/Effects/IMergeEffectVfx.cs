using DG.Tweening;
using UnityEngine;

namespace Blobs.Visuals
{
    /// <summary>
    /// Interface for merge effect VFX. Implemented by MergeEffect.
    /// Allows BlobAnimator to trigger effects without a direct type dependency.
    /// </summary>
    public interface IMergeEffectVfx
    {
        Sequence Play(Vector3 position, Color color);
        Sequence PlayWithBlobColor(Vector3 position, BlobColor blobColor);
    }
}
