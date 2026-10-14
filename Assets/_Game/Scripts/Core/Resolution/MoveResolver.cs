using System.Collections.Generic;

namespace Blobs.Core
{
    /// <summary>
    /// Deterministic Core rule entry point for move resolution. A move intent is decomposed
    /// into a per-tile timeline of steps: the mover traverses empty tiles (firing movement
    /// behaviors such as trail spawning) and chain-merges with every occupant on its path.
    /// The whole intent is validated on a simulation board and committed atomically.
    /// </summary>
    public sealed class MoveResolver
    {
        private readonly IBlobRuleBook _rules;
        private readonly IBlobIdFactory _idFactory;

        public MoveResolver(
            IBlobRuleBook rules = null,
            IBlobIdFactory idFactory = null)
        {
            _rules = rules ?? BlobRuleBook.CreateDefault();
            _idFactory = idFactory ?? new SequentialBlobIdFactory();
        }

        public MoveFailureReason ValidateSourceSelection(
            BoardState board,
            string blobId)
        {
            BlobState blob = board.GetBlob(blobId);

            if (blob == null)
                return MoveFailureReason.SourceOrTargetMissing;

            return _rules.GetTraits(blob.Type).CanBeSource
                ? MoveFailureReason.None
                : MoveFailureReason.SourceCannotMove;
        }

        public MoveResult Resolve(
            BoardState board,
            MoveIntent intent,
            LevelObjectiveDefinition objective = null)
        {
            BlobState source = intent.Source;
            BlobState intendedTarget = intent.Target;

            if (source == null || (!intent.IsDirectional && intendedTarget == null))
                return MoveResult.Failed(source?.Id, intendedTarget?.Id, MoveFailureReason.SourceOrTargetMissing);

            if (!intent.IsDirectional && source.Id == intendedTarget.Id)
                return MoveResult.Failed(source.Id, intendedTarget.Id, MoveFailureReason.SameBlob);

            if (!_rules.GetTraits(source.Type).CanBeSource)
                return MoveResult.Failed(source.Id, intendedTarget?.Id, MoveFailureReason.SourceCannotMove);

            if (!intent.IsDirectional && !source.Position.IsAlignedWith(intendedTarget.Position))
                return MoveResult.Failed(source.Id, intendedTarget.Id, MoveFailureReason.NotAligned);

            BoardState simulation = board.Clone();

            MovePlan movePlan;
            if (intent.IsDirectional)
            {
                movePlan = MovePlan.Directional(
                    source,
                    GetBoardEdge(source.Position, intent.Direction, simulation));
            }
            else
            {
                if (!_rules.TryGetMoveStrategy(source.Type, intendedTarget.Type, out IMoveStrategy moveStrategy))
                {
                    return MoveResult.Failed(source.Id, intendedTarget.Id, MoveFailureReason.UnsupportedInteraction);
                }
                movePlan = moveStrategy.BuildPlan(new MoveContext(
                    board: simulation,
                    plan: MovePlan.Default(source, intendedTarget),
                    source: source,
                    target: intendedTarget,
                    intent: intent,
                    isFinalTarget: true
                ));
            }



            BlobState target = movePlan.Target;
            if (!movePlan.Succeeded)
            {
                return MoveResult.Failed(source.Id, target?.Id, movePlan.FailureReason);
            }

            // Plan the whole move on a simulation board so mid-path collisions observe
            _rules.TryGetMoveBehavior(source.Type, out IMoveBehavior behavior);

            var endPosition = movePlan.EndPosition;
            var startPosition = movePlan.StartPosition;
            var current = startPosition;
            var direction = (endPosition - startPosition).Normalized();

            var steps = new List<MoveStep>();
            var followUpSteps = new List<MoveStep>();
            var mergeSites = new HashSet<GridPosition>();




            BlobState mover = source;

            int stepCount = 0;
            while (current != movePlan.EndPosition)
            {
                if (stepCount > 100)
                {
                    return MoveResult.Failed(source.Id, target?.Id, MoveFailureReason.MoveTimeout);
                }
                stepCount++;
                var next = current + direction;
                var tile = simulation.GetTileAt(next);
                if (!simulation.IsInside(next) || simulation.EmptyPositions.Contains(next) ||
                    (tile != null && !tile.Type.IsTraversable()))
                    return MoveResult.Failed(source.Id, intendedTarget?.Id, MoveFailureReason.PathBlocked);

                BlobState occupant = simulation.GetBlobAt(next);
                var stepEffects = new List<IBoardEffect>();
                MoveStepKind kind;
                bool moverConsumed = false;
                if (occupant == null)
                {
                    kind = MoveStepKind.Traverse;
                    stepEffects.Add(new MoveBlobEffect(mover.Id, current, next));
                }
                else
                {
                    kind = MoveStepKind.Merge;

                    if (!_rules.TryGetCollisionStrategy(
                            mover.Type,
                            occupant.Type,
                            out ICollisionStrategy collisionStrategy))
                    {
                        return MoveResult.Failed(
                            mover.Id, target?.Id, MoveFailureReason.UnsupportedInteraction);
                    }

                    var context = new MoveContext(
                      board: simulation,
                      plan: movePlan,
                      source: mover,
                      target: occupant,
                      intent: intent,
                      isFinalTarget: !intent.IsDirectional && occupant.Id == target.Id);
                    CollisionPlan collisionPlan = collisionStrategy.BuildPlan(context);
                    if (!collisionPlan.Succeeded)
                        return MoveResult.Failed(mover.Id, target?.Id, collisionPlan.FailureReason);

                    if (intent.IsDirectional)
                        target = occupant;

                    // Resolve the collision tile first so the mover can enter it.
                    stepEffects.AddRange(collisionPlan.Effects);
                    if (collisionPlan.Kind.BlocksMover())
                    {
                        target = occupant;
                        break;
                    }
                    if (collisionPlan.Kind.ConsumesMover())
                    {
                        moverConsumed = true;
                        target = occupant;
                    }

                    if (!moverConsumed)
                        stepEffects.Add(new MoveBlobEffect(mover.Id, mover.Position, next));

                    if (collisionPlan.FollowUpSteps.Count > 0)
                        followUpSteps.AddRange(collisionPlan.FollowUpSteps);

                    mergeSites.Add(next);
                }

                // Departure hooks fire after the mover has left the tile, so spawned
                // blobs never contest occupancy with the mover itself. Tiles that
                // hosted a merge earlier in this move never receive departure spawns.
                behavior?.OnTileDeparted(
                    new MoveBehaviorContext(
                        simulation,
                        mover,
                        current,
                        mergeSites.Contains(current),
                        _idFactory),
                    stepEffects);

                ApplyEffects(simulation, stepEffects);
                steps.Add(new MoveStep(kind, stepEffects));

                // A consuming merge or reaching the intent's target ends locomotion.
                if (moverConsumed || (!intent.IsDirectional &&
                    occupant != null && occupant.Id == intendedTarget.Id))
                    break;

                mover = simulation.GetBlob(mover.Id);
                current = next;
            }

            // Validate and finalize aftermath against the post-departure simulation.
            foreach (MoveStep followUp in followUpSteps)
            {
                var resolved = new List<IBoardEffect>();
                foreach (IBoardEffect effect in followUp.Effects)
                {

                    effect.Apply(simulation);
                    resolved.Add(effect);
                }
                steps.Add(new MoveStep(followUp.Kind, resolved));
            }

            if (!intent.IsDirectional && intendedTarget.Type == BlobType.Flag &&
                (target.Id != intendedTarget.Id || simulation.GetBlob(source.Id) != null))
                return MoveResult.Failed(source.Id, intendedTarget.Id, MoveFailureReason.FlagCaptureRequired);

            // Commit atomically: replay the validated timeline onto the real board.
            var flattened = new List<IBoardEffect>();
            foreach (MoveStep step in steps)
                flattened.AddRange(step.Effects);

            ApplyEffects(board, flattened);

            return new MoveResult(
                source.Id,
                target?.Id,
                true,
                MoveFailureReason.None,
                flattened,
                ObjectiveEvaluator.IsComplete(board, objective),
                steps
            );
        }

        private static GridPosition GetBoardEdge(
            GridPosition start,
            Direction direction,
            BoardState board)
        {
            switch (direction)
            {
                case Direction.North:
                    return new GridPosition(start.X, board.Height - 1);
                case Direction.East:
                    return new GridPosition(board.Width - 1, start.Y);
                case Direction.South:
                    return new GridPosition(start.X, 0);
                case Direction.West:
                    return new GridPosition(0, start.Y);
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(direction));
            }
        }

        /// <summary>
        /// Applies an ordered effect list to board state. This is intentionally validation-free
        /// because effects are assumed to come from a completed resolution pass.
        /// </summary>
        public void ApplyEffects(BoardState board, IReadOnlyList<IBoardEffect> effects)
        {
            foreach (var effect in effects)
                effect.Apply(board);
        }


    }
}
