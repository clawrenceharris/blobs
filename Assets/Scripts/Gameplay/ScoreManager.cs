using System;
using UnityEngine;

public static class PointsData
{
    public static int PelletPoints = 10;
    public static int PowerPelletPoints = 30;

    public static int EnemyPoints = 40;

    
}

public class ScoreManager : MonoBehaviour
{
   
    public int score;
    private int cheeseCount;
    private int enemyCount;

    public static int TotalPellets;
    public static int TotalPowerPellets;
    public int Score
    {
        get
        {
            return score;
        }
        private set
        {
            score = value;
            OnScore?.Invoke(score);

        }
    }
    public static event Action<int> OnScore;
    public int PelletCount
    {
        get
        {
            return cheeseCount;
        }
        private set
        {
            cheeseCount = value;
            OnCheeseCollected?.Invoke(cheeseCount);

        }
    }
    public static event Action<int> OnCheeseCollected;


   
    private void OnBoardCreated(BoardPresenter board)
    {
    }

    
    public void Reset()
    {
        Score = 0;
        PelletCount = 0;
    }
   


   
    

   

    public int GetMaxScore()
    {
        int maxScore = 0;
        maxScore += enemyCount * 20;
        maxScore += TotalPellets * PointsData.PelletPoints;
        maxScore += TotalPowerPellets * PointsData.PowerPelletPoints;
        return maxScore;
        
    }
}