

using System.Linq;
using UnityEngine;


/// <summary>
/// The Concrete Strategy for Bomb Blobs.
/// This class encapsulates the logic for creating a trail.
/// </summary>
public class BombBlobBehavior : BlobMergeBehavior
{
    public BombBlobBehavior(Blob blob) : base(blob)
    {
    }
    private void ExecuteBehavior(MergeContext context)
    {
        var plan = context.Plan;
        var board = context.Board;
        var deferredPlan = new MergePlan();
        //remove the source and target blob
        deferredPlan.BlobsToRemoveOnPath.TryAdd(plan.TargetBlob, plan.TargetBlob.GridPosition);
        deferredPlan.BlobsToRemoveOnPath.TryAdd(plan.SourceBlob, plan.SourceBlob.GridPosition);



        Vector2Int start = plan.EndPosition; // the bomb ends up here

        Vector2Int[] directions = {
                Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
                new(1, 1), new(1, -1),
                new(-1, 1), new(-1, -1)
            };

        foreach (var dir in directions)
        {
            Vector2Int targetPos = start + dir;

            // First check: is there already a blob on the board at that position?
            Blob existingBlob = board.Model.GetBlobAt(targetPos);
            if (existingBlob != null)
            {
                deferredPlan.BlobsToRemoveAfterMerge.Add(existingBlob);
                continue;
            }

            // Second check: is a new blob being created in this position?
            var createdBlob = plan.BlobsToCreateOnPath
                .FirstOrDefault(b => b.Value == targetPos).Key;

            if (createdBlob != null)
            {
                deferredPlan.BlobsToRemoveAfterMerge.Add(createdBlob);
            }


        }
        plan.DeferredPlans.Add(deferredPlan);

    }
    public override void ModifyMergeFromSource(MergeContext context)
    {
        ExecuteBehavior(context);

    }
     public override void ModifyMergeFromTarget(MergeContext context)
    {
        ExecuteBehavior(context);
        
    }
}

