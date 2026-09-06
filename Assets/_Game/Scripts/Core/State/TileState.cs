namespace Blobs.Core
{
    public sealed class TileState
    {
        public string Id { get; }
        public GridPosition Position { get; }
        public TileType Type { get; }

        public TileState(
            string id,  
            GridPosition position,
            TileType type
           )
        {
            Id = id;
            Position = position;
            Type = type;
        }

        
    }
}
