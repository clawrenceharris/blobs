using System;
using System.Collections;
using System.Collections.Generic;
using Blobs.Animation;
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

    /// <summary>Update model reference when respawning (e.g. undo).</summary>
    void SetModel(Blob model);
    /// <summary>Rebind to a (pooled) view when respawning.</summary>
    void BindView(BlobView view);

    // Blob Actions
    Sequence MoveToGrid(Vector2Int gridPos);
    Sequence MoveToPath(List<Vector2Int> moveGroup);
    Sequence Remove();
    Sequence ScaleTo(float targetScale);
    Sequence Spawn();
    Sequence MergeWith(IBlobPresenter blobToRemove, Vector2Int to);
    void EnableBlob();
    void DisableBlob();
    void SpawnParticles();
    Sequence NudgeInDirection(Vector2Int direction);


    // Blob Selection
    void Select();
    void Deselect();

    // Blob Events
    event Action<IBlobPresenter> OnBlobRemoved;
    event Action<IBlobPresenter> OnBlobSpawned;

    /// <summary>
    /// Called after a merge. The first parameter is the blob that is being moved, the second parameter is the blob that is being removed.
    /// </summary>
    event Action<IBlobPresenter, IBlobPresenter> OnBlobMerged;
    event Action<IBlobPresenter> OnBlobMoved;
    event Action<IBlobPresenter> OnBlobResized;
    event Action<IBlobPresenter> OnBlobSelected;
    event Action<IBlobPresenter> OnBlobDeselected;
    event Action<IBlobPresenter> OnBlobEnabled;
    event Action<IBlobPresenter> OnBlobDisabled;

}
