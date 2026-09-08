using UnityEngine;

/// <summary>
/// Input-level description of what the player attempted.
/// Kept for future expansion; currently only Merge is used (all moves require source + target).
/// </summary>
public enum MoveIntentType
{
    Merge,
}

/// <summary>
/// Describes what the player attempted, nothing else. Source and target are required for resolution.
/// </summary>
public readonly struct MoveIntent
{
    public readonly MoveIntentType Type;
    public readonly string SourceId;
    public readonly string TargetId;
    public readonly Vector2Int Direction;

    public MoveIntent(
        MoveIntentType type,
        string sourceId,
        string targetId,
        Vector2Int direction
    )
    {
        Type = type;
        SourceId = sourceId;
        TargetId = targetId;
        Direction = direction;
    }

    public static MoveIntent Merge(string sourceId, string targetId, Vector2Int direction)
        => new(MoveIntentType.Merge, sourceId, targetId, direction);
}
