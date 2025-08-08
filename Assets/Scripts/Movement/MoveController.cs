// using System.Collections;
// using System.Collections.Generic;
// using Unity.VisualScripting;
// using UnityEngine;

// public class MoveController : MonoBehaviour
// {
//     private Board board;
//     private Blob selectedBlob;
//     private static MoveController instance;
//     private void Awake()
//     {
//         board = FindFirstObjectByType<Board>();
//         DontDestroyOnLoad(gameObject);
//         if (instance == null)
//         {
//             // This is the first instance, so persist it across scenes
//             instance = this;
//             DontDestroyOnLoad(gameObject);
//         }
//         else
//         {
//             // There is already an instance, so destroy this duplicate
//             Destroy(gameObject);
//         }
    
// }


//     private void Start(){

//         // BlobInput.OnBlobDeselected += OnBlobDeselected;
//         // BlobInput.OnBlobSelected += OnBlobSelected;
//         LevelManager.OnLevelRestart += OnLevelRestart;
//     }


//     private void OnLevelRestart()
//     {
//         foreach(Transform child in transform)
//         {
//             Destroy(child.gameObject);
//         }
//     }
//     private void OnBlobDeselected(Blob blob)
//     {

//         if (IsValidInput(selectedBlob, blob))
//         {
//             // if (selectedBlob && blob)
//             // {
//             //     Move move = new(selectedBlob.Position, blob.Position);
//             //     InvokeMove(move);
//             // }

//         }
//     }
//     private void OnBlobSelected(Blob blob)
//     {
//         selectedBlob = blob;
//     }

//     private bool IsValidInput(Blob selectedBlob, Blob targetBlob)
//     {
//         // bool isValid = (selectedBlob.Position.x == targetBlob.Position.x || selectedBlob.Position.y == targetBlob.Position.y) && !selectedBlob.Disabled;
//         return true;
//     }
//     /// <summary>
//     /// triggers when the player moves from one spot on the board to another
//     /// </summary>
//     /// <param name="move">represents the two positions, start and end, the player inputted</param>
//     private void InvokeMove(Move move){
//         Blob selectedBlob = board.BoardLogic.GetBlobAt(move.start.x, move.start.y);
//         List<Move> submoves = GetSubMoves(move);

//         List<SubmoveAction> submoveActions = new();
//         //invoke the merge action passing in the blob to move to
       
//             if (selectedBlob != null && submoves.Count > 0 && IsLegalMove(submoves))
//             {
//                 foreach (Move submove in submoves)
//                 {
//                     submoveActions.Add(new SubmoveAction(selectedBlob, submove));
//                 }



//                 GameObject gameObject = new("Move Action", typeof(MoveAction));
//                 MoveAction moveAction = gameObject.GetComponent<MoveAction>();
//                 moveAction.Init(submoveActions);
//                 moveAction.transform.parent = transform;
//                 ActionInvoker.Instance.InvokeAction(moveAction);

//             }



        

//     }



//     private List<Move> GetSubMoves(Move move)
//     {
        
//         List<Move> moves = new List<Move>();
//         //initialize the move value so we know which direction to move to from the start pos
//         int moveDirectionX = Direction.GetMoveValue(move).x;
//         int moveDirectionY = Direction.GetMoveValue(move).y;
        
//         //initialize the temporary startPos for the first move (same as the startPos in the move object)
//         int tempStartX = move.start.x;
//         int tempStartY = move.start.y;

//         int tempTargetX = move.start.x;
//         int tempTargetY = move.start.y;
//         Move submove;
//         //loop until our temporay end position equals our true end position in move object
//         while(!move.end.IsEqual(new Position(tempTargetX, tempTargetY)))
//         {
//             //add the move value to the temporary endPos 
//             tempTargetX += moveDirectionX;
//             tempTargetY += moveDirectionY;

//             submove = new Move(new Position(tempStartX, tempStartY), new Position(tempTargetX, tempTargetY));
            
//             moves.Add(submove);
          
//             //add the move value to the temp start position 
//             tempStartX += moveDirectionX;
//             tempStartY += moveDirectionY;
//         }

//         return moves;
    
//     }

//     private bool IsLegalMove(List<Move> submoves){
//         Blob selectedBlob = board.BoardLogic.GetBlobAt(submoves[0].start.x, submoves[0].start.y);//get the first blob selected

//         //check that each suplementary move is legal based on postion and color 
//         foreach(Move submove in submoves){
//             // if(selectedBlob && !board.IsLeagalMove(selectedBlob, submove)){

//             //     return false;
//             // }
//             Blob  blobToMoveTo = board.BoardLogic.GetBlobAt(submove.end.x, submove.end.y);
            
//             // if (blobToMoveTo && !blobToMoveTo.CanMoveTo(selectedBlob))
//             // {

//             //     return false;
//             // }



//         }
//         return true;

        
//     }

    
    
    
    
    
// }
