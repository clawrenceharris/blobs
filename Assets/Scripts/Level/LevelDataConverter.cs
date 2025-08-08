using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class LevelDataConverter : JsonConverter<LevelData>
{

    private Blob[] DeserializeBlobsArray(JArray array)

    {

        Blob[] deserializedArray = new Blob[array.Count];
        for (int i = 0; i < array.Count; i++)
        {
            JObject itemObject = (JObject)array[i];
            deserializedArray[i] = BlobDataFactory.CreateBlobData(itemObject);
        }

        return deserializedArray;
    }


    private Tile[] DeserializeTilesArray(JArray array)

    {

        Tile[] deserializedArray = new Tile[array.Count];
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
            tutorialSteps = jsonObject[LevelDataKeys.TutorialSteps]?.ToObject<TutorialStep[]>(),

        };

        // Deserialize arrays
        JArray tilesArray = (JArray)jsonObject[LevelDataKeys.Tiles];
        levelData.tiles = DeserializeTilesArray(tilesArray);

        JArray blobsArray = (JArray)jsonObject[LevelDataKeys.Blobs];
        levelData.blobs = DeserializeBlobsArray(blobsArray);

        JArray laserLinks = (JArray)jsonObject[LevelDataKeys.LaserLinks];
        levelData.laserLinks = DeserializeLaserLinks(laserLinks);


        return levelData;
    }
    private List<LaserLink> DeserializeLaserLinks(JArray laserLinkArray)
    {
        if(laserLinkArray == null)
        {
            return new List<LaserLink>();
        }
        List<LaserLink> links = new();
        JObject[] array = laserLinkArray.Cast<JObject>().ToArray();
        foreach (JObject link in array)
        {
            string idA = (string)link["idA"];
            string idB = (string)link["idB"];
            string color = (string)link["color"];

            links.Add(new LaserLink
            {
                idA = idA,
                idB = idB,
                color = color
            });
        }
    

    return links;
    }
}