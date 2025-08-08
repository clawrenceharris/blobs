

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
    private void ExecuteBehavior(MergePlan plan, BoardLogic board)
    {
        plan.DeferredPlan = new();
        //remove the source and target blob
        plan.DeferredPlan.BlobsToRemoveDuringMerge.Add(plan.TargetBlob);
        plan.DeferredPlan.BlobsToRemoveDuringMerge.Add(plan.SourceBlob);



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
            Blob existingBlob = board.GetBlobAt(targetPos);
            if (existingBlob != null)
            {
                plan.DeferredPlan.BlobsToRemoveDuringMerge.Add(existingBlob);
                continue;
            }

            // Second check: is a new blob being created in this position?
            var createdBlob = plan.BlobsToCreateDuringMerge
                .FirstOrDefault(b => b.GridPosition == targetPos);

            if (createdBlob != null && !plan.DeferredPlan.BlobsToRemoveDuringMerge.Contains(createdBlob))
            {
                plan.DeferredPlan.BlobsToRemoveDuringMerge.Add(createdBlob);
            }


        }
    }
    public override void ModifyMergeFromSource(MergePlan plan, BoardLogic board)
    {
        ExecuteBehavior(plan, board);

    }
     public override void ModifyMergeFromTarget(MergePlan plan, BoardLogic board)
    {
        ExecuteBehavior(plan, board);
        
    }
}

