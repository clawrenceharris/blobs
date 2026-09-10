using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Blobs.Core;
using DG.Tweening;

namespace Blobs.Presentation
{
    /// <summary>
    /// Dispatches explicit Core effects by type and composes their animation beats.
    /// A MergeEffect is one interaction; movement/removal pairs are never inferred as merges.
    /// Flat lists preserve caller order. MoveStep lists additionally group concurrent effects
    /// (for example, trail spawning joins the beginning of movement in the same step).
    /// </summary>
    internal sealed class BoardEffectPresentationPipeline
    {
        private static readonly BoardEffectPresentationPhase[] PhaseOrder =
        {
            BoardEffectPresentationPhase.Travel,
            BoardEffectPresentationPhase.Departure,
            BoardEffectPresentationPhase.Arrival,
            BoardEffectPresentationPhase.Aftermath
        };

        private readonly Dictionary<Type, IBoardEffectPresentationHandler> _handlers = new();
        private readonly List<IMoveStepPresentationHandler> _moveStepHandlers = new();
        private readonly BlobPresenter _blobs;
        private readonly TilePresenter _tiles;

        public BoardEffectPresentationPipeline(BlobPresenter blobs, TilePresenter tiles)
        {
            _blobs = blobs ?? throw new ArgumentNullException(nameof(blobs));
            _tiles = tiles ?? throw new ArgumentNullException(nameof(tiles));

            Register(new MoveBlobPresentationHandler());
            Register(new SpawnBlobPresentationHandler());
            Register(new RemoveBlobPresentationHandler());
            Register(new MergePresentationHandler());
            Register(new GhostHauntPresentationHandler());
            Register(new GhostRestPresentationHandler());
        }

        public void Register(IBoardEffectPresentationHandler handler)
        {
            ValidateHandler(handler);
            _handlers[handler.EffectType] = handler;
        }

        public void Register(IMoveStepPresentationHandler handler)
        {
            ValidateHandler(handler);
            _moveStepHandlers.Remove(handler);
            _moveStepHandlers.Insert(0, handler);
        }

        public static void ValidateHandler(IBoardEffectPresentationHandler handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            if (handler.EffectType == null ||
                !typeof(IBoardEffect).IsAssignableFrom(handler.EffectType))
            {
                throw new ArgumentException(
                    "Effect handlers must declare an IBoardEffect type.",
                    nameof(handler));
            }
            if (!Enum.IsDefined(typeof(BoardEffectPresentationPhase), handler.Phase))
                throw new ArgumentOutOfRangeException(nameof(handler), "Unknown presentation phase.");
        }

        public static void ValidateHandler(IMoveStepPresentationHandler handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
        }

        public async UniTask<bool> PresentOrderedEffectsAsync(
            IReadOnlyList<IBoardEffect> effects, PresentationTimeline timeline,
            CancellationToken cancellationToken)
        {
            if (!PresentOrderedEffects(effects, timeline)) return false;
            await timeline.PlayAsync(cancellationToken);
            return true;
        }

        public async UniTask<bool> PresentStepsAsync(IReadOnlyList<MoveStep> steps,
            PresentationTimeline timeline, Action contactFeedback, CancellationToken cancellationToken)
        {
            if (!PresentSteps(steps, timeline, contactFeedback)) return false;
            await timeline.PlayAsync(cancellationToken);
            return true;
        }

        /// <summary>
        /// Flat-list compatibility path. Dispatch each effect exactly once in its supplied order;
        /// interaction semantics must already be present in the effect itself.
        /// </summary>
        public bool PresentOrderedEffects(
            IReadOnlyList<IBoardEffect> effects,
            PresentationTimeline timeline)
        {
            for (int i = 0; i < effects.Count; i++)
            {
                if (!TryResolve(effects[i], out IBoardEffectPresentationHandler handler))
                    return false;

                var context = CreateContext(
                    timeline,
                    Ease.Linear,
                    contactFeedback: null);
                if (!handler.PresentOrdered(effects[i], context))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Canonical move path: each step becomes one beat; phase placement is local to that beat.
        /// Playback awaits the beats in sequence after composition succeeds.
        /// </summary>
        public bool PresentSteps(
            IReadOnlyList<MoveStep> steps,
            PresentationTimeline timeline,
            Action contactFeedback)
        {
            int lastMovementStep = FindLastMovementStep(steps);

            for (int i = 0; i < steps.Count; i++)
            {
                Action stepContact = i == lastMovementStep ? contactFeedback : null;
                Ease movementEase = i == lastMovementStep ? Ease.OutQuad : Ease.Linear;
                if (!PresentStep(steps[i], timeline, movementEase, stepContact))
                    return false;
            }

            return true;
        }

        private bool PresentStep(
            MoveStep step,
            PresentationTimeline outerTimeline,
            Ease movementEase,
            Action contactFeedback)
        {
            PresentationTimeline beat = outerTimeline.CreateBeat();
            if (TryResolve(step, out IMoveStepPresentationHandler stepHandler))
            {
                var context = CreateContext(beat, movementEase, contactFeedback);
                if (!stepHandler.Present(step, context, out IReadOnlyList<IBoardEffect> handledEffects))
                {
                    beat.Kill();
                    return false;
                }

                if (!PresentUnhandledEffects(
                        step.Effects,
                        handledEffects,
                        beat,
                        movementEase,
                        stepHandler.RepresentsMovement ? null : contactFeedback))
                {
                    beat.Kill();
                    return false;
                }
            }
            else if (!PresentPhasedEffects(
                         step.Effects,
                         beat,
                         movementEase,
                         contactFeedback))
            {
                beat.Kill();
                return false;
            }

            outerTimeline.Append(beat);
            return true;
        }

        private bool PresentUnhandledEffects(
            IReadOnlyList<IBoardEffect> effects,
            IReadOnlyList<IBoardEffect> handledEffects,
            PresentationTimeline beat,
            Ease movementEase,
            Action contactFeedback)
        {
            List<EffectWorkItem>[] phases = CreatePhaseBuckets();
            foreach (IBoardEffect effect in effects)
            {
                if (ContainsReference(handledEffects, effect))
                    continue;

                if (!TryResolve(effect, out IBoardEffectPresentationHandler handler))
                    return false;
                phases[(int)handler.Phase].Add(new EffectWorkItem(effect, handler));
            }

            return PresentPhaseBuckets(
                phases,
                beat,
                movementEase,
                contactFeedback);
        }

        private bool PresentPhasedEffects(
            IReadOnlyList<IBoardEffect> effects,
            PresentationTimeline beat,
            Ease movementEase,
            Action contactFeedback)
        {
            List<EffectWorkItem>[] phases = CreatePhaseBuckets();
            foreach (IBoardEffect effect in effects)
            {
                if (!TryResolve(effect, out IBoardEffectPresentationHandler handler))
                    return false;
                phases[(int)handler.Phase].Add(new EffectWorkItem(effect, handler));
            }

            return PresentPhaseBuckets(phases, beat, movementEase, contactFeedback);
        }

        private bool PresentPhaseBuckets(
            IReadOnlyList<EffectWorkItem>[] phases,
            PresentationTimeline beat,
            Ease movementEase,
            Action contactFeedback)
        {
            // Travel establishes the beat duration. Departure effects join its start,
            // while arrival and aftermath effects are appended in readable order.
            foreach (BoardEffectPresentationPhase phase in PhaseOrder)
            {
                foreach (EffectWorkItem item in phases[(int)phase])
                {
                    var context = CreateContext(
                        beat,
                        movementEase,
                        contactFeedback);
                    if (!item.Handler.PresentInBeat(item.Effect, context))
                        return false;
                }
            }

            return true;
        }

        private static List<EffectWorkItem>[] CreatePhaseBuckets()
        {
            return new[]
            {
                new List<EffectWorkItem>(),
                new List<EffectWorkItem>(),
                new List<EffectWorkItem>(),
                new List<EffectWorkItem>()
            };
        }

        private int FindLastMovementStep(IReadOnlyList<MoveStep> steps)
        {
            int lastMovementStep = -1;
            for (int i = 0; i < steps.Count; i++)
            {
                if (TryResolve(steps[i], out IMoveStepPresentationHandler stepHandler) &&
                    stepHandler.RepresentsMovement)
                {
                    lastMovementStep = i;
                    continue;
                }

                foreach (IBoardEffect effect in steps[i].Effects)
                {
                    if (TryResolve(effect, out IBoardEffectPresentationHandler handler) &&
                        handler.RepresentsMovement)
                    {
                        lastMovementStep = i;
                    }
                }
            }

            return lastMovementStep;
        }

        private static bool ContainsReference(
            IReadOnlyList<IBoardEffect> effects,
            IBoardEffect candidate)
        {
            if (effects == null)
                return false;

            foreach (IBoardEffect effect in effects)
            {
                if (ReferenceEquals(effect, candidate))
                    return true;
            }

            return false;
        }

        private bool TryResolve(
            MoveStep step,
            out IMoveStepPresentationHandler handler)
        {
            foreach (IMoveStepPresentationHandler candidate in _moveStepHandlers)
            {
                if (candidate.CanPresent(step))
                {
                    handler = candidate;
                    return true;
                }
            }

            handler = null;
            return false;
        }

        private bool TryResolve(
            IBoardEffect effect,
            out IBoardEffectPresentationHandler handler)
        {
            handler = null;
            return effect != null && _handlers.TryGetValue(effect.GetType(), out handler);
        }

        private BoardEffectPresentationContext CreateContext(
            PresentationTimeline timeline,
            Ease movementEase,
            Action contactFeedback)
        {
            return new BoardEffectPresentationContext(
                _blobs,
                _tiles,
                timeline,
                movementEase,
                contactFeedback);
        }

        private readonly struct EffectWorkItem
        {
            public EffectWorkItem(
                IBoardEffect effect,
                IBoardEffectPresentationHandler handler)
            {
                Effect = effect;
                Handler = handler;
            }

            public IBoardEffect Effect { get; }
            public IBoardEffectPresentationHandler Handler { get; }
        }

    }
}
