

using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(TileVisuals))]
public class TileView : MonoBehaviour
{

    // References to visual components.
    private TileVisuals _visuals;
    public TileVisuals Visuals => _visuals;

    public T GetVisuals<T>() where T : IVisuals
    {
        if (_visuals is T t)
            return t;
        return default;
    }
   

    private void Awake()
    {
        _visuals = GetComponent<TileVisuals>();
    }

   

    public virtual void Initialize(Tile tile)
    {
        _visuals.SpriteRenderer.color = ColorSchemeManager.CurrentColorScheme.TileColor;
        _visuals.SpriteRenderer.sortingOrder = -(int)(transform.localPosition.y * 100) + (int)transform.localPosition.x;
        gameObject.name = $"{tile.Type} - {tile.GridPosition}";
    }

    internal void SetRotationFromDirection(Vector2Int dir)
    {
        throw new NotImplementedException();
    }
}
