using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class GameEndedState : State
{
    private new LevelStateManager context;
    public GameEndedState(LevelStateManager context) : base(context)
    {
        this.context = context;
        
    }

    public override void EnterState()
    {
        BoardPresenter board = context.LevelManager.Board;

    }


    public override void UpdateState()
    {
    }
    
    public override void ExitState()
    {
       
    }

}