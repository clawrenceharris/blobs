using System.Collections.Generic;
using UnityEngine;

public sealed class BuildBasicMergeRule : IMoveRule
{
    public int Priority => 100;
    public bool Apply(MoveContext ctx, Queue<IEffect> queue, out MergeFailReason reason)
    {
        reason = MergeFailReason.None;
        if (ctx?.Path == null || ctx.Path.Count == 0) return true;

        foreach (var cell in ctx.Path)
        {
            
            queue.Enqueue(new TriggerEffect(EffectTags.AfterMove, cell.Pos));

            var source = ctx.Source;
            var target = cell.Blob;
            // Skip if the starting cell
            if (cell.Pos == ctx.Start.Pos) continue;
            // Multi-merge: remove every blob along the path (not the source), so intermediate blobs are released.   
            if (target != null && target.ID != source.ID)
            {

                if(!source.MatchRule.Validate(source, target, out MergeFailReason sourceFailReason))
                {
                    reason = sourceFailReason;
                    return false;
                }
                if (!target.MatchRule.Validate(target, source, out MergeFailReason targetFailReason))
                {
                    reason = targetFailReason;
                    return false;
                }
                // If the source is smaller than the target, remove the target and trigger the after merge effect
                if (source.Size < target.Size)
                {
                    // queue.Enqueue(new MoveBlobEffect(blobId: target.ID, from: cell.Pos, to: target.GridPosition, moveTag: EffectTags.Merge));
                    queue.Enqueue(new MergeEffect(
                        blobToMoveId: source.ID,
                        blobToRemoveId: source.ID,
                        to: target.GridPosition,
                        from: source.GridPosition,
                        mergeTag: EffectTags.Merge)
                    );

                    queue.Enqueue(new TriggerEffect(EffectTags.AfterMerge, target.GridPosition));

                    return true;
                }

                // If the source is the same size as the target, resize the source to big
                else if (source.Size == BlobSize.Small && target.Size == BlobSize.Small)
                {
                    queue.Enqueue(new ResizeBlobEffect(source.ID, BlobSize.Small, BlobSize.Normal, cell));
                }
                queue.Enqueue(new MergeEffect(
                    blobToMoveId: source.ID,
                    blobToRemoveId: target.ID,
                    to: cell.Pos,
                    from: source.GridPosition,
                    mergeTag: EffectTags.Merge)
                );

            }





        }
        // Trigger after merge effect, passing the final cell holding the source blob  
        queue.Enqueue(new TriggerEffect(EffectTags.AfterMerge, ctx.End.Pos));
        return true;
    }
}

