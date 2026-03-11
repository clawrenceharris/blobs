using UnityEngine;

/// <summary>
/// Visual/VFX hook; no model change. Consumed by presenters for animation.
/// </summary>
public readonly struct TriggerEffect : IEffect
{
    public readonly string Tag;
    public  readonly Vector2Int At;
    public TriggerEffect(string tag,Vector2Int at)
    {
        Tag = tag;
        At = at;
    }
}
