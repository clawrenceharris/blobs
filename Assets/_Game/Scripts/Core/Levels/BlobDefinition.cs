using System;
using System.Collections.Generic;

namespace Blobs.Core
{


    /// <summary>
    /// Immutable authored blob data after content has been converted into Core types.
    /// </summary>
    public abstract class BlobDefinition
    {
        public string Id { get; }
        public GridPosition Position { get; }
        public BlobType Type { get; }

        public BlobDefinition(
            string id,
            GridPosition position,
            BlobType type)
        {
            Id = id;
            Position = position;
            Type = type;
        }
    }

    public abstract class ColorBlobDefinition : BlobDefinition
    {
        public BlobColor Color { get; }

        public ColorBlobDefinition(string id, GridPosition position, BlobColor color, BlobType type) : base(id, position, type)
        {
            Color = color;
        }



    }
    /// <summary>
    /// Basic blob definition used by the current source-to-target merge rules.
    /// </summary>
    public sealed class NormalBlobDefinition : ColorBlobDefinition
    {
        public NormalBlobDefinition(
            string id,
            GridPosition position,
            BlobColor color
            )
            : base(id, position, color, BlobType.Normal)
        {
        }
    }
    public sealed class FlagBlobDefinition : ColorBlobDefinition
    {
        public FlagBlobDefinition(
            string id,
            GridPosition position,
            BlobColor color)
            : base(
                id,
                position,
                color,
                BlobType.Flag)
        {
        }
    }

    public sealed class RockBlobDefinition : BlobDefinition
    {
        public RockBlobDefinition(
            string id,
            GridPosition position)
            : base(id, position, BlobType.Rock)
        {
        }
    }

    /// <summary>
    /// Trail blob definition. Moves like a normal blob but leaves normal blobs of
    /// <see cref="TrailColor"/> on the empty tiles it departs during a move.
    /// </summary>
    public sealed class TrailBlobDefinition : ColorBlobDefinition
    {
        public TrailBlobDefinition(
            string id,
            GridPosition position,
            BlobColor color,
            BlobColor trailColor)
            : base(
                id,
                position,
                color,
                BlobType.Trail)
        {
            TrailColor = trailColor;
        }

        public BlobColor TrailColor { get; }
    }

    public sealed class GhostBlobDefinition : BlobDefinition
    {
        public GhostBlobDefinition(
            string id,
            GridPosition position)
            : base(id, position, BlobType.Ghost)
        {
        }

    }

}
