using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;


/// <summary>
/// Interface for blob presenter (business logic)
/// </summary>
public interface IBlobPresenter
{
    BlobView View { get; }
    bool Enabled { get; }
    Blob Model { get; }

  

    // Actions
    Sequence MoveToGrid(Vector2Int gridPos);
    Sequence Remove();
    Sequence ScaleTo(float targetScale);

    Sequence Spawn();
    // Lifecycle
    void Initialize(IBoardPresenter board);
    void Select();
    void Deselect();
    void EnableBlob();
    void DisableBlob();
    Sequence Merge(IBlobPresenter blobToRemove);
    void PlayMergeEffect();
    Sequence Respawn();
}
