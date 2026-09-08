
using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using Blobs.Visuals;
using Blobs.Utilities;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(BlobVisuals))]
public class BlobView : MonoBehaviour
{


    private Blob _model;
    private BlobVisuals _visuals;
    public bool Enabled => _model.Enabled;

    public BlobVisuals Visuals => _visuals;
    private SpriteController _spriteController;
    public string ID => _model.ID;
    public BlobType Type => _model.Type;
    public T GetModel<T>() where T : Blob
    {
        return (T)_model;
    }
    public T GetVisuals<T>() where T : BlobVisuals
    {
        return (T)_visuals;
    }
    void Awake()
    {
        _visuals = GetComponent<BlobVisuals>();

    }

    public Vector3 Scale => Vector3.one * _model.GetScaleFromBlobSize();

    public ExpressionController ExpressionController => _expressionController;

    public ExpressionController _expressionController;


    public virtual void Initialize(Blob model)
    {
        _model = model;
        _visuals = GetComponent<BlobVisuals>();
        gameObject.name = $"{model.Type} Blob {model.GridPosition}";
        _visuals.SpriteRenderer.sortingOrder = -(int)(transform.localPosition.y * 100) + (int)transform.localPosition.x;
       if (TryGetComponent<ExpressionController>(out var ec))
        {
            _expressionController = ec;
        }
        if (TryGetComponent<SpriteController>(out var sc))
        {
            _spriteController = sc;
        }
    }
    // private void Update()
    // {
    //     transform.position = GridUtility.GridToWorldWithBlobOffset(_model.GridPosition);
    // }
   
    public void UpdateView()
    {
        
    }
     public void ChangeSortingLayer(string layerName)
    {
        _spriteController.ChangeSortingLayer(layerName, transform);
    }

    public void ShowExpression(ExpressionType type)
    {
        if (_expressionController == null) return;
        _expressionController.ShowExpression(type);
    }
}
