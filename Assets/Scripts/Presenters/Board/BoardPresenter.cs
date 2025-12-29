
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;

public class BoardPresenter : MonoBehaviour
{
    // --- References ---
    [Header("Prefabs & Scene")]

    // --- Model & View State ---
    public BoardModel Model { get; private set; }
    private Blob _firstSelectedBlob = null;
    public static event Action<Blob> OnBlobActivated;
    private LaserBeamPresenter _laserPresenter;
    private readonly Dictionary<string, BlobPresenter> _blobPresenters = new();
    private readonly Dictionary<string, TilePresenter> _tilePresenters = new();
    private MergeModel _mergeModel;

    private LevelManager _levelManager;
    public static event Action<Blob> OnBlobDeactivated;
    private void Awake()
    {
        _levelManager = FindFirstObjectByType<LevelManager>();
        _laserPresenter = FindFirstObjectByType<LaserBeamPresenter>();
    }

    void OnDestroy()
    {
        if (Model != null)
        {
            BoardModel.OnBlobCreated -= HandleBlobCreated;
            BoardModel.OnTileCreated -= HandleTileCreated;
            BoardModel.OnBlobRemoved -= HandleBlobRemoved;
        }

    }
    private void Start()
    {
        BoardModel.OnBlobCreated += HandleBlobCreated;
        BoardModel.OnTileCreated += HandleTileCreated;

        BoardModel.OnBlobRemoved += HandleBlobRemoved;

    }

    

    public void Init(LevelManager levelManager)
    {

       
        LevelData level = levelManager.Level;
        Model = new BoardModel(level.Width, level.Height);
       
        _mergeModel = new(this);

        SetUpBoard(level);
        StartCoroutine(AnimateInitialBlobs());
    }



    private IEnumerator AnimateInitialBlobs()
    {
        foreach (BlobPresenter bp in _blobPresenters.Values)
        {
            StartCoroutine(bp.SpawnBlob());
            yield return new WaitForSeconds(0.3f);
        }
    }



    private void SetUpBoard(LevelData level)
    {        
        
        _firstSelectedBlob = null;
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
            MergeAction merge = MergeInvoker.UndoMerge(Model);
            if (merge.Plan != null)
                StartCoroutine(StartMergeSequenceReversed(merge.Plan));
        }
    }




    #region Event Handlers
    private void HandleBlobCreated(Blob blob)
    {

        int gridX = blob.GridPosition.x;
        int gridY = blob.GridPosition.y;
        Vector3 isoPosition = GridToIso(gridX, gridY);
        isoPosition.y += BlobPresenter.BlobOffsetY;

        var view = Instantiate(PrefabLibrary.Instance.FromBlobType(blob.Type), isoPosition, Quaternion.identity, transform);

        // Link the view to its data model
        view.Setup(blob);
        if (view.Input)
            view.Input.OnBlobSelected += HandleBlobSelected;

        var presenter = BlobFactory.CreateBlobPresenter(view);
        presenter.Initialize(this);
        _blobPresenters.Add(blob.ID, presenter);
    }
  
    private void HandleBlobRemoved(Blob blob)
    {
        if (BlobPresenter.GetBlobView(blob.ID) is {} view){

            view.Input.OnBlobSelected -= HandleBlobSelected;
        }
    }
    
    private void HandleTileCreated(Tile tile)
    {
        int gridX = tile.GridPosition.x;
        int gridY = tile.GridPosition.y;

        Vector3 isoPosition = GridToIso(gridX, gridY);
        TileView view = Instantiate(PrefabLibrary.Instance.FromTileType(tile.Type), isoPosition, Quaternion.identity, transform);

        // Link the view to its data model
        view.Setup(tile, this);
        var presenter = TileFactory.CreateTilePresenter(view);
        presenter.Initialize(this);
        _tilePresenters.Add(tile.ID, presenter);
    }   
    

    private void HandleBlobSelected(Blob selectedBlob)
    {

        if (_firstSelectedBlob == null)
        {
            _firstSelectedBlob = selectedBlob;
            OnBlobActivated?.Invoke(selectedBlob);
        }
        else if (selectedBlob.GridPosition.Equals(_firstSelectedBlob.GridPosition))
        {
            _firstSelectedBlob = null;
            OnBlobDeactivated?.Invoke(selectedBlob);
        }
        else
        {
            Blob sourceBlob = _firstSelectedBlob;
            Blob targetBlob = selectedBlob;
            OnBlobDeactivated?.Invoke(_firstSelectedBlob);
            _firstSelectedBlob = null;
            
            MergePlan plan = _mergeModel.CalculateMergePlan(sourceBlob, targetBlob);

            // If the move is null it is not valid
            if (plan == null)
            {
                return;
            }

            if (!_levelManager.Tutorial.IsValidMove(sourceBlob, targetBlob))
            {
                return;
            }

            MergeInvoker.ExecuteMerge(plan, Model);
            StartCoroutine(StartMergeSequence(plan));
        }
    }
    #endregion
    #region Animation

   

    private IEnumerator StartMergeSequence(MergePlan plan)
    {
        yield return _mergeModel.DoMerge(plan);
        foreach(MergePlan deferredPlan in plan.DeferredPlans)
        {
            yield return _mergeModel.DoMerge(deferredPlan);

        }

    }
    public IEnumerator AnimateEndTurnSequence()
    {

        // Add the dramatic pause
        yield return new WaitForSeconds(1.5f);
        var blobPresenters = new List<BlobPresenter>(_blobPresenters.Values);
        foreach (var bp in blobPresenters)
        {
            yield return bp.RemoveBlob();
        } 

        yield return new WaitForSeconds(0.3f);
        var tilePresenters = new List<TilePresenter>(_tilePresenters.Values);
        foreach (var tp in tilePresenters)
        {
            yield return tp.RemoveTile();
        }
    }
    private IEnumerator StartMergeSequenceReversed(MergePlan plan)
    {
        plan.DeferredPlans.Reverse();
        foreach(MergePlan deferredPlan in plan.DeferredPlans)
        {
            yield return _mergeModel.DoMerge(-deferredPlan);

        }
        
        yield return _mergeModel.DoMerge(-plan);
        
    }


    
   
    #endregion


   
    public Vector2 GridToIso(int gridX, int gridY)
    {
        return Model.GridToIso(gridX, gridY, TilePresenter.TileSize);
    }
    public Vector2Int IsoToGrid(float isoX, float isoY)
    {
        return Model.IsoToGrid(isoX, isoY, TilePresenter.TileSize);
    }

    public Vector3 GridToIso(Vector2Int position)
    {
        return GridToIso(position.x, position.y);
    }

    public BlobPresenter GetBlobPresenter(string id)
    {
        if (_blobPresenters.TryGetValue(id, out var presenter))
        {
            return presenter;
        }
        return null;
    }

    public void RemoveBlobPresenter(string id)
    {
        _blobPresenters.Remove(id);
    }

    public void RemoveTilePresenter(string id)
    {
        _tilePresenters.Remove(id);
    }
}
