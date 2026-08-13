using System;
using System.Collections.Generic;

namespace Blobs.Core
{
    public sealed class BoardState
    {
        private readonly Dictionary<string, BlobState> _blobsById;
        private readonly Dictionary<GridPosition, string> _blobIdsByPosition;

        public BoardState(int width, int height, IEnumerable<BlobState> blobs)
        {
            if (width <= 0)
                throw new ArgumentOutOfRangeException(nameof(width), "Board width must be positive.");
            if (height <= 0)
                throw new ArgumentOutOfRangeException(nameof(height), "Board height must be positive.");

            Width = width;
            Height = height;
            _blobsById = new Dictionary<string, BlobState>();
            _blobIdsByPosition = new Dictionary<GridPosition, string>();

            if (blobs == null)
                return;

            foreach (var blob in blobs)
                AddBlob(blob);
        }

        private BoardState(int width, int height, Dictionary<string, BlobState> blobsById)
        {
            Width = width;
            Height = height;
            _blobsById = new Dictionary<string, BlobState>(blobsById);
            _blobIdsByPosition = new Dictionary<GridPosition, string>();

            foreach (var blob in _blobsById.Values)
                _blobIdsByPosition.Add(blob.Position, blob.Id);
        }

        public int Width { get; }
        public int Height { get; }
        public int BlobCount => _blobsById.Count;

        public IEnumerable<BlobState> Blobs => _blobsById.Values;

        public BoardState Clone()
        {
            return new BoardState(Width, Height, _blobsById);
        }

        public bool IsInBounds(GridPosition position)
        {
            return position.X >= 0 && position.Y >= 0 && position.X < Width && position.Y < Height;
        }

        public BlobState GetBlob(string id)
        {
            if (id == null)
                return null;

            _blobsById.TryGetValue(id, out var blob);
            return blob;
        }

        public BlobState GetBlobAt(GridPosition position)
        {
            if (!_blobIdsByPosition.TryGetValue(position, out var id))
                return null;

            return _blobsById[id];
        }

        public void AddBlob(BlobState blob)
        {
            if (blob == null)
                throw new ArgumentNullException(nameof(blob));
            if (!IsInBounds(blob.Position))
                throw new ArgumentOutOfRangeException(nameof(blob), "Blob position is outside the board.");
            if (_blobsById.ContainsKey(blob.Id))
                throw new ArgumentException("Duplicate blob id: " + blob.Id, nameof(blob));
            if (_blobIdsByPosition.ContainsKey(blob.Position))
                throw new ArgumentException("Multiple blobs cannot occupy " + blob.Position + ".", nameof(blob));

            _blobsById.Add(blob.Id, blob);
            _blobIdsByPosition.Add(blob.Position, blob.Id);
        }

        public void RemoveBlob(string id)
        {
            var blob = GetBlob(id);
            if (blob == null)
                throw new InvalidOperationException("Blob does not exist: " + id);

            _blobsById.Remove(id);
            _blobIdsByPosition.Remove(blob.Position);
        }

        public void MoveBlob(string id, GridPosition to)
        {
            var blob = GetBlob(id);
            if (blob == null)
                throw new InvalidOperationException("Blob does not exist: " + id);
            if (!IsInBounds(to))
                throw new ArgumentOutOfRangeException(nameof(to), "Move target is outside the board.");
            if (_blobIdsByPosition.TryGetValue(to, out var occupantId) && occupantId != id)
                throw new InvalidOperationException("Target position is occupied: " + to);

            _blobIdsByPosition.Remove(blob.Position);
            var moved = blob.WithPosition(to);
            _blobsById[id] = moved;
            _blobIdsByPosition[to] = id;
        }

        public bool HasAnyClearableBlobs()
        {
            foreach (var blob in _blobsById.Values)
            {
                if (blob.IsClearable)
                    return true;
            }

            return false;
        }
    }
}
