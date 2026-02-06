using System.Collections.Generic;
using System.Linq;

namespace Blobs.Core.Merge
{

/// <summary>
/// Resolves merge requests by validating the merge operation and building an execution plan.
/// </summary>
/// <remarks>
/// This sealed class coordinates the merge resolution process by:
/// 1. Resolving the path from source to target blob
/// 2. Validating the source blob is movable and target exists (if specified)
/// 3. Creating a merge context and plan
/// 4. Applying all merge rules in priority order
/// 5. Adding the final move event to the plan
/// 
/// Rules are applied in priority order as determined during construction.
/// </remarks>
public sealed class MergeResolver
{
    private readonly PathResolver _pathResolver;
    private readonly List<IMergeRule> _rules;

    public MergeResolver(PathResolver pathResolver, IEnumerable<IMergeRule> rules)
    {
        _pathResolver = pathResolver;
        _rules = rules.OrderBy(r => r.Priority).ToList();
    }

    /// <summary>
    /// Attempts to build a merge execution plan for the given request.
    /// </summary>
    /// <param name="board">The board presenter containing the blobs. Must not be null.</param>
    /// <param name="request">The merge request specifying source and optional target.</param>
    /// <param name="plan">The generated merge plan if successful; otherwise null.</param>
    /// <returns>A <see cref="MergeResolveResult"/> indicating success or the reason for failure.</returns>
    public MergeResolveResult TryBuildPlan(IBoardPresenter board, MergeRequest request, out MergePlan plan)
    {
        plan = null;
        if (board == null) return MergeResolveResult.Fail(MergeFailReason.InvalidSource);
        
        // 1) Resolve path (single traversal)
        var pathResult = _pathResolver.ResolvePath(board, request, out var path);
        if (!pathResult.Ok) return MergeResolveResult.Fail(pathResult.FailReason);
        
        var source = board.GetBlob(path.SourceId);

        if (source == null) return MergeResolveResult.Fail(MergeFailReason.InvalidSource);
        if(!source.Model.Type.CanInitiateMerge()) return MergeResolveResult.FailSilently();
        IBlobPresenter hitBlob = null;
        if (!string.IsNullOrEmpty(path.HitBlobId))
            hitBlob = board.GetBlob(path.HitBlobId);
        if (source.Model.GridPosition.x != hitBlob.Model.GridPosition.x && source.Model.GridPosition.y != hitBlob.Model.GridPosition.y)
        {
            return MergeResolveResult.FailSilently();
        }
        // If the path never hit a blob, default is no move
        if (hitBlob == null && path.Termination != PathTermination.ForcedStop)
            return MergeResolveResult.Fail(MergeFailReason.NoTargetInDirection);

        // 2) Create context + plan shell
        var ctx = new MergeContext
        {
            Board = board,
            Source = source.Model,
            HitBlob = hitBlob.Model,
            Path = path
        };

        plan = new MergePlan
        {
            SourceId = source.Model.ID,
            HitBlobId = hitBlob?.Model.ID,
            Path = path
        };

        // 3) Run batch rules (ordered). Rules append events into plan.Events
        foreach (var rule in _rules)
        {
            if (!rule.Apply(ctx, plan, out var fail))
            {
                plan = null;
                return MergeResolveResult.Fail(fail == MergeFailReason.None ? MergeFailReason.ColorRuleRejected : fail);
            }
        }
        
        plan.Events.Add(new MoveBlobEvent
        {
            BlobId = source.Model.ID,
            From = path.Start,
            To = path.End
        });
        

        return MergeResolveResult.SuccessWithPlan(plan);
    }
}

}