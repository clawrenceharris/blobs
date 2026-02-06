using System;
using System.Collections;
using System.Collections.Generic;
using Blobs.Animation;
using DG.Tweening;
using UnityEngine;
using Object = UnityEngine.Object;

public class TilePresenter : ITilePresenter
{
    /// <summary>
    /// Maps tile IDs to their GameObject views
    /// </summary>
    private static readonly Dictionary<string, TileView> _tileViews = new();

    protected readonly TileView _view;
    protected readonly Tile _model;

    public Tile Model => _model;

    public TileView View => _view;
    private readonly TileAnimator _animator;



    public static float TileSize => 1.5f;


    protected IBoardPresenter _board;
    public TilePresenter(Tile model, TileView view)
    {
        _model = model;
        _view = view;
        _animator = view.GetComponent<TileAnimator>();

    }

    public void Initialize(IBoardPresenter board)
    {
        _board = board;
        _tileViews.TryAdd(_model.ID, _view);
        _animator.Initialize();


    }
    public IEnumerator SpawnTile()
    {
        Tween tween = _view.transform.DOScale(TileSize, 0.3f);

        yield return tween.WaitForCompletion();
    }

 
    public Tween Remove(){
        _tileViews.Remove(_model.ID);
       return _animator.PlayDespawnAnimation();
    }
    public Tween Spawn(){

        _tileViews.TryAdd(_model.ID, _view);
        return _animator.PlaySpawnAnimation();
    }

    public void PlayTraversalEffect() => _animator.PlayTraversalEffect();

    
}