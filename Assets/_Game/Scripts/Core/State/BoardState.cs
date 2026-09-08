using System;
using System.Collections.Generic;
using System.Linq;

namespace Blobs.Core
{
    /// <summary>
    /// Mutable Core board model with single-occupant blob and tile registries.
    /// This type is intentionally Unity-free so rules can be tested without a scene.
    /// </summary>
    public sealed class BoardState
    {
        private readonly Dictionary<string, BlobState> _blobsById;
        private readonly Dictionary<string, TileState> _tilesById;
        private readonly Dictionary<GridPosition, string> _blobIdsByPosition;
        private readonly Dictionary<GridPosition, string> _tileIdsByPosition;
        private readonly HashSet<GridPosition> _emptyPositions;

        /// <summary>
        /// Creates a board from initial blob and tile state, enforcing bounds, unique ids, and occupancy.
        /// </summary>
        public BoardState(int width, int height, IEnumerable<BlobState> blobs, IEnumerable<TileState> tiles, IEnumerable<GridPosition> emptyPositions = null)
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
            _emptyPositions = new HashSet<GridPosition>();
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
            if (emptyPositions != null)
            {
                foreach (var emptyPosition in emptyPositions)
                    AddEmpty(emptyPosition);
            }
        }



        private BoardState(
            int width,
            int height,
            Dictionary<string, BlobState> blobsById,
            Dictionary<string, TileState> tilesById,
            IEnumerable<GridPosition> emptyPositions = null)
        {
            Width = width;
            Height = height;
            _blobsById = new Dictionary<string, BlobState>(blobsById);
            _tilesById = new Dictionary<string, TileState>(tilesById);
            _blobIdsByPosition = new Dictionary<GridPosition, string>();
            _tileIdsByPosition = new Dictionary<GridPosition, string>();
            _emptyPositions = new HashSet<GridPosition>(emptyPositions);

            foreach (var blob in _blobsById.Values)
                _blobIdsByPosition.Add(blob.Position, blob.Id);
            foreach (var tile in _tilesById.Values)
                _tileIdsByPosition.Add(tile.Position, tile.Id);

        }

        public int Width { get; }
        public int Height { get; }
        public int BlobCount => _blobsById.Count;
        public HashSet<GridPosition> EmptyPositions => _emptyPositions;

        public IReadOnlyList<BlobState> Blobs => _blobsById.Values.ToList();
        public IReadOnlyList<TileState> Tiles => _tilesById.Values.ToList();
        /// <summary>
        /// Returns a board copy with the same logical state.
        /// </summary>
        public BoardState Clone()
        {
            return new BoardState(Width, Height, _blobsById, _tilesById, _emptyPositions);
        }

        /// <summary>
        /// Returns true when a logical grid position is within board dimensions.
        /// </summary>
        public bool IsInside(GridPosition position)
        {
            return position.X >= 0 && position.Y >= 0 && position.X < Width && position.Y < Height;
        }

        /// <summary>
        /// Looks up a blob by stable id.
        /// </summary>
        public BlobState GetBlob(string id)
        {
            if (id == null)
                return null;

            _blobsById.TryGetValue(id, out var blob);
            return blob;
        }

        /// <summary>
        /// Looks up the blob occupying a grid position, or null if the cell has no blob.
        /// </summary>
        public BlobState GetBlobAt(GridPosition position)
        {
            if (!IsInside(position))
                return null;

            if (!_blobIdsByPosition.TryGetValue(position, out var id))
                return null;

            return _blobsById[id];
        }

        public TileState GetTileAt(GridPosition position)
        {
            if (!IsInside(position))
                return null;

            if (!_tileIdsByPosition.TryGetValue(position, out var id))
                return null;

            return _tilesById[id];
        }

        /// <summary>
        /// Adds a blob while enforcing id uniqueness and single-blob cell occupancy.
        /// </summary>
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

        private void AddEmpty(GridPosition position)
        {
            if (position == null)
                throw new ArgumentNullException(nameof(position));
            if (_emptyPositions.Any(p => p.Equals(position)))
                throw new ArgumentException("Position is already empty: " + position, nameof(position));
            if (!IsInside(position))
                throw new ArgumentOutOfRangeException(nameof(position), "Position is outside the board.");
            if (_blobIdsByPosition.ContainsKey(position) || _tileIdsByPosition.ContainsKey(position))
                throw new ArgumentException("Position is occupied: " + position, nameof(position));
            _emptyPositions.Add(position);
        }
        /// <summary>
        /// Adds a tile while enforcing id uniqueness and single-tile cell occupancy.
        /// </summary>
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

        /// <summary>
        /// Removes a blob by id.
        /// </summary>
        public void RemoveBlob(string id)
        {
            var blob = GetBlob(id);
            if (blob == null)
                throw new InvalidOperationException("Blob does not exist: " + id);

            _blobsById.Remove(id);
            _blobIdsByPosition.Remove(blob.Position);
        }



        /// <summary>
        /// Moves an existing blob to an empty in-bounds cell.
        /// </summary>
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

        /// <summary>
        /// Returns true when any remaining blob counts toward the clearable objective.
        /// </summary>
        public bool HasAnyClearableBlobs()
        {
            foreach (var blob in _blobsById.Values)
            {
                var rules = BlobRuleBook.CreateDefault();
                if (rules.GetTraits(blob.Type).IsClearable)
                    return true;
            }

            return false;
        }
    }
}
