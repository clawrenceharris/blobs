using UnityEngine;
using System;

namespace Blobs.Services
{
    /// <summary>
    /// Move validation result with reason for failure.
    /// </summary>
    public enum MoveValidationResult
    {
        Valid,
        NoTarget,
        SameColor,
        IncompatibleType,
        NotAligned,
        PathBlocked
    }

    /// <summary>
    /// Move finding and validation service.
    /// SRP: Only handles move-finding, validation, and execution.
    /// </summary>
    public interface IMoveService
    {
        /// <summary>Fired when a merge is successfully executed</summary>
        event Action<IBlobPresenter, IBlobPresenter> OnMergeExecuted;

        /// <summary>Fired when a merge attempt fails</summary>
        event Action<IBlobPresenter, MoveValidationResult> OnMergeAttemptFailed;

        /// <summary>Find target blob in direction from source</summary>
        IBlobPresenter FindTargetInDirection(IBlobPresenter source, Vector2Int direction);

        /// <summary>Validate if merge is possible between source and target</summary>
        MoveValidationResult ValidateMerge(IBlobPresenter source, IBlobPresenter target);

        /// <summary>Attempt merge in direction from selected blob</summary>
        void TryMergeInDirection(IBlobPresenter source, Vector2Int direction);

        /// <summary>Attempt merge between two blobs</summary>
        void TryMerge(IBlobPresenter source, IBlobPresenter target);
    }
}
