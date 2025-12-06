using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// A strategy that defines a contract for any special logic
/// a blob might perform during a merge.
/// </summary>
public interface IMergeBehavior
{
    /// <summary>
    /// Called by the source blob in a merge to modify the outcome of the merge plan. Used for when a source Blob needs to react to the target Blob's actions.
    /// </summary>
    /// <param name="plan">The current merge plan to be modified.</param>
    /// <param name="boardLogic">A reference to the board for querying state.</param>
    void ModifyMergeFromSource(MergePlan plan, BoardModel boardLogic);

    /// <summary>
    /// Called by the target blob in a merge to modify the outcome of the merge plan. Used for when a target Blob needs to react to the source blob's actions.
    /// </summary>
    /// <param name="plan">The current merge plan to be modified.</param>
    /// <param name="boardLogic">A reference to the board for querying state.</param>
    void ModifyMergeFromTarget(MergePlan plan, BoardModel boardLogic);
}





/// <summary>
/// A strategy that defines a contract for any special logic
/// a blob might perform during a merge.
/// </summary>

public class BlobMergeBehavior : IMergeBehavior
{
    protected readonly Blob _blob;
    public BlobMergeBehavior(Blob blob)
    {
        _blob = blob;
    }
    public virtual void ModifyMergeFromSource(MergePlan plan, BoardModel board)
    {
        if (plan.TargetBlob == null)
            return;
        if (plan.SourceBlob.Size < plan.TargetBlob.Size)
        {
            plan.EndPosition = plan.TargetBlob.GridPosition;
            plan.BlobsToRemoveAfterMerge.Add(plan.SourceBlob);
            plan.ShouldTerminate = true;

        }
        else if (plan.SourceBlob.Size > plan.TargetBlob.Size)
        {
            RemoveTargetBlob(plan);
        }
        //if both blobs are normal size
        else if (plan.SourceBlob.Size == BlobSize.Normal)
        {
            RemoveTargetBlob(plan);
        }
        //if both blobs are small size
        else if (plan.SourceBlob.Size == BlobSize.Small)
        {
            //remove the target blob and increase the source blob's size
            plan.SizeChanges[plan.SourceBlob] = BlobSize.Normal; // Track original size
            RemoveTargetBlob(plan);
        }

    }

    public virtual void ModifyMergeFromTarget(MergePlan plan, BoardModel boardLogic)
    {
        return;
    }

    protected virtual void RemoveTargetBlob(MergePlan plan)
    {
        if (plan.TargetBlob is not IMergable) return;
        if (plan.BlobsToRemoveDuringMerge.Contains(plan.TargetBlob)) return;

        plan.BlobsToRemoveDuringMerge.Add(plan.TargetBlob);
    }


}





