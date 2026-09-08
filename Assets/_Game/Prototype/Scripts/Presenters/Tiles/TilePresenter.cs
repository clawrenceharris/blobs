using System.Collections;
using System.Collections.Generic;
using Blobs.Animation;
using Blobs.Utilities;
using DG.Tweening;
using UnityEngine;

public class TilePresenter : ITilePresenter
{
    /// <summary>
    /// Maps tile IDs to their GameObject views
    /// </summary>
    private static readonly Dictionary<string, TileView> _tileViews = new();

    protected TileView _view;
    protected Tile _model;

    public Tile Model => _model;

    public TileView View => _view;
    private TileAnimator _animator;
    public TilePresenter(Tile model, TileView view)
    {
        _model = model;
        BindView(view);
    }
    public void SetModel(Tile model)
    {
        _model = model;
    }

    public void BindView(TileView view)
    {
        _view = view;
        _animator = view != null ? view.GetComponent<TileAnimator>() : null;

        if (view != null)
        {
            view.Initialize(_model);
            _view.UpdateView();
            view.transform.position = new Vector3(view.transform.position.x, -Camera.main.orthographicSize * 2f);
            
        }
    }




    public Sequence Remove()
    {
        return _animator.PlayDespawnAnimation();
    }
    public Sequence Spawn()
    {

        return Enter();
    }


    public Sequence Enter()
    {
        var targetPosition = GridUtility.GridToWorld(_model.GridPosition);
        
        return _animator.PlayEnterAnimation(targetPosition);
    }
    public Sequence Exit()
    {
        return _animator.PlayExitAnimation();
    }
    public Sequence LeaveTrail(BlobColor blobColor)
    {
        Debug.Log("Leaving trail");
        var color = ColorSchemeManager.FromBlobColor(blobColor);
        var pos = GridUtility.GridToWorldWithBlobOffset(_model.GridPosition);
        return _animator.PlayLeaveTrailAnimation(color, pos);
    }
    public Sequence RemoveTrail()
    {
        Debug.Log("Removing trail");
        return _animator.PlayRemoveTrailAnimation();
    }   
}