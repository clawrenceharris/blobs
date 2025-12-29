
using System.Collections;

public class ScaleAnimationCommand : AnimationCommand
{
     public ScaleAnimationCommand(Blob blob, MergeContext context) : base(blob, context)
    {
    }

    public override IEnumerator Execute()
    {
        yield return Presenter.ScaleBlob();
    }

}