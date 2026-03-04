
using System;
using Blobs.Core;
using Blobs.Input;
using UnityEngine;


/// <summary>
/// Represents the active gameplay state where the player and enemies can move and interact.
/// </summary>
/// <remarks>
/// In this state, player and enemy movement is enabled, and collisions between the player and other objects are checked.
/// </remarks>
public class WinState : State<GameStateManager>
{
    public WinState(GameStateManager context) : base(context)
    {
    }

    public override void EnterState()
    {
        Debug.Log("[WinState] EnterState");
        InputService.Gate.SetEnabled(false);

        // Calculate score
        LevelData level = context.GameManager?.StartingLevel;
        int score = 0;
        int stars = 1; // minimum 1 star for completing the level

        if (level != null)
        {
            Scoring scoring = level.Scoring;
            score = Mathf.Max(0, scoring.BaseScore - (context.MoveCount * scoring.MovePenalty));

            // Determine stars from thresholds (higher score = more stars)
            stars = 0;
            if (scoring.StarThresholds != null)
            {
                for (int i = 0; i < scoring.StarThresholds.Length; i++)
                {
                    if (score >= scoring.StarThresholds[i])
                        stars = i + 1;
                }
            }
            stars = Mathf.Max(1, stars); // always at least 1 star
        }

        // Save progress
        int levelIndex = PlayerPrefs.GetInt("SelectedLevel", 0);
        LevelProgressManager.SetStars(levelIndex, stars);

        // Show win panel
        Debug.Log($"[WinState] Calling ShowWinPanel — UIManager: {context.UIManager != null}, WinView: {context.UIManager?.WinView != null}");
        context.UIManager?.WinView.ShowWinPanel(stars, score);
    }



    public override void UpdateState()
    {
    }
    public override void ExitState()
    {

    }



}