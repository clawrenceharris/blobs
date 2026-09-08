using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Blobs.Animation;
using Blobs.Utilities;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// Builds and runs animation sequences from a list of resolution effects (Core/Resolution).
/// Takes the effect list and board presenter; for each effect type builds the corresponding animation profile.
/// </summary>
public sealed class EffectAnimator
{
  
    /// <summary>
    /// Runs animations for each effect in order. Use for forward play (move/merge).
    /// </summary>
    public IEnumerator AnimateEffects( IReadOnlyList<IEffect> effects, IBoardPresenter board)
    {
        if (board == null) yield break;



        foreach (var effect in effects)
        {
            var seq = BuildSequence(effect, board);
            yield return seq?.WaitForCompletion();

        }
    }


   

    /// <summary>
    /// Builds the animation sequence for a single effect. Returns null if no animation for this effect type.
    /// </summary>
    public Sequence BuildSequence(IEffect effect, IBoardPresenter board)
    {
        if (effect == null || board == null) return null;
        switch (effect)
        {
            case MoveBlobEffect m:
                return BuildMoveBlob(m, board);
            case RemoveBlobEffect r:
                return BuildRemoveBlob(r, board);
            case SpawnBlobEffect s:
                return BuildSpawnBlob(s, board);
            case ResizeBlobEffect z:
                return BuildResizeBlob(z, board);
            case MergeEffect m:
                return BuildMergeBlob(m, board);
            case TriggerEffect t:
                return BuildTrigger(t, board);
            case SetTileStateEffect set:
                return ApplyTileState(set, board);
           
            default:
                return null;
        }
    }

    private Sequence BuildMergeBlob(MergeEffect m, IBoardPresenter board)
    {
        var blobToMove = board.GetBlob(m.BlobToMoveId);
        var blobToRemove = board.GetBlob(m.BlobToRemoveId);
        if (blobToMove == null || blobToRemove == null) return null;
        var seq = DOTween.Sequence();
        
        
        seq.Append(blobToMove.MergeWith(blobToRemove, m.To));
        
        
        return seq;

    }

    public Sequence ApplyTileState(SetTileStateEffect set, IBoardPresenter board)
    {
        var tile = board.GetTile(set.TileId);
        if (tile.Model is LaserTile laser && set.Key == "IsActive" && set.To is bool toActive)
        {
            if (laser.IsActive != toActive)
            {
                if (toActive)
                    laser.Toggle();
                else
                    laser.Deactivate();
            }
        }
        if (tile.Model is NormalTile && set.Key == "Trail" && set.To is bool toTrail)
        {
            if (toTrail)
            {
                BlobColor color = set.GetProperty<BlobColor>(LevelDataKeys.Properties.TrailColor);
                 return tile.LeaveTrail(color);

            }
            else
            {
                 return tile.RemoveTrail();
            }
        }
        
        return null;
    }
    private static Sequence BuildMoveBlob(MoveBlobEffect m, IBoardPresenter board)
    {
        var blob = board.GetBlob(m.BlobId);
        if (blob == null) return null;
        return blob.MoveToGrid(m.To);
    }

    private static Sequence BuildRemoveBlob(RemoveBlobEffect r, IBoardPresenter board)
    {
        var blob = board.GetBlob(r.BlobId);
        if (blob == null)
        {
            return null;
        }
        var seq = blob.Remove();
        return seq;
    }

    private static Sequence BuildSpawnBlob(SpawnBlobEffect s, IBoardPresenter board)
    {
        var blob = board.GetBlob(s.Blob.ID);
        if (blob == null) return null;
        return blob.Spawn()
            .JoinCallback(() => blob.SpawnParticles());
    }

    private static Sequence BuildResizeBlob(ResizeBlobEffect z, IBoardPresenter board)
    {
        var blob = board.GetBlob(z.BlobId);
        if (blob == null) return null;
        var scale = blob.Model.GetScaleFromBlobSize();
        return blob.ScaleTo(scale).AppendCallback(() => blob.SpawnParticles());
    }

    private static Sequence BuildTrigger(TriggerEffect t, IBoardPresenter board)
    {
    
        return null;
    }

   
}
