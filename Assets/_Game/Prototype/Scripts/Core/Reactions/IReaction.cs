using System.Collections.Generic;

/// <summary>
/// Event-driven hook for resolution: can enqueue more effects when an effect is applied.
/// </summary>
public interface IReaction
{
    void OnEffectApplied(MoveContext ctx, IEffect effect, Queue<IEffect> queue);
}
