using System;
using System.Collections;
using System.Collections.Generic;
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

    public ITileAnimator Animator => _animator;


    public static float TileSize => 1.5f;


    protected IBoardPresenter _board;
    public TilePresenter(TileView view)
    {
        _view = view;
        _model = view.Model;
        _animator = TileFactory.CreateTileAnimator(view);

    }
    
    public void Initialize(IBoardPresenter board)
    {
        _board = board;
        _tileViews.TryAdd(_model.ID, _view);
    }
    public IEnumerator SpawnTile()
    {
        Tween tween = _view.transform.DOScale(TileSize, 0.3f);

        yield return tween.WaitForCompletion();
    }

 
    public Tween Remove(float duration){
        _tileViews.Remove(_view.Model.ID);
       return _animator.CreateRemoveTween(duration);
    }
    public Tween Spawn(float duration){

        _tileViews.TryAdd(_view.Model.ID, _view);
        return _animator.CreateSpawnTween(duration);
    }
    
    public static TileView GetTileView(string id)
    {
        if (_tileViews.TryGetValue(id, out TileView view))
        {
            return view;
        }
        return null;

    }

    
}