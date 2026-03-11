

using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using Blobs.Visuals;
using Blobs.Utilities;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(TileVisuals))]
public class TileView : MonoBehaviour
{
    private Tile _model;
    // References to visual components.
    private TileVisuals _visuals;
    public TileVisuals Visuals => _visuals;

    private TileSpriteController _spriteController;
    public T GetVisuals<T>() where T : IVisuals
    {
        if (_visuals is T t)
            return t;
        return default;
    }



    public virtual void Initialize(Tile model)
    {
        _model = model;
        _visuals = GetComponent<TileVisuals>();
        _visuals.SpriteRenderer.color = ColorSchemeManager.CurrentColorScheme.TileColor;
        gameObject.name = $"{model.Type} Tile - {model.GridPosition}";

        if (TryGetComponent<TileSpriteController>(out var sc))
        {
            _spriteController = sc;
        }
        
    }

    /// <summary>
    /// Updates the tile sprite based on neighboring tiles.
    /// </summary>
    /// <param name="boardModel">The board model to query for neighbors</param>
    public void UpdateTileSprite(BoardModel boardModel)
    {
        if (_visuals != null && _model != null)
        {
            _spriteController.UpdateSprite(boardModel, _model.GridPosition);
        }
    }
      
    public void UpdateView()
    {
        _spriteController.UpdateSortingOrder(transform.position);

    }
    public void ChangeSortingLayer(string layerName)
    {
        _spriteController.ChangeSortingLayer(layerName, transform);
    }
}
