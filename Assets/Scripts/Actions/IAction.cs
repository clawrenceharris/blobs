using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IAction{
    void Execute(BoardLogic board);
    void Undo(BoardLogic board);
}

