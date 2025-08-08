using System;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class BlobDataFactory
{

    public static Blob CreateBlobData(JObject itemObject)
    {
        JToken jsonType = itemObject[LevelDataKeys.Type];
        JToken size = itemObject[LevelDataKeys.Size];
        JToken color = itemObject[LevelDataKeys.Color];

        JToken x = itemObject[LevelDataKeys.X];
        JToken y = itemObject[LevelDataKeys.Y];
        JToken trailColor = itemObject[LevelDataKeys.BlobColors.TrailColor];
        BlobType type = LevelDataKeys.Types.GetBlobTypeFromKey((string)jsonType);
        Vector2Int position = new((int)x, (int)y);


        switch (type)
        {
            case BlobType.Normal:
                {

                    return new NormalBlob(LevelDataKeys.BlobColors.GetBlobColorFromKey((string)color), LevelDataKeys.BlobSizes.GetBlobSizeFromKey((string)size), position);


                }
            case BlobType.Ghost:
                {
                    return new GhostBlob(position);


                }
            case BlobType.Enemy:
                {

                    return new EnemyBlob(LevelDataKeys.BlobColors.GetBlobColorFromKey((string)color), position);


                }
            case BlobType.Bomb:
                {
                    return new BombBlob(position);


                }
            case BlobType.Trail:
                {

                    return new TrailBlob(
                    LevelDataKeys.BlobColors.GetBlobColorFromKey((string)color),
                    LevelDataKeys.BlobSizes.GetBlobSizeFromKey((string)size), LevelDataKeys.BlobColors.GetBlobColorFromKey((string)trailColor),
                    position);


                }
            case BlobType.Flag:
                {
                    return new FlagBlob(LevelDataKeys.BlobColors.GetBlobColorFromKey((string)color), position);

                }
            case BlobType.Switch:
                {
                    return new SwitchBlob(LevelDataKeys.BlobColors.GetBlobColorFromKey((string)color), position);

                }
                 case BlobType.Rock:
                {
                    return new RockBlob(position);

                }
            default: throw new ArgumentException();
        }
    }
    
    
}
