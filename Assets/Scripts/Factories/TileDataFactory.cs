using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class TileDataFactory
{
    

    public static BlobsData CreateTileData(JObject itemObject)
    {
        string type = (string)itemObject[LevelDataKeys.Type];
        JToken color = itemObject[LevelDataKeys.Color];
        JToken x = itemObject[LevelDataKeys.X];
        JToken y = itemObject[LevelDataKeys.Y];

        BlobsData tileData = new()
        {
            Type = type,
            Col = x.ToObject<int>(),
            Row = y.ToObject<int>(),

        };
        switch (type)
        {
            case LevelDataKeys.Types.TargetTile:
                tileData.SetProperty(BlobsData.Property.Color, (string)color);
                break;
           
            
        };
        return tileData;
    }
}
