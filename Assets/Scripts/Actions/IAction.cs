using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IAction{
    void Execute(BoardModel board);
    void Undo(BoardModel board);
}

