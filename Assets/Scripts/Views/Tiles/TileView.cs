

using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(TileVisuals))]
public class TileView : MonoBehaviour
{
    public Tile Model { get; private set; }

    // References to visual components.
    private TileVisuals _visuals;
    public T GetVisuals<T>() where T : IVisuals
    {
        if (_visuals is T t)
            return t;
        return default;
    }
    public T GetModel<T>() where T : Tile
    {
        if (Model is T t)
            return t;
        return default;
    }

    private void Awake()
    {
        _visuals = GetComponent<TileVisuals>();
    }

    // The Presenter calls this to link the View to its data Model.
    public virtual void Setup(Tile tile, BoardPresenter board)
    {
        Model = tile;
        _visuals.SpriteRenderer.color = ColorSchemeManager.CurrentColorScheme.TileColor;
        _visuals.SpriteRenderer.sortingOrder = -(int)(transform.localPosition.y * 100) + (int)transform.localPosition.x;
        gameObject.name = $"{tile.Type} Tile {tile.GridPosition}";


    }
  
    
}
