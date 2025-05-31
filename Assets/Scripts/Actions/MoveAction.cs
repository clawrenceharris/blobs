using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveAction : MonoBehaviour, IAction
{
    public List<SubmoveAction> submoves {get; private set;}
    
    private StripeAction stripeAction;
    private Blob _blob;
    public delegate void OnMoveComplete(Blob blob);
    public static event OnMoveComplete onMoveComplete;
    public delegate void OnMoveStart(Blob blob);
    
       public delegate void OnMoveUndone(Blob blob);
    public static event OnMoveUndone onMoveUndone;
    public static event OnMoveStart onMoveStart;
    public float waitTime {get; private set;} = 0f;
    public void Init(List<SubmoveAction> submoveActions){
        this.submoves = submoveActions;
        this._blob = submoveActions[0].Blob;

    }
    
    public void Execute(Board board){
        onMoveStart?.Invoke(_blob);
        StartCoroutine(ExecuteCo(board));


    }
    
    public IEnumerator ExecuteCo(Board board)
    {
        foreach(SubmoveAction submove in submoves)
        {
            submove.Execute(board);

            //execute each action that this current submove created
            for (int i = 0; i < submove.actions.Count; i++){
                yield return new WaitForSeconds(0.5f);
                submove.actions[i].Execute(board);
            }

        }

        stripeAction = new StripeAction(_blob.GetComponent<ColorBlob>());
        stripeAction.Execute(board);
        onMoveComplete?.Invoke(_blob);

    }

    public void Undo(Board board){

        for(int i = submoves.Count - 1; i >= 0; i--)
        {
            submoves[i].Undo(board);

            for (int j = submoves[i].actions.Count - 1; j >= 0; j--)
                submoves[i].actions[j].Undo(board);


        }
        stripeAction.Undo(board);
        onMoveUndone?.Invoke(_blob);
}
}
