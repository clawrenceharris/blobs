using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Type
{
    public enum TileType{
        None,
        Normal,
        Target,
        Train,
        Spike,
        Laser,


        
    }

    public enum BlobType
    {
        None,
        Normal,
        Switch,
        Trail,
        Flag,
        Moving,
        Button,
        
    }


    public static bool IsColorBlob(this BlobType type)
    {
        return type == BlobType.Normal || type == BlobType.Switch || type == BlobType.Flag || type == BlobType.Trail;
    }

    public static bool IsTrailBlob(this BlobType type)
    {
        return type == BlobType.Trail;
    }

    public static bool IsNormalBlob(this BlobType type)
    {
        return type == BlobType.Normal;
    }

    public static bool IsSwitchBlob(this BlobType type)
    {
        return type == BlobType.Switch;
    }

    public static bool IsFlagBlob(this BlobType type)
    {
        return type == BlobType.Flag;
    }
    public static bool IsButtonBlob(this BlobType type)
    {
        return type == BlobType.Button;
    }
    public static bool IsBlob(this BlobType type)
    {
        return type == BlobType.Normal ||
            type == BlobType.Switch ||
            type == BlobType.Flag ||
            type == BlobType.Trail||
            type == BlobType.Button ||
            type == BlobType.Moving ;
    }

    public static bool IsUnMovableBlob(this BlobType type)
    {
        return type == BlobType.Flag || type == BlobType.Switch || type == BlobType.Button;
    }

    public static bool IsMaxSizedBlob(this BlobType type)
    {
        return type == BlobType.Flag || type == BlobType.Switch;
    }
    public static bool IsTargetTile(this TileType type)
    {
        return type == TileType.Target;
    }

    public static bool IsSpikeTile(this TileType type)
    {
        return type == TileType.Spike;
    }

    public static bool IsLaserTile(this TileType type)
    {
        return type == TileType.Laser;
    }

    public static bool IsTrainTile(this TileType type)
    {
        return type == TileType.Train;
    }
}
