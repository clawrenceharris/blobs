using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;
using Object = UnityEngine.Object;

public class BlobPresenter
{
    /// <summary>
    /// Maps blob IDs to their GameObject views
    /// </summary>
    private static readonly Dictionary<string, BlobView> _blobViews = new();
    protected readonly BlobView _view;
    protected readonly Blob _blob;
    private readonly float _scaleDuration = 0.3f;
    private readonly float _moveDuration = 0.3f;

    protected BoardPresenter _board;
    public static readonly float BlobOffsetY =  -0.9f;
    public BlobPresenter(BlobView view)
    {
        _view = view;
        _blob = _view.Model;
        MergeModel.OnMergeComplete += HandleMergeComplete;



    }

    public void Initialize(BoardPresenter board)
    {
        _board = board;
        _blobViews.TryAdd(_blob.ID, _view);
    }

    private void HandleMergeComplete(MergePlan plan)
    {
        if (_blob.ID == plan.SourceBlob.ID || _blob.ID == plan.TargetBlob.ID)
        {
            _board.StartCoroutine(Merge());
        }
    }
    
    public virtual IEnumerator MoveBlob(Vector2Int to, Ease ease = Ease.Linear)
    {

        _view.Visuals.ChangeSortingLayer("Foreground", _view.transform);
        Vector2 isoPosition = _board.GridToIso(to.x, to.y);
        Tween tween = _view.transform.DOMove(new Vector3(isoPosition.x, isoPosition.y + BlobOffsetY), _moveDuration).SetEase(ease);
        yield return null;
    }

    public virtual IEnumerator Merge()
    {
        if(_view != null && _view.isActiveAndEnabled)
        {
            _view.Visuals.ChangeSortingLayer("Blobs", _view.transform);
            _board.StartCoroutine(_view.CreateParticles());
        }
        yield return null;
    }
    
    public virtual IEnumerator ScaleBlob()
    {
        Tween tween = _view.transform.DOScale(_blob.GetScaleFromBlobSize(), _scaleDuration).SetEase(Ease.InBack);
        yield return tween.WaitForCompletion();
    }
   
    public virtual IEnumerator RemoveBlob()
    {
        Tween tween = _view.transform.DOScale(Vector3.zero, _scaleDuration).SetEase(Ease.InBack);
        tween.OnComplete(() =>
        {
            _board.StartCoroutine(_view.CreateParticles());
            _blobViews.Remove(_blob.ID);
            _board.RemoveBlobPresenter(_blob.ID);
            Object.Destroy(_view.gameObject);
        });
        yield return null;
      
        
    }

    public virtual IEnumerator SpawnBlob()
    {
        _view.transform.DOScale(_blob.GetScaleFromBlobSize(), 0.3f);
        _board.StartCoroutine(_view.CreateParticles());
        yield return null;
        
    }
    
    public static BlobView GetBlobView(string id)
    {
        if (_blobViews.TryGetValue(id, out BlobView view))
        {
            return view;
        }
        return null;

    }
}