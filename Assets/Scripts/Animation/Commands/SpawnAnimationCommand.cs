using System.Collections;

public class SpawnAnimationCommand : AnimationCommand
{
     public SpawnAnimationCommand(Blob blob, MergeContext context) : base(blob, context)
    {
    }

    public override IEnumerator Execute()
    {
        yield return Presenter.SpawnBlob();
    }

}