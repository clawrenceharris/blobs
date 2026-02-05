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
    public static Tile CreateTileFromSpawnData(TileSpawnData spawn)
    {
        if (spawn == null) return null;
        Vector2Int pos = spawn.GridPosition;
        switch (spawn.Type)
        {
            case TileType.Normal:
                return new NormalTile(pos);
            case TileType.Spike:
                return new SpikeTile(pos);
            case TileType.Sigil:
                return new SigilTile(pos);
            case TileType.Laser:
                string id = GetProperty(spawn.Properties, "id") ?? spawn.GridPosition.ToString();
                string colorKey = GetProperty(spawn.Properties, "color") ?? "pk";
                return new LaserTile(LevelDataKeys.BlobColors.GetBlobColorFromKey(colorKey), id, pos);
            case TileType.Sticky:
            case TileType.Ice:
            case TileType.Target:
                return new NormalTile(pos);
            default:
                return new NormalTile(pos);
        }
    }

    private static string GetProperty(List<StringKeyValue> properties, string key)
    {
        if (properties == null || string.IsNullOrEmpty(key)) return null;
        var entry = properties.FirstOrDefault(p => p != null && string.Equals(p.Key, key, StringComparison.OrdinalIgnoreCase));
        return entry?.Value;
    }

    public static Tile CreateTileData(JObject itemObject)
    {
        JToken jsonType = itemObject[LevelDataKeys.Type];
        JToken color = itemObject[LevelDataKeys.Color];
        JToken x = itemObject[LevelDataKeys.X];
        JToken y = itemObject[LevelDataKeys.Y];
        TileType type = LevelDataKeys.Types.GetTileTypeFromKey((string)jsonType);
        Vector2Int position = new((int)x, (int)y);

        switch (type)
        {
            case TileType.Normal:
                {
                    return new NormalTile(position);
                }
           
            case TileType.Spike:
                {
                    return new SpikeTile(position);
                }
            case TileType.Sigil:
                {
                    return new SigilTile(position);
                }
            case TileType.Laser:
                {
                    string id = (string)itemObject["id"];
                    return new LaserTile(LevelDataKeys.BlobColors.GetBlobColorFromKey((string)color), id, position);

                }
            default: throw new ArgumentException();




        }
        ;
    }
    public static TilePresenter CreateTilePresenter(TileView view)
    {
        switch (view.Model.Type)
        {
            default: return new(view);
        }
    }

    public static TileAnimator CreateTileAnimator(TileView view)
    {
        switch (view.Model.Type)
        {
            default: return new(view);
        }
    }
}
