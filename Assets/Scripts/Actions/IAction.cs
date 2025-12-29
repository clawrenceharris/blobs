using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IAction{

    Blob Blob { get; set; }
    void Execute(BoardModel model);
    void Undo(BoardModel model);

}

