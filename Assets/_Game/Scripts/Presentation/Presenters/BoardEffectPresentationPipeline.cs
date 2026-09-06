using System;
using System.Collections.Generic;
using Blobs.Core;
using DG.Tweening;

namespace Blobs.Presentation
{
    /// <summary>
    /// Identifies where an effect contributes within a composed move beat.
    /// The composer uses phases to preserve readable animation ordering without knowing effect types.
    /// </summary>
    public enum BoardEffectPresentationPhase
    {
        /// <summary>Movement that establishes the duration of a beat.</summary>
        Travel,

        /// <summary>Feedback created at the position an actor leaves.</summary>
        Departure,

        /// <summary>Changes that become visible after travel reaches its target.</summary>
        Arrival,

        /// <summary>Secondary feedback that follows the primary interaction.</summary>
        Aftermath
    }

    /// <summary>
    /// Presentation services and timing supplied to an effect handler.
    /// </summary>
    public readonly struct BoardEffectPresentationContext
    {
        internal BoardEffectPresentationContext(
            BlobPresenter blobs,
            TilePresenter tiles,
            PresentationTimeline timeline,
            Ease movementEase,
            Action contactFeedback)
        {
            Blobs = blobs;
            Tiles = tiles;
            Timeline = timeline;
            MovementEase = movementEase;
            ContactFeedback = contactFeedback;
        }

        public BlobPresenter Blobs { get; }
        public TilePresenter Tiles { get; }
        public PresentationTimeline Timeline { get; }
        public Ease MovementEase { get; }
        public Action ContactFeedback { get; }
        public bool IsAnimated => Timeline.IsAnimated;
    }

    /// <summary>
    /// Adapts one Core board-effect type into presentation behavior.
    /// Registering another handler extends effect presentation without changing <see cref="BoardPresenter"/>.
    /// </summary>
    public interface IBoardEffectPresentationHandler
    {
        Type EffectType { get; }
        BoardEffectPresentationPhase Phase { get; }

        /// <summary>
        /// Whether this effect establishes an arrival point for final-step easing and contact feedback.
        /// </summary>
        bool RepresentsMovement { get; }

        /// <summary>Presents the effect in its original order outside a semantic move step.</summary>
        bool PresentOrdered(IBoardEffect effect, BoardEffectPresentationContext context);

        /// <summary>Presents the effect within the handler's declared beat phase.</summary>
        bool PresentInBeat(IBoardEffect effect, BoardEffectPresentationContext context);
    }

    /// <summary>
    /// Strongly typed base class for presentation handlers registered by Core effect type.
    /// </summary>
    public abstract class BoardEffectPresentationHandler<TEffect> :
        IBoardEffectPresentationHandler where TEffect : IBoardEffect
    {
        public Type EffectType => typeof(TEffect);
        public abstract BoardEffectPresentationPhase Phase { get; }
        public virtual bool RepresentsMovement => false;

        public bool PresentOrdered(
            IBoardEffect effect,
            BoardEffectPresentationContext context)
        {
            return PresentOrdered((TEffect)effect, context);
        }

        public bool PresentInBeat(
            IBoardEffect effect,
            BoardEffectPresentationContext context)
        {
            return PresentInBeat((TEffect)effect, context);
        }

        protected abstract bool PresentOrdered(
            TEffect effect,
            BoardEffectPresentationContext context);

        protected abstract bool PresentInBeat(
            TEffect effect,
            BoardEffectPresentationContext context);
    }

    /// <summary>
    /// Resolves effect handlers and composes ordered effects or phase-aware move steps.
    /// Concrete effect knowledge is intentionally kept out of the board coordinator.
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
        private readonly BlobPresenter _blobs;
        private readonly TilePresenter _tiles;

        public BoardEffectPresentationPipeline(BlobPresenter blobs, TilePresenter tiles)
        {
            _blobs = blobs ?? throw new ArgumentNullException(nameof(blobs));
            _tiles = tiles ?? throw new ArgumentNullException(nameof(tiles));

            Register(new MoveBlobPresentationHandler());
            Register(new SpawnBlobPresentationHandler());
            Register(new RemoveBlobPresentationHandler());
            Register(new MergeIntoFlagPresentationHandler());
        }

        public void Register(IBoardEffectPresentationHandler handler)
        {
            ValidateHandler(handler);
            _handlers[handler.EffectType] = handler;
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

        public bool PresentOrderedEffects(
            IReadOnlyList<IBoardEffect> effects,
            PresentationTimeline timeline)
        {
            for (int i = 0; i < effects.Count; i++)
            {
                if (TryPresentNormalMerge(
                        effects,
                        ref i,
                        timeline,
                        out bool mergeApplied))
                {
                    if (!mergeApplied)
                        return false;
                    continue;
                }

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

            if (step.Kind == MoveStepKind.Merge &&
                TryFindNormalMerge(
                    step,
                    out MoveBlobEffect mergeSource,
                    out RemoveBlobEffect mergeTarget))
            {
                if (!_blobs.NormalMerges.Present(
                        mergeSource,
                        mergeTarget,
                        beat,
                        contactFeedback))
                {
                    beat.Kill();
                    return false;
                }

                if (!PresentRemainingMergeEffects(
                        step,
                        mergeSource,
                        mergeTarget,
                        beat,
                        movementEase))
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

        private bool PresentRemainingMergeEffects(
            MoveStep step,
            MoveBlobEffect mergeSource,
            RemoveBlobEffect mergeTarget,
            PresentationTimeline beat,
            Ease movementEase)
        {
            List<EffectWorkItem>[] phases = CreatePhaseBuckets();
            foreach (IBoardEffect effect in step.Effects)
            {
                if (ReferenceEquals(effect, mergeSource) ||
                    ReferenceEquals(effect, mergeTarget))
                {
                    continue;
                }

                if (!TryResolve(effect, out IBoardEffectPresentationHandler handler))
                    return false;
                phases[(int)handler.Phase].Add(new EffectWorkItem(effect, handler));
            }

            return PresentPhaseBuckets(
                phases,
                beat,
                movementEase,
                contactFeedback: null);
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

        private static bool TryFindNormalMerge(
            MoveStep step,
            out MoveBlobEffect move,
            out RemoveBlobEffect remove)
        {
            move = null;
            remove = null;

            foreach (IBoardEffect effect in step.Effects)
            {
                if (effect is MoveBlobEffect candidateMove)
                {
                    move = candidateMove;
                    break;
                }
            }

            if (move == null)
                return false;

            foreach (IBoardEffect effect in step.Effects)
            {
                if (effect is RemoveBlobEffect candidateRemove &&
                    candidateRemove.BlobId != move.BlobId &&
                    candidateRemove.At == move.To)
                {
                    remove = candidateRemove;
                    break;
                }
            }

            return remove != null;
        }

        private bool TryPresentNormalMerge(
            IReadOnlyList<IBoardEffect> effects,
            ref int index,
            PresentationTimeline timeline,
            out bool applied)
        {
            applied = false;
            if (effects[index] is not RemoveBlobEffect target ||
                index + 1 >= effects.Count ||
                effects[index + 1] is not MoveBlobEffect source ||
                source.To != target.At)
            {
                return false;
            }

            applied = _blobs.NormalMerges.Present(
                source,
                target,
                timeline);
            index++;
            return true;
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

        private sealed class MoveBlobPresentationHandler :
            BoardEffectPresentationHandler<MoveBlobEffect>
        {
            public override BoardEffectPresentationPhase Phase =>
                BoardEffectPresentationPhase.Travel;
            public override bool RepresentsMovement => true;

            protected override bool PresentOrdered(
                MoveBlobEffect effect,
                BoardEffectPresentationContext context)
            {
                if (!context.Blobs.Transitions.TryMove(
                        effect.BlobId,
                        effect.To,
                        Ease.Linear,
                        context.Timeline,
                        onArrival: null,
                        out Tween animation))
                {
                    return false;
                }

                context.Timeline.Append(animation);
                return true;
            }

            protected override bool PresentInBeat(
                MoveBlobEffect effect,
                BoardEffectPresentationContext context)
            {
                if (!context.Blobs.Transitions.TryMove(
                        effect.BlobId,
                        effect.To,
                        context.MovementEase,
                        context.Timeline,
                        context.ContactFeedback,
                        out Tween animation))
                {
                    return false;
                }

                context.Timeline.Append(animation);
                return true;
            }
        }

        private sealed class SpawnBlobPresentationHandler :
            BoardEffectPresentationHandler<SpawnBlobEffect>
        {
            public override BoardEffectPresentationPhase Phase =>
                BoardEffectPresentationPhase.Departure;

            protected override bool PresentOrdered(
                SpawnBlobEffect effect,
                BoardEffectPresentationContext context)
            {
                if (!context.Blobs.Transitions.TryCreate(
                        effect.Blob,
                        context.Timeline,
                        out Tween animation))
                {
                    return false;
                }

                context.Timeline.Append(animation);
                return true;
            }

            protected override bool PresentInBeat(
                SpawnBlobEffect effect,
                BoardEffectPresentationContext context)
            {
                if (!context.Blobs.Transitions.TryCreate(
                        effect.Blob,
                        context.Timeline,
                        out Tween animation))
                {
                    return false;
                }

                context.Timeline.Join(animation);
                return true;
            }
        }

        private sealed class RemoveBlobPresentationHandler :
            BoardEffectPresentationHandler<RemoveBlobEffect>
        {
            public override BoardEffectPresentationPhase Phase =>
                BoardEffectPresentationPhase.Arrival;

            protected override bool PresentOrdered(
                RemoveBlobEffect effect,
                BoardEffectPresentationContext context)
            {
                return PresentRemoval(effect, context);
            }

            protected override bool PresentInBeat(
                RemoveBlobEffect effect,
                BoardEffectPresentationContext context)
            {
                return PresentRemoval(effect, context);
            }

            private static bool PresentRemoval(
                RemoveBlobEffect effect,
                BoardEffectPresentationContext context)
            {
                if (!context.Blobs.Transitions.TryRemove(
                        effect.BlobId,
                        context.Timeline,
                        out Tween animation))
                {
                    return false;
                }

                context.Timeline.Append(animation);
                return true;
            }
        }

        private sealed class MergeIntoFlagPresentationHandler :
            BoardEffectPresentationHandler<MergeIntoFlagEffect>
        {
            public override BoardEffectPresentationPhase Phase =>
                BoardEffectPresentationPhase.Travel;
            public override bool RepresentsMovement => true;

            protected override bool PresentOrdered(
                MergeIntoFlagEffect effect,
                BoardEffectPresentationContext context)
            {
                return context.Blobs.FlagCaptures.Present(
                    effect,
                    context.Timeline);
            }

            protected override bool PresentInBeat(
                MergeIntoFlagEffect effect,
                BoardEffectPresentationContext context)
            {
                return context.Blobs.FlagCaptures.Present(
                    effect,
                    context.Timeline,
                    context.ContactFeedback);
            }
        }
    }
}
