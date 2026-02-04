using System.Collections.Generic;
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

    public IBlobAnimator Animator => _animator;

    public BlobView View => _view;

    public BlobPresenter(BlobView view)
    {
        _view = view;
        _model = view.Model;
        _animator = view.GetComponent<BlobAnimator>();


    }

    public void Initialize(IBoardPresenter board)
    {
        _board = board;
        _blobViews.TryAdd(_model.ID, _view);
    }
    
   

    public Tween MoveToGrid(Vector2Int gridPos, float duration)
    {
        _view.Visuals.ChangeSortingLayer("Foreground", _view.transform);
        Vector3 target = _board.Layout.GridToWorldWithBlobOffset(gridPos);
        return _animator.CreateMoveTween(target, duration);
    }

    public Tween ScaleTo(float targetScale, float duration) => _animator.CreateScaleTween(targetScale, duration);
    
    public Tween Remove(float duration){
        _blobViews.Remove(_view.Model.ID);
       return _animator.CreateRemoveTween(duration);
    }
    public Tween Spawn(float duration){

        _blobViews.TryAdd(_view.Model.ID, _view);
        return _animator.CreateSpawnTween(duration);
    }
   
    public void Select()
    {
        _animator.StartSelectionLoop();
    }

    public void Deselect()
    {
        _animator.StopSelectionLoop();
    }
}