using System.Collections.Generic;
using UnityEngine;

namespace Blobs.Core.Merge
{
    public interface IMergeService
    {
        MergeResolveResult TryCreateMergeCommand(string sourceBlobId, string targetBlobId);
    }

    /// <summary>
    /// Service responsible for creating and executing merge commands between blobs on the board.
    /// Implements the merge resolution strategy by delegating to a MergeResolver with predefined merge rules.
    /// </summary>
    public class MergeService : IMergeService
    {
        private IBoardPresenter _board;
       
        public MergeService(IBoardPresenter board)
        {
            _board = board;
        }

        /// <summary>
        /// Attempts to create and execute a merge command between a source blob and a target blob.
        /// </summary>
        /// <param name="sourceBlobId">The identifier of the source blob to merge.</param>
        /// <param name="targetBlobId">The identifier of the target blob to merge into.</param>
        /// <returns>
        /// A MergeResolveResult indicating success with the execution plan, or failure with the reason.
        /// Possible failure reasons include invalid source board or merge resolution failures.
        /// </returns>
        /// <remarks>
        /// The merge process uses a resolver with multiple merge rules (Default, Normal, Flag, Trail, Bomb, Remove)
        /// that are evaluated in order to determine the merge strategy. If resolution succeeds, the merge plan
        /// is immediately executed on the board.
        /// </remarks>
        public MergeResolveResult TryCreateMergeCommand(string sourceBlobId, string targetBlobId)
        {
            var pathResolver = new PathResolver(null);
            var mergeResolver = new MergeResolver(pathResolver, new List<IMergeRule>
            {
                new DefaultMergeRule(),
                new NormalRule(),
                new FlagRule(),
                new TrailRule(),
                new BombRule(),
            });

            var result = mergeResolver.TryBuildPlan(_board, new MergeRequest(sourceBlobId, targetBlobId), out var plan);
            if (!result.Ok)
                return MergeResolveResult.Fail(result.FailReason);
          
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
        LaserBlocked,
        ColorRuleRejected,
        InfiniteLoopGuard,
        FlagColorRuleRejected,
        FlagMergeRuleRejected
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
        public static MergeResolveResult FailSilently() => new(false, MergeFailReason.None, null);

    }
}