using DG.Tweening;
using UnityEngine;

public interface IBlobAnimator
{
    Tween CreateScaleTween(float targetScale, float duration);
    Tween CreateRemoveTween(float duration);
    Tween CreateSpawnTween(float duration);
    Tween CreateMoveTween(Vector3 position, float duration);
    
    /// <summary>
    /// Starts a looping squish in/out animation for selection feedback.
    /// </summary>
    void StartSelectionLoop();
    
    /// <summary>
    /// Stops the selection loop and smoothly blends back to base scale.
    /// </summary>
    void StopSelectionLoop();
}