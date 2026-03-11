using UnityEngine;
using System.Collections.Generic;
using Blobs.Core;
using Blobs.Utilities;
using System.Collections;
using System;


/// <summary>
/// View component for grid visual representation.
/// Handles tile creation and blob view spawning. Uses BlobViewPool for blob reuse.
/// </summary>
public class BoardView : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float _tileSize = 1.5f;
    [SerializeField] private float _blobOffsetY = -0.9f;
    public static float TileSize;
    public static float BlobOffsetY;
    private readonly Dictionary<string, TileView> _tileViews = new();
    private readonly Dictionary<string, BlobView> _blobViews = new();
    private BoardModel _boardModel;
    private BlobViewPool _blobPool;

    private void Awake()
    {
        _blobPool = FindFirstObjectByType<BlobViewPool>();
    }

    public void Initialize(BoardModel model)
    {
        _boardModel = model;
        TileSize = _tileSize;
        BlobOffsetY = _blobOffsetY;
       
    }
    private void Update()
    {
        BlobOffsetY = _blobOffsetY;

    }
    public void ClearBoard()
    {
        ClearTiles();
        ClearBlobs();
    }

    private void ClearTiles()
    {
        foreach (var tileView in _tileViews.Values)
        {
            if (tileView != null && tileView.gameObject != null)
                Destroy(tileView.gameObject);
        }
        _tileViews.Clear();
    }

    private void ClearBlobs()
    {
        if (_blobPool != null)
        {
            foreach (var blobView in _blobViews.Values)
            {
                if (blobView != null)
                    _blobPool.Release(blobView);
            }
        }
        else
        {
            foreach (var blobView in _blobViews.Values)
            {
                if (blobView != null && blobView.gameObject != null)
                    Destroy(blobView.gameObject);
            }
        }
        _blobViews.Clear();
    }

    /// <summary>
    /// Release a single blob view to the pool and remove from registry. Call when a blob is removed from the board but view may be reused (e.g. for undo).
    /// </summary>
    public void ReleaseBlobView(string blobId)
    {
        if (!_blobViews.TryGetValue(blobId, out var view)) return;
        _blobViews.Remove(blobId);
        if (_blobPool != null && view != null)
            _blobPool.Release(view);
        else if (view != null && view.gameObject != null)
            Destroy(view.gameObject);
    }

    public TileView CreateTileView(Tile tile)
    {
        int gridX = tile.GridPosition.x;
        int gridY = tile.GridPosition.y;

        Vector3 worldPos = GridUtility.GridToWorld(gridX, gridY);

        var view = Instantiate(PrefabLibrary.Instance.FromTileType(tile.Type), worldPos, Quaternion.identity, transform);
        view.Initialize(tile);
        _tileViews.TryAdd(tile.ID, view);
        return view;


    }
    public BlobView CreateBlobView(Blob blob)
    {
        int gridX = blob.GridPosition.x;
        int gridY = blob.GridPosition.y;
        Vector3 worldPos = GridUtility.GridToWorldWithBlobOffset(gridX, gridY);

        BlobView view = null;
        if (_blobPool != null)
        {
            view = _blobPool.GetById(blob.ID, transform, worldPos);
            if (view == null)
                view = _blobPool.Get(blob.Type, transform, worldPos);
        }
        if (view == null)
            view = Instantiate(PrefabLibrary.Instance.FromBlobType(blob.Type), worldPos, Quaternion.identity, transform);

        
        _blobViews.TryAdd(blob.ID, view);
        return view;
    }
}

