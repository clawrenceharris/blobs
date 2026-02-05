using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;


/// <summary>
/// Interface for blob presenter (business logic)
/// </summary>
public interface IBlobPresenter
{
    // Blob Model { get; }
    BlobView View { get; }
    bool Enabled { get; }
    Blob Model { get; }

  

    // Actions
    void MoveToGrid(Vector2Int gridPos,Action onComplete = null);
    void Remove(Action onComplete = null);
    void ScaleTo(float targetScale,Action onComplete = null);

    void Spawn(Action onComplete = null);
    // Lifecycle
    void Initialize(IBoardPresenter board);
    void Select();
    void Deselect();
    void EnableBlob();
    void DisableBlob();
}
