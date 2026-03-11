using System.Collections.Generic;

/// <summary>
/// Command that records a resolved move for undo. Effects are already applied during resolution;
/// Execute() is a no-op for the model. Undo() applies InverseEffects to restore the board.
/// </summary>
public class MergeCommand : ICommand
{
    private readonly MoveResult _result;
    private readonly IBoardPresenter _board;
    public Blob Source => _result.Context.Source;
    public Blob Target => _result.Context.Target;
    public MergeCommand(MoveResult result, IBoardPresenter board)
    {
        _result = result;
        _board = board;
    }

    public IReadOnlyList<IEffect> Effects => _result.Effects;
    public IReadOnlyList<IEffect> InverseEffects => _result.InverseEffects;

    public void Execute()
    {
        // Model was already updated during MoveResolver.Resolve (pipeline applied effects via BoardTransaction).
    }

    public void Undo()
    {
        var txn = new BoardTransaction(_board);
        foreach (var e in _result.InverseEffects)
            txn.Apply(e);
    }
}
