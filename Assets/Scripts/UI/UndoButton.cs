using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class UndoButton : MonoBehaviour
{
    private Button button;
    private ActionInvoker actionInvoker;
    private void Awake()
    {
        button = GetComponent<Button>();
        actionInvoker = FindFirstObjectByType<ActionInvoker>();
    }
    private void Start()
    {
        MoveAction.onMoveUndone += OnMoveUndone;
        MoveAction.onMoveComplete += OnMoveComplete;
        
        button.interactable = false;
        button.onClick.AddListener(OnClick);
    }


    private void OnClick()
    {
        actionInvoker.UndoAction();
    }
    private void OnMoveUndone(Blob blob)
    {
        if (ActionInvoker.Instance.Actions.Count <= 0)
        {
            button.interactable = false;
        }
    }

    private void OnMoveComplete(Blob blob)
    {
        button.interactable = true;
    }
}
