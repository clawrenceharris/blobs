
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class BoardModel
{
    private readonly Dictionary<string, Blob> _blobsById;
    private readonly Dictionary<string, Tile> _tilesById;

    public Blob[,] BlobGrid { get; private set; }
    public Tile[,] TileGrid { get; private set; }

    public int Width { get; private set; }
    public int Height { get; private set; }
    // Events for the Presenter to subscribe to.
    public static event Action<Blob> OnBlobCreated;
    public static event Action<Tile> OnTileCreated;
    public static event Action<Blob, Vector2Int, Vector2Int> OnBlobMoved; // ID, From, To
    public List<Blob> GetAllBlobs() => _blobsById.Values.ToList();
    public List<Tile> GetAllTiles() => _tilesById.Values.ToList();
    public int BlobCount => _blobsById.Count;

    public static event Action<Blob> OnBlobRemoved;
    public static event Action<Tile> OnTileRemoved;

    public static event Action OnBoardCleared;
    public static event Action<BoardModel> OnBoardCreated;

    public BoardModel(int width, int height)
    {
        Width = width;
        Height = height;
        _blobsById = new Dictionary<string, Blob>();
        _tilesById = new Dictionary<string, Tile>();

    }

    // Called by the Presenter to start a level.
    public void CreateInitialBoard(List<Blob> blobs, List<Tile> tiles)
    {
        BlobGrid = new Blob[Width, Height];
        TileGrid = new Tile[Width, Height];

        _blobsById.Clear();
        _tilesById.Clear();
        foreach (var blobData in blobs)
        {
            PlaceBlob(blobData);
        }
        foreach (var tileData in tiles)
        {
            PlaceTile(tileData);
        }

        OnBoardCreated?.Invoke(this);
    }
    public bool IsLaserBlocking(Blob blob, Vector2Int sourcePosition)
    {
        List<LaserTile> lasersInColumn = FindObjectsInColumn<LaserTile>(sourcePosition.x);
        List<LaserTile> lasersInRow = FindObjectsInRow<LaserTile>(sourcePosition.y);
        foreach (LaserTile laser in lasersInColumn)
        {

            if (laser.IsActive)
            {
                if (laser.LaserColor == blob.Color)
                {
                    if (IsBetweenTiles(sourcePosition, laser.GridPosition, laser.LinkedLaser.GridPosition, laser.Direction))
                    {
                        return true;
                    }
                }
            }

        }

        foreach (LaserTile laser in lasersInRow)
        {
            if (laser.IsActive)
            {
                if (laser.LaserColor == blob.Color)
                {
                    if (IsBetweenTiles(sourcePosition, laser.GridPosition, laser.LinkedLaser.GridPosition, laser.Direction))
                    {
                        return true;
                    }
                }
            }

        }
        return false;

    }
    public void LinkLasers(LevelData level)
    {
        foreach (var link in level.LaserLinks)
        {
            if (_tilesById.TryGetValue(link.idA, out Tile tileA) &&
                _tilesById.TryGetValue(link.idB, out Tile tileB) &&
                tileA is LaserTile laserA &&
                tileB is LaserTile laserB)
            {
                laserA.LinkedLaserId = link.idB;
                laserB.LinkedLaserId = link.idA;
                laserA.LinkedLaser = laserB;
                laserB.LinkedLaser = laserA;
            }
            else
            {
                Debug.LogWarning($"Invalid laser link between {link.idA} and {link.idB}");
            }
        }
    }


    private bool IsBetweenTiles(Vector2Int sourcePosition, Vector2Int from, Vector2Int to, Vector2Int direction)
    {
        // Horizontal
        if (direction == Vector2Int.right || direction == Vector2Int.left)
        {
            if (from.y != sourcePosition.y || to.y != sourcePosition.y)
                return false; // Not on same row

            int minX = Mathf.Min(from.x, to.x);
            int maxX = Mathf.Max(from.x, to.x);

            // Is laser beam position between the two x positions?
            return sourcePosition.x > minX && sourcePosition.x < maxX;
        }

        // Vertical
        if (direction == Vector2Int.up || direction == Vector2Int.down)
        {
            if (from.x != sourcePosition.x || to.x != sourcePosition.x)
                return false; // Not on same column

            int minY = Mathf.Min(from.y, to.y);
            int maxY = Mathf.Max(from.y, to.y);

            // Is laser beam position between the two y positions?
            return sourcePosition.y > minY && sourcePosition.y < maxY;
        }

        return false;
    }
    private void PlaceTile(Tile tileData)
    {
        if (tileData.GridPosition.x < 0 || tileData.GridPosition.x >= Width ||
            tileData.GridPosition.y < 0 || tileData.GridPosition.y >= Height)
        {
            Debug.LogError($"Attempted to place tile outside board bounds: {tileData.GridPosition}");
            return;
        }
        if (TileGrid[tileData.GridPosition.x, tileData.GridPosition.y] != null)
        {
            throw new ArgumentException("A tile already exists at this position: " + tileData.GridPosition);
        }

        _tilesById.Add(tileData.ID, tileData);
        TileGrid[tileData.GridPosition.x, tileData.GridPosition.y] = tileData; // Place in grid
        OnTileCreated?.Invoke(tileData);
    }

    public void PlaceBlob(Blob blob)
    {
        if (blob.GridPosition.x < 0 || blob.GridPosition.x >= Width ||
            blob.GridPosition.y < 0 || blob.GridPosition.y >= Height)
        {
            Debug.LogError($"Attempted to place blob outside board bounds: {blob.GridPosition}");
            return;
        }


        if (_blobsById.TryAdd(blob.ID, blob))
        {
            BlobGrid[blob.GridPosition.x, blob.GridPosition.y] = blob;
            OnBlobCreated?.Invoke(blob);

        }



    }

    public void RemoveBlob(string id)
    {
        if (_blobsById.TryGetValue(id, out Blob blobToRemove))
        {
            _blobsById.Remove(id);
            BlobGrid[blobToRemove.GridPosition.x, blobToRemove.GridPosition.y] = null;
            OnBlobRemoved?.Invoke(blobToRemove);
            if (_blobsById.Count == 0)
            {
                OnBoardCleared?.Invoke();

            }
        }
    }
    public void RemoveTile(string id)
    {
         if (_tilesById.TryGetValue(id, out Tile tileToRemove))
        {
            _blobsById.Remove(id);
            BlobGrid[tileToRemove.GridPosition.x, tileToRemove.GridPosition.y] = null;
            OnTileRemoved?.Invoke(tileToRemove);
           
        }
    }
    public void MoveBlob(Blob blob, Vector2Int toPosition)
    {
        if (_blobsById.TryGetValue(blob.ID, out Blob blobToMove))
        {
            Vector2Int fromPosition = blobToMove.GridPosition;
            
            BlobGrid[fromPosition.x, fromPosition.y] = null;
            BlobGrid[toPosition.x, toPosition.y] = blobToMove;
            blob.GridPosition = toPosition;
            OnBlobMoved?.Invoke(blob, fromPosition, toPosition);
        }
    }
    public Blob GetBlob(string id)
    {
        _blobsById.TryGetValue(id, out var blob);
        return blob;
    }
    public Tile GetTile(string id)
    {
        _tilesById.TryGetValue(id, out var tile);
        return tile;
    }

    public Blob GetBlobAt(Vector2Int position)
    {
        if (position.x >= 0 && position.x < Width && position.y >= 0 && position.y < Height)
        {
            return BlobGrid[position.x, position.y];
        }
        return null;
    }

    public T GetBlobAt<T>(Vector2Int position)
    {
        if (position.x >= 0 && position.x < Width && position.y >= 0 && position.y < Height)
        {
            if (BlobGrid[position.x, position.y] is T t)
                return t;
            else
                return default;
        }
        return default;
    }

    public T GetTileAt<T>(Vector2Int position)
    {
        if (position.x >= 0 && position.x < Width && position.y >= 0 && position.y < Height)
        {
            if (TileGrid[position.x, position.y] is T t)
                return t;
            else
                return default;
        }
        return default;
    }
    public List<T> FindObjectsInRow<T>(int row)
    {
        List<T> objects = new();
        if (row < 0 || row >= Height)
        {
            return new();


        }
        for (int col = 0; col < TileGrid.GetLength(0); col++)
        {
            if (TileGrid[col, row] is T tile)
            {
                objects.Add(tile);
            }
            if (BlobGrid[col, row] is T blob)
            {
                objects.Add(blob);
            }

        }
        return objects;

    }
    public List<T> FindObjectsInColumn<T>(int col)
    {
        List<T> objects = new();
        if (col < 0 || col >= Width)
        {
            return new();


        }
        for (int row = 0; row < TileGrid.GetLength(1); row++)

        {
            if (TileGrid[col, row] is T tile)
            {
                objects.Add(tile);
            }
            if (BlobGrid[col, row] is T blob)
            {
                objects.Add(blob);
            }

        }
        return objects;

    }
    public Blob GetBlobAt(int x, int y)
    {
        if (x >= 0 && x < Width && y >= 0 && y < Height)
        {
            return BlobGrid[x, y];
        }
        return null;
    }
    public Tile GetTileAt(Vector2Int position)
    {
        if (position.x >= 0 && position.x < Width && position.y >= 0 && position.y < Height)
        {
            return TileGrid[position.x, position.y];
        }
        return null;
    }
    public Tile GetTileAt(int x, int y)
    {
        if (x >= 0 && y < Width && y >= 0 && y < Height)
        {
            return TileGrid[x, y];
        }
        return null;
    }

    public bool CheckMerge(string sourceId, string targetId)
    {
        if (!_blobsById.ContainsKey(sourceId) || !_blobsById.ContainsKey(targetId)) return false;

        Blob sourceBlob = _blobsById[sourceId];
        Blob targetBlob = _blobsById[targetId];

        //can't merge the source blob on itself
        if (sourceId == targetId) return false;
        //if the blobs are not on the same row or same column it is invalid
        if (sourceBlob.GridPosition.x != targetBlob.GridPosition.x &&
            sourceBlob.GridPosition.y != targetBlob.GridPosition.y) return false;



        return true;
    }


    public Vector2 GridToIso(int gridX, int gridY, float tileSize)
    {
        return new Vector2(
            (gridX - gridY) * tileSize / 2f,
            (gridX + gridY) * tileSize / 4f
        );
    }
    public Vector2Int IsoToGrid(float isoX, float isoY, float tileSize)
    {
        float x = (isoX / (tileSize / 2) + isoY / (tileSize / 4)) / 2f;
        float y = (isoY / (tileSize / 2) - isoX / (tileSize / 4)) / 2f;
        return new Vector2Int(Mathf.FloorToInt(x), Mathf.FloorToInt(y));
    }

    public override string ToString()
    {
        string str = "";
        foreach (Blob blob in BlobGrid)
        {
            if (blob == null) continue;
            str += blob + " \n";

        }
        return str;
    }

   
}

