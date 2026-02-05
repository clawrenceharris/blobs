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



    public void MoveToGrid(Vector2Int gridPos,Action onComplete = null)
    {
        _view.Visuals.ChangeSortingLayer("Foreground", _view.transform);
        Vector3 target = _board.Layout.GridToWorldWithBlobOffset(gridPos);
        _animator.AnimateMoveTo(target);
    }

    public void ScaleTo(float targetScale,Action onComplete = null) {
        _animator.PlayResizeAnimation(targetScale); 
    }

    public void Remove(Action onComplete = null)
    {
        _blobViews.Remove(_model.ID);
        _animator.PlayDespawnAnimation();
    }
    public void Spawn(Action onComplete = null)
    {

        _blobViews.TryAdd(_model.ID, _view);
        _animator.PlaySpawnAnimation();
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
}