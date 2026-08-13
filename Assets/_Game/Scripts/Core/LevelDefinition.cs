using System;
using System.Collections.Generic;

namespace Blobs.Core
{
    public sealed class LevelDefinition
    {
        private readonly List<BlobState> _blobs;

        public LevelDefinition(string id, int width, int height, IEnumerable<BlobState> blobs)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Level id cannot be empty.", nameof(id));

            Id = id;
            Width = width;
            Height = height;
            _blobs = blobs != null ? new List<BlobState>(blobs) : new List<BlobState>();

            Validate();
        }

        public string Id { get; }
        public int Width { get; }
        public int Height { get; }
        public IReadOnlyList<BlobState> Blobs => _blobs;

        public BoardState CreateInitialBoard()
        {
            return new BoardState(Width, Height, _blobs);
        }

        private void Validate()
        {
            _ = new BoardState(Width, Height, _blobs);
        }
    }
}
