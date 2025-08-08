
using System;
using UnityEngine;


/// <summary>
/// Represents the active gameplay state where the player and enemies can move and interact.
/// </summary>
/// <remarks>
/// In this state, player and enemy movement is enabled, and collisions between the player and other objects are checked.
/// </remarks>
public class PlayingState : State
{
    private readonly BoardPresenter board;
    public PlayingState(LevelStateManager context) : base(context)
    {
        board = context.LevelManager.Board;
    }

    public override void EnterState()
    {
        BlobInput.EnableInput();

    }

   

   public override void UpdateState()
    {
       
    }
    public override void ExitState()
    {
        BlobInput.DisableInput();

    }

   
    
}