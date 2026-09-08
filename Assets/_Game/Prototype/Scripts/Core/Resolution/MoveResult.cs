using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Result of MoveResolver.Resolve. Effects and InverseEffects are ordered for apply/undo.
/// </summary>
public sealed class MoveResult
{
    public bool IsValid { get; set; }
    public MoveContext Context { get; set; }
    public MergeFailReason MoveFailReason { get; set; }
    public List<IEffect> Effects { get; set; } = new();
    public List<IEffect> InverseEffects { get; set; } = new();
}
