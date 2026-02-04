using System.Collections.Generic;
using System.Linq;

namespace Blobs.Core.Merge
{

public sealed class MergeResolver
{
    private readonly PathResolver _pathResolver;
    private readonly List<IMergeRule> _rules;

    public MergeResolver(PathResolver pathResolver, IEnumerable<IMergeRule> rules)
    {
        _pathResolver = pathResolver;
        _rules = rules.OrderBy(r => r.Priority).ToList();
    }

    public MergeResolveResult TryBuildPlan(BoardModel board, MergeRequest request, out MergePlan plan)
    {
        plan = null;
        if (board == null) return MergeResolveResult.Fail(MergeFailReason.InvalidSource);
        
        // 1) Resolve path (single traversal)
        var pathResult = _pathResolver.ResolvePath(board, request, out var path);
        if (!pathResult.Ok) return MergeResolveResult.Fail(pathResult.FailReason);

        var source = board.GetBlob(path.SourceId);
        
        if (source == null) return MergeResolveResult.Fail(MergeFailReason.InvalidSource);
        if(source is not IMovable) return MergeResolveResult.Fail(MergeFailReason.InvalidSource);
        Blob hitBlob = null;
        if (!string.IsNullOrEmpty(path.HitBlobId))
            hitBlob = board.GetBlob(path.HitBlobId);

        // If the user clicked a specific target, enforce it:
        if (request.HasTarget)
        {
            if (hitBlob == null || hitBlob.ID != request.TargetId)
                return MergeResolveResult.Fail(MergeFailReason.InvalidTarget);
        }

        // If the path never hit a blob, default is no move
        if (hitBlob == null && path.Termination != PathTermination.ForcedStop)
            return MergeResolveResult.Fail(MergeFailReason.NoTargetInDirection);

        // 2) Create context + plan shell
        var ctx = new MergeContext
        {
            Board = board,
            Source = source,
            HitBlob = hitBlob,
            Path = path
        };

        plan = new MergePlan
        {
            SourceId = source.ID,
            HitBlobId = hitBlob?.ID,
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

        // 4) Basic default: if no rule added movement, add default movement now
        // You can make this a rule instead if you prefer.
        if (!plan.Events.OfType<MoveBlobEvent>().Any())
        {
            plan.Events.Add(new MoveBlobEvent
            {
                BlobId = source.ID,
                From = path.Start,
                To = path.End
            });
        }

        return MergeResolveResult.SuccessWithPlan(plan);
    }
}

}