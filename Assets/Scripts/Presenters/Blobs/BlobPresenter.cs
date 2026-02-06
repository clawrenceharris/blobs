using System;
using System.Collections.Generic;
using Blobs.Animation;
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
        Vector3 target = _board.Layout.GridToWorldWithBlobOffset(gridPos);
        return _animator.AnimateMoveTo(target);
    }

    public Sequence ScaleTo(float targetScale) {
        return _animator.PlayResizeAnimation(targetScale);
    }

    public Sequence Remove()
    {
        _blobViews.Remove(_model.ID);
        return _animator.PlayDespawnAnimation();
    }
    public Sequence Spawn()
    {

        _blobViews.TryAdd(_model.ID, _view);
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

    public Sequence Merge() => _animator.PlayMergeAnimation(_board.Layout.GridToWorldWithBlobOffset(_model.GridPosition));

    public void PlayMergeEffect() => _animator.SpawnMergeParticles(ColorSchemeManager.FromBlobColor(_model.Color));
}