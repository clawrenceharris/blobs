using UnityEngine;
using System;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using System.Collections.Generic;
using Unity.VisualScripting;
using System.Linq;

public class DotsFactory
{
    public static T CreateDotsGameObject<T>(BlobsData dObject)
        where T : BlobsGameObject
    {
        BlobsGameObject dotsGameObject = Object.Instantiate(LevelLoader.FromJsonBlobType(dObject.Type));
       
        if (dotsGameObject is IColorable colorable)
        {
            
            string color = dObject.GetProperty<string>(BlobsData.Property.Color);

           

            colorable.Color = LevelLoader.FromJsonColor(color);
            
            
        }



        if (dotsGameObject is ISizable sizable)
        {
            int size  = dObject.GetProperty<int>(BlobsData.Property.Size);
            sizable.Size = size;
        }
        return (T)dotsGameObject;
    }

}