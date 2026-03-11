using System;
using System.Collections.Generic;
using UnityEngine;

public class SpriteController : MonoBehaviour
{
    private SpriteRenderer[] _renderers;
    protected SpriteRenderer _renderer;
    private readonly Dictionary<int, int> _sortingOrderOffsets = new();
    private string _initialSortingLayer = "Default";
    private void Awake()
    {

        _renderers = GetComponentsInChildren<SpriteRenderer>() ?? Array.Empty<SpriteRenderer>();
        if(TryGetComponent<SpriteRenderer>(out var renderer))
        {
            _renderer = renderer;
        }
        foreach (var r in _renderers)
        {
            _sortingOrderOffsets[r.GetInstanceID()] = r.sortingOrder;

        }
        _initialSortingLayer = _renderers[0].sortingLayerName;

    }
    public void UpdateSortingOrder(Vector3 position)
    {

        int baseOrder = CalculateSortingOrder(position);



        foreach (var sr in _renderers)
        {
            sr.sortingOrder = baseOrder + _sortingOrderOffsets[sr.GetInstanceID()];
        }



    }
    public void ChangeSortingLayer(string layerName, Transform transform)
    {
        foreach (Transform child in transform)
        {
            if (child.TryGetComponent<SpriteRenderer>(out var sr))
            {
                sr.sortingLayerID = SortingLayer.NameToID(layerName);
            }
            if (child.childCount > 0)
            {
                ChangeSortingLayer(layerName, child);
            }

        }

    }
    public static int CalculateSortingOrder(Vector3 position)
    {
        // Top-down sorting: higher Y values render on top (lower sorting order)
        // Using negative Y so tiles further up (higher Y) have lower sorting order
        return -(int)Mathf.Round(position.y * 100);
    }
    private void Update()
    {
        UpdateSortingOrder(transform.position);
    }
    // Track previous position to detect movement
    private Vector3 _lastPosition;
    private bool _isForeground = false;

    private void LateUpdate()
    {
        // Detect if the blob has moved since the last frame
        if (transform.position != _lastPosition)
        {
            if (!_isForeground)
            {
                ChangeSortingLayer("Foreground", transform);
                _isForeground = true;
            }
        }
        else
        {
            if (_isForeground)
            {
                ChangeSortingLayer(_initialSortingLayer, transform); // Assumes default sorting layer is named "Default"
                _isForeground = false;
            }
        }

        _lastPosition = transform.position;
    }
       
    

}