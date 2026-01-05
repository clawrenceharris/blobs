

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
    
    public override void FinalizeMergeFromSource(MergeContext context)
    {
        var plan = context.Plan;
        var board = context.Board;
        //remove the source and target blob
        plan.BlobsToRemoveAfterMerge.Add(plan.TargetBlob);


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
                plan.BlobsToRemoveAfterMerge.Add(existingBlob);
                continue;
            }

            // Second check: is a new blob being created in this position?
            var createdBlob = plan.BlobsToCreateOnPath
                .FirstOrDefault(b => b.Value == targetPos).Key;

            if (createdBlob != null)
            {
                plan.BlobsToRemoveAfterMerge.Add(createdBlob);
            }


        }
    }
  
    
}

