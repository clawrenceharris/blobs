
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
    public BoardModel BoardModel { get; private set; }
    public Dictionary<string, BlobView> BlobViews { get; private set; } // Maps blob IDs to their GameObject Views
    public Dictionary<string, TileView> TileViews { get; private set; } // Maps blob IDs to their GameObject Views
    public static readonly float TileSize = 1.2f;
    public static event Action<MergePlan> OnMergeComplete;
    private Blob _firstSelectedBlob = null;
    public static event Action<Blob> OnBlobActivated;
    private LaserBeamPresenter _laserPresenter;

    private TutorialPresenter _tutorial;

    public static event Action<Blob> OnBlobDeactivated;
    private void Awake()
    {
        _laserPresenter = FindFirstObjectByType<LaserBeamPresenter>();
        _tutorial = FindFirstObjectByType<TutorialPresenter>();
    }
    private void Start()
    {
        BoardModel.OnBlobCreated += HandleBlobCreated;
        BoardModel.OnBoardCleared += HandleBoardCleared;
        BoardModel.OnTileCreated += HandleTileCreated;
        BlobInput.OnBlobSelected += HandleBlobSelected;



    }
    public void Init(LevelManager levelManager)
    {


        BlobViews = new Dictionary<string, BlobView>();
        TileViews = new Dictionary<string, TileView>();
        LevelData level = levelManager.Level;

        BoardModel = new BoardModel(level.width, level.height);


        SetUpBoard(level);
        StartCoroutine(AnimateInitialBlobs());
    }

    void OnDestroy()
    {
        if (BoardModel != null)
        {
            BoardModel.OnBlobCreated -= HandleBlobCreated;
            BoardModel.OnTileCreated -= HandleTileCreated;
            BoardModel.OnBoardCleared -= HandleBoardCleared;

        }

    }





    private void SetUpBoard(LevelData level)
    {
        // Clear any existing views
        foreach (var view in BlobViews.Values)
        {
            Destroy(view.gameObject);
        }
        BlobViews.Clear();
        _firstSelectedBlob = null;
        List<Blob> blobs = level.blobs.ToList();
        List<Tile> tiles = level.tiles.ToList();



        BoardModel.CreateInitialBoard(blobs, tiles);
        BoardModel.LinkLasers(level);
        _laserPresenter.Setup(this);

    }
    public IEnumerator CompleteMergeCo(Blob winningBlob)
    {

        // Add the dramatic pause
        yield return new WaitForSeconds(1f);
        yield return AnimateRemove(winningBlob);

        yield return new WaitForSeconds(0.3f);
        foreach (var tile in BoardModel.TileGrid)
        {
            if (tile != null && GetTileView(tile.ID) is { } view)
            {
                CoroutineHandler.StartStaticCoroutine(view.Remove(), () =>
                {
                    Destroy(view);
                });
            }
        }
        foreach (var blob in BoardModel.BlobGrid)
        {
            if (blob != null && GetBlobView(blob.ID) is { } view)
            {
                CoroutineHandler.StartStaticCoroutine(view.Remove(scaleDuration), () =>
                {
                    Destroy(view);
                });
            }
        }

        
    }  

    private void HandleBlobCreated(Blob blob)
    {
        // Instantiate a view prefab
        Vector3 worldPosition = new(blob.GridPosition.x, blob.GridPosition.y, 0);
        BlobView view = Instantiate(GameAssets.Instance.FromBlobType(blob.Type), worldPosition * TileSize, Quaternion.identity, transform);

        // Link the view to its data and subscribe to its click event
        view.Setup(blob);
        // Store the view for later access
        BlobViews.Add(blob.ID, view);

    }

    private IEnumerator AnimateInitialBlobs()
    {
        foreach (BlobView view in BlobViews.Values)
        {
            StartCoroutine(AnimateSpawn(view.Model));
            yield return new WaitForSeconds(0.3f);
        }
    }
    private void HandleTileCreated(Tile model)
    {
        Vector3 worldPosition = new(model.GridPosition.x, model.GridPosition.y, 0);
        TileView view = Instantiate(GameAssets.Instance.FromTileType(model.TileType), worldPosition * TileSize, Quaternion.identity, transform);

        // Link the view to its data and subscribe to its click event
        view.Setup(model, this);

        // Store the view for later access
        TileViews.Add(model.ID, view);
    }
    





   
    public BlobView GetBlobView(string id)
    {
        if (BlobViews.TryGetValue(id, out BlobView view))
        {
            return view;
        }
        return null;

    }
    public TileView GetTileView(string id)
    {
        if (TileViews.TryGetValue(id, out TileView view))
        {
            return view;
        }
        return null;

    }

    private void HandleBoardCleared()
    {
        Invoke(nameof(SetUpBoard), 3f);
    }

    private IEnumerator AnimateTurnSequence(MergePlan plan)
    {

        yield return AnimateMergeFromPlan(plan);
        if (plan.DeferredPlan != null)
        {
            yield return AnimateMergeFromPlan(plan.DeferredPlan);

        }

        OnMergeComplete?.Invoke(plan);

    }
    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.Z))
        {
            MergeAction action = MergeInvoker.UndoMerge(BoardModel);
            if (action.Plan != null)
                StartCoroutine(AnimateTurnSequence(-action.Plan));
        }
    }
    
    private void HandleBlobSelected(Blob selectedBlob)
    {

        if (_firstSelectedBlob == null)
        {
            _firstSelectedBlob = selectedBlob;
            OnBlobActivated?.Invoke(selectedBlob);
        }
        else if(selectedBlob.GridPosition.Equals(_firstSelectedBlob.GridPosition))
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
            MergePlan plan = BoardModel.CalculateMergePlan(sourceBlob, targetBlob);
           
            // If the move is null it is not valid
            if (plan == null)
            {
                _firstSelectedBlob = null;
                return;
            }

            if (_tutorial.IsActivated && _tutorial.TutorialLogic.IsValidMove(sourceBlob, targetBlob))
            {
                MergeAction action = new(plan);
                MergeInvoker.ExecuteMerge(action, BoardModel);
                StartCoroutine(AnimateTurnSequence(plan));


            }



        }
    }

    #region Animation

   
    [SerializeField] private float moveDuration;
    [SerializeField] private float scaleDuration;

   


    public IEnumerator AnimateMergeFromPlan(Plan plan)
    {

        Vector2Int endPosition = plan.EndPosition;
        Vector2Int startPosition = plan.StartPosition;

        // Normal directional merge
        Vector2Int direction = plan.Direction;
        Vector2Int currentPos = startPosition;

        while (currentPos != endPosition + direction)
        {
            Ease ease = startPosition.Equals(currentPos) ? Ease.InBack : Ease.Linear;
            // Check if a blob needs to be removed at this new position
            Blob blobToRemoveOnPath = plan.BlobsToRemoveDuringMerge.FirstOrDefault(b => b.GridPosition == currentPos);
            Blob blobToCreateOnPath = plan.BlobsToCreateDuringMerge.FirstOrDefault(b => b.GridPosition == currentPos);
            yield return AnimateMove(plan.SourceBlob, currentPos, moveDuration, ease);

            if (blobToRemoveOnPath != null)
            {
                StartCoroutine(AnimateRemove(blobToRemoveOnPath));

            }
            if (blobToCreateOnPath != null)
            {
                StartCoroutine(AnimateSpawn(blobToCreateOnPath));
            }
            currentPos += direction;
        }
        if (GetBlobView(plan.SourceBlob.ID) is { } view)
        {
            view.transform.DOScale(view.GetScaleFromBlobSize(), scaleDuration);

            StartCoroutine(view.Merge());
        }

        
        foreach (Blob blob in plan.BlobsToRemoveAfterMerge)
        {
            yield return AnimateRemove(blob);
        }
        foreach (var blob in plan.BlobsToCreateAfterMerge)
        {
            yield return AnimateSpawn(blob);
        }

        plan.OnMergeComplete?.Invoke(plan);
       



    }
   
    public IEnumerator AnimateMove(Blob blob, Vector2Int toPosition, float duration = 0.5f, Ease ease = Ease.Linear)
    {

        if (GetBlobView(blob.ID) is { } view)
        {
            Vector3 worldPos = new Vector3(toPosition.x, toPosition.y, 0) * BoardPresenter.TileSize;
            yield return view.StartMove();

            yield return view.transform.DOMove(worldPos, duration).SetEase(ease).WaitForCompletion();
       
            yield return view.EndMove();


        }

    }

    public IEnumerator AnimateRemove(Blob blob)
    {
        if (GetBlobView(blob.ID) is { } view)
        {
            yield return view.Remove(scaleDuration);           
            
            BlobViews.Remove(view.ID);
            if (view != null) Destroy(view.gameObject);
        


        }
        
        
    }

    public IEnumerator AnimateSpawn(Blob blob)
    {
        if (GetBlobView(blob.ID) is { } view)
        {
            view.transform.localScale = Vector2.zero;
            yield return view.transform.DOScale(view.GetScaleFromBlobSize(), scaleDuration);
            yield return view.Spawn();
        }

    }

    #endregion


}
