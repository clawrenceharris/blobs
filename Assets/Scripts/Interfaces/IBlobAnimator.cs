using DG.Tweening;
using UnityEngine;

public interface IBlobAnimator
{
    Animator Animator { get; }
    Tween CreateScaleTween(float targetScale, float duration);
    Tween CreateRemoveTween(float duration);
    Tween CreateSpawnTween(float duration);
    Tween CreateMoveTween(Vector3 position, float duration);
    



}