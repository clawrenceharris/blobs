using System;
using Blobs.Content;
using Blobs.Core;
using DG.Tweening;
using UnityEngine;

namespace Blobs.Presentation
{
    /// <summary>
    /// Inspector-assignable merge choreography. Core owns survivor identity; BlobPresenter
    /// owns retirement. Implementations defer visual changes until their sequence plays.
    /// </summary>
    public abstract class MergeAnimationOrchestratorBase : MonoBehaviour
    {
        /// <summary>
        /// Builds a merge, invoking onContact at arrival and onTargetConsumed once playback
        /// completes (the consumed participant can be either source or target).
        /// </summary>
        public abstract Sequence CreateMergeBeat(
            BlobView source,
            BlobView target,
            bool sourceSurvives,
            Vector2Int gridDirection,
            Action onContact,
            Action onTargetConsumed,
            BlobMergeImpactSettings settings,
            GridPosition? destination = null);
    }
}
