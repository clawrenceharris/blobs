using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// The Strategy interface. Defines a contract for any special logic
/// a blob might perform during a merge.
/// </summary>
public interface ITileMergeBehavior
{
    /// <summary>
    /// Allows a tile's behavior to modify the outcome of the merge plan.
    /// </summary>
    /// <param name="plan">The current merge plan to be modified.</param>
    /// <param name="boardLogic">A reference to the board for querying state.</param>
    void ModifyMerge(MergeContext context);

    
}





public class TileMergeBehavior : ITileMergeBehavior
{
    protected readonly Tile _tile;
    public TileMergeBehavior(Tile tile)
    {
        _tile = tile;
    }
    public virtual void ModifyMerge(MergeContext context)
    {
        return;
    }

  
    /// <summary>
    /// Adds the source blob to the list of blobs that will be removed during the merge
    /// </summary>
    /// <param name="context">The merge context</param>
    /// <param name="position">The grid position from which the source blob will be removed</param>
    protected virtual void RemoveSourceBlob(MergeContext context, Vector2Int position)
    {
        var plan = context.Plan;
        if (plan.SourceBlob is not IClearable) return;

        plan.BlobsToRemoveOnPath.TryAdd(plan.SourceBlob, position);
    }


}





