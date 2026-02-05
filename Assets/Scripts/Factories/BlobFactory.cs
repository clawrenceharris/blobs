using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
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
        string typeKey = LevelLoader.ToJsonBlobType(spawn.Type);
        var data = new BlobData
        {
            Type = typeKey,
            X = spawn.GridPosition.x,
            Y = spawn.GridPosition.y
        };
        data.SetProperty(BlobData.Property.Color, LevelLoader.ToJsonColor(spawn.Color));
        data.SetProperty(BlobData.Property.Size, LevelLoader.ToJsonSize(spawn.Size));
        if (spawn.Type == BlobType.Trail)
            data.SetProperty(BlobData.Property.TrailColor, LevelLoader.ToJsonColor(spawn.TrailColor));
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

    public static BlobPresenter CreateBlobPresenter(BlobView view)
    {
        switch (view.Model.Type)
        {
            case BlobType.Ghost: return new GhostBlobPresenter(view);
            case BlobType.Bomb: return new BombBlobPresenter(view);
            default: return new BlobPresenter(view);
        }
    }
    public static Blob CreateBlobModel(BlobData data)
    {
        int x = data.X;
        int y = data.Y;
        string type = data.Type;
        string color = data.GetProperty<string>(BlobData.Property.Color);
        string size = data.GetProperty<string>(BlobData.Property.Size);
        string trailColor = data.GetProperty<string>(BlobData.Property.TrailColor);


        Vector2Int position = new(x, y);


        switch (type)
        {
            case LevelDataKeys.Types.NormalBlob:
                {
                    BlobColor blobColor = LevelLoader.FromJsonColor(color);
                    BlobSize blobSize = LevelLoader.FromJsonSize(size);

                    return new NormalBlob(blobColor, blobSize, position);


                }
            case LevelDataKeys.Types.GhostBlob:
                {
                    return new GhostBlob(position);
                }
            case LevelDataKeys.Types.EnemyBlob:
                {
                    BlobColor blobColor = LevelLoader.FromJsonColor(color);

                    return new EnemyBlob(blobColor, position);
                }
            case LevelDataKeys.Types.BombBlob:
                {
                    return new BombBlob(position);

                }
            case LevelDataKeys.Types.TrailBlob:
                {
                    BlobColor blobColor = LevelLoader.FromJsonColor(color);
                    BlobColor blobTrailColor = LevelLoader.FromJsonColor(trailColor);
                    BlobSize blobSize = LevelLoader.FromJsonSize(size);

                    return new TrailBlob(blobColor, blobSize, blobTrailColor, position);
                }
            case LevelDataKeys.Types.FlagBlob:
                {
                    BlobColor blobColor = LevelLoader.FromJsonColor(color);

                    return new FlagBlob(blobColor, position);

                }

            case LevelDataKeys.Types.SwitchBlob:
                {
                    BlobColor blobColor = LevelLoader.FromJsonColor(color);

                    return new SwitchBlob(blobColor, position);

                }
            case LevelDataKeys.Types.RockBlob:
                {
                    return new RockBlob(position);

                }
            default: throw new ArgumentException();
        }
    }

}

