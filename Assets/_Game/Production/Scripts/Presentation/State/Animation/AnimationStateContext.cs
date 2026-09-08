using Blobs.Content;
using UnityEngine;
namespace Blobs.Presentation
{
    public sealed class AnimationStateContext
    {
        public BlobView BlobView { get; set; }
        public BlobAnimationSettingsAsset BlobAnimationSettings { get; set; }
        public BlobMotionAnimator BlobAnimator { get; set; }
        public Vector3 BaseScale { get; set; }

    }


}