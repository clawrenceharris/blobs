
/// <summary>
/// Interface for validating if two blobs can merge. 
/// Only validates basic merge conditions based on shared properties like size, color, and grid positions
/// </summary>
public interface IMergeRule
{
    bool Validate(Blob source, Blob target, out MergeFailReason failReason);
}
