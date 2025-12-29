using System.Collections;
using UnityEngine;

public class MoveAnimationCommand : AnimationCommand
{
    public MoveAnimationCommand(Blob blob, MergeContext context) : base(blob, context)
    {
    }

    public override IEnumerator Execute()
    {
        yield return Presenter.MoveBlob(_context.CurrentPos);
    }
}