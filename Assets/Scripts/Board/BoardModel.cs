
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MergePlan
{
    public Blob SourceBlob { get; set; }
    public Blob TargetBlob { get; set; }
    public Vector2Int StartPosition { get; set; }
    public Vector2Int EndPosition { get; set; }
    public Vector2Int Direction
    {
        get
        {
            var direction = EndPosition - StartPosition;
            return new Vector2Int(
                Mathf.Clamp(direction.x, -1, 1),
                Mathf.Clamp(direction.y, -1, 1)
            );
        }
    }
    // public Dictionary<Blob, Vector2Int> BlobsToRemoveAfterMerge { get; private set; } = new();
    public Dictionary<Blob, BlobSize> SizeChanges { get; set; } = new();
    public MergePlan DeferredPlan { get; set; }
    public bool ShouldTerminate { get; set; } = false;
    // public Dictionary<Blob, Vector2Int> BlobsToCreateAfterMerge { get; set; } = new();
    public Dictionary<Blob, Vector2Int> BlobsToCreateOnPath { get; set; } = new();
    public Dictionary<Blob, Vector2Int> BlobsToRemoveOnPath { get; set; } = new();
    public List<Blob> BlobsToRemoveAfterMerge{ get; set; } = new();
    public List<Blob> BlobsToCreateAfterMerge{ get; set; } = new();

    public Action<MergePlan> OnMergeComplete;
   
    public static MergePlan operator -(MergePlan plan) => new()
    {
        StartPosition = plan.EndPosition,
        EndPosition = plan.StartPosition,
        TargetBlob = plan.TargetBlob,
        SourceBlob = plan.SourceBlob,
        OnMergeComplete = plan.OnMergeComplete,
        BlobsToRemoveOnPath =  plan.BlobsToCreateOnPath,
        BlobsToCreateOnPath =plan.BlobsToRemoveOnPath,

        BlobsToCreateAfterMerge =  plan.BlobsToRemoveAfterMerge,
        BlobsToRemoveAfterMerge = plan.BlobsToCreateAfterMerge,
    };
}



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
    private bool IsLaserBlocking(Blob blob, Vector2Int sourcePosition)
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
        foreach (var link in level.laserLinks)
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
            Debug.LogError($"Attempted to place blob outside board bounds: {tileData.GridPosition}");
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

    public MergePlan CalculateMergePlan(Blob source, Blob target)
    {
        if (!CheckMerge(source.ID, target.ID)) return null; 
        var context = new MergeContext()
        {
            Board = this,
            Plan = new MergePlan
            {
                SourceBlob = source,
                TargetBlob = target,
                StartPosition = source.GridPosition,
                EndPosition = target.GridPosition,
            }
        };

        var plan = context.Plan;
        var direction = plan.Direction;

        Vector2Int current = source.GridPosition + direction;

        while (current != plan.EndPosition + direction)
        {
            Tile currentTile = GetTileAt(current.x, current.y);
            Blob currentBlob = GetBlobAt(current.x, current.y);
            
            context.CurrentPosition = current;
            context.CurrentBlob = currentBlob;

            if (currentTile == null || !currentTile.TileType.IsTraversable()) return null;
            else if (currentBlob != null && !CheckMerge(source.ID, currentBlob.ID)) return null;
            else if (currentBlob != null && !currentBlob.CanMergeWith(source, plan, this)) return null;
            else if (IsLaserBlocking(source, current)) return null;

            currentTile.Behavior.ModifyMerge(context);
            target.Behavior.ModifyMergeFromTarget(context);
            source.Behavior.ModifyMergeFromSource(context);
          
            var next = current + direction;
            current = next;
            
            if (plan.ShouldTerminate)
            {
                break;
            }

        }
        
        return plan;
    }
   
    public void RemoveBlob(Blob blob)
    {
        if (_blobsById.TryGetValue(blob.ID, out Blob blobToRemove))
        {
            _blobsById.Remove(blob.ID);
            BlobGrid[blobToRemove.GridPosition.x, blobToRemove.GridPosition.y] = null;
            OnBlobRemoved?.Invoke(blobToRemove);
            if (_blobsById.Count == 0)
            {
                OnBoardCleared?.Invoke();

            }
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




}

