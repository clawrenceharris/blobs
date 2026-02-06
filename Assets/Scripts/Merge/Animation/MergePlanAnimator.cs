using System.Collections;
using Blobs.Merge.Animation;
using DG.Tweening;

namespace Blobs.Core.Merge
{
    /// <summary>
    /// Animates a MergePlan (Events + DeferredEvents) for visuals.
    /// Dispatches to IEventAnimator via EventAnimatorRegistry; uses recipe provider for blob-specific tuning.
    /// </summary>
    public sealed class MergePlanAnimator
    {
        private static EventAnimatorRegistry _registry;

        /// <summary>Registry mapping IMergeEvent types to IEventAnimator. Set before use (e.g. from MergeAnimationBootstrap).</summary>
        public static EventAnimatorRegistry Registry
        {
            get
            {
                if (_registry == null)
                {
                    _registry = new EventAnimatorRegistry();
                    RegisterDefaultAnimators(_registry);
                }
                return _registry;
            }
            set => _registry = value;
        }

        private static void RegisterDefaultAnimators(EventAnimatorRegistry registry)
        {
            // registry.Register<MoveBlobEvent>(new MoveBlobEventAnimator());
            registry.Register<RemoveBlobEvent>(new RemoveBlobEventAnimator());
            registry.Register<SpawnBlobEvent>(new SpawnBlobEventAnimator());
            registry.Register<ResizeBlobEvent>(new ResizeBlobEventAnimator());
            registry.Register<MergeBlobsEvent>(new MergeBlobsEventAnimator());
            registry.Register<ExpressionEvent>(new ExpressionEventAnimator());
            registry.Register<PlayVfxEvent>(new PlayVfxEventAnimator());
        }

        public static IEnumerator AnimatePlan(MergePlan plan, IBoardPresenter board)
        {
            if (plan == null) yield break;
            foreach (var e in plan.Events)
            {
                yield return AnimateEvent(e, board, forUndo: false);
            }
            foreach (var e in plan.DeferredEvents)
            {
                yield return AnimateEvent(e, board, forUndo: false);
            }
        }


        public static IEnumerator AnimateUndo(MergePlan plan, IBoardPresenter board)
        {
            if (plan == null) yield break;
            for (int i = plan.DeferredEvents.Count - 1; i >= 0; i--)
            {
                yield return AnimateEvent(plan.DeferredEvents[i], board, forUndo: true);
            }
            for (int i = plan.Events.Count - 1; i >= 0; i--)
            {
                yield return AnimateEvent(plan.Events[i], board, forUndo: true);
            }
        }

        private static IEnumerator AnimateEvent(IMergeEvent e, IBoardPresenter board, bool forUndo)
        {
            if (board == null) { yield return null; yield break; }
            var eventType = e.GetType();
            if (!Registry.TryGetAnimator(eventType, forUndo, out var animator))
            {
                yield return null;
                yield break;
            }

            if (forUndo)
            {
                yield return animator.BuildUndoSequence(e, board)?.WaitForCompletion();
            }
            else
            {
                yield return animator.BuildSequence(e, board)?.WaitForCompletion();
            }


        }
    }

}
