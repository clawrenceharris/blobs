
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Blobs.Core.Merge;
using Blobs.Utilities;
using DG.Tweening;
using UnityEngine;


public class BoardPresenter : MonoBehaviour, IBoardPresenter
{
    // Board State
    private BoardModel _model;

    public int Width => _model.Width;

    public int Height => _model.Height;

    public LevelData CurrentLevel => throw new NotImplementedException();



   

    // Merge Events
    public static Action<MergeAction> OnMergeAnimationStart;
    public static Action<MergeAction> OnMergeAnimationComplete;
    public static Action<MergeAction> OnMergeUndo;
    public static Action<MergeAction> OnMergeUndoComplete;

    // Board Events
    public static event Action OnBoardCleared;
    public static event Action<IBoardPresenter> OnBoardInitialized;

    // Presenters
    private LaserBeamPresenter _laserBeam;
    private readonly Dictionary<string, IBlobPresenter> _blobs = new();
    private readonly Dictionary<string, ITilePresenter> _tiles = new();

    private TutorialPresenter _tutorial;


    #region Board Lifecycle
    private void Awake()
    {
        _laserBeam = FindFirstObjectByType<LaserBeamPresenter>();
        _tutorial = FindFirstObjectByType<TutorialPresenter>();
    }


    private void Start()
    {

        MergeInvoker.OnMergeExecuted += HandleMergeExecuted;
        MergeInvoker.OnMergeUndone += HandleMergeUndone;
        

    }

    void OnDestroy()
    {
        if (_model != null)
        {
            _model.OnBlobSpawned -= HandleBlobSpawned;
            _model.OnTileCreated -= HandleTileCreated;
        }


    }

    #endregion
    

    #region  Initialization
    public void Initialize(LevelData level)
    {
        _model = new BoardModel(level.Width, level.Height); 
        
        _model.OnBlobSpawned += HandleBlobSpawned;
        _model.OnTileCreated += HandleTileCreated;
        
        SetupBoard(level);
        OnBoardInitialized?.Invoke(this);

    }

    private void SetupBoard(LevelData level)
    {
        var blobs = CreateBlobs(level);
        var tiles = CreateTiles(level);

        _model.CreateInitialBoard(blobs, tiles);
        _model.LinkLasers(level);
        _laserBeam.Setup(this);
        StartCoroutine(AnimateInitialBlobs());

    }
    

    public List<Blob> CreateBlobs(LevelData level)
    {
        var blobs = new List<Blob>();

        if (level.Blobs != null)
        {
            foreach (var spawn in level.Blobs)
            {
                var blob = BlobFactory.CreateBlobModel(spawn);
                if (blob != null)
                    blobs.Add(blob);
            }
        }
        
        return blobs;
    }
    public List<Tile> CreateTiles(LevelData level)
    {
        var tiles = new List<Tile>();
        var tileSpawns = BuildTileSpawns(level);
        Debug.Log($"Creating {tileSpawns.Count} tiles");
        foreach (var spawn in tileSpawns)
        {
            Debug.Log($"Creating tile: {spawn.Type} at {spawn.GridPosition}");
            var tile = TileFactory.CreateTileModel(spawn);
            if (tile != null)
                tiles.Add(tile);
            else
            {
                Debug.LogError($"Failed to create tile: {spawn.Type} at {spawn.GridPosition}");
            }
        }
        return tiles;
    }

    /// <summary>
    /// Builds the full list of tile spawns: use level.Tiles when present, otherwise one Normal per blob.
    /// When level.Tiles exists, ensures every blob position has a tile (adds Normal if missing).
    /// </summary>
    private static List<TileSpawnData> BuildTileSpawns(LevelData level)
    {
        if (level?.Blobs == null) return new List<TileSpawnData>();

        var list = level.Tiles != null && level.Tiles.Count > 0
            ? new List<TileSpawnData>(level.Tiles)
            : new List<TileSpawnData>();

        foreach (var b in level.Blobs)
        {
            if (list.Exists(t => t.GridPosition == b.GridPosition)) continue;
            list.Add(new TileSpawnData { GridPosition = b.GridPosition, Type = TileType.Normal });
        }
        return list;
    }
    #endregion


    #region Event Handlers

    private void HandleMergeExecuted(MergeAction action)
    {
        OnMergeAnimationStart?.Invoke(action);
        CoroutineHandler.StartStaticCoroutine(MergePlanAnimator.AnimatePlan(action.Plan, this), () =>
        {
            OnMergeAnimationComplete?.Invoke(action);
        });
    }

    private void HandleMergeUndone(MergeAction action)
    {
        OnMergeUndo?.Invoke(action);
        CoroutineHandler.StartStaticCoroutine(MergePlanAnimator.AnimateUndo(action.Plan, this), () =>
        {
            OnMergeUndoComplete?.Invoke(action);
        });
    }

    private void HandleBlobSpawned(Blob blob)
    {

        int gridX = blob.GridPosition.x;
        int gridY = blob.GridPosition.y;
        Vector3 worldPos = GridUtility.GridToWorldWithBlobOffset(gridX, gridY);

        var view = Instantiate(PrefabLibrary.Instance.FromBlobType(blob.Type), worldPos, Quaternion.identity, transform);
        view.Initialize(blob);

        var presenter = BlobFactory.CreateBlobPresenter(blob, view);

        presenter.Initialize(this);
        _blobs.Add(blob.ID, presenter);
    }


    private void HandleTileCreated(Tile tile)
    {
        int gridX = tile.GridPosition.x;
        int gridY = tile.GridPosition.y;

        Vector3 worldPos = GridUtility.GridToWorld(gridX, gridY);

        var view = Instantiate(PrefabLibrary.Instance.FromTileType(tile.Type), worldPos, Quaternion.identity, transform);
        view.Initialize(tile);

        
        var presenter = TileFactory.CreateTilePresenter(tile, view);
        
        presenter.Initialize(this);
        _tiles.Add(tile.ID, presenter);
    }



    #endregion




    #region Animation

    private IEnumerator AnimateInitialBlobs()
    {

        foreach (IBlobPresenter presenter in _blobs.Values)
        {
            presenter.Spawn();
            yield return new WaitForSeconds(0.2f);
        }
        
    }




    public IEnumerator AnimateEndTurnSequence()
    {

        // Add the dramatic pause
        yield return new WaitForSeconds(1.5f);
        var blobPresenters = new List<IBlobPresenter>(_blobs.Values);
        
        foreach (var bp in blobPresenters)
        {
            yield return bp.Remove().WaitForCompletion();
        }

        yield return new WaitForSeconds(0.3f);
        var tilePresenters = new List<ITilePresenter>(_tiles.Values);
        foreach (var tp in tilePresenters)
        {
            yield return tp.Remove().WaitForCompletion();
        }
    }

    #endregion





    #region  Board Management

    public void ClearBoard()
    {
        throw new NotImplementedException();
    }
    #endregion

    #region Board Queries
    public List<IBlobPresenter> GetAllBlobs() => _blobs.Values.ToList();

    public int GetPlayableBlobCount() => _model.GetAllBlobs().OfType<IClearable>().Count();


    public IBlobPresenter GetBlobAt(Vector2Int position) => GetBlobAt(position.x, position.y);

    public IBlobPresenter GetBlobAt(int x, int y)
    {

        var blob = _model.GetBlobAt(x, y);
        if (blob != null)
        {
            if (_blobs.TryGetValue(blob.ID, out var presenter))
            {
                return presenter;
            }
        }
        return null;
    }
    public IBlobPresenter GetBlob(string id) => _blobs.TryGetValue(id, out var p) ? p : null;

    #endregion




    #region Tile Queries

    public List<ITilePresenter> GetAllTiles() => _tiles.Values.ToList();

    public ITilePresenter GetTileAt(Vector2Int position) => GetTileAt(position.x, position.y);

    public ITilePresenter GetTileAt(int x, int y)
    {

        var tile = _model.GetTileAt(x, y);
        if (tile != null)
        {
            if (_tiles.TryGetValue(tile.ID, out var presenter))
            {
                return presenter;
            }
        }
        return null;
    }

    #endregion

    #region  Blob Management
    public void MoveBlob(string id, Vector2Int endPosition)
    {
        _model.MoveBlob(id, endPosition);
    }

    public void SpawnBlob(Blob blob)
    {
        _model.SpawnBlob(blob);
    }

    public void RespawnBlob(string id)
    {
        
        // we can only respawn if it existed to begin with
        if (_blobs.TryGetValue(id, out var presenter))
        {
            _model.RespawnBlob(presenter.Model);
        }
        else
        {
            Debug.LogError($"Attempted to respawn blob {id} that does not exist");
        }
    }
   
    public void RemoveBlob(string id)
    {
        _model.RemoveBlob(id);
        if (_model.BlobCount == 0)
        {
            OnBoardCleared?.Invoke();
        }
    }

   
    #endregion

    #region Tile Management

    public void RemoveTile(string id)
    {
        _tiles.Remove(id);
        _model.RemoveTile(id);

    }

    public void PlaceTile(Tile tile) => _model.PlaceTile(tile);

    public ITilePresenter GetTile(string id) => _tiles.TryGetValue(id, out var presenter) ? presenter : null;

    public bool IsValidPosition(Vector2Int position) => _model.IsValidPosition(position);

    public bool IsLaserBlocking(IBlobPresenter blob, Vector2Int position)
    {
        return _model.IsLaserBlocking(blob.Model.ID, position);
    }


    #endregion


}

