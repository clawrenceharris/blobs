using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using Object = UnityEngine.Object;

public class TilePresenter
{
    /// <summary>
    /// Maps tile IDs to their GameObject views
    /// </summary>
    private static readonly Dictionary<string, TileView> _tileViews = new();

    protected readonly TileView _view;
    protected readonly Tile _tile;
    private readonly float _scaleDuration = 0.3f;

    public static float TileSize => 1.5f;

    public static readonly float BlobOffsetY =  -0.9f;
   
    protected BoardPresenter _board;
    public TilePresenter(TileView view)
    {
        _view = view;
        _tile = view.Model;

    }
    
    public void Initialize(BoardPresenter board)
    {
        _board = board;
        _tileViews.TryAdd(_tile.ID, _view);
    }
    public IEnumerator SpawnTile()
    {
        Tween tween = _view.transform.DOScale(TileSize, 0.3f);

        yield return tween.WaitForCompletion();
    }

    public IEnumerator RemoveTile()
    {

        Tween tween = _view.transform.DOScale(Vector3.zero, _scaleDuration).SetEase(Ease.InBack);
        yield return tween.WaitForCompletion();
        _tileViews.Remove(_tile.ID);
        _board.RemoveTilePresenter(_tile.ID);
        Object.Destroy(_view.gameObject);

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