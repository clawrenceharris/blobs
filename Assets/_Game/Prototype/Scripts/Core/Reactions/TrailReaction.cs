using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Trail blob reaction: Phase 1 (during move) leaves trail residue on tiles.
/// Phase 2 (after merge) spawns Normal blobs on path and clears trail residue.
/// </summary>
public sealed class TrailReaction : IReaction
{
    public void OnEffectApplied(MoveContext ctx, IEffect e, Queue<IEffect> queue)
    {
        // if (e is TriggerEffect moveTrigger && moveTrigger.Tag == EffectTags.AfterMove)
        // {
        //     if (!ctx.Source.Type.IsTrailBlob()) return;
        //     if (ctx.Path == null || ctx.Path.Count == 0) return;

        //     var tile = ctx.Board.GetTileAt(moveTrigger.At);
        //     var setTileState = new SetTileStateEffect(
        //             tileId: tile.Model.ID,
        //             key: "Trail",
        //             from: false,
        //             to: true,
        //             at: moveTrigger.At
        //         );
        //         queue.Enqueue(setTileState);

        // }
        if (e is TriggerEffect mergeTrigger && mergeTrigger.Tag == EffectTags.AfterMerge)
        {
            if (ctx?.Source is not TrailBlob trail) return;
            if (ctx.Path == null || ctx.Path.Count == 0) return;
            // Trail cells = Start + Path[i] for i = 0..Path.Count-2 (exclude final cell where trail blob ends/merges)
            var trailCells = new List<CellContext> { ctx.Start };
            for (int i = 0; i < ctx.Path.Count - 1; i++)
            {
                trailCells.Add(ctx.Path[i]);
            }

            foreach (var cell in trailCells)
            {
                Debug.Log("[TrailReaction] Spawning normal blob at cell: " + cell.Pos);
               

                queue.Enqueue(new SetTileStateEffect(
                    tileId: cell.Tile.ID,
                    key: "Trail",
                    from: false,
                    to: true,
                    at: cell.Pos));




            }
            foreach (var cell in trailCells)
            {
                Debug.Log("[TrailReaction] Spawning normal blob at cell: " + cell.Pos);
                
                 // Spawn Normal blob at this trail cell
                var newBlob = new NormalBlob(
                    trail.TrailColor,
                    trail.Size,
                    cell.Pos,
                    ResolutionIds.Next()

                );
                queue.Enqueue(new SetTileStateEffect(
                    tileId: cell.Tile.ID,
                    key: "Trail",
                    from: true,
                    to: false,
                    at: cell.Pos));

                queue.Enqueue(new SpawnBlobEffect(newBlob, cell.Pos));



            }

            
        }
    }
}
