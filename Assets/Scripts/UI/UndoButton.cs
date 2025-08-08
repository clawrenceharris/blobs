using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class UndoButton : MonoBehaviour
{
    private Button button;
    private void Awake()
    {
        button = GetComponent<Button>();
    }
    private void Start()
    {
        // MoveAction.onMoveUndone += OnMoveUndone;
        // MoveAction.onMoveComplete += OnMoveComplete;
        
        button.interactable = false;
        button.onClick.AddListener(OnClick);
    }


    private void OnClick()
    {
        // actionInvoker.UndoAction();
    }
    private void OnMoveUndone(Blob blob)
    {
        // if (ActionInvoker.Actions.Count <= 0)
        // {
        //     button.interactable = false;
        // }
    }

    private void OnMoveComplete(Blob blob)
    {
        button.interactable = true;
    }
}
