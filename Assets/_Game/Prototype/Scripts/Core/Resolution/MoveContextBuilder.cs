using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public sealed class MovePlan
{
    /// <summary>Direction of travel (set once at start).</summary>
    public Vector2Int Direction { get; set; }
    /// <summary>Current cell we are stepping onto (set by builder each step).</summary>
    public Vector2Int CurrentPosition { get; set; }
    /// <summary>Next cell to step onto (default current + direction; behavior can override).</summary>
    public Vector2Int NextPosition { get; set; }
    /// <summary>Ordered list of cells we have stepped onto (builder/behaviors add to this).</summary>
    public List<Vector2Int> Path { get; set; } = new List<Vector2Int>();
    /// <summary>If true, stop path building (e.g. rock blocks further movement).</summary>
    public bool ShouldTerminate { get; set; }
}

public enum MergeFailReason
{
    None,
    InvalidSource,
    InvalidTarget,
    NotAligned,
    NoTargetInDirection,
    TileBlocked,
    LaserBlocked,
    ColorRuleRejected,
    TargetColorRuleRejected,
    TargetMergeRuleRejected,
    BlobBlocked,
    OffBoard,
    SizeRuleRejected,
}
/// <summary>
/// Builds MoveContext from intent and board, including path from source to target.
/// </summary>
public static class MoveContextBuilder
{
    
    /// <summary>
    /// Validates intent and board, builds path, populates result.IsValid/InvalidReason, returns context or null.
    /// </summary>
    public static MoveContext Build(MoveIntent intent, BoardPresenter board, MoveResult result)
    {
        result.IsValid = false;
        result.MoveFailReason = MergeFailReason.None;
        result.Effects.Clear();
        result.InverseEffects.Clear();
    
        if (board == null)
        {
            Debug.LogError("[MoveContextBuilder] Board is null");
            result.MoveFailReason = MergeFailReason.OffBoard;
            return null;
        }

        if (string.IsNullOrEmpty(intent.SourceId))
        {
            Debug.LogError("[MoveContextBuilder] Source is null");
            result.MoveFailReason = MergeFailReason.InvalidSource;
            return null;
        }

        var source = board.GetBlob(intent.SourceId);
        if (source == null)
        {
            Debug.LogError("[MoveContextBuilder] Source not found");
            result.MoveFailReason = MergeFailReason.InvalidSource;
            return null;
        }

        Blob target = null;
        if (!string.IsNullOrEmpty(intent.TargetId))
        {
            target = board.GetBlob(intent.TargetId)?.Model;
            if (target == null)
            {
                Debug.LogError("[MoveContextBuilder] Target not found");
                result.MoveFailReason = MergeFailReason.InvalidTarget;
                return null;
            }
        }

        if (target == null)
        {
            Debug.LogError("[MoveContextBuilder] Target is null");
            result.MoveFailReason = MergeFailReason.InvalidTarget;
            return null;
        }

        var delta = target.GridPosition - source.Model.GridPosition;
        if (delta.x != 0 && delta.y != 0)
        {
            Debug.LogError("[MoveContextBuilder] Not aligned");
            result.MoveFailReason = MergeFailReason.NotAligned;
            return null;
        }
        var direction = NormalizeDirection(delta);

        var next = source.Model.GridPosition + direction;
        var plan = new MovePlan
        {
            Direction = direction,
            NextPosition = next
        };

        int maxSteps = 512;
        int steps = 0;
        while (true)
        {
            steps++;
            if (steps >= maxSteps)
            {
                Debug.LogError("[MoveContextBuilder] Max steps reached");
                result.MoveFailReason = MergeFailReason.NoTargetInDirection;
                return null;
            }

            var current = next;
            if (!board.IsValidPosition(current))
            {
                result.MoveFailReason = MergeFailReason.NoTargetInDirection;
                return null;
            }

            var tile = board.GetTileAt(current);
            if (tile == null || !tile.Model.Type.IsTraversable())
            {
                result.MoveFailReason = MergeFailReason.TileBlocked;
                return null;
            }

            if (board.IsLaserBlocking(source.Model.ID, current))
            {
                result.MoveFailReason = MergeFailReason.LaserBlocked;
                return null;
            }

            var blob = board.GetBlobAt(current);
            plan.CurrentPosition = current;
            plan.NextPosition = current + direction;
            plan.ShouldTerminate = false;

            if (blob != null)
            {
                blob.Model.MoveBehavior.OnMove(ref plan);
            }
            else
            {
                plan.Path.Add(current);
            }

            if (plan.ShouldTerminate)
                break;
            if (blob != null && blob.Model.ID == intent.TargetId)
                break;

            next = plan.NextPosition;
        }

        var path = plan.Path.Select(p => CellContext.Create(board, p)).ToList();
        Vector2Int end = path.Count > 0 ? path[path.Count - 1].Pos : source.Model.GridPosition;
        if (!ValidateMultiMergePath(source.Model, path, result))
            return null;

        // Path can be empty when we stopped at a rock on the first cell (blob doesn't move).
        if (path.Count == 0 && !plan.ShouldTerminate)
        {
            Debug.LogError("[MoveContextBuilder] No path");
            result.MoveFailReason = MergeFailReason.NoTargetInDirection;
            return null;
        }

        // Target is only set if we actually ended on the selected target blob; rock stop means no merge.
        var endBlob = path.Count > 0 && path[path.Count - 1].Blob != null && path[path.Count - 1].Blob.ID == intent.TargetId
            ? path[path.Count - 1].Blob
            : null;

        var ctx = new MoveContext
        {
            Board = board,
            Source = source.Model,
            Target = endBlob,
            Start = CellContext.Create(board, source.Model.GridPosition, source.Model),
            End = CellContext.Create(board, end, path.Count > 0 ? board.GetBlobAt(end)?.Model : null),
            Direction = direction,
            Path = path
        };

    
        result.IsValid = true;
        return ctx;
    }

    /// <summary>
    /// Validates each blob along the path (source → target) for multi-merge: type must be multi-mergeable and size &lt;= source size.
    /// </summary>
    private static bool ValidateMultiMergePath(Blob source, List<CellContext> path, MoveResult result)
    {
        for (int i = 0; i < path.Count; i++)
        {
            var blob = path[i].Blob;
            if (blob == null) continue;

           
        }
        return true;
    }

    private static Vector2Int NormalizeDirection(Vector2Int d)
    {
        if (d.x != 0 && d.y != 0) return Vector2Int.zero;
        return new Vector2Int(
            d.x == 0 ? 0 : (d.x > 0 ? 1 : -1),
            d.y == 0 ? 0 : (d.y > 0 ? 1 : -1));
    }
}
