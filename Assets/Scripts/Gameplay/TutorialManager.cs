using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using DG.Tweening;

public class TutorialManager : MonoBehaviour
{

    [SerializeField] private TextMeshProUGUI _topText;
    [SerializeField] private CanvasGroup TutorialPanel;

    [SerializeField] private TextMeshProUGUI _bottomText;
    public static event Action OnTutorialComplete;
    private int _index;
    private TutorialStep[] _tutorialSteps;
    private BoardPresenter _board;
    private List<Blob> _blobs;
    public Blob StartBlob { get; private set; }
    public Blob EndBlob { get; private set; }
    public bool IsFinished => _tutorialSteps == null || _index >= _tutorialSteps.Length;
    private LevelManager _levelManager;

 private readonly float _offsetX = 0.2f;
    private readonly float _offsetY = -0.5f;
    private float _elapsedTime = 0;
    private readonly float _cooldown = 6f;
    [SerializeField]private SpriteRenderer _tutorialPointer;
    

    public IEnumerator Show()
    {
        
        _tutorialPointer.DOFade(1, 0.3f);
        Vector2 startPosition = new(StartBlob.GridPosition.x + _offsetX, StartBlob.GridPosition.y + _offsetY);
        Vector2 endPosition = new(EndBlob.GridPosition.x + _offsetX, EndBlob.GridPosition.y + _offsetY);
        _tutorialPointer.transform.position = startPosition * BoardPresenter.TileSize;
        _tutorialPointer.transform.DOMove(endPosition * BoardPresenter.TileSize, 0.8f).SetEase(Ease.InOutCirc);
        yield return new WaitForSeconds(1.2f);
        Hide();
        
        
    }

    private void Hide()
    {
        _tutorialPointer.DOFade(0, 0.3f);

    }

    private void Update()
    {
        if (IsFinished)
        {
            Hide();
            return;
        }
        _elapsedTime += Time.deltaTime;
        if (_elapsedTime > _cooldown)
        {
            _elapsedTime = 0;


            StartCoroutine(Show());
            


        }

        

    }
    private void Awake()
    {
        _levelManager = FindFirstObjectByType<LevelManager>();
    }

    public void Init(BoardPresenter board)
    {
        BoardPresenter.OnMergeComplete += HandleMergeComplete;
        LevelManager.OnLevelRestart += OnLevelRestart;
        _board = board;
        _tutorialSteps = _levelManager.Level.tutorialSteps;
        _index = 0;
        _blobs = _board.BoardLogic.GetAllBlobs();
    }


    public void StartTutorial()
    {
        DisableAllBlobs();
        NextTutorialStep();

    }


    private void HighlightBlobs(TutorialStep step)
    {
        if (IsFinished)
        {
            return;
        }

        DisableBlob(StartBlob);
        DisableBlob(EndBlob);
        StartBlob = _board.BoardLogic.GetBlobAt(step.startX, step.startY);
        EndBlob = _board.BoardLogic.GetBlobAt(step.endX, step.endY);

        if (_board.GetBlobView(StartBlob.ID) is { } startView)
        {
            startView.Input.enabled = true;

        }
        if (_board.GetBlobView(EndBlob.ID) is { } endView)
        {
            endView.Input.enabled = true;

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

    private void OnLevelRestart()
    {

        _index = 0;
        NextTutorialStep();


    }


    public bool IsValidMove(Blob source, Blob target)
    {

        if (StartBlob != null && EndBlob != null)
        {
            if (source.ID != StartBlob.ID || target.ID != EndBlob.ID)
            {
                return false;
            }

        }
        return true;
    }
    private void HandleMergeComplete(MergePlan plan)
    {
        _index++;
        NextTutorialStep();
    }


    private void NextTutorialStep()
    {


        if (IsFinished)
        {

            EnableAllBlobs();
            StartCoroutine(SetMessages());
            return;

        }

        TutorialStep step = _tutorialSteps[_index];
        HighlightBlobs(step);
        StartCoroutine(SetMessages());
    }
    private IEnumerator SetMessages()
    {
        yield return FadeOut();

        if (_index < _tutorialSteps.Length)
        {

            _topText.text = _tutorialSteps[_index].topText;
            _bottomText.text = _tutorialSteps[_index].bottomText;
            yield return FadeIn();

        }
        else if (_index == _tutorialSteps.Length)
        {
            _topText.text = "";
            _bottomText.text = "";
        }

    }

    private IEnumerator FadeIn(float duration = 0.6f)
    {
    
        yield return  TutorialPanel.DOFade(1, duration).WaitForCompletion();
    }
    private IEnumerator FadeOut(float duration = 0.6f)
    {
        
        yield return  TutorialPanel.DOFade(0, duration).WaitForCompletion();

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

}


