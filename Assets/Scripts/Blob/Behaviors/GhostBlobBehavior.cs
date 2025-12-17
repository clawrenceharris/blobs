using System.Linq;
using Unity.VisualScripting;
using UnityEngine;


/// <summary>
/// The concrete behavior strategy for Ghost Blobs.
/// Ghost blobs modify the merge plan by removing any blob's in the source blob's
/// position after the merge and overtaking it's spot 
/// </summary>
public class GhostBlobBehavior : BlobMergeBehavior
{
    public GhostBlobBehavior(Blob blob) : base(blob)
    {
    }
    public override void ModifyMergeFromTarget(MergeContext context)
    {

        var plan = context.Plan;
        var board = context.Board;

        plan.BlobsToRemoveOnPath.TryAdd(plan.SourceBlob, _blob.GridPosition);

        // Make a new plan for the ghost to move to the source blob's start position from the original plan.
        // The ghost blob will reoccupy or "haunt" the original source blob's place.
        MergePlan hauntedPlan = board.CalculateMergePlan(_blob, plan.SourceBlob);

        // Get any blob from this original merge plan that may already be occupying 
        // the target position of the new source blob (the ghost) within the new haunted plan.
        Blob createdBlob = plan.BlobsToCreateOnPath.FirstOrDefault((b) => b.Value == hauntedPlan.EndPosition).Key;
        Debug.Log(createdBlob);
        if (createdBlob != null)
        {
            // Remove the blob from the original plan occupying the target end position. 
            // It will be removed on the path when the ghost blob reaches the end position
            hauntedPlan.BlobsToRemoveAfterMerge.Add(createdBlob);
        }

        // Make this new plan a part of the deferred plan since the new plan 
        // should happen after the original merge
        plan.DeferredPlan = hauntedPlan;


    }
}
