using System;
using System.Collections.Generic;

namespace Blobs.Core
{


    /// <summary>
    /// Immutable authored tile data after content has been converted into Core types.
    /// </summary>
    public abstract class TileDefinition
    {
        public string Id { get; }
        public GridPosition Position { get; }
        public TileType Type { get; }

        protected TileDefinition(
            string id,
            GridPosition position,
            TileType type)
        {
            Id = id;
            Position = position;
            Type = type;
        }
    }



    /// <summary>
    /// Basic traversable tile definition.
    /// </summary>
    public sealed class GraveTileDefinition : TileDefinition
    {
        public GraveTileDefinition(string id, GridPosition position)
            : base(id, position, TileType.Grave) { }
    }



}
