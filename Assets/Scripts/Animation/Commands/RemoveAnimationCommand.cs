using System.Collections;

public class RemoveAnimationCommand : AnimationCommand
{
    public RemoveAnimationCommand(Blob blob, MergeContext context) : base(blob, context)
    {
    }

    public override IEnumerator Execute()
    {
        yield return Presenter.RemoveBlob();
    }

}