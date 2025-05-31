using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SubmoveAction : IAction{
    
    public  Move Move {get; private set;} 
    public  Blob Blob {get; private set;}
    public bool IsStriped {get; private set;}
  
    public List<IAction> actions = new();
    
    public float waitTime {get; private set;} = 0.4f;
    public SubmoveAction(Blob blob, Move move){
        
        Blob = blob;
        Move = move;
    }

    private void AddAction(Board board){
        Position end = Move.end;
        Tile moveTile = board.GetTileAt(end);
        Blob targetBlob = board.GetBlobAt(end);
        //TODO: check if a laser is on the target move tile. if so assign the remove action

        //adds the remove action if the blob moves onto a spike tile
        if (moveTile.TileType == Type.TileType.Spike){
            actions.Add(new RemoveAction(Blob));
        }

        //adds the merge action if there is a blob at the end position
        if(board.GetBlobAt(end)){
            actions.Add(new MergeAction(Blob, targetBlob));
        }

        //if this blob we are moving is a trail blob add the spawn action to the submove
        if (Type.IsTrailBlob(Blob.BlobType))
        {
            TrailBlob blob = Blob.GetComponent<TrailBlob>();
            actions.Add(new SpawnAction(new Position(blob.Position.x - Move.direction.x, blob.Position.y - Move.direction.y), blob.TrailColor,  blob.Size, Type.BlobType.Normal));

        }



    }

    public void Execute(Board board){

        //moves the blob to the target end position
        Blob.DoMove(Move.end);

        //adds the other necessary actions to the actions list
        AddAction(board);
    }


    public void Undo(Board board)
    {
        
        //moves the blob back to its original start position
        Blob.DoMove(Move.start);

    }


}


