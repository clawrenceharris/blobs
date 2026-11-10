namespace Blobs.Core
{
    /// <summary>
    /// What a level objective item counts. New mechanics add a subclass or an optional filter
    /// rather than changing how progress is tracked.
    /// </summary>
    public abstract class ObjectiveTarget
    {
        public abstract bool Matches(ObjectiveEvent objectiveEvent);
    }

    /// <summary>
    /// Counts blobs of one type, optionally narrowed by component values. Nests count when a
    /// monster is nested in them; every other type counts when the blob is cleared.
    /// </summary>
    public sealed class BlobObjectiveTarget : ObjectiveTarget
    {
        public BlobObjectiveTarget(BlobType type, BlobColor? color = null, BlobSize? size = null)
        {
            Type = type;
            Color = color;
            Size = size;
        }

        public BlobType Type { get; }

        /// <summary>Required color, or null to accept any color.</summary>
        public BlobColor? Color { get; }

        /// <summary>Required size, or null to accept any size.</summary>
        public BlobSize? Size { get; }

        public ObjectiveEventKind CountedEvent =>
            Type == BlobType.Nest ? ObjectiveEventKind.BlobNested : ObjectiveEventKind.BlobCleared;

        public override bool Matches(ObjectiveEvent objectiveEvent)
        {
            return objectiveEvent.Kind == CountedEvent &&
                objectiveEvent.Blob != null &&
                Matches(objectiveEvent.Blob);
        }

        public bool Matches(BlobState blob)
        {
            return blob.Type == Type &&
                (!Color.HasValue || blob.Components.Color?.Color == Color.Value) &&
                (!Size.HasValue || blob.Components.Size?.Size == Size.Value);
        }
    }

    /// <summary>
    /// Counts activations of tiles of one type.
    /// </summary>
    public sealed class TileObjectiveTarget : ObjectiveTarget
    {
        public TileObjectiveTarget(TileType type)
        {
            Type = type;
        }

        public TileType Type { get; }

        public override bool Matches(ObjectiveEvent objectiveEvent)
        {
            return objectiveEvent.Kind == ObjectiveEventKind.TileActivated &&
                objectiveEvent.Tile != null &&
                objectiveEvent.Tile.Type == Type;
        }
    }
}
