using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class LevelDataConverter : JsonConverter<LevelData>
{
   
    private BlobsData[] DeserializeBlobsArray(JArray array)

    {

        BlobsData[] deserializedArray = new BlobsData[array.Count];
        for (int i = 0; i < array.Count; i++)
        {
            JObject itemObject = (JObject)array[i];
            deserializedArray[i] = BlobDataFactory.CreateBlobData(itemObject);
        }

        return deserializedArray;
    }


    private BlobsData[] DeserializeTilesArray(JArray array)

    {

        BlobsData[] deserializedArray = new BlobsData[array.Count];
        for (int i = 0; i < array.Count; i++)
        {
            JObject itemObject = (JObject)array[i];
            deserializedArray[i] = TileDataFactory.CreateTileData(itemObject);
        }

        return deserializedArray;
    }

    public override void WriteJson(JsonWriter writer, LevelData value, JsonSerializer serializer)
    {

        throw new NotImplementedException();

    }

    public override LevelData ReadJson(JsonReader reader, System.Type objectType, LevelData existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
         // Load JSON into a JObject to inspect its properties
        JObject jsonObject = JObject.Load(reader);
        // Deserialize common properties
        LevelData levelData = new()
        {
            levelNum = (int)jsonObject[LevelDataKeys.LevelNum],
            width = (int)jsonObject[LevelDataKeys.Width],
            height = (int)jsonObject[LevelDataKeys.Height],

        };

        // Deserialize arrays
        JArray tilesArray = (JArray)jsonObject[LevelDataKeys.Tiles];
        levelData.tiles = DeserializeTilesArray(tilesArray);

        JArray dotsOnBoardArray = (JArray)jsonObject[LevelDataKeys.Blobs];
        levelData.blobs = DeserializeBlobsArray(dotsOnBoardArray);



        return levelData;
    }
}