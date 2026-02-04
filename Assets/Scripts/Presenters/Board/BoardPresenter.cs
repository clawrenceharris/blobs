
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Blobs.Core.Merge;
using Blobs.Utilities;
using DG.Tweening;
using UnityEngine;


public class BoardLayout : IBoardLayout
{

    public Vector2Int WorldToGrid(float worldX, float worldY)
    {
        float x = (worldX / (TilePresenter.TileSize / 2) + worldY / (TilePresenter.TileSize / 4)) / 2f;
        float y = (worldY / (TilePresenter.TileSize / 2) - worldX / (TilePresenter.TileSize / 4)) / 2f;
        return new Vector2Int(Mathf.FloorToInt(x), Mathf.FloorToInt(y));
    }
    public Vector2Int WorldToGrid(Vector3 worldPos)
    {

        return WorldToGrid(worldPos.x, worldPos.y);
    }

    public Vector3 GridToWorld(Vector2Int gridPos)
    {
        return GridToWorld(gridPos.x, gridPos.y);
    }
    public Vector3 GridToWorld(int gridX, int gridY)
    {
        return new Vector2(
            (gridX - gridY) * TilePresenter.TileSize / 2f,
            (gridX + gridY) * TilePresenter.TileSize / 4f
        );
    }
    public Vector2Int WorldToGridWithBlobOffset(Vector3 worldPos)
    {
      
        return WorldToGridWithBlobOffset(worldPos.x, worldPos.y);
    }
    public Vector2Int WorldToGridWithBlobOffset(float worldX, float worldY)
    {
        return WorldToGrid(new Vector3(worldX, worldY - BlobPresenter.BlobOffsetY, 0));
    }
    public Vector3 GridToWorldWithBlobOffset(Vector2Int gridPos)
    {
        var worldPos = GridToWorld(gridPos.x, gridPos.y);
        return new Vector2(worldPos.x, worldPos.y + BlobPresenter.BlobOffsetY);
    }
    public Vector3 GridToWorldWithBlobOffset(int x, int y)
    {
        var worldPos = GridToWorld(x, y);
        return new Vector2(worldPos.x, worldPos.y + BlobPresenter.BlobOffsetY);
    }
}
public class BoardPresenter : MonoBehaviour, IBoardPresenter
{
    // Board State
    public BoardModel Model { get; private set; }

    public int Width => Model.Width;

    public int Height => Model.Height;

    public LevelData CurrentLevel => throw new NotImplementedException();
    
    
    // Layout
    public IBoardLayout Layout => new BoardLayout();


    // Merge Events
    public static Action<MergeAction> OnMergeStart;
    public static Action<MergeAction> OnMergeComplete;
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
        if (Model != null)
        {
            Model.OnBlobCreated -= HandleBlobCreated;
            Model.OnTileCreated -= HandleTileCreated;
        }


    }

    #endregion
    


    #region Event handlers
    private void HandleMergeExecuted(MergeAction action)
    {
        OnMergeStart?.Invoke(action);
        CoroutineHandler.StartStaticCoroutine(MergePlanAnimator.AnimatePlan(action.Plan, this), () =>
        {
            OnMergeComplete?.Invoke(action);
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

    #endregion



    #region  Initialization
    public void Initialize(LevelData level)
    {
        Model = new BoardModel(level.Width, level.Height); 
        
        Model.OnBlobCreated += HandleBlobCreated;
        Model.OnTileCreated += HandleTileCreated;
        SetUpBoard(level);
        StartCoroutine(AnimateInitialBlobs());
        OnBoardInitialized?.Invoke(this);

    }

    private void SetUpBoard(LevelData level)
    {

        List<Blob> blobs = level.Blobs.ToList();
        List<Tile> tiles = level.Tiles.ToList();

        Model.CreateInitialBoard(blobs, tiles);
        Model.LinkLasers(level);
        _laserBeam.Setup(this);

    }
    #endregion
   

    #region Event Handlers
    private void HandleBlobCreated(Blob blob)
    {

        int gridX = blob.GridPosition.x;
        int gridY = blob.GridPosition.y;
        Vector3 isoPosition = GridUtility.GridToIsoWithBlobOffset(gridX, gridY);

        var view = Instantiate(PrefabLibrary.Instance.FromBlobType(blob.Type), isoPosition, Quaternion.identity, transform);

        // Link the view to its data model
        view.Setup(blob);

        var presenter = BlobFactory.CreateBlobPresenter(view);
        presenter.Initialize(this);
        _blobs.Add(blob.ID, presenter);
    }


    private void HandleTileCreated(Tile tile)
    {
        int gridX = tile.GridPosition.x;
        int gridY = tile.GridPosition.y;

        Vector3 isoPosition = GridUtility.GridToIso(gridX, gridY);
        TileView view = Instantiate(PrefabLibrary.Instance.FromTileType(tile.Type), isoPosition, Quaternion.identity, transform);

        // Link the view to its data model
        view.Setup(tile, this);
        var presenter = TileFactory.CreateTilePresenter(view);
        presenter.Initialize(this);
        _tiles.Add(tile.ID, presenter);
    }



    #endregion




    #region Animation

    private IEnumerator AnimateInitialBlobs()
    {
        foreach (IBlobPresenter bp in _blobs.Values)
        {
            yield return bp.Spawn().WaitForCompletion();
        }
    }




    public IEnumerator AnimateEndTurnSequence()
    {

        // Add the dramatic pause
        yield return new WaitForSeconds(1.5f);
        var blobPresenters = new List<IBlobPresenter>(_blobs.Values);
        foreach (var bp in blobPresenters)
        {
            yield return bp.Remove();
        }

        yield return new WaitForSeconds(0.3f);
        var tilePresenters = new List<ITilePresenter>(_tiles.Values);
        foreach (var tp in tilePresenters)
        {
            yield return tp.Remove(0.3f).WaitForCompletion();
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

    public int GetPlayableBlobCount()
    {
        return Model.GetAllBlobs().OfType<IClearable>().Count();
    }


    public IBlobPresenter GetBlobAt(Vector2Int position) => GetBlobAt(position.x, position.y);

    public IBlobPresenter GetBlobAt(int x, int y)
    {

        var blob = Model.GetBlobAt(x, y);
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

        var tile = Model.GetTileAt(x, y);
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
        Model.MoveBlob(id, endPosition);
    }

    public void PlaceBlob(Blob blob)
    {
        Model.PlaceBlob(blob);
    }
    public void RemoveBlob(string id)
    {
        _blobs.Remove(id);
        Model.RemoveBlob(id);
        if (_blobs.Count == 0)
        {

            OnBoardCleared?.Invoke();

        }
    }
    #endregion

    #region Tile Management

    public void RemoveTile(string id)
    {
        _tiles.Remove(id);
        Model.RemoveTile(id);

    }

    public void PlaceTile(Tile tile)
    {
        throw new NotImplementedException();
    }

    public ITilePresenter GetTile(string id)
    {
        throw new NotImplementedException();
    }

    public bool IsValidPosition(Vector2Int position)
    {
        return Model.IsValidPosition(position);
    }

    public bool IsLaserBlocking(IBlobPresenter blob, Vector2Int position)
    {
        return Model.IsLaserBlocking(blob.Model.ID, position);
    }


    #endregion


}

