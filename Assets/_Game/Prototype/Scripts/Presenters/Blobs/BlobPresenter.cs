using System;
using System.Collections.Generic;
using System.Linq;
using Blobs.Animation;
using Blobs.Utilities;
using DG.Tweening;
using UnityEngine;

public class BlobPresenter : IBlobPresenter
{
    private BlobView _view;
    protected Blob _model;
    public Blob Model => _model;
    private BlobAnimator _animator;
    public bool Enabled => _model != null && _model.Enabled;
    public event Action<IBlobPresenter> OnBlobRemoved;
    public event Action<IBlobPresenter> OnBlobSpawned;
    public event Action<IBlobPresenter, IBlobPresenter> OnBlobMerged;
    public event Action<IBlobPresenter> OnBlobMoved;
    public event Action<IBlobPresenter> OnBlobResized;
    public event Action<IBlobPresenter> OnBlobSelected;
    public event Action<IBlobPresenter> OnBlobDeselected;
    public event Action<IBlobPresenter> OnBlobEnabled;
    public event Action<IBlobPresenter> OnBlobDisabled;
    public BlobView View => _view;

    public void SetModel(Blob model)
    {
        _model = model;
    }

    public void BindView(BlobView view)
    {
        _view = view;
        _animator = view != null ? view.GetComponent<BlobAnimator>() : null;
        if (_animator != null)
        {
          _animator.Initialize();
        }
      
        if (view != null)
        {
            view.Initialize(_model);

            view.transform.localScale = Vector2.zero;
        }


    }

    public BlobPresenter(Blob model, BlobView view)
    {
        _model = model;
        BindView(view);
        BoardPresenter.OnMergeAnimationStart += OnMergeAnimationStart;
        
    }
    private void OnMergeAnimationStart(ICommand command)
    {
        if (command is not MergeCommand merge)
        {
            return;
        }
        if (merge.Source.ID == _model.ID)
        {
            _view?.ExpressionController.ShowExpression(ExpressionType.Merging);
            _view.ChangeSortingLayer("Foreground");
        }
        else if (merge.Target != null && merge.Target.ID == _model.ID)
        {
            _view.ShowExpression(ExpressionType.Surprised);
        }
        
       
    }
   

    public Sequence MoveToGrid(Vector2Int gridPos)
    {
        if (_animator == null) return null;
        Vector3 worldPos = GridUtility.GridToWorldWithBlobOffset(gridPos);
        return _animator.AnimateMoveTo(worldPos).OnComplete(() =>
        {
            OnBlobMoved?.Invoke(this);
        });
    }
    public Sequence MoveToPath(List<Vector2Int> moveGroup)
    {
        if (_animator == null) return null;
        var path = moveGroup.Select(GridUtility.GridToWorldWithBlobOffset).ToList();
        return _animator.AnimateMovePath(path).OnComplete(() =>
        {
            OnBlobMoved?.Invoke(this);
        });
    }

    public Sequence ScaleTo(float targetScale)
    {
        if (_animator == null) return null;
        return _animator.PlayResizeAnimation(targetScale)
        .OnComplete(() =>
        {
            OnBlobResized?.Invoke(this);
        });
    }

    /// <summary>
    /// Play despawn animation only
    /// </summary>
    public Sequence Remove()
    {
        if (_animator == null) return null;
        return _animator.PlayDespawnAnimation()
        .OnComplete(() =>
        {
            OnBlobRemoved?.Invoke(this);
        });
    }

    public Sequence Spawn()
    {
        if (_animator == null) return null;
        var initialScale = Vector2.one * _model.GetScaleFromBlobSize();
        var color = ColorSchemeManager.FromBlobColor(_model.Color);
        return _animator.PlaySpawnAnimation(initialScale).JoinCallback(() => {
            _animator.SpawnMergeParticles(color);
        })
        .OnComplete(() =>
        {
            OnBlobSpawned?.Invoke(this);
        });
    }



    public void Select()
    {
        if (_animator == null) return;
        _animator.PlaySelectAnimation();
        OnBlobSelected?.Invoke(this);
    }

    public void Deselect()
    {
        if (_animator == null) return;
        _animator.PlayDeselectAnimation();
        OnBlobDeselected?.Invoke(this);
    }

    public void EnableBlob()
    {
        _model.EnableBlob();
        OnBlobEnabled?.Invoke(this);
    }

    public void DisableBlob()
    {
        _model.DisableBlob();
        OnBlobDisabled?.Invoke(this);
    }
    public Sequence MergeWith(IBlobPresenter blobToRemove, Vector2Int to)
    {
        if (_animator == null) return null;
        var targetWorldPos = GridUtility.GridToWorldWithBlobOffset(to);
        return _animator.PlayMoveForMerge(targetWorldPos)
        .AppendCallback(() =>
        {
            
            PlayMergeEffect(blobToRemove);
            OnBlobMerged?.Invoke(this, blobToRemove);

            blobToRemove.View.ExpressionController.RemoveExpression();
            blobToRemove.Remove().OnComplete(() =>
            {
                OnBlobRemoved?.Invoke(blobToRemove);
                _view.ExpressionController.ShowExpression(ExpressionType.Normal);
                
            });



        });
    }

    public void PlayMergeEffect(IBlobPresenter blobToRemove)
    {
        if (_animator != null)
            _animator.PlayMergeEffect(this, blobToRemove);
    }
    public void SpawnParticles()
    {
        if (_animator != null)
            _animator.SpawnMergeParticles(ColorSchemeManager.FromBlobColor(_model.Color));
    }
    public Sequence NudgeInDirection(Vector2Int direction)
    {
        if (_animator == null) return null;
        Debug.Log("Nudging blob in direction: " + direction);
        var worldDirection = GridUtility.GridToWorld(direction);
        _view.ChangeSortingLayer("Foreground");
        return _animator.PlayNudgeInDirectionAnimation(worldDirection)
        .OnComplete(() =>
        {
            _view.ChangeSortingLayer("Blobs");

        });
    }
}