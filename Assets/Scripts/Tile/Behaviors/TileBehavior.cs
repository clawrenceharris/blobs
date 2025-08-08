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
    void ModifyMerge(MergePlan plan, BoardLogic boardLogic);

    
}





public class TileMergeBehavior : ITileMergeBehavior
{
    protected readonly Tile _tile;
    public TileMergeBehavior(Tile tile)
    {
        _tile = tile;
    }
    public virtual void ModifyMerge(MergePlan plan, BoardLogic board)
    {
        return;
    }

  

    protected virtual void RemoveSourceBlob(MergePlan plan)
    {
        if (plan.SourceBlob is not IMergable) return;
        if (plan.BlobsToRemoveDuringMerge.Contains(plan.SourceBlob)) return;

        plan.BlobsToRemoveDuringMerge.Add(plan.SourceBlob);
    }


}





