using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Direction
{
    public static Vector3[] cardinalDirections = new Vector3[4]
    {
        //up
        new Vector3(0,1),
        //left
        new Vector3(-1,0),
        //right
        new Vector3(1,0),
        //down
        new Vector3(0,-1),
    };
    public int x {get; private set;}
    public int y {get; private set;}
    
    public Direction(int x, int y){
        this.x = x;
        this.y = y;
    }

    public static Direction FromMove(Move move){
        int x = move.end.x - move.start.x ;
        int y = move.end.y - move.start.y ;

        
        return new Direction(x, y);
    }

    public static Direction GetMoveValue(Move move){
        int x = 0;
        int y =0;
        if(move.end.x - move.start.x > 0){
            x = 1;
        }
        else if (move.end.x - move.start.x < 0){
            x = -1;
        }

        if (move.end.y - move.start.y > 0){
            y = 1;
        }
        else if (move.end.y - move.start.y < 0){
            y = -1;
        }
        
        
        return new Direction(x, y);
    }



    public static Direction GetOppositeDirection(Direction direction){
        return new Direction(-direction.x, -direction.y);
    }

    public override string ToString(){
        return "(" + x + ", " + y + ")";
    }

    
}
