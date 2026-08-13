using System;
using System.Collections.Generic;
using System.Linq;

namespace Blobs.Core
{
    public sealed class BoardState
    {
        private readonly Dictionary<string, BlobState> _blobsById;
        private readonly Dictionary<string, TileState> _tilesById;
        private readonly Dictionary<GridPosition, string> _blobIdsByPosition;
        private readonly Dictionary<GridPosition, string> _tileIdsByPosition;

        public BoardState(int width, int height, IEnumerable<BlobState> blobs, IEnumerable<TileState> tiles)
        {
            if (width <= 0)
                throw new ArgumentOutOfRangeException(nameof(width), "Board width must be positive.");
            if (height <= 0)
                throw new ArgumentOutOfRangeException(nameof(height), "Board height must be positive.");

            Width = width;
            Height = height;
            _blobsById = new Dictionary<string, BlobState>();
            _tilesById = new Dictionary<string, TileState>();
            _blobIdsByPosition = new Dictionary<GridPosition, string>();
            _tileIdsByPosition = new Dictionary<GridPosition, string>();    
            if (blobs != null)
            {
                foreach (var blob in blobs)
                    AddBlob(blob);
            }

            if (tiles != null)
            {
                foreach (var tile in tiles)
                    AddTile(tile);
            }
        }

        private BoardState(
            int width,
            int height,
            Dictionary<string, BlobState> blobsById,
            Dictionary<string, TileState> tilesById)
        {
            Width = width;
            Height = height;
            _blobsById = new Dictionary<string, BlobState>(blobsById);
            _tilesById = new Dictionary<string, TileState>(tilesById);
            _blobIdsByPosition = new Dictionary<GridPosition, string>();
            _tileIdsByPosition = new Dictionary<GridPosition, string>();

            foreach (var blob in _blobsById.Values)
                _blobIdsByPosition.Add(blob.Position, blob.Id);
            foreach (var tile in _tilesById.Values)
                _tileIdsByPosition.Add(tile.Position, tile.Id);
        }

        public int Width { get; }
        public int Height { get; }
        public int BlobCount => _blobsById.Count;

        public IReadOnlyList<BlobState> Blobs => _blobsById.Values.ToList();
        public IReadOnlyList<TileState> Tiles => _tilesById.Values.ToList();
        public BoardState Clone()
        {
            return new BoardState(Width, Height, _blobsById, _tilesById);
        }

        public bool IsInside(GridPosition position)
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
            if (!IsInside(blob.Position))
                throw new ArgumentOutOfRangeException(nameof(blob), "Blob position is outside the board.");
            if (_blobsById.ContainsKey(blob.Id))
                throw new ArgumentException("Duplicate blob id: " + blob.Id, nameof(blob));
            if (_blobIdsByPosition.ContainsKey(blob.Position))
                throw new ArgumentException("Multiple blobs cannot occupy " + blob.Position + ".", nameof(blob));

            _blobsById.Add(blob.Id, blob);
            _blobIdsByPosition.Add(blob.Position, blob.Id);
        }
        public void AddTile(TileState tile)
        {
            if (tile == null)
                throw new ArgumentNullException(nameof(tile));
            if (!IsInside(tile.Position))
                throw new ArgumentOutOfRangeException(nameof(tile), "Tile position is outside the board.");
            if (_tilesById.ContainsKey(tile.Id))
                throw new ArgumentException("Duplicate tile id: " + tile.Id, nameof(tile));
            if (_tileIdsByPosition.ContainsKey(tile.Position))
                throw new ArgumentException("Multiple tiles cannot occupy " + tile.Position + ".", nameof(tile));

            _tilesById.Add(tile.Id, tile);
            _tileIdsByPosition.Add(tile.Position, tile.Id);
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
            if (!IsInside(to))
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
