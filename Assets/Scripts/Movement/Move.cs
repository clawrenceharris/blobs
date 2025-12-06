

using UnityEngine;


/// <summary>
/// Represents a movement from a given start position 
/// to end position
/// </summary>
public class Move
{
    public Vector2Int Start { get; set; }
    public Vector2Int End { get; set; }
    public Vector2Int Direction { get; private set; }

    public Move(Vector2Int start, Vector2Int end)
    {
        Start = start;
        End = end;
        Direction = GetMoveDirection(start, end);
    }

    
    public override string ToString()
    {
        return "start: (" + Start.x + ", " + Start.y + ") end: (" + End.x + ", " + End.y + ")";
    }

    public static Vector2Int GetMoveDirection(Vector2Int start, Vector2Int end){
        int x = 0;
        int y =0;
        if(end.x - start.x > 0){
            x = 1;
        }
        else if (end.x - start.x < 0){
            x = -1;
        }

        if (end.y - start.y > 0){
            y = 1;
        }
        else if (end.y - start.y < 0){
            y = -1;
        }
        
        
        return new Vector2Int(x, y);
    }







}
