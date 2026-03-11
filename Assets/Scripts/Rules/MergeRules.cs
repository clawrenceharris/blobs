
/// <summary>
/// Basic merge rule. checks that two blobs can merge based on color, size and position
/// </summary>
public sealed class BasicMergeRule : IMergeRule
{
    public bool Validate(Blob source, Blob target, out MergeFailReason failReason)
    {
        failReason = MergeFailReason.None;
        if (source is not NormalBlob || target is not NormalBlob)
        {
            return true;
        }
        if (source.Color == target.Color)
        {
            failReason = MergeFailReason.ColorRuleRejected;
            return false;
        }
        if(source.Size != target.Size)
        {
            failReason = MergeFailReason.SizeRuleRejected;
            return false;
        }
        var delta = target.GridPosition - source.GridPosition;
        if (delta.x != 0 && delta.y != 0)
        {
            failReason = MergeFailReason.NotAligned;
            return false;
        }
      
        return true;
    }
}

/// <summary>
/// Target color merge rule. checks that the source blob and target blob are same color
/// </summary>
public sealed class TargetColorMergeRule : IMergeRule
{
    public bool Validate(Blob source, Blob target, out MergeFailReason failReason)
    {
        failReason = MergeFailReason.None;
        if (source.Color != target.Color)
        {
            failReason = MergeFailReason.TargetColorRuleRejected;
            return false;
        }
        return true;
    }
}