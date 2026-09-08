

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Rule used by ResolutionPipeline. Can validate and/or enqueue effects.
/// </summary>
public interface IMoveRule
{
    int Priority { get; }
    bool Apply(MoveContext ctx, Queue<IEffect> queue, out MergeFailReason failReason);
}