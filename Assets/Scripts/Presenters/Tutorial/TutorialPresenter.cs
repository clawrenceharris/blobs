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
public class TutorialPresenter
{

    public TutorialModel _model;
    private TutorialStep[] _tutorialSteps;
    public bool IsFinished => _model.IsFinished;
    private List<IBlobPresenter> _blobs;

    public event Action<TutorialStep> OnNextTutorialStep;
    public event Action OnTutorialComplete;

    private IBlobPresenter CurrentStartBlob => _model.GetStartBlobAtStep(_model.CurrentStep);
    private IBlobPresenter CurrentEndBlob => _model.GetEndBlobAtStep(_model.CurrentStep);


    private void InitializeTutorial(IBoardPresenter board, TutorialStep[] steps)
    {
        _tutorialSteps = steps;
        _model = new TutorialModel();
        _blobs = board.GetAllBlobs();
        _tutorialSteps = steps;
        _model.InitializeTutorial(_tutorialSteps, board);
        _model.OnNextTutorialStep += step => OnNextTutorialStep?.Invoke(step);
        _model.OnTutorialComplete += () => OnTutorialComplete?.Invoke();
    }
    public void StartTutorial(IBoardPresenter board, LevelData level)
    {
        
        InitializeTutorial(board, level.TutorialSteps);
        DisableAllBlobs();
        
        BoardPresenter.OnMergeAnimationComplete += OnMergeAnimationComplete;
        InputService.BlobClicked += HandleBlobClicked;
        CurrentStartBlob?.EnableBlob();

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

    private void OnMergeAnimationComplete(IAction cmd)
    {
    
        // Disable all blobs to prevent player from interacting with the wrong blob
        DisableAllBlobs(); 

        _model.NextTutorialStep();
        CurrentStartBlob?.EnableBlob();
       
    }
  
    
    
    
    public void StopTutorial()
    {
        MergeInvoker.OnMergeExecuted -= OnMergeAnimationComplete;

        EnableAllBlobs();
        
        Debug.Log("Tutorial stopped");
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