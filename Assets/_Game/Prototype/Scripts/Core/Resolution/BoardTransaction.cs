using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Applies effects to the board model and records inverse effects for granular undo.
/// </summary>
public sealed class BoardTransaction
{
    private readonly IBoardPresenter _board;
    private readonly List<IEffect> _applied = new();
    private readonly List<IEffect> _inverse = new();

    public BoardTransaction(IBoardPresenter board)
    {
        _board = board ?? throw new ArgumentNullException(nameof(board));
    }

    /// <summary>
    /// Effects applied so far, in order.
    /// </summary>
    public IReadOnlyList<IEffect> EffectsApplied => _applied;

    /// <summary>
    /// Applies the effect to the board and records the inverse effect.
    /// </summary>
    public void Apply(IEffect effect)
    {
        switch (effect)
        {
            case MoveBlobEffect m:
                _inverse.Add(new MoveBlobEffect(m.BlobId, m.To, m.From, m.MoveTag));
                _board.MoveBlob(m.BlobId, m.To);
                break;

            case ResizeBlobEffect r:
                _inverse.Add(new ResizeBlobEffect(r.BlobId, r.To, r.From, r.Cell));
                var blobToResize = _board.GetBlob(r.BlobId);
                if (blobToResize != null)
                    blobToResize.Model.Size = r.To;
                break;
            
            case RemoveBlobEffect rem:
            {
                
                if (_board.GetBlob(rem.BlobId)?.Model is {} removed) 
                {
                    _inverse.Add(new SpawnBlobEffect(removed, rem.At));
                    _board.RemoveBlob(rem.BlobId);
                }
                
                break;
            }
            case MergeEffect m:
                if (_board.GetBlob(m.BlobToRemoveId)?.Model is {} blobToRemove)
                {
                    _inverse.Add(new SpawnBlobEffect(blobToRemove, m.From));
                    _inverse.Add(new MoveBlobEffect(m.BlobToMoveId, m.To, m.From, m.MergeTag));

                    _board.RemoveBlob(m.BlobToRemoveId);
                    _board.MoveBlob(m.BlobToMoveId, m.To);
                }
               
                break;

            case SpawnBlobEffect sp:
                _inverse.Add(new RemoveBlobEffect(sp.Blob.ID, sp.At, EffectTags.UndoSpawn));
                var spawned = sp.Blob;
                if (spawned != null)
                    _board.SpawnBlob(spawned);
                break;

            case SetTileStateEffect set:
                _inverse.Add(new SetTileStateEffect(
                    set.TileId,
                    set.Key,
                    set.To,
                    set.From,
                    set.At,
                    data: set.Data != null ? new Dictionary<string, object>(set.Data) : null
                ));

                break;

            case TriggerEffect:
                // No model change; no inverse.
                break;

          

            default:
                throw new ArgumentException($"Unhandled effect type: {effect?.GetType().Name ?? "null"}", nameof(effect));
        }

        _applied.Add(effect);
    }

    /// <summary>
    /// Returns the inverse effects in reverse order (ready to apply for undo).
    /// </summary>
    public IReadOnlyList<IEffect> GetInverseEffectsReversed()
    {
        var list = new List<IEffect>(_inverse);
        list.Reverse();
        return list;
    }

    
}
