using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Blobs.Core.Merge
{
    /// <summary>
    /// Resolves a move path by traversing step-by-step. Path modifiers (sticky, ice, portal, etc.)
    /// can be supplied to change direction or force stop; see IPathModifier.
    /// </summary>
public sealed class PathResolver
{
    private readonly List<IPathModifier> _mods;

    public PathResolver(IEnumerable<IPathModifier> modifiers = null)
    {
        _mods = modifiers != null
            ? modifiers.OrderBy(m => m.Priority).ToList()
            : new List<IPathModifier>();
    }

    public MergeResolveResult ResolvePath(IBoardPresenter board, MergeRequest request, out ResolvedPath path)
    {
        path = null;

        var source = board.GetBlob(request.SourceId);
        if (source == null) return MergeResolveResult.Fail(MergeFailReason.InvalidSource);

        Vector2Int dir = ResolveDirection(board, source.Model, request, out var fail);
        if (fail != MergeFailReason.None) return MergeResolveResult.Fail(fail);

        path = new ResolvedPath
        {
            SourceId = source.Model.ID,
            Start = source.Model.GridPosition,
            End = source.Model.GridPosition,
            InitialDirection = dir
        };

        var current = source.Model.GridPosition;
        int safety = 0;

        while (true)
        {
            safety++;
            if (safety > 512)
            {
                path.Termination = PathTermination.NoTargetFound;
                return MergeResolveResult.Fail(MergeFailReason.InfiniteLoopGuard);
            }

            var next = current + dir;
            if (!board.IsValidPosition(next))
            {
                path.Termination = PathTermination.OffBoard;
                return MergeResolveResult.Fail(MergeFailReason.NoTargetInDirection);
            }

            var tile = board.GetTileAt(next);
            if (tile == null || !tile.Model.Type.IsTraversable())
            {
                path.Termination = PathTermination.BlockedByTile;
                return MergeResolveResult.Fail(MergeFailReason.TileBlocked);
            }

            if (board.IsLaserBlocking(source, next))
            {
                path.Termination = PathTermination.BlockedByLaser;
                return MergeResolveResult.Fail(MergeFailReason.LaserBlocked);
            }

            var blob = board.GetBlobAt(next);

            var cell = new PathCell
            {
                Pos = next,
                TileId = tile.Model.ID,
                BlobId = blob?.Model.ID,
                Flags = PathFlags.None
            };
            path.Cells.Add(cell);

            // modifiers can redirect or stop
            foreach (var mod in _mods)
            {
                mod.Apply(board, source.Model, path, cell, ref dir, ref next, out bool stopNow);
                if (stopNow)
                {
                    path.End = cell.Pos;
                    path.Termination = PathTermination.ForcedStop;
                    return MergeResolveResult.SuccessWithPlan(null); // plan created later
                }
            }

            // default termination: first blob hit
            if (blob != null)
            {
                cell.Flags |= PathFlags.HitBlob;
                path.End = cell.Pos;
                path.HitBlobId = blob.Model.ID;
                path.Termination = PathTermination.HitBlob;
                return MergeResolveResult.SuccessWithPlan(null);
            }

            current = cell.Pos;
            path.End = current;
        }
    }

    private static Vector2Int ResolveDirection(IBoardPresenter board, Blob source, MergeRequest req, out MergeFailReason fail)
    {
        fail = MergeFailReason.None;

        if (req.HasDirection)
            return req.Direction.Value;

        if (!req.HasTarget)
        {
            fail = MergeFailReason.InvalidTarget;
            return Vector2Int.zero;
        }

        var target = board.GetBlob(req.TargetId);
        if (target == null)
        {
            fail = MergeFailReason.InvalidTarget;
            return Vector2Int.zero;
        }

        var delta = target.Model.GridPosition - source.GridPosition;
        if (delta.x != 0 && delta.y != 0)
        {
            fail = MergeFailReason.NotAligned;
            return Vector2Int.zero;
        }

        return new Vector2Int(Mathf.Clamp(delta.x, -1, 1), Mathf.Clamp(delta.y, -1, 1));
    }
    }
}
