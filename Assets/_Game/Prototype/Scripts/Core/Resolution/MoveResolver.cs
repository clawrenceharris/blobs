using System.Collections.Generic;

/// <summary>
/// API for the input/game loop: resolve a move intent to a result with effects and inverse effects.
/// </summary>
public sealed class MoveResolver
{
    private readonly ResolutionPipeline _pipeline;

    public MoveResolver(ResolutionPipeline pipeline)
    {
        _pipeline = pipeline ?? ResolutionPipeline.CreateDefault();
    }

    /// <summary>
    /// Resolves the intent against the board. Builds context (Phase 4), runs pipeline (Phase 5), applies effects via transaction.
    /// </summary>
    public MoveResult Resolve(MoveIntent intent, BoardPresenter board)
    {
        var result = new MoveResult();
        var ctx = MoveContextBuilder.Build(intent, board, result);
        if (ctx == null)
            return result;

        var txn = new BoardTransaction(board);
        _pipeline.Resolve(ctx, txn, result);

        result.InverseEffects = new List<IEffect>(txn.GetInverseEffectsReversed());
        return result;
    }
}
