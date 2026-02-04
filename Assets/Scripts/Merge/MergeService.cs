using Blobs.Commands;
using Blobs.Input;
using System.Collections.Generic;
using UnityEngine;

namespace Blobs.Core.Merge
{
    public interface IMergeService
    {
        MergeResolveResult TryCreateMergeCommand(string sourceBlobId, string targetBlobId);
    }

    public class MergeService : IMergeService
    {
        private readonly IBoardPresenter _board;

        public MergeService(IBoardPresenter board)
        {
            _board = board;
        }

    

        public MergeResolveResult TryCreateMergeCommand(string sourceBlobId, string targetBlobId)
        {
            var boardPresenter = _board as BoardPresenter;
            var board = boardPresenter.Model;
            if (board == null)
                return MergeResolveResult.Fail(MergeFailReason.InvalidSource);

            var pathResolver = new PathResolver(null);
            var mergeResolver = new MergeResolver(pathResolver, new List<IMergeRule>
            {
                new DefaultMergeRule(),
                new NormalRule(),
                new FlagRule(),
                new TrailRule(),
                new BombRule(),
                new RemoveRule()
            });

            var result = mergeResolver.TryBuildPlan(board, new MergeRequest(sourceBlobId, targetBlobId), out var plan);
            if (!result.Ok)
                return MergeResolveResult.Fail(result.FailReason);

            MergeInvoker.ExecuteMerge(plan, board);
            return MergeResolveResult.SuccessWithPlan(plan);
        }
    }

    public readonly struct MergeRequest
    {
        public readonly string SourceId;
        public readonly string TargetId;      // optional
        public readonly Vector2Int? Direction; // optional

        public bool HasTarget => !string.IsNullOrEmpty(TargetId);
        public bool HasDirection => Direction.HasValue;

        public MergeRequest(string sourceId, string targetId)
        {
            SourceId = sourceId;
            TargetId = targetId;
            Direction = null;
        }

        public MergeRequest(string sourceId, Vector2Int direction)
        {
            SourceId = sourceId;
            TargetId = null;
            Direction = direction;
        }
    }

    public enum MergeFailReason
    {
        None,
        InvalidSource,
        InvalidTarget,
        NotAligned,
        NoTargetInDirection,
        TileBlocked,
        FlagRejected,
        LaserBlocked,
        ColorRuleRejected,
        InfiniteLoopGuard
    }

    public readonly struct MergeResolveResult
    {
        public readonly bool Ok;
        public readonly MergeFailReason FailReason;
        public readonly MergePlan Plan;

        private MergeResolveResult(bool ok, MergeFailReason reason, MergePlan plan)
        {
            Ok = ok;
            FailReason = reason;
            Plan = plan;
        }

        public static MergeResolveResult SuccessWithPlan(MergePlan plan) => new(true, MergeFailReason.None, plan);
        public static MergeResolveResult Fail(MergeFailReason reason) => new(false, reason, null);
    }
}