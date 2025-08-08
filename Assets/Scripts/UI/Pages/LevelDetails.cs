using System;
using TMPro;
using UnityEngine;

public class LevelDetails : MonoBehaviour
{
    [SerializeField]private TextMeshProUGUI appleCountText;
    [SerializeField]private TextMeshProUGUI scoreText;

    private void Start()
    {
        ScoreManager.OnScore += OnScore;
    }

    // private void OnAppleCollected(int appleCount)
    // {
    //     appleCountText.text = "Apples: " + appleCount.ToString() + "/" + ScoreManager.TotalApples;
    // }

    private void OnScore(int score)
    {
        scoreText.text = "Score: " + score.ToString();
    }
}