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
    public static Blob CreateBlobModel(BlobSpawnData data)
    {
        
        if (data.Properties != null)
        {
            foreach (var p in data.Properties)
            {
                if (!string.IsNullOrEmpty(p.Key))
                    data.SetProperty(p.Key, p.Value);
            }
        }



        switch (data.Type)
        {
            case BlobType.Normal:
                return new NormalBlob(data.Color, data.Size, data.GridPosition);

            case BlobType.Ghost:
                return new GhostBlob(data.GridPosition);

            case BlobType.Enemy:
                return new EnemyBlob(data.Color, data.GridPosition);

            case BlobType.Bomb:
                return new BombBlob(data.GridPosition);

            case BlobType.Trail:
                BlobColor trailColor = data.GetProperty<BlobColor>(LevelDataKeys.Properties.TrailColor);
                return new TrailBlob(data.Color, data.Size, trailColor, data.GridPosition);

            case BlobType.Flag:
                return new FlagBlob(data.Color, data.GridPosition);

            case BlobType.Switch:
                return new SwitchBlob(data.Color, data.GridPosition);

            case BlobType.Rock:
                return new RockBlob(data.GridPosition);

            default: throw new ArgumentException();
        }
    }

    public static BlobPresenter CreateBlobPresenter(Blob model, BlobView view)
    {
        switch (model.Type)
        {
            case BlobType.Ghost: return new GhostBlobPresenter(model, view);
            default: return new BlobPresenter(model, view);
        }
    }
    

}

