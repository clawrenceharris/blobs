using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Unity.Mathematics;
using UnityEngine;

public class BlobFactory
{
    /// <summary>
    /// Creates a Blob model from editor/scriptable level data (BlobSpawnData).
    /// Uses common fields plus optional Properties list for type-specific data.
    /// </summary>
    public static Blob CreateBlobFromSpawnData(BlobSpawnData spawn)
    {
        if (spawn == null) return null;
        BlobType type = spawn.Type;
        var data = new BlobData
        {
            Type = type,
            X = spawn.GridPosition.x,
            Y = spawn.GridPosition.y
        };
        data.SetProperty(BlobData.Property.Color, spawn.Color);
        data.SetProperty(BlobData.Property.Size, spawn.Size);
        if (spawn.Type == BlobType.Trail)
            data.SetProperty(BlobData.Property.TrailColor,spawn.TrailColor);
        if (spawn.Properties != null)
        {
            foreach (var p in spawn.Properties)
            {
                if (!string.IsNullOrEmpty(p?.Key))
                    data.SetProperty(p.Key, p.Value ?? "");
            }
        }
        return CreateBlobModel(data);
    }

    public static BlobPresenter CreateBlobPresenter(Blob model, BlobView view)
    {
        switch (model.Type)
        {
            case BlobType.Ghost: return new GhostBlobPresenter(model, view);
            default: return new BlobPresenter(model, view);
        }
    }
    public static Blob CreateBlobModel(BlobData data)
    {
        int x = data.X;
        int y = data.Y;
        BlobType type = data.Type;
        BlobColor color = data.GetProperty<BlobColor>(BlobData.Property.Color);
        BlobSize size = data.GetProperty<BlobSize>(BlobData.Property.Size);
        BlobColor trailColor = data.GetProperty<BlobColor>(BlobData.Property.TrailColor);


        Vector2Int position = new(x, y);


        switch (type)
        {
            case BlobType.Normal:
                return new NormalBlob(color, size, position);

            case BlobType.Ghost:
                return new GhostBlob(position);

            case BlobType.Enemy:
                return new EnemyBlob(color, position);

            case BlobType.Bomb:
                return new BombBlob(position);

            case BlobType.Trail:
                return new TrailBlob(color, size, trailColor, position);

            case BlobType.Flag:
                return new FlagBlob(color, position);

            case BlobType.Switch:
                return new SwitchBlob(color, position);

            case BlobType.Rock:
                return new RockBlob(position);

            default: throw new ArgumentException();
        }
    }

}

