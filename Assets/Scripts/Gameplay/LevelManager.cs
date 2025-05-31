
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using UnityEngine.UI;
using Ricimi;



public class LevelManager : MonoBehaviour


{
    public static event Action OnLevelEnd;
    public static event Action OnLevelRestart;
    public LevelData Level { get; private set; }
    [SerializeField] private bool isTutorial;
    private TutorialManager tutorialManager;
    public static int MoveCount { get; private set; } = 0;
    public bool IsHighscore{get; private set;}
    public static event Action<LevelCompletedEventArgs> OnLevelComplete;
    public Board Board { get; private set; }

    public bool IsTutorial
    {
        get
        {
            return isTutorial;
        }
    }
    private Board board;

    private static ColorScheme theme;
    
    public ColorScheme Theme
    {
        get
        {
            if(theme == null)
            {
                theme = FindFirstObjectByType<ColorScheme>();
            }
            return theme;
        }
    }


    private void Awake()
    {
        
        MoveAction.onMoveComplete += OnMoveComplete;
        MoveAction.onMoveUndone += OnMoveUndone;
        Board.OnWinStart += OnWinStart;
        Board.onBlobsCreated += OnBlobsCreated;
        board = FindFirstObjectByType<Board>();
        tutorialManager = FindFirstObjectByType<TutorialManager>();

    }



    private void Start()
    {
        

    }

    public void LogClick()
    {

        Debug.Log("Clicked");
    }


    public void StartLevel(int levelNum)
    {

        Level = LevelLoader.LoadLevelData(levelNum);
        MoveCount = 0;
        board.Init(Level);

    }
    private void OnWinStart()
    {
        int thisLevel = Level.levelNum;
        int stars = DetermineStars();
        int score = CalculateScore() + Level.scoring.gemBonus;
        PlayerData data = GameManager.PlayerData;

        //if we have not completed this level yet
        if (!data.completedLevels.Contains(thisLevel))
        {
            //adds this level to completed levels
            data.completedLevels.Add(thisLevel);

        }

        //if the level does not have any stars at all or the level's stars are
        //less than the amount just earned
        if (!data.levelStars.ContainsKey(thisLevel) || data.levelStars[thisLevel] < stars)
        {
            data.levelStars[thisLevel] = stars;
        }

        //if the level does not have points yet or the amount of points for this level
        //is less than the amount just earned 
        if (!data.levelPoints.ContainsKey(thisLevel) || data.levelPoints[thisLevel] < score)
        {
            IsHighscore = true;
            data.levelPoints[thisLevel] = score;
        }

        //Saves the updated data back into the game's player data
        GameManager.PlayerData = data;
        MenuController.Instance.ReplacePage(PageType.LevelComplete);

    }
    
    private int CalculateScore()
    {
        int baseScore = Level.scoring.baseScore;
        int movePenalty = Level.scoring.movePenalty;
       
        float score = baseScore - (movePenalty * Math.Max(0, MoveCount - Level.minMoves));
        return (int)Math.Round(score);
    }

   
    private int DetermineStars()
    {
        double score = CalculateScore();
        double percentage = score / Level.scoring.baseScore *100;
        Debug.Log(percentage + " >= " + Level.scoring.starThresholds[2]);
        
        if (percentage >= Level.scoring.starThresholds[2])
            return 3;

        
        else if (percentage >= Level.scoring.starThresholds[1])
            return 2;

        
        else if (percentage >= Level.scoring.starThresholds[0])
            return 1;

        
        else
            return 0;



    }
    private void OnBlobsCreated(Blob[,] blobs)
    {
        if (Level.tutorialSteps.Length >0)
            tutorialManager.StartTutorial(Level.tutorialSteps, board);
    }



    public void LeaveLevel()
    {   
        OnLevelEnd?.Invoke();
        SceneTransition.PerformTransition("Levels", 1, Color.black);

    }

    private void OnMoveComplete(Blob blob)
    {
        MoveCount++;
    }

   

    private void OnMoveUndone(Blob blob)
    {
        MoveCount--;

    }


    public void StartNextLevel()
    {
        OnLevelEnd?.Invoke();
        StartLevel(Level.levelNum +1);
        
    }


    public void Restart()
    {
        OnLevelRestart?.Invoke();
        StartLevel(Level.levelNum);

    }

}
