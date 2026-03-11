
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;


public class BoardPresenter : MonoBehaviour, IBoardPresenter
{
    // Board State
    private BoardModel _boardModel;

    /// <summary>Exposed for MoveResolver and EffectAnimator.</summary>
    public BoardModel BoardModel => _boardModel;

    public int Width => _boardModel.Width;

    public int Height => _boardModel.Height;


    // Merge Events (ICommand for both MoveCommand and MergeAction)
    public static Action<ICommand> OnMergeAnimationStart;
    public static Action<ICommand> OnMergeAnimationComplete;
    public static Action<ICommand> OnMergeUndo;
    public static Action<ICommand> OnMergeUndoComplete;

    // Board Events

    /// <summary>
    /// Called when the board presenter, view and model is initialized
    /// </summary>
    public static event Action<IBoardPresenter> OnBoardInitialized;
    /// <summary>
    /// Called when the board is fully initialized and initial spawn animations have completed
    /// </summary>
    public static event Action<IBoardPresenter> OnBoardSetupComplete;
    // Presenters
    private LaserBeamPresenter _laserBeam;
    private readonly Dictionary<string, IBlobPresenter> _blobPresenters = new();
    private readonly Dictionary<string, IBlobPresenter> _inactiveBlobPresenters = new();
    private readonly Dictionary<string, ITilePresenter> _tilePresenters = new();

    private BoardView _boardView;

    #region Board Lifecycle
    private void Awake()
    {
        _boardView = FindFirstObjectByType<BoardView>();
        _laserBeam = FindFirstObjectByType<LaserBeamPresenter>();
    }


    private void Start()
    {

        MergeInvoker.OnMergeExecuted += OnMergeExecuted;
        MergeInvoker.OnMergeUndone += OnMergeUndone;



    }

    void OnDestroy()
    {
       
        MergeInvoker.OnMergeExecuted -= OnMergeExecuted;
        MergeInvoker.OnMergeUndone -= OnMergeUndone;
    }

    #endregion


    #region  Initialization/Setup
    public void Initialize(LevelData level)
    {
        ResolutionIds.Reset();
        ClearBoard();
        _boardModel = new BoardModel(level);
       
        if (_boardView == null)
        {
            _boardView = new GameObject("BoardView").AddComponent<BoardView>();
            _boardView.transform.SetParent(transform);

        }
        _boardView.Initialize(_boardModel);
        OnBoardInitialized?.Invoke(this);
        SetupBoard(level);

    }
    public void SetupBoard(LevelData level)
    {


        var blobs = _boardModel.CreateBlobs(level);
        var tiles = _boardModel.CreateTiles(level);
        foreach (var blob in blobs)
        {
            SpawnBlob(blob);
        }
        foreach (var tile in tiles)
        {
            SpawnTile(tile);
        }
        
        // Update all tile sprites after all tiles are spawned to ensure correct neighbor detection
        foreach (var tilePresenter in _tilePresenters.Values)
        {
            if (tilePresenter.View != null)
            {
                tilePresenter.View.UpdateTileSprite(_boardModel);
            }
        }
        
        _boardModel.LinkLasers(level);
        _laserBeam.Setup(this);
        CoroutineHandler.StartStaticCoroutine(AnimateInitialSpawns(),
        () => {
            OnBoardSetupComplete?.Invoke(this);
        });
    }
    #endregion


    #region Event Handlers

    private void OnMergeExecuted(ICommand command)
    {
        if (command is MergeCommand mergeCommand)
        {
            OnMergeAnimationStart?.Invoke(mergeCommand);
            var effectAnimator = new EffectAnimator();
            CoroutineHandler.StartStaticCoroutine(
                effectAnimator.AnimateEffects(mergeCommand.Effects, this),
                () =>
                {
                    OnMergeAnimationComplete?.Invoke(mergeCommand);
                    Debug.Log(_boardModel.ToString());

                });
        }
    }

    private void OnMergeUndone(ICommand command)
    {
        if (command is MergeCommand mergeCommand)
        {
            OnMergeUndo?.Invoke(mergeCommand);
            var effectAnimator = new EffectAnimator();
            CoroutineHandler.StartStaticCoroutine(
                effectAnimator.AnimateEffects(mergeCommand.InverseEffects, this),
                () =>
                {
                    Debug.Log(_boardModel.ToString());

                    OnMergeUndoComplete?.Invoke(mergeCommand);
                });
        }
    }

    #endregion

    #region Animation

    private IEnumerator AnimateInitialSpawns()
    {
        int tileCount = _tilePresenters.Count;
        int completedTiles = 0;
        foreach (ITilePresenter tp in _tilePresenters.Values)
        {
            tp.Enter().OnComplete(() => completedTiles++);
            yield return new WaitForSeconds(0.1f);
        }
        yield return new WaitUntil(() => completedTiles == tileCount);
        int blobCount = _blobPresenters.Count;
        int completedBlobs = 0;
        foreach (IBlobPresenter bp in _blobPresenters.Values)
        {
            bp.Spawn().OnComplete(() => completedBlobs++);
            yield return new WaitForSeconds(0.1f);
        }
        yield return new WaitUntil(() => completedBlobs == blobCount);
        
       
    }


    public IEnumerator AnimateBlobRemoval(IBlobPresenter bp)
    {
        yield return bp.Remove().WaitForCompletion();
        Destroy(bp.View.gameObject);
    }
    public IEnumerator AnimateTileRemoval(ITilePresenter tp)
    {
        yield return tp.Exit().WaitForCompletion();
        Destroy(tp.View.gameObject);
    }
    public IEnumerator AnimateEndTurnSequence()
    {

        // Dramatic pause
        yield return new WaitForSeconds(1.5f);

        var tilePresenters = new List<ITilePresenter>(_tilePresenters.Values);
        int blobCount = _blobPresenters.Count;
        int completedBlobs = 0;
        foreach (IBlobPresenter bp in _blobPresenters.Values)
        {
            bp.Remove().OnComplete(() =>
            {
                completedBlobs++;
                RemoveBlob(bp.Model.ID);
                RemoveBlobPresenter(bp);
                Destroy(bp.View.gameObject);


            });
            yield return new WaitForSeconds(0.1f);

        }
        yield return new WaitUntil(() => completedBlobs == blobCount);
        
        int tileCount = tilePresenters.Count;
        int completedTiles = 0;
        foreach (var tp in tilePresenters)
        {
            tp.Exit().OnComplete(() => {
                completedTiles++;
                RemoveTile(tp.Model.ID);
                RemoveTilePresenter(tp);
                Destroy(tp.View.gameObject);
            });
            yield return new WaitForSeconds(0.1f);
        }
        yield return new WaitUntil(() => completedTiles == tileCount);
        
    }

    #endregion





    #region  Board Management
    /// <summary>
    /// Clears the board by clearing the tile and blob presenters and the board model.
    /// </summary>
    public void ClearBoard()
    {
        _tilePresenters?.Clear();
        _blobPresenters?.Clear();
        _inactiveBlobPresenters?.Clear();
        _boardModel?.ClearBoard();
    }
    #endregion

    #region Board Queries


    /// <summary>
    /// Returns all blob presenters on the board. Includes blobs that have been removed from the board (model).
    /// Use GetPlayableBlobs to get only blobs that are on the board and active
    /// </summary>
    /// <returns>
    /// List of all blob presenters on the board.
    /// </returns>
    public List<IBlobPresenter> GetBlobPresenters() => _blobPresenters.Values.ToList();
    public List<IBlobPresenter> GetBlobsBetween(Vector2Int gridPosition1, Vector2Int gridPosition2)
    {
        var blobs = new List<IBlobPresenter>();
        for (int x = gridPosition1.x; x <= gridPosition2.x; x++)
        {
            for (int y = gridPosition1.y; y <= gridPosition2.y; y++)
            {
                var blob = GetBlobAt(x, y);
                blobs.Add(blob);
            }
        }
        return blobs;
    }

    /// <summary>
    /// Returns all playable blobs on the board.
    /// </summary>
    /// <returns>
    /// List of all playable blob presenters on the board.
    /// </returns>
    public List<IBlobPresenter> GetBlobsOnBoard() => _boardModel.GetAllBlobs().Select(b => _blobPresenters[b.ID]).ToList();



    public IBlobPresenter GetBlobAt(Vector2Int position) => GetBlobAt(position.x, position.y);

    public IBlobPresenter GetBlobAt(int x, int y)
    {

        var blob = _boardModel.GetBlobAt(x, y);
        if (blob != null)
        {
            if (_blobPresenters.TryGetValue(blob.ID, out var presenter))
            {
                return presenter;
            }
        }
        return null;
    }
    public IBlobPresenter GetBlob(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (!_blobPresenters.TryGetValue(id, out var p))
        {
            Debug.LogWarning($"[BoardPresenter] Blob {id} does not exist");
            return null;
           
        }
         return p;;

    }
    public bool BlobExists(string id) {
        if (!_blobPresenters.TryGetValue(id, out var _))
        {
            Debug.LogError($"[BoardPresenter] Blob {id} does not exist");
            return false;
        }
        if (_boardModel.GetBlob(id) == null)
        {
            Debug.LogError($"[BoardPresenter] Blob {id} does not exist");
            return false;
        }
        
        return true;
    }
    #endregion




    #region Tile Queries

    public List<ITilePresenter> GetAllTiles() => _tilePresenters.Values.ToList();

    public ITilePresenter GetTileAt(Vector2Int position) => GetTileAt(position.x, position.y);

    public ITilePresenter GetTileAt(int x, int y)
    {

        var tile = _boardModel.GetTileAt(x, y);
        if (tile != null)
        {
            if (_tilePresenters.TryGetValue(tile.ID, out var presenter))
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
        _boardModel.MoveBlob(id, endPosition);
    }

    public IBlobPresenter SpawnBlob(Blob blob)
    {
        if (_boardView == null)
        {
            Debug.LogError("[BoardPresenter] BoardView is null");
            return null;
        }

        // Reuse pooled presenter when undoing a remove (same ID)
        if (_inactiveBlobPresenters.Remove(blob.ID, out var pooledPresenter))
        {
            Debug.Log("[BoardPresenter] Respawning blob: " + blob.ID);
            RespawnBlobWith(blob, pooledPresenter);
            return pooledPresenter;
        }
       
        Debug.Log("[BoardPresenter] Spawning blob: " + blob.ID);
        BlobView view = _boardView.CreateBlobView(blob);
        var presenter = BlobFactory.CreateBlobPresenter(blob, view);
        _blobPresenters.Add(blob.ID, presenter);
        presenter.OnBlobRemoved += RemoveBlobPresenter;
        _boardModel.SpawnBlob(blob);
        return presenter;
    }

    /// <summary>
    /// Puts a pooled (inactive) presenter back on the board with a new view and model reference.
    /// </summary>
    private void RespawnBlobWith(Blob blob, IBlobPresenter presenter)
    {
        BlobView view = _boardView.CreateBlobView(blob);
        presenter.SetModel(blob);
        presenter.BindView(view);
        _blobPresenters.Add(blob.ID, presenter);
        _boardModel.RespawnBlob(blob);

        presenter.OnBlobRemoved += RemoveBlobPresenter;
    }

   
    public void RemoveBlob(string id)
    {

        _boardModel.RemoveBlob(id);


    }
    /// <summary>
    /// Called after remove animation completes. Releases the blob view to the pool and moves the presenter
    /// to the inactive set so it can be reused on undo (RespawnBlob).
    /// </summary>
    private void RemoveBlobPresenter(IBlobPresenter presenter)
    {
        Debug.Log("[BoardPresenter] Removing blob presenter: " + presenter.Model.ID);
        // Release the view to the pool (so it can be reused by ID on undo)
        if (_boardView != null)
            _boardView.ReleaseBlobView(presenter.Model.ID);

        _blobPresenters.Remove(presenter.Model.ID);
        _inactiveBlobPresenters.Add(presenter.Model.ID, presenter);
        presenter.OnBlobRemoved -= RemoveBlobPresenter;
    }

    #endregion

    #region Tile Management

    public void RemoveTile(string id)
    {
        _boardModel.RemoveTile(id);

    }
    private void RemoveTilePresenter(ITilePresenter presenter)
    {
        if (!_tilePresenters.TryGetValue(presenter.Model.ID, out var _))
        {
            Debug.LogError($"[BoardPresenter] RemoveTilePresenter: no presenter for {presenter.Model.ID}");
            return;
        }
        
        Vector2Int removedPosition = presenter.Model.GridPosition;
        _tilePresenters.Remove(presenter.Model.ID);
        
        // Update neighbors after removing this tile
        UpdateNeighborTileSprites(removedPosition);
    }

    public void SpawnTile(Tile tile)
    {
        if (_boardView == null)
        {
            Debug.LogError("[BoardPresenter] BoardView is null");
            return;
        }
        var view = _boardView.CreateTileView(tile);

        var presenter = TileFactory.CreateTilePresenter(tile, view);
        _tilePresenters.Add(tile.ID, presenter);
        _boardModel.SpawnTile(tile);
        
        // Update sprite for the new tile and its neighbors
        view.UpdateTileSprite(_boardModel);
        UpdateNeighborTileSprites(tile.GridPosition);
    }

    public ITilePresenter GetTile(string id) => _tilePresenters.TryGetValue(id, out var presenter) ? presenter : null;

    /// <summary>
    /// Updates the sprites of tiles neighboring the given position.
    /// Called when a tile is spawned or removed to refresh neighbor sprites.
    /// </summary>
    private void UpdateNeighborTileSprites(Vector2Int position)
    {
        if (_boardModel == null) return;

        // Check all 4 cardinal directions
        Vector2Int[] offsets = new Vector2Int[]
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        foreach (var offset in offsets)
        {
            Vector2Int neighborPos = position + offset;
            
            if (_boardModel.IsValidPosition(neighborPos))
            {
                Tile neighborTile = _boardModel.GetTileAt(neighborPos);
                if (neighborTile != null)
                {
                    ITilePresenter neighborPresenter = GetTileAt(neighborPos);
                    if (neighborPresenter != null && neighborPresenter.View != null)
                    {
                        neighborPresenter.View.UpdateTileSprite(_boardModel);
                    }
                }
            }
        }
    }

    public bool IsValidPosition(Vector2Int position) => _boardModel.IsValidPosition(position);

    /// <summary>Run after merge execute/undo in debug to verify grid integrity. See BoardIntegrityValidator.</summary>
    public bool ValidateBoardIntegrity(string context = "Board")
    {
        return BoardIntegrityValidator.ValidateAndLog(_boardModel, context);
    }

    public bool IsLaserBlocking(string blobId, Vector2Int position) => _boardModel.IsLaserBlocking(blobId, position);


    #endregion

    public override string ToString()
    {
        return _boardModel.ToString();
    }


}

