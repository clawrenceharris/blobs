using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class MergeContext
{
    public Blob CurrentBlob { get; set; }

    public  MergePlan Plan { get; set; }
   
    public Vector2Int CurrentPosition { get; set; }
    public BoardModel Board { get; set; }
}


public interface IMergeBehavior2
{
    IEnumerable<IAction> EvaluateMergeEffects(MergeContext context);
}
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
    /// <param name="boardModel">A reference to the board model for querying state.</param>
    void ModifyMergeFromSource(MergeContext context);

    /// <summary>
    /// Called by the target blob in a merge to modify the outcome of the merge plan. Used for when a target Blob needs to react to the source blob's actions.
    /// </summary>
    /// <param name="plan">The current merge plan to be modified.</param>
    /// <param name="boardModel">A reference to the board model for querying state.</param>
    void ModifyMergeFromTarget(MergeContext context);

   
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

   
    public virtual void ModifyMergeFromSource(MergeContext context)
    {
        var current = context.CurrentBlob;
        var source = context.Plan.SourceBlob;
        var plan = context.Plan;
        
        if (current == null)
            return;
        if (source.Size < current.Size)
        {
            plan.EndPosition = current.GridPosition;
            plan.ShouldTerminate = true;
            RemoveSourceBlob(context, current.GridPosition);

        }
        // If the  source blob is bigger than than the current
        else if (source.Size > current.Size)
        {
            // The current blob must be removed
            RemoveCurrentBlob(context);
        }
        // If both blobs are normal size
        else if (source.Size == BlobSize.Normal)
        {
            RemoveCurrentBlob(context);
        }
        // If both blobs are small size
        else if (source.Size == BlobSize.Small)
        {
            // Remove the current blob and increase the source blob's size
            plan.SizeChanges[source] = BlobSize.Normal; // Track original size
            RemoveCurrentBlob(context);
        }

    }

    public virtual void ModifyMergeFromTarget(MergeContext context)
    {
        return;
    }


    /// <summary>
    /// Adds the current blob to list of blobs that will be removed during the merge
    /// </summary>
    /// <param name="context">The merge context</param>
    protected void RemoveCurrentBlob(MergeContext context)
    {
        if (context.CurrentBlob is not IClearable) return;

        context.Plan.BlobsToRemoveOnPath.TryAdd(context.CurrentBlob, context.CurrentPosition);
    }
    
    /// <summary>
    /// Adds the source blob to list of blobs that will be removed after the merge
    /// </summary>
    /// <param name="context"></param>
    protected void RemoveSourceBlob(MergeContext context, Vector2Int position)
    {
        var plan = context.Plan;
        if (plan.SourceBlob is not IClearable) return;

        plan.BlobsToRemoveAfterMerge.Add(plan.SourceBlob);
    }
    
   
}





