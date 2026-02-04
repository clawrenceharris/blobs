
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

    public BoardModel Model { get; private set; }

    public int Width => Model.Width;

    public int Height => Model.Height;

    public LevelData CurrentLevel => throw new NotImplementedException();

    public IBoardLayout Layout => new BoardLayout();

    public static Action<MergeAction> OnMergeStart;
    public static Action<MergeAction> OnMergeComplete;
    public static Action<MergeAction> OnMergeUndo;
    public static Action<MergeAction> OnMergeUndoComplete;
    
    private LaserBeamPresenter _laserPresenter;
    private readonly Dictionary<string, IBlobPresenter> _blobPresenters = new();
    private readonly Dictionary<string, TilePresenter> _tilePresenters = new();

    private void Awake()
    {
        _laserPresenter = FindFirstObjectByType<LaserBeamPresenter>();
    }

    void OnDestroy()
    {
        if (Model != null)
        {
            BoardModel.OnBlobCreated -= HandleBlobCreated;
            BoardModel.OnTileCreated -= HandleTileCreated;
        }


    }
    private void Start()
    {
        BoardModel.OnBlobCreated += HandleBlobCreated;
        BoardModel.OnTileCreated += HandleTileCreated;
        MergeInvoker.OnMergeExecuted += HandleMergeExecuted;
        MergeInvoker.OnMergeUndone += HandleMergeUndone;

    }

    private void HandleMergeExecuted(MergeAction action)
    {
        OnMergeStart?.Invoke(action);
        CoroutineHandler.StartStaticCoroutine(MovePlanAnimator.AnimatePlan(action.Plan, this),()=>
        {
            OnMergeComplete?.Invoke(action);
        });
    }

    private void HandleMergeUndone(MergeAction action)
    {
        OnMergeUndo?.Invoke(action);
        CoroutineHandler.StartStaticCoroutine(MovePlanAnimator.AnimateUndo(action.Plan, this),()=>
        {
            OnMergeUndoComplete?.Invoke(action);
        });
    }

    public void Initialize(LevelData level)
    {
        Model = new BoardModel(level.Width, level.Height);
        SetUpBoard(level);
        StartCoroutine(AnimateInitialBlobs());
    }



    private IEnumerator AnimateInitialBlobs()
    {
        foreach (IBlobPresenter bp in _blobPresenters.Values)
        {
            yield return bp.Spawn().WaitForCompletion();
        }
    }



    private void SetUpBoard(LevelData level)
    {

        List<Blob> blobs = level.Blobs.ToList();
        List<Tile> tiles = level.Tiles.ToList();

        Model.CreateInitialBoard(blobs, tiles);
        Model.LinkLasers(level);
        _laserPresenter.Setup(this);

    }
    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.Z))
        {
            var cmd = MergeInvoker.UndoMerge();
            if (cmd is MergeAction mpc && mpc.Plan != null)
            {
                StartCoroutine(MovePlanAnimator.AnimateUndo(mpc.Plan, this));
            }
        }
    }




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
        _blobPresenters.Add(blob.ID, presenter);
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
        _tilePresenters.Add(tile.ID, presenter);
    }



    #endregion



   
    #region Animation



    
    public IEnumerator AnimateEndTurnSequence()
    {

        // Add the dramatic pause
        yield return new WaitForSeconds(1.5f);
        var blobPresenters = new List<IBlobPresenter>(_blobPresenters.Values);
        foreach (var bp in blobPresenters)
        {
            yield return bp.Remove();
        }

        yield return new WaitForSeconds(0.3f);
        var tilePresenters = new List<TilePresenter>(_tilePresenters.Values);
        foreach (var tp in tilePresenters)
        {
            yield return tp.RemoveTile();
        }
    }

    #endregion



    public void RemoveTilePresenter(string id)
    {
        _tilePresenters.Remove(id);
    }

   
    public void ClearBoard()
    {
        throw new NotImplementedException();
    }

    public IBlobPresenter GetBlobAt(Vector2Int position) => GetBlobAt(position.x, position.y);

    public IBlobPresenter GetBlobAt(int x, int y)
    {

        var blob = Model.GetBlobAt(x, y);
        if (blob != null)
        {
            if (_blobPresenters.TryGetValue(blob.ID, out var presenter))
            {
                return presenter;
            }
        }
        return null;
    }


    public void RemoveBlob(Blob blob)
    {
        _blobPresenters.Remove(blob.ID);
        Model.RemoveBlob(blob.ID);
    }

    public List<IBlobPresenter> GetAllBlobs() =>  _blobPresenters.Values.ToList();

    public int GetPlayableBlobCount()
    {
        return Model.GetAllBlobs().OfType<IClearable>().Count();
    }

    public IBlobPresenter GetBlobById(string id) => _blobPresenters.TryGetValue(id, out var p) ? p : null;
       

    public void MoveBlob(Blob blob, Vector2Int endPosition)
    {
        Model.MoveBlob(blob, endPosition);
    }

    public void PlaceBlob(Blob blob)
    {
        Model.PlaceBlob(blob);
    }
}
