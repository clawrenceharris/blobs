using System;
using Unity.VisualScripting;
using UnityEngine;

public class TutorialLogic
{
    // This class will handle the tutorial logic for the game.
    // It will guide players through the initial steps of gameplay.
    public Blob StartBlob { get; private set; }
    public Blob EndBlob { get; private set; }
    private int _index;
    private TutorialStep[] _tutorialSteps;
    public bool IsFinished => _tutorialSteps == null || _index >= _tutorialSteps.Length;
    private BoardLogic _board;
    public Action<Blob, Blob> OnBlobsHighlighted;
    public TutorialStep CurrentStep { get; private set; }

    public void InitializeTutorial(TutorialStep[] tutorialSteps, BoardLogic board)
    {
        _index = 0;
        _tutorialSteps = tutorialSteps;
        _board = board;

    }
    public void StartTutorial()
    {
        StartTutorialStep(_index);

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
    public void NextTutorialStep()
    {
       
        _index++;
        StartTutorialStep(_index);
        

    }
    private void StartTutorialStep(int index)
    {
        if (!IsFinished)
        {
            TutorialStep step = _tutorialSteps[index];
            CurrentStep = step;
            HighlightBlobs(step);
        }
       
    }
    
    public void HighlightBlobs(TutorialStep step)
    {
        if (IsFinished)
        {
            return;
        }

        
        StartBlob = _board.GetBlobAt(step.startX, step.startY);
        EndBlob = _board.GetBlobAt(step.endX, step.endY);
        OnBlobsHighlighted?.Invoke(StartBlob, EndBlob);
    }
    

}