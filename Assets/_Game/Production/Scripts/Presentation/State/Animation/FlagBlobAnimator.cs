using UnityEngine;

namespace Blobs.Presentation
{

    public class FlagBlobAnimator : BlobMotionAnimator
    {
        public override void SetMerging()
        {
            SetState(new FlagBlobMergeState());
        }

    }
}