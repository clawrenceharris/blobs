using System.Collections;
using Blobs.Input;
using Blobs.Utilities;
using DG.Tweening;
using UnityEngine;

public class TutorialState : State<GameStateManager>
{
    private SpriteRenderer _tutorialPointer;
    private readonly float _offsetX = -0.2f;
    private readonly float _offsetY = -0.7f;
    private float _elapsedTime = 0;
    private readonly float _cooldown = 6f;
    private TutorialStep _currentStep;
    public TutorialState(GameStateManager context) : base(context)
    {
    }

    public override void EnterState()
    {
        // Enable input to allow player to interact with the game
        InputService.Gate.SetEnabled(true);


        // Get first tutorial step
        _currentStep = context.GameManager.StartingLevel.TutorialSteps[0];

        // Start tutorial
        context.Tutorial.StartTutorial(context.Board, context.GameManager.StartingLevel);


        // Update tutorial text
        context.Tutorial.OnNextTutorialStep += OnNextTutorialStep;
        context.Tutorial.OnTutorialComplete += OnTutorialComplete;
        // Hide HUD to reduce distractions during tutorial state
        context.UIManager.HudView.gameObject.SetActive(false);

        // Show tutorial pointer
        _tutorialPointer = Object.Instantiate(context.UIManager.TutorialView.TutorialPointerPrefab).GetComponent<SpriteRenderer>();
        _tutorialPointer.transform.SetParent(context.UIManager.transform);
        BoardPresenter.OnBoardSetupComplete += OnBoardSetupComplete;
    }
    private void OnBoardSetupComplete(IBoardPresenter board)
    {
        UpdateTutorialText(_currentStep);
        BoardPresenter.OnBoardSetupComplete -= OnBoardSetupComplete;
    }
    public override void UpdateState()
    {
        _elapsedTime += Time.deltaTime;
        
        if (_elapsedTime > _cooldown)
        {
            _elapsedTime = 0;
            CoroutineHandler.StartStaticCoroutine(ShowPointer(_currentStep));
        }

    }
    private void UpdateTutorialText(TutorialStep step)
    {
       
        CoroutineHandler.StartStaticCoroutine(context.UIManager.TutorialView.UpdateTutorialText(step.TopText, step.BottomText));
    }



    public IEnumerator ShowPointer(TutorialStep step)
    {
        if (step == null)
        {
            Debug.LogWarning("[TutorialState] Step is null");
            yield break;
        }
        int startX = step.StartX;
        int startY = step.StartY;
        int endX = step.EndX;
        int endY = step.EndY;

        _tutorialPointer.DOFade(1, 0.3f);


        Vector2 startPosition = GridUtility.GridToWorld(startX, startY);
        Vector2 endPosition = GridUtility.GridToWorld(endX, endY);
        _tutorialPointer.transform.position = new Vector3(startPosition.x + _offsetX, startPosition.y + _offsetY);
        _tutorialPointer.transform.DOMove(new Vector3(endPosition.x + _offsetX, endPosition.y + _offsetY), 0.8f).SetEase(Ease.InOutCirc);
        yield return new WaitForSeconds(1.2f);
        HidePointer();


    }

    private void HidePointer()
    {
        _tutorialPointer.DOFade(0, 0.3f);

    }
    private void OnNextTutorialStep(TutorialStep step)
    {
        _currentStep = step;
        UpdateTutorialText(_currentStep);
        
    }
    private void OnTutorialComplete()
    {
        context.ChangeState(new PlayingState(context));
    }
    public override void ExitState()
    {
        context.Tutorial.StopTutorial();
        context.UIManager.HudView.UndoButton.gameObject.SetActive(true);
        context.UIManager.TutorialView.HideTutorialView();
        context.Tutorial.OnNextTutorialStep -= OnNextTutorialStep;
        context.Tutorial.OnTutorialComplete -= OnTutorialComplete;
        Object.Destroy(_tutorialPointer.gameObject);

    }
    
}