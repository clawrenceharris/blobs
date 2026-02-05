using System;
using Unity.VisualScripting;
using UnityEngine;


/// <summary>
/// This class handles the tutorial logic for the game. 
/// It will guide players through the initial steps of gameplay.
/// </summary>
public class TutorialModel
{

    private int _index;
    private TutorialStep[] _tutorialSteps;
    private IBoardPresenter _board;
    public TutorialStep CurrentStep => _tutorialSteps[Mathf.Clamp(_index, 0, _tutorialSteps.Length -1)];
    public Action<Blob, Blob> OnBlobsHighlighted;
    public bool IsFinished => _index >= _tutorialSteps.Length;
    public void InitializeTutorial(TutorialStep[] tutorialSteps, IBoardPresenter board)
    {
        _index = 0;
        _tutorialSteps = tutorialSteps;
        _board = board;

    }
    public IBlobPresenter GetStartBlobAtStep(TutorialStep step)
    {
        return _board.GetBlobAt(step.startX, step.startY);
    }
    public IBlobPresenter GetEndBlobAtStep(TutorialStep step)
    {
        return _board.GetBlobAt(step.endX, step.endY);
    }
    public bool IsValidMove(Blob source, Blob target)
    {
        var startBlob = GetStartBlobAtStep(CurrentStep);
        var endBlob = GetEndBlobAtStep(CurrentStep);
        
        if (startBlob != null && endBlob != null)
        {
            if (source.ID != startBlob.Model.ID || target.ID != endBlob.Model.ID)
            {
                return false;
            }

        }
        return true;
    }
    public void NextTutorialStep()
    {
        _index = Mathf.Clamp(_index + 1, 0, _tutorialSteps.Length - 1);
        Debug.Log("Next tutorial step: " + _index);
    }

    public void PreviousTutorialStep()
    {
        _index = Mathf.Clamp(_index - 1, 0, _tutorialSteps.Length - 1);
        Debug.Log("Previous tutorial step: " + _index);
    }
}