using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IAction{
    float waitTime {get;}
    void Execute(Board board);
    void Undo(Board board);
}

