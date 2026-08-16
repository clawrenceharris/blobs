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
                return MoveFailureReason.SourceMissing;

            return _rules.GetTraits(blob.Type).CanBeSource
                ? MoveFailureReason.None
                : MoveFailureReason.SourceCannotMove;
        }

        public MoveResult Resolve(
            BoardState board,
            MoveIntent intent,
            LevelObjectiveDefinition objective = null)
        {
            BlobState source = board.GetBlob(intent.SourceId);
            if (source == null)
                return MoveResult.Failed(MoveFailureReason.SourceMissing);

            BlobState target = board.GetBlob(intent.TargetId);
            if (target == null)
                return MoveResult.Failed(MoveFailureReason.TargetMissing);

            if (source.Id == target.Id)
                return MoveResult.Failed(MoveFailureReason.SameBlob);

            if (!_rules.GetTraits(source.Type).CanBeSource)
                return MoveResult.Failed(MoveFailureReason.SourceCannotMove);

            if (!source.Position.IsAlignedWith(target.Position))
                return MoveResult.Failed(MoveFailureReason.NotAligned);

            // Plan the whole move on a simulation board so mid-path collisions observe
            // true occupancy and failures leave the real board untouched.
            BoardState simulation = board.Clone();
            _rules.TryGetMoveBehavior(source.Type, out IMoveBehavior behavior);

            var steps = new List<MoveStep>();
            var followUpSteps = new List<MoveStep>();
            var mergeSites = new HashSet<GridPosition>();

            BlobState mover = source;

            MovePlan movePlan = null;
            if (_rules.TryGetMoveStrategy(target.Type, out IMoveStrategy moveStrategy))
            {
                movePlan = moveStrategy?.BuildPlan(new MoveContext(simulation, mover, target, mover.Position == target.Position));
                if (movePlan != null && !movePlan.Succeeded)
                {
                    return MoveResult.Failed(movePlan.FailureReason);
                }
            }

            GridPosition current = movePlan?.Move.From ?? source.Position;
            GridPosition goal = movePlan?.Move.To ?? target.Position;

            int stepX = goal.X == current.X ? 0 : goal.X > current.X ? 1 : -1;
            int stepY = goal.Y == current.Y ? 0 : goal.Y > current.Y ? 1 : -1;


            int stepCount = 0;
            while (current != goal)
            {
                if (stepCount > 100)
                {
                    return MoveResult.Failed(MoveFailureReason.MoveTimeout);
                }
                stepCount++;
                var next = new GridPosition(current.X + stepX, current.Y + stepY);
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

                    if (!_rules.TryGetMergeStrategy(
                            mover.Type,
                            occupant.Type,
                            out IMergeStrategy mergeStrategy))
                    {
                        return MoveResult.Failed(
                            MoveFailureReason.UnsupportedInteraction);
                    }

                    var context = new MoveContext(
                      simulation,
                      mover,
                      occupant,
                      isFinalTarget: occupant.Id == target.Id);
                    CollisionPlan collisionPlan = mergeStrategy.BuildPlan(context);
                    if (!collisionPlan.Succeeded)
                        return MoveResult.Failed(collisionPlan.FailureReason);

                    // Resolve the collision tile first so the mover can enter it.
                    stepEffects.AddRange(collisionPlan.Effects);

                    moverConsumed = collisionPlan.ConsumesMover;
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
                if (moverConsumed || (occupant != null && occupant.Id == target.Id))
                    break;

                mover = simulation.GetBlob(mover.Id);
                current = next;
            }

            steps.AddRange(followUpSteps);

            // Commit atomically: replay the validated timeline onto the real board.
            var flattened = new List<IBoardEffect>();
            foreach (MoveStep step in steps)
                flattened.AddRange(step.Effects);

            ApplyEffects(board, flattened);

            return new MoveResult(
                true,
                MoveFailureReason.None,
                flattened,
                ObjectiveEvaluator.IsComplete(board, objective),
                steps);
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
