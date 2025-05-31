using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class TutorialManager : MonoBehaviour
{

   private TextMeshProUGUI topText;
   [SerializeField]private CanvasGroup TutorialPanel;

   private TextMeshProUGUI bottomText;
    public static event Action OnTutorialComplete;
    private int index;
    public bool isTutorial { get; private set; }
    private TutorialStep[] tutorialSteps;
    private Board board;
    private Blob[] blobs;
    public Blob StartBlob { get; private set; }
    public Blob EndBlob { get; private set; }


    private void Awake()
    {

        topText = GameObject.Find("Top Text").GetComponent<TextMeshProUGUI>();
        bottomText = GameObject.Find("Bottom Text").GetComponent<TextMeshProUGUI>();

    }

    private void Start()
    {
        MoveAction.onMoveComplete += OnMoveComplete;
        MoveAction.onMoveUndone += OnMoveUndone;
        LevelManager.OnLevelRestart += OnLevelRestart;
       

    }

    public void StartTutorial(TutorialStep[] tutorialSteps,Board board)
    {
        float elapsedTime = 0f;
        while (elapsedTime < 1)
        {
            elapsedTime += Time.deltaTime;
            TutorialPanel.alpha += elapsedTime;

        }
        this.tutorialSteps = tutorialSteps;
        this.board = board;
        isTutorial = true;
        
        index = 0;
        blobs = board.GetBlobsOnBoard();
        
        NextTutorialStep();
    }

   

    private void HighlightBlobs()
    {
        TutorialStep step = tutorialSteps[index];
        Blob startBlob = board.GetBlobAt(step.startX, step.startY);
        Blob endBlob = board.GetBlobAt(step.endX, step.endY);
        StartBlob = startBlob;
        EndBlob = endBlob;
        if(startBlob)
        startBlob.Disabled =false;
    }

    private void DisableBlobs()
    {
        foreach(Blob blob in blobs)
        {
           blob.Disabled = true;
        }

    }

    private void OnLevelRestart()
    {
        if (isTutorial)
        {
            //reset index to zero
            index = 0;
            NextTutorialStep();
        }
        
    }


    private void OnMoveUndone(Blob blob)
    {
        if (isTutorial)
        {
            //go back one step
            index--;

            NextTutorialStep();
        }


    }
    private void OnMoveComplete(Blob blob)
    {
        if (isTutorial)
        {
            //move forward one step
            index++;

            NextTutorialStep();
        }



    }

    private void NextTutorialStep()
    {
        

        DisableBlobs();
        SetMessages();

        if (index < tutorialSteps.Length)
        {
            HighlightBlobs();
        }
       


    }
    private void SetMessages()
    {
        if(index < tutorialSteps.Length)
        {
            topText.text = tutorialSteps[index].topText;
            bottomText.text = tutorialSteps[index].bottomText;

        }
        else if(index == tutorialSteps.Length)
        {
            topText.text = "";
            bottomText.text = "";
        }

    }

}


