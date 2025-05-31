using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*

Order of Execution:
    1. Move Action
    2. Merge Action
    3. Spawn action
    4. Stripe Action
    5. Remove Action

Order of Undo:
    1. Remove Action
    2. Stripe Action
    3. Spawn action

    4. Merge Action
    5. Move Action
*/


public class ActionInvoker : MonoBehaviour
{

    public Stack<IAction> Actions { get; private set; } = new Stack<IAction>();
    public static ActionInvoker Instance;
    private Board board;

    private void Awake(){
        Instance = this;
        board = FindFirstObjectByType<Board>();
    }

    private void Start()
    {
        LevelManager.OnLevelRestart += OnLevelRestart;
        LevelManager.OnLevelEnd += OnLevelEnd;
    }
    private void OnLevelEnd()
    {
        Actions.Clear();
    }
    private void OnLevelRestart()
    {
        Actions.Clear();
    }



    public void InvokeAction(IAction action)
    {
        Actions.Push(action);
        action.Execute(board);


    }

    public void UndoAction(){
        if(Actions.Count == 0){
            return;
        }
        var action = Actions.Pop();
        action.Undo(board);
    }
    
    
   

    
    

}