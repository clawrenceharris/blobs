using System;
using System.Collections.Generic;
using Blobs.Animation;
using Blobs.Utilities;
using DG.Tweening;
using UnityEngine;

public class BlobPresenter : IBlobPresenter
{

    /// <summary>
    /// Maps blob IDs to their GameObject views
    /// </summary>
    private static readonly Dictionary<string, BlobView> _blobViews = new();
    private readonly BlobView _view;
    protected readonly Blob _model;
    public static readonly float BlobOffsetY = -0.9f;

    protected IBoardPresenter _board;

    public Blob Model => _model;
    private readonly BlobAnimator _animator;
    public bool Enabled => _model.Enabled;

    public BlobView View => _view;

    public BlobPresenter(Blob model, BlobView view)
    {
        _model = model;
        _view = view;
        _animator = view.GetComponent<BlobAnimator>();


    }

    public void Initialize(IBoardPresenter board)
    {
        _board = board;
        _blobViews.TryAdd(_model.ID, _view);
        _animator.Initialize();

        _view.transform.localScale = Vector2.zero;


    }



    public Sequence MoveToGrid(Vector2Int gridPos)
    {
        _view.Visuals.ChangeSortingLayer("Foreground", _view.transform);
        Vector3 worldPos = GridUtility.GridToWorldWithBlobOffset(gridPos);
        return _animator.AnimateMoveTo(worldPos);
    }

    public Sequence ScaleTo(float targetScale) {
        return _animator.PlayResizeAnimation(targetScale);
    }

    public Sequence Remove()
    {
        return _animator.PlayDespawnAnimation().OnComplete(() =>
        {
            _blobViews.Remove(_model.ID);
            _view.gameObject.SetActive(false);

        });
    }
    public Sequence Spawn()
    {

        _blobViews.TryAdd(_model.ID, _view);
        return _animator.PlaySpawnAnimation();
    }
    
    public Sequence Respawn()
    {
        _view.gameObject.SetActive(true);

        // Re-enable SpriteRenderers that were hidden during the merge animation.
        foreach (var sr in _view.GetComponentsInChildren<SpriteRenderer>(true))
            sr.enabled = true;

        return _animator.PlaySpawnAnimation();
    }

    public void Select()
    {
        _animator.PlaySelectAnimation();
    }

    public void Deselect()
    {
       
        
        _animator.PlayDeselectAnimation();
        
            
    }
    

    public void EnableBlob() => _model.EnableBlob();

    public void DisableBlob() => _model.DisableBlob();

    public Sequence Merge(IBlobPresenter blobToRemove) {

        if (blobToRemove?.View == null || _view == null)
        {
            Debug.LogWarning("[BlobPresenter] Merge skipped — null view reference");
            return DOTween.Sequence();
        }

        return _animator.PlayMergeAnimation(blobToRemove, this, GridUtility.GridToWorldWithBlobOffset(_model.GridPosition));

    }

    public void PlayMergeEffect() => _animator.SpawnMergeParticles(ColorSchemeManager.FromBlobColor(_model.Color));
}