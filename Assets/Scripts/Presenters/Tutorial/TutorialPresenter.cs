using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// Rsponsible for managing the tutorial flow and interactions.
/// It  handles the display of tutorial messages, user inputs, and progression through the tutorial steps.
/// </summary>
public class TutorialPresenter : MonoBehaviour
{

    public TutorialModel _model;
    public bool IsActivated;
    private BoardPresenter _board;
    private TutorialStep[] _tutorialSteps;
    private readonly float _offsetX = -0.2f;
    private readonly float _offsetY = -0.7f + BlobPresenter.BlobOffsetY;
    private float _elapsedTime = 0;
    private readonly float _cooldown = 6f;
    private List<Blob> _blobs;

    [SerializeField] private GameObject _tutorialPointerPrefab;
    private SpriteRenderer _tutorialPointerSprite;
    [SerializeField] private CanvasGroup TutorialPanel;

    [SerializeField] private TextMeshProUGUI _topText;

    [SerializeField] private TextMeshProUGUI _bottomText;

    private Blob CurrentStartBlob => _model.GetStartBlobAtStep(_model.CurrentStep);
    private Blob CurrentEndBlob => _model.GetEndBlobAtStep(_model.CurrentStep);


    void Awake()
    {
        _tutorialPointerSprite = Instantiate(_tutorialPointerPrefab).GetComponent<SpriteRenderer>();

    }

    private void InitializeTutorial(BoardPresenter board, TutorialStep[] steps)
    {
        _board = board;
        _tutorialSteps = steps;
        _model = new TutorialModel();
        _blobs = board.Model.GetAllBlobs();
        _tutorialSteps = steps;
        _model.InitializeTutorial(_tutorialSteps, board.Model);

    }
    public void StartTutorial(BoardPresenter board, TutorialStep[] steps)
    {
        
        InitializeTutorial(board, steps);
        DisableAllBlobs();
        
        MergeModel.OnMergeComplete += HandleMergeComplete;
        
        CurrentStartBlob.EnableBlob();
        CurrentEndBlob.EnableBlob();

        IsActivated = true;

        CoroutineHandler.StartStaticCoroutine(UpdateMessages());
    }


   
    public void Update()
    {
        if (!IsActivated) return;
        
        _elapsedTime += Time.deltaTime;
        if (_elapsedTime > _cooldown)
        {
            _elapsedTime = 0;
            CoroutineHandler.StartStaticCoroutine(ShowPointer(CurrentStartBlob, CurrentEndBlob));
        } 
    }


    private void HandleMergeComplete(MergePlan plan)
    {
        if (_model.IsFinished) return;


        CurrentStartBlob.DisableBlob();
        CurrentEndBlob.DisableBlob();
        
        _model.NextTutorialStep();

        CurrentStartBlob.EnableBlob();
        CurrentEndBlob.EnableBlob();

        CoroutineHandler.StartStaticCoroutine(UpdateMessages());
    }
    private IEnumerator UpdateMessages()
    {
        yield return FadeOut();

        _topText.text = _model.CurrentStep.topText;
        _bottomText.text = _model.CurrentStep.bottomText;

        yield return FadeIn();

    }
    private void ClearMessages()
    {
        _topText.text = "";
        _bottomText.text = "";
    }
    public void StopTutorial()
    {
        if (!IsActivated) return;
        MergeModel.OnMergeComplete -= HandleMergeComplete;
        
        IsActivated = false;
        
        EnableAllBlobs();
        ClearMessages();
    }
    private IEnumerator FadeIn(float duration = 0.6f)
    {

        yield return TutorialPanel.DOFade(1, duration).WaitForCompletion();
    }
    private IEnumerator FadeOut(float duration = 0.6f)
    {

        yield return TutorialPanel.DOFade(0, duration).WaitForCompletion();

    }

    public IEnumerator ShowPointer(Blob startBlob, Blob endBlob)
    {
        if (startBlob == null || endBlob == null) yield break;

        _tutorialPointerSprite.DOFade(1, 0.3f);

        
        Vector2 startPosition = _board.GridToIso(startBlob.GridPosition.x, startBlob.GridPosition.y);
        Vector2 endPosition = _board.GridToIso(endBlob.GridPosition.x, endBlob.GridPosition.y);
        _tutorialPointerSprite.transform.position = new Vector3(startPosition.x + _offsetX, startPosition.y + _offsetY);
        _tutorialPointerSprite.transform.DOMove(new Vector3(endPosition.x + _offsetX, endPosition.y + _offsetY ), 0.8f).SetEase(Ease.InOutCirc);
        yield return new WaitForSeconds(1.2f);
        HidePointer();
        
        
    }

    private void HidePointer()
    {
        _tutorialPointerSprite.DOFade(0, 0.3f);

    }
    private void EnableAllBlobs()
    {
        foreach (Blob blob in _blobs)
        {
            if (blob == null)
            {
                continue;
            }
            blob.EnableBlob();
        }
    }
    private void DisableAllBlobs()
    {
        foreach (Blob blob in _blobs)
        {
            if (blob == null)
            {
                continue;
            }
            blob.DisableBlob();
        }
    }

    public bool IsValidMove(Blob sourceBlob, Blob targetBlob)
    {
        return !IsActivated || _model.IsValidMove(sourceBlob, targetBlob);
    }

    
}