using System;
using System.Collections;
using System.Collections.Generic;
using Blobs.Core.Merge;
using Blobs.Input;
using Blobs.Utilities;
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
    private TutorialStep[] _tutorialSteps;
    private readonly float _offsetX = -0.2f;
    private readonly float _offsetY = -0.7f + BlobPresenter.BlobOffsetY;
    private float _elapsedTime = 0;
    private readonly float _cooldown = 6f;
    private List<IBlobPresenter> _blobs;

    [SerializeField] private GameObject _tutorialPointerPrefab;
    private SpriteRenderer _tutorialPointerSprite;
    [SerializeField] private CanvasGroup TutorialPanel;

    [SerializeField] private TextMeshProUGUI _topText;

    [SerializeField] private TextMeshProUGUI _bottomText;

    private IBlobPresenter CurrentStartBlob => _model.GetStartBlobAtStep(_model.CurrentStep);
    private IBlobPresenter CurrentEndBlob => _model.GetEndBlobAtStep(_model.CurrentStep);


    void Awake()
    {
        _tutorialPointerSprite = Instantiate(_tutorialPointerPrefab).GetComponent<SpriteRenderer>();

    }

    private void InitializeTutorial(BoardPresenter board, TutorialStep[] steps)
    {
        _tutorialSteps = steps;
        _model = new TutorialModel();
        _blobs = board.GetAllBlobs();
        _tutorialSteps = steps;
        _model.InitializeTutorial(_tutorialSteps, board);

    }
    public void TryStartTutorial(BoardPresenter board, LevelData startingLevel)
    {
        if (startingLevel.IsTutorial)
        {
            
            StartTutorial(board, startingLevel);
        }
        
    }
    public void StartTutorial(BoardPresenter board, LevelData level)
    {
        
        InitializeTutorial(board, level.TutorialSteps);
        DisableAllBlobs();
        
        MergeInvoker.OnMergeExecuted += HandleMergeExecuted;
        InputService.BlobClicked += HandleBlobClicked;
        CurrentStartBlob?.EnableBlob();

        IsActivated = true;

        CoroutineHandler.StartStaticCoroutine(UpdateMessages());
    }

    private void HandleBlobClicked(BlobView view)
    {
        if (CurrentEndBlob == null || CurrentStartBlob == null) return;
        if (view.Model.ID == CurrentStartBlob.Model.ID && !CurrentEndBlob.Enabled && CurrentStartBlob.Enabled)
        {
            CurrentEndBlob.EnableBlob();
        }
        else
        {
            CurrentEndBlob.DisableBlob();
        }

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


    private void HandleMergeExecuted(IAction cmd)
    {
        if (_model.IsFinished) {
            StopTutorial();
            return;
        }
        DisableAllBlobs();
        _model.NextTutorialStep();
        
        CurrentStartBlob?.EnableBlob();
        CoroutineHandler.StartStaticCoroutine(UpdateMessages());
       
    }
    
    
    
    private IEnumerator UpdateMessages()
    {
        yield return FadeOut();
        _topText.text = _model.CurrentStep.TopText;
        _bottomText.text = _model.CurrentStep.BottomText;

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
        MergeInvoker.OnMergeExecuted -= HandleMergeExecuted;

        IsActivated = false;

        EnableAllBlobs();
        ClearMessages();
        Debug.Log("Tutorial stopped");
    }
    private IEnumerator FadeIn(float duration = 0.6f)
    {

        yield return TutorialPanel.DOFade(1, duration).WaitForCompletion();
    }
    private IEnumerator FadeOut(float duration = 0.6f)
    {

        yield return TutorialPanel.DOFade(0, duration).WaitForCompletion();

    }

    public IEnumerator ShowPointer(IBlobPresenter startBlob, IBlobPresenter endBlob)
    {
        if (startBlob == null || endBlob == null) yield break;

        _tutorialPointerSprite.DOFade(1, 0.3f);

        
        Vector2 startPosition = GridUtility.GridToWorld(startBlob.Model.GridPosition.x, startBlob.Model.GridPosition.y);
        Vector2 endPosition = GridUtility.GridToWorld(endBlob.Model.GridPosition.x, endBlob.Model.GridPosition.y);
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
        foreach (IBlobPresenter blob in _blobs)
        {
            blob?.EnableBlob();
        }
    }
    private void DisableAllBlobs()
    {
        foreach (IBlobPresenter blob in _blobs)
        { 
            blob?.DisableBlob();
        }
    }

    public bool IsValidMove(Blob sourceBlob, Blob targetBlob)
    {
        return _model.IsValidMove(sourceBlob, targetBlob);
    }

    
}