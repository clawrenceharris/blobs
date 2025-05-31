using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

public class SpawnAction : IAction
{
    private RemoveAction removeAction;
    public delegate void OnBlobSpawned(Blob blob);
    public static event OnBlobSpawned onBlobSpawned;
    private delegate void OnSpwanUndon(Blob blob);
    public static event OnBlobSpawned onSpawnUndone;
    private Blob _blob;
    private BlobColor _trailColor;
    private int _size;
    private Type.BlobType _type;
    private Position _position;
    private BlobColor _color;
    public float waitTime {get; private set;} = 0.1f;

    public SpawnAction(Position position, BlobColor color, int size, Type.BlobType type){
        _position = position;
        _color = color;
        _size = size;
        _type = type;

    }

    
    public SpawnAction(Position position, BlobColor color, Type.BlobType type)
    {
        _position = position;
        _color = color;
        _type = type;

    }

    //trail blob constructor
    public SpawnAction(Position position, BlobColor color, BlobColor trailColor, int size)
    {
        _position = position;
        _color = color;
        _size = size;
        _type = Type.BlobType.Trail;
        _trailColor = trailColor;
    }

    public void Execute(Board board)
    {

        _blob = board.InitBlob(_position, _color, _size, _type);
        
        //makes the blob appear
        _blob.DoMerge();

        
        _blob.name = "Blob-(" + _blob.Position.x + ", " + _blob.Position.y + ")";
        _blob.transform.parent = board.BlobsTransform;

        onBlobSpawned?.Invoke(_blob);

    }

    public void Undo(Board board)
    {
        removeAction = new(_blob);
        removeAction.Execute(board);

        onSpawnUndone?.Invoke(_blob);
    }
}
