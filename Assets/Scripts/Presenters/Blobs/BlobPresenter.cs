using System;
using System.Collections;
using System.Collections.Generic;
using Blobs.Core.Merge;
using Blobs.Utilities;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;
using Object = UnityEngine.Object;

public class BlobPresenter : IBlobPresenter
{
    public const string PARAM_IS_SELECTED = "IsSelected";
    public const string PARAM_TRIGGER_MERGE = "TriggerMerge";
    /// <summary>
    /// Maps blob IDs to their GameObject views
    /// </summary>
    private static readonly Dictionary<string, BlobView> _blobViews = new();
    private readonly BlobView _blobView;
    protected readonly IBlobModel _blobModel;
    public static readonly float BlobOffsetY = -0.9f;

    protected IBoardPresenter _board;

    public IBlobModel Model => _blobModel;
    private readonly IBlobAnimator _animator;

    public BlobView View => _blobView;

    public BlobPresenter(BlobView view)
    {
        _blobView = view;
        _blobModel = view.Model;
        _animator = BlobFactory.CreateBlobAnimator(view);


    }

    public void Initialize(IBoardPresenter board)
    {
        _board = board;
        _blobViews.TryAdd(_blobModel.ID, _blobView);
    }
    
   

    public Tween MoveToGrid(Vector2Int gridPos, float duration)
    {
        _blobView.Visuals.ChangeSortingLayer("Foreground", _blobView.transform);

        Vector3 target = _board.Layout.GridToWorldWithBlobOffset(gridPos);
        return _animator.CreateMoveTween(target, duration).SetEase(Ease.OutQuad);
    }

    public Tween ScaleTo(float targetScale, float duration) => _animator.CreateScaleTween(targetScale, duration);
    public Tween Remove(float duration){
        _blobViews.Remove(_blobView.Model.ID);
       return _animator.CreateRemoveTween(duration);
    }
    public Tween Spawn(float duration){

        _blobViews.TryAdd(_blobView.Model.ID, _blobView);
        return _animator.CreateSpawnTween(duration);
    }
   
    public void Select()
    {
        _animator.Animator.SetBool(PARAM_IS_SELECTED, true);

    }

    public void Deselect()
    {
        _animator.Animator.SetBool(PARAM_IS_SELECTED, false);
    }
}