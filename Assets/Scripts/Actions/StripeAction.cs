// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;

// public class StripeAction : IAction
// {
//     private readonly ColorBlob blob;
//     private List<MergeAction> mergeActions = new List<MergeAction>();
//     public float waitTime {get; private set;} = 0.4f;
//     public StripeAction(ColorBlob blob){
//         this.blob = blob;
        
//     }   


//     private void MergeAllBlobsInColumn(Board board){
//         List<MergeAction> mergeActions = new List<MergeAction>();
//         for(int x = 0; x < board.BoardLogic.Width; x++){
//             //if the blob we are on is not null and it is not this one
//             if(board.BoardLogic.BlobGrid[x, blob.GridPosition.y] != null && board.BoardLogic.BlobGrid[x, blob.GridPosition.y] != blob ){
                
//                 MergeAction mergeAction = new MergeAction(blob, board.BoardLogic.BlobGrid[x, blob.GridPosition.y]);
//                 mergeActions.Add(mergeAction);
//                 mergeAction.Execute(board);
//             }
            
//         }
//     }

//     private void MergeAllBlobsInRow(Board board){
//         List<MergeAction> mergeActions = new List<MergeAction>();
//         for(int y = 0; y < board.BoardLogic.Height; y++){
//             //if the blob we are on is not null and it is not this one
//             if(board.BoardLogic.BlobGrid[blob.GridPosition.x, y] != null && board.BoardLogic.BlobGrid[blob.GridPosition.x, y] != blob ){
//                 MergeAction mergeAction = new MergeAction(blob, board.BoardLogic.BlobGrid[blob.GridPosition.x, y]);
//                 mergeActions.Add(mergeAction);
//                 mergeAction.Execute(board);
//             }
            
//         }
//     }


//     public void Execute(Board board){
//         if(blob != null){
//             //if blob is vertically striped
//             //if(blob.Context.CurrentState == blob.Context.VStripeState)
//             //{
//             //    MergeAllBlobsInRow(); 
//             //}
//             ////if blob is horizontally striped
//             //if(blob.Context.CurrentState == blob.Context.HStripeState)
//             //{ 
//             //    MergeAllBlobsInColumn(); 
//             //}
//             ////if blob is perpendicularly striped
//             //if(blob.Context.CurrentState == blob.Context.TStripeState)
//             //{ 
//             //    MergeAllBlobsInColumn();                 
//             //    MergeAllBlobsInRow();  
//             //}
//         }
            
        
    
//     }

//     public void Undo(Board board){
//         foreach(MergeAction action in mergeActions){
//             action.Undo(board);
//         }
        
//     }
// }
