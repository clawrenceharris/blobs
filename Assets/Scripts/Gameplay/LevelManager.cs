
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using UnityEngine.UI;
using System.Collections;



public class LevelManager : MonoBehaviour


{
    public static event Action OnLevelEnd;
    public static event Action OnLevelRestart;
    public int LevelNum;
    public LevelData Level { get; private set; }
    public static int MoveCount { get; private set; } = 0;
    public bool IsHighscore{get; private set;}
    public static event Action<LevelCompletedEventArgs> OnLevelComplete;
    private LevelStateManager _stateManager;
    private WinConditionSystem _winConditionSystem;

    public bool IsTutorial
    {
        get
        {
            return Level != null && Level.tutorialSteps.Length > 0;
        }
    }
    public BoardPresenter Board { get; private set; }

    private static ColorScheme _theme;
    
    public ColorScheme Theme
    {
        get
        {
            if(_theme == null)
            {
                _theme = FindFirstObjectByType<ColorScheme>();
            }
            return _theme;
        }
    }


    private void Awake()
    {
        
      
        Board = FindFirstObjectByType<BoardPresenter>();
        _stateManager = GetComponent<LevelStateManager>();
    }



    private void Start()
    {

        StartLevel(LevelNum);
        BoardModel.OnBoardCleared += OnBoardCleared;
        BoardModel.OnBlobMoved += OnBlobMoved;
        BoardPresenter.OnMergeComplete += HandleMergeComplete;
    }



    private void HandleMergeComplete(MergePlan plan)
    {
        Blob winningBlob = _winConditionSystem.CheckForWin(Board.BoardModel);
        if (winningBlob != null)
        {
            _stateManager.SetState(null);
            OnLevelComplete?.Invoke(new LevelCompletedEventArgs(LevelNum));
            CoroutineHandler.StartStaticCoroutine(Board.CompleteMergeCo(winningBlob),() => {
                
                StartLevel(LevelNum + 1);

            });

        }
    }

   
    public void StartLevel(int levelNum)
    {

        Level = LevelLoader.LoadLevelData(levelNum);
        if (Level == null) return;
        MoveCount = 0;

        Board.Init(this);

        _winConditionSystem = new WinConditionSystem();
        _stateManager.SetState(new PlayingState(_stateManager));

    }
    private void OnBoardCleared()
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
        _stateManager.SetState(null);
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
        
        if (percentage >= Level.scoring.starThresholds[2])
            return 3;
        else if (percentage >= Level.scoring.starThresholds[1])
            return 2;
        else if (percentage >= Level.scoring.starThresholds[0])
            return 1;
        else
            return 0;
    }
    


    public void LeaveLevel()
    {   
        OnLevelEnd?.Invoke();

    }

    private void OnBlobMoved(Blob blob, Vector2Int from, Vector2Int to)
    {
        MoveCount++;
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
