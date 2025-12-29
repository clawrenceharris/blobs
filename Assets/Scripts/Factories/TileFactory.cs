using System;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class TileFactory
{


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

}
