using Newtonsoft.Json.Linq;

public class BlobDataFactory
{

    public static BlobsData CreateBlobData(JObject itemObject)
    {
        JToken type = itemObject[LevelDataKeys.Type];
        JToken size = itemObject[LevelDataKeys.Size];
                JToken color = itemObject[LevelDataKeys.Color];

        JToken x = itemObject[LevelDataKeys.X];
        JToken y = itemObject[LevelDataKeys.Y];

        BlobsData dotData = new()
        {
            Type = (string)type,
            Col = (int)x,
            Row = (int)y,

        };
        switch ((string)type)
        {
            case LevelDataKeys.Types.SwitchBlob:
            case LevelDataKeys.Types.FlagBlob:

                dotData.SetProperty(BlobsData.Property.Color, (string)color);
                break;
            case LevelDataKeys.Types.NormalBlob:
                dotData.SetProperty(BlobsData.Property.Color, (string)color);
                dotData.SetProperty(BlobsData.Property.Size, (int)size);
                break;


        }
        ;
        return dotData;
    }
    
    
}
