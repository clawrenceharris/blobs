using System;
using UnityEngine;

/// <summary>
/// Serializable snapshot of blob state for undo/respawn. Used by merge events instead of caching live Blob references.
/// </summary>
public sealed class BlobSnapshot
{
    public string Id { get; set; }
    public BlobType Type { get; set; }
    public BlobColor Color { get; set; }
    public BlobSize Size { get; set; }
    public Vector2Int GridPosition { get; set; }

    /// <summary>Used for TrailBlob.</summary>
    public BlobColor? TrailColor { get; set; }
        public static Blob ToBlob(BlobSnapshot snapshot, string id)
    {
        if (snapshot == null) return null;
        switch (snapshot.Type)
        {
            case BlobType.Normal:
                return new NormalBlob(snapshot.Color, snapshot.Size, snapshot.GridPosition, id);
            case BlobType.Ghost:
                return new GhostBlob(snapshot.GridPosition, id);
            case BlobType.Enemy:
                return new EnemyBlob(snapshot.Color, snapshot.GridPosition, id);
            case BlobType.Bomb:
                return new BombBlob(snapshot.GridPosition, id);
            case BlobType.Trail:
                return new TrailBlob(snapshot.Color, snapshot.Size, snapshot.TrailColor ?? snapshot.Color, snapshot.GridPosition, id);
            case BlobType.Target:
                return new TargetBlob(snapshot.Color, snapshot.GridPosition, id);
            case BlobType.Switch:
                return new SwitchBlob(snapshot.Color, snapshot.GridPosition,id);
            default: 
                throw new ArgumentException("Unhandled blob type: " + snapshot.Type);
        }
    }
    public static BlobSnapshot From(Blob blob)
    {
        if (blob == null) return null;
        var s = new BlobSnapshot
        {
            Id = blob.ID,
            Type = blob.Type,
            Color = blob.Color,
            Size = blob.Size,
            GridPosition = blob.GridPosition
        };

        // Use reflection to copy any matching properties from the blob into the snapshot if they exist
        var snapshotType = typeof(BlobSnapshot);
        var blobType = blob.GetType();
        foreach (var snapProp in snapshotType.GetProperties())
        {
            // Skip the base properties already assigned above
            if (snapProp.Name is nameof(Id) or nameof(Type) or nameof(Color) or nameof(BlobSnapshot.Size) or nameof(BlobSnapshot.GridPosition))
                continue;

            // Try get a property with the same name on blob
            var blobProp = blobType.GetProperty(snapProp.Name);
            if (blobProp != null && blobProp.CanRead && snapProp.CanWrite)
            {
                var value = blobProp.GetValue(blob);
                snapProp.SetValue(s, value);
            }
        }
        return s;
    }

    /// <summary>
    /// Creates a snapshot for runtime spawns (e.g. trail blobs) with a deterministic id.
    /// GridPosition is not set here; the SpawnBlobEffect.At is used when applying.
    /// </summary>
    public static BlobSnapshot Create(string id, BlobType type, BlobColor color, BlobSize size)
    {
        var snapshot = new BlobSnapshot
        {
            Id = id,
            Type = type,
            Color = color,
            Size = size,

        };
        return snapshot;
        
    }

    /// <summary>
    /// Returns a copy of this snapshot with GridPosition set. Used when applying SpawnBlobEffect at a specific cell.
    /// </summary>
    public BlobSnapshot WithPosition(Vector2Int at)
    {
        return new BlobSnapshot
        {
            Id = Id,
            Type = Type,
            Color = Color,
            Size = Size,
            GridPosition = at,
        };
    }
}
