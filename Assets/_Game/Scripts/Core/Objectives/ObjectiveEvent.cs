namespace Blobs.Core
{
    public enum ObjectiveEventKind
    {
        /// <summary>A blob left the board by being eaten or otherwise cleared.</summary>
        BlobCleared,

        /// <summary>A monster settled into a nest. The nest stays on the board.</summary>
        BlobNested,

        /// <summary>A tile was used by a mechanic, such as a ghost resting on a Grave.</summary>
        TileActivated,
    }

    /// <summary>
    /// One objective-relevant outcome extracted from a resolved move. Blob and tile data are
    /// captured as they were at the moment of the outcome.
    /// </summary>
    public readonly struct ObjectiveEvent
    {
        private ObjectiveEvent(ObjectiveEventKind kind, BlobState blob, TileState tile)
        {
            Kind = kind;
            Blob = blob;
            Tile = tile;
        }

        public ObjectiveEventKind Kind { get; }
        public BlobState Blob { get; }
        public TileState Tile { get; }

        public static ObjectiveEvent Cleared(BlobState blob) =>
            new(ObjectiveEventKind.BlobCleared, blob, null);

        public static ObjectiveEvent Nested(BlobState nest) =>
            new(ObjectiveEventKind.BlobNested, nest, null);

        public static ObjectiveEvent Activated(TileState tile) =>
            new(ObjectiveEventKind.TileActivated, null, tile);
    }
}
