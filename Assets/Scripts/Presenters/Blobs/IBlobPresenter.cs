using System.Collections;
using DG.Tweening;
using UnityEngine;


/// <summary>
/// Interface for blob presenter (business logic)
/// </summary>
public interface IBlobPresenter
{
    Blob Model { get; }
    BlobView View { get; }

    IBlobAnimator Animator { get; }


    // Actions
    Tween MoveToGrid(Vector2Int gridPos, float duration);
    Tween Remove(float duration = 0.35f);
    Tween ScaleTo(float targetScale, float duration = 0.25f);

    Tween Spawn(float duration = 0.35f);
    // Lifecycle
    void Initialize(IBoardPresenter board);
    void Select();
    void Deselect();
}
