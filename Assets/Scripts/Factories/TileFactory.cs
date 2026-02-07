using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class TileFactory
{
    /// <summary>
    /// Creates a Tile model from editor/scriptable level data (TileSpawnData).
    /// Only creates tiles for cells that have an entry in the level; empty cells have no tile (sparse layout).
    /// For Laser tiles, use Properties with keys "id" and "color" (e.g. color = "pk" for pink).
    /// </summary>
    public static Tile CreateTileModel(TileSpawnData data)
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
            case TileType.Spike:
                return new SpikeTile(data.GridPosition);
            case TileType.Sigil:
                return new SigilTile(data.GridPosition);
            case TileType.Laser:
                string id = data.GetProperty<string>(LevelDataKeys.Properties.LaserId);
                BlobColor color = data.GetProperty<BlobColor>(LevelDataKeys.Properties.Color);
                return new LaserTile(color, id, data.GridPosition);
            case TileType.Normal:
            default:
                return new NormalTile(data.GridPosition);
        }
    }

    public static TilePresenter CreateTilePresenter(Tile model, TileView view)
    {
        switch (model.Type)
        {
            default: return new(model, view);
        }
    }

   
}
