
public readonly struct MoveFailContext
{
    public readonly MergeFailReason Reason;
    public readonly MoveIntent Intent;

    public MoveFailContext(MergeFailReason reason, MoveIntent intent)
    {
        Reason = reason;
        Intent = intent;
    }
}
