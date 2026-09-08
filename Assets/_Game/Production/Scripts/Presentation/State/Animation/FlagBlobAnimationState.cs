using System.Collections;
namespace Blobs.Presentation
{
    public class FlagBlobMergeState : AnimationState
    {
        public override BlobAnimationState State => BlobAnimationState.Merging;
        public override void Enter(AnimationStateContext context)
        {
        }
        public override void Exit(AnimationStateContext context)
        {
            VisualTransform(context).localScale = context.BaseScale;
        }
        public override IEnumerator Update(AnimationStateContext context)
        {
            yield return null;
        }
    }
}
