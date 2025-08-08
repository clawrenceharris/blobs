

using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(SpriteRenderer))]
public class TileView : MonoBehaviour
{
    // The data this view represents.
    private Tile _model;
    public string ID => _model.ID;

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
        if (_model is T t)
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
        _model = tile;
        
        _visuals.SpriteRenderer.color = ColorSchemeManager.CurrentColorScheme.TileColor;

        


        // Configure the visuals based on the data.
        gameObject.name = $"{tile.TileType} Tile {tile.GridPosition}";


    }
  

    public IEnumerator Remove()
    {
       yield return transform.DOScale(Vector2.zero, 0.4f)
             .OnComplete(() => Destroy(gameObject)).WaitForCompletion();
    }
     public IEnumerator Spawn()
    {
        yield return transform.DOScale(BoardPresenter.TileSize, 0.3f)
            .OnComplete(() => Destroy(gameObject)).WaitForCompletion();
    }


   
   

    
   

}
