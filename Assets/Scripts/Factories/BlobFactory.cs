using System;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class BlobFactory
{
    /// <summary>
    /// Creates a BlobData object using the JSON object representing a blob
    /// </summary>
    /// <param name="itemObject">The JSON object with the blob data</param>
    /// <returns></returns>
    public static BlobData CreateBlobData(JObject itemObject)
    {
        JToken type = itemObject[LevelDataKeys.Type];
        JToken color = itemObject[LevelDataKeys.Color];
        JToken size = itemObject[LevelDataKeys.Size];

        JToken x = itemObject[LevelDataKeys.X];
        JToken y = itemObject[LevelDataKeys.Y];
        JToken trailColor = itemObject[LevelDataKeys.BlobColors.TrailColor];
        Vector2Int position = new((int)x, (int)y);
        BlobData blobData = new()
        {
            Type = (string)type,
            X = position.x,
            Y = position.y,

        };

        switch ((string)type)
        {
            case LevelDataKeys.Types.NormalBlob:

                blobData.SetProperty(BlobData.Property.Color, (string)color);
                blobData.SetProperty(BlobData.Property.Size, (string)size);
                break;
            case LevelDataKeys.Types.SwitchBlob:
            case LevelDataKeys.Types.FlagBlob:
            case LevelDataKeys.Types.EnemyBlob:


                blobData.SetProperty(BlobData.Property.Color, (string)color);
                break;
            case LevelDataKeys.Types.TrailBlob:
                blobData.SetProperty(BlobData.Property.Size, (string)size);

                blobData.SetProperty(BlobData.Property.Color, (string)trailColor);
                blobData.SetProperty(BlobData.Property.TrailColor, (string)color);
                break;
        }
        return blobData;
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
                    BlobSize blobSize =  LevelLoader.FromJsonSize(size);
                    
                    return new NormalBlob(blobColor,blobSize, position);
                    

                }
            case LevelDataKeys.Types.GhostBlob:
                {
                    return new GhostBlob(position);
                }
            case LevelDataKeys.Types.EnemyBlob:
                {
                    BlobColor blobColor =  LevelLoader.FromJsonColor(color);

                    return new EnemyBlob(blobColor, position);
                }
            case LevelDataKeys.Types.BombBlob:
                {
                    return new BombBlob(position);

                }
            case LevelDataKeys.Types.TrailBlob:
                {
                    BlobColor blobColor =  LevelLoader.FromJsonColor(color);
                    BlobColor blobTrailColor = LevelLoader.FromJsonColor(trailColor);
                    BlobSize blobSize =  LevelLoader.FromJsonSize(size);

                    return new TrailBlob(blobColor, blobSize, blobTrailColor, position);
                }
            case LevelDataKeys.Types.FlagBlob:
                {
                     BlobColor blobColor =  LevelLoader.FromJsonColor(color);

                    return new FlagBlob(blobColor, position);

                }
               
            case LevelDataKeys.Types.SwitchBlob:
                {
                    BlobColor blobColor =  LevelLoader.FromJsonColor(color);

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
