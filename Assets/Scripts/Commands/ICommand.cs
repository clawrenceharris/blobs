using System.Collections;
using System.Collections.Generic;
using Blobs.Commands;

public interface IAction{

    Blob Blob { get; set; }
    void Execute(CommandManager context);
    void Undo(CommandManager context);

}

