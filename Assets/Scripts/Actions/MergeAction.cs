using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class MergeAction : IAction{
    public  Blob targetBlob {get; private set;}
    public bool selectedBlobIsSmaller { get; private set; }
    private RemoveAction _removeAction;
    public delegate void OnBlobMerged(MergeAction mergeAction);
    public static event OnBlobMerged onBlobMerged;
    public Blob blob {get; private set;}
    public float waitTime {get; private set;} = 0.4f;

    public MergeAction(Blob blob, Blob targetBlob){
        this.blob = blob;
        this.targetBlob = targetBlob;
    }  

    public void Execute(Board board){
        if (Type.IsMaxSizedBlob(targetBlob.BlobType))
        {
            _removeAction = new(blob);
        }


        //the size of the blob we select is >= to the target blob or we are >= the max size
        else if(blob.Size >= targetBlob.Size || blob.Size >= 4){
            //move the selected blob over top of the destination blob and grow by its size
            blob.DoMerge(targetBlob.Size);

            // remove the destination blob from the board
            _removeAction = new(targetBlob);
        }
        //if the destination blob's size is bigger than the selected blob
        else{
            selectedBlobIsSmaller = true;
            targetBlob.DoMerge(blob.Size);

            _removeAction = new(blob);
        }
        onBlobMerged?.Invoke(this);
        _removeAction.Execute(board);

    }

    /// <summary>
    /// first undoes the remove action. Then it undoes the 
    /// merge action by altering the target blob's size 
    ///</summary>
    ///<remarks>
    /// the Undoing the remove action will take care of changing the target blob's size
    ///</remarks>

    public void Undo(Board board)
    {
        _removeAction.Undo(board);

        //irregular move 
        if (selectedBlobIsSmaller)
        {
            //changes the blob that we moved to size to by the slected blob's size
            targetBlob.DoMerge(-blob.Size);

        }
        //normal move
        else
        {

            blob.DoMerge(-targetBlob.Size);


        }

    }
}
