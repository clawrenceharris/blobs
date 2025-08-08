using System;
using System.Linq;
using UnityEngine;

public class TargetCollisionCommand : CollisionCommand
{
    public override void Execute(Blob blob, ICollidable other, BoardLogic board)
    {
        // if (other is ITarget target && blob is ColorBlob colorBlob)
        // {
           
        //     TargetRule targetRule = new();
        //     if (targetRule.Validate(colorBlob, target, board))
        //     {
        //         // board.RemoveBlob(blob);
                
        //     }

        // }
        
        base.Execute(blob, other, board);

    }
}