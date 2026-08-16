using System.Collections.Generic;

namespace Blobs.Core
{
    /// <summary>
    /// Context for a movement-behavior hook fired while a blob travels along its path.
    /// <see cref="Board"/> is the resolver's simulation board.
    /// </summary>
    public sealed class MoveBehaviorContext
    {
        public MoveBehaviorContext(
            BoardState board,
            BlobState mover,
            GridPosition departedTile,
            bool departedTileWasMergeSite,
            IBlobIdFactory idFactory)
        {
            Board = board;
            Mover = mover;
            DepartedTile = departedTile;
            DepartedTileWasMergeSite = departedTileWasMergeSite;
            IdFactory = idFactory;
        }

        public BoardState Board { get; }

        /// <summary>Moving blob state at the moment of departure (position = departed tile).</summary>
        public BlobState Mover { get; }

        public GridPosition DepartedTile { get; }

        /// <summary>
        /// True when a merge resolved at the departed tile earlier in this move.
        /// A successful merge should not also leave a new blob behind.
        /// </summary>
        public bool DepartedTileWasMergeSite { get; }

        public IBlobIdFactory IdFactory { get; }
    }

    /// <summary>
    /// Per-blob-type locomotion hooks, registered in the rule book like traits.
    /// Behaviors contribute effects to the step in which the mover departs a tile.
    /// </summary>
    public interface IMoveBehavior
    {
        void OnTileDeparted(MoveBehaviorContext context, List<IBoardEffect> effects);
    }

    /// <summary>
    /// Trail blob behavior: leaves a normal blob of the mover's trail color on every
    /// departed tile that was not the site of a merge during this move.
    /// </summary>
    public sealed class TrailMoveBehavior : IMoveBehavior
    {
        public void OnTileDeparted(MoveBehaviorContext context, List<IBoardEffect> effects)
        {
            if (context.DepartedTileWasMergeSite)
                return;


            string id = context.IdFactory.CreateId(
                context.Board,
                $"{context.Mover.Id}-trail");
            var state = new BlobState(
                id,
                BlobType.Normal,
                context.DepartedTile);
            state.AddModel(new ColorBlobModel(context.Mover.GetModel<TrailBlobModel>().TrailColor));
            effects.Add(new SpawnBlobEffect(state));
        }
    }
}
