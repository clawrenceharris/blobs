using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class TutorialPresenter : MonoBehaviour
{
    // This class is responsible for managing the tutorial flow and interactions.
    // It will handle the display of tutorial messages, user inputs, and progression through the tutorial steps.
    public TutorialLogic TutorialLogic { get;  private set;}
    private LevelManager _levelManager;
    private BoardPresenter _board;
    private TutorialStep[] _tutorialSteps;
    private readonly float _offsetX = -0.2f;
    private readonly float _offsetY = -0.4f;
    private float _elapsedTime = 0;
    private readonly float _cooldown = 6f;
    private List<Blob> _blobs;

    [SerializeField] private GameObject _tutorialPointerPrefab;

    private SpriteRenderer _tutorialPointerSprite;
    [SerializeField] private CanvasGroup TutorialPanel;

    [SerializeField] private TextMeshProUGUI _topText;

    [SerializeField] private TextMeshProUGUI _bottomText;


   

   
    private void Awake()
    {
        _levelManager = FindFirstObjectByType<LevelManager>();
        _board = FindFirstObjectByType<BoardPresenter>();
    }
   
    public void InitializeTutorial()
    {
        ResetTutorial();
        if (!_levelManager.IsTutorial) return;
        TutorialLogic = new TutorialLogic();

        _tutorialSteps = _levelManager.Level.tutorialSteps;

        _blobs = _board.BoardLogic.GetAllBlobs();

        TutorialLogic.InitializeTutorial(_tutorialSteps, _board.BoardLogic);

        Setup();
        TutorialLogic.StartTutorial();
        StartCoroutine(UpdateMessages());

    }


    private void Setup()
    {
        DisableAllBlobs();

        _tutorialPointerSprite = Instantiate(_tutorialPointerPrefab, transform).GetComponent<SpriteRenderer>();
        var color = _tutorialPointerSprite.color;
        color.a = 0;
        _tutorialPointerSprite.color = color;
        BoardPresenter.OnMergeComplete += HandleMergeComplete;
        TutorialLogic.OnBlobsHighlighted += HandleBlobsHighlighted;

    }
    
    private void Update()
    {
        
        if (TutorialLogic == null || TutorialLogic.IsFinished)
        {
            return;
        }
        _elapsedTime += Time.deltaTime;
        if (_elapsedTime > _cooldown)
        {
            _elapsedTime = 0;


            StartCoroutine(ShowPointer());
        } 
    }
   
    private void HandleBlobsHighlighted(Blob startBlob, Blob endBlob)
    {

        if (_board.GetBlobView(startBlob.ID) is { } startView)
        {
            startView.Input.enabled = true;

        }
        if (_board.GetBlobView(endBlob.ID) is { } endView)
        {
            endView.Input.enabled = true;

        }
    }

    private void HandleMergeComplete(MergePlan plan)
    {
        // A completion callback may have already reset this tutorial.
        if (TutorialLogic == null || TutorialLogic.IsFinished) return;
        StopAllCoroutines();
        TutorialPanel.DOKill();
        HidePointer();
        _elapsedTime = 0;
        DisableBlob(TutorialLogic.StartBlob);
        DisableBlob(TutorialLogic.EndBlob);
        TutorialLogic.NextTutorialStep();
        if (TutorialLogic.IsFinished) EnableAllBlobs();
        StartCoroutine(UpdateMessages());
    }

    public void ResetTutorial()
    {
        BoardPresenter.OnMergeComplete -= HandleMergeComplete;
        if (TutorialLogic != null)
            TutorialLogic.OnBlobsHighlighted -= HandleBlobsHighlighted;
        StopAllCoroutines();
        if (TutorialPanel != null)
        {
            TutorialPanel.DOKill();
            TutorialPanel.alpha = 0;
        }
        if (_tutorialPointerSprite != null)
        {
            _tutorialPointerSprite.DOKill();
            _tutorialPointerSprite.transform.DOKill();
            _tutorialPointerSprite.gameObject.SetActive(false);
            Destroy(_tutorialPointerSprite.gameObject);
        }
        _tutorialPointerSprite = null;
        if (_topText != null) _topText.text = "";
        if (_bottomText != null) _bottomText.text = "";
        _elapsedTime = 0;
        TutorialLogic = null;
        _tutorialSteps = null;
        _blobs = null;
    }

    private void OnDisable()
    {
        ResetTutorial();
    }

    private IEnumerator UpdateMessages()
    {
        yield return FadeOut();

        if (!TutorialLogic.IsFinished)
        {

            _topText.text = TutorialLogic.CurrentStep.topText;
            _bottomText.text = TutorialLogic.CurrentStep.bottomText;
            yield return FadeIn();

        }
        else
        {
            _topText.text = "";
            _bottomText.text = "";
        }

    }

    private IEnumerator FadeIn(float duration = 0.6f)
    {

        yield return TutorialPanel.DOFade(1, duration).WaitForCompletion();
    }
    private IEnumerator FadeOut(float duration = 0.6f)
    {

        yield return TutorialPanel.DOFade(0, duration).WaitForCompletion();

    }

    public IEnumerator ShowPointer()
    {
        
        _tutorialPointerSprite.DOFade(1, 0.3f);
        Vector2 startPosition = new(TutorialLogic.StartBlob.GridPosition.x + _offsetX, TutorialLogic.StartBlob.GridPosition.y + _offsetY);
        Vector2 endPosition = new(TutorialLogic.EndBlob.GridPosition.x + _offsetX, TutorialLogic.EndBlob.GridPosition.y + _offsetY);
        _tutorialPointerSprite.transform.position = startPosition * BoardPresenter.TileSize;
        _tutorialPointerSprite.transform.DOMove(endPosition * BoardPresenter.TileSize, 0.8f).SetEase(Ease.InOutCirc);
        yield return new WaitForSeconds(1.2f);
        HidePointer();
        
        
    }

    private void HidePointer()
    {
        if (_tutorialPointerSprite == null) return;
        _tutorialPointerSprite.DOKill();
        _tutorialPointerSprite.transform.DOKill();
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
            EnableBlob(blob);
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
            DisableBlob(blob);
        }
    }
    
     private void EnableBlob(Blob blob)
    {
        if (blob != null && _board.GetBlobView(blob.ID) is { } view)
        {
            view.Input.enabled = true;

        }
    }

    private void DisableBlob(Blob blob)
    {

        if (blob != null && _board.GetBlobView(blob.ID) is { } view)
        {
            view.Input.enabled = false;

        }


    }

    
}