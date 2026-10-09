using System;
using System.Collections.Generic;
using Blobs.Core;

namespace Blobs.Core
{


    /// <summary>
    /// Immutable authored blob data after content has been converted into Core types.
    /// </summary>
    public abstract class BlobDefinition
    {

        public BlobDefinition(
            string id,
            GridPosition position,
            BlobType type)
        {
            Id = id;
            Position = position;
            Type = type;
        }
        public string Id { get; }
        public GridPosition Position { get; }
        public BlobType Type { get; }

    }




    /// <summary>
    /// Basic blob definition used by the current source-to-target merge rules.
    /// </summary>
    public sealed class NormalBlobDefinition : BlobDefinition
    {

        public NormalBlobDefinition(
            string id,
            GridPosition position,
            BlobColor color,
            BlobSize size = BlobSize.Normal
            )
            : base(id, position, BlobType.Normal)
        {
            Size = size;
            Color = color;
        }
        public BlobSize Size { get; }
        public BlobColor Color { get; }
    }
    public sealed class FlagBlobDefinition : BlobDefinition
    {
        public FlagBlobDefinition(
            string id,
            GridPosition position,
            BlobColor color)
            : base(id, position, BlobType.Flag)
        {
            Color = color;
        }
        public BlobColor Color { get; }

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
    public sealed class TrailBlobDefinition : BlobDefinition
    {
        public TrailBlobDefinition(
     string id,
     GridPosition position,
     BlobColor color,
     BlobColor trailColor,
     BlobSize size = BlobSize.Normal)
     : base(
         id,
         position,
         BlobType.Trail)
        {
            TrailColor = trailColor;
            Color = color;
            Size = size;
        }
        public BlobSize Size { get; }
        public BlobColor TrailColor { get; }
        public BlobColor Color { get; }


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
