using System;
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

        /// <summary>
        /// Composes this explicit effect in flat-list order. No neighboring effects are examined.
        /// Animated work is appended to Timeline and played later by its async owner.
        /// </summary>
        bool PresentOrdered(IBoardEffect effect, BoardEffectPresentationContext context);

        /// <summary>
        /// Composes this effect inside a grouped step after phase dispatch. A handler may
        /// Join existing travel (departure spawns) or Append later work (arrival removal).
        /// </summary>
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

}
