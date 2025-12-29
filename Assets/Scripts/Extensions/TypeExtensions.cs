public static class TypeExtensions
{
    

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
    
    public static bool IsBlob(this BlobType type)
    {
        return type == BlobType.Normal ||
            type == BlobType.Switch ||
            type == BlobType.Flag ||
            type == BlobType.Trail;
    }

   
    public static bool IsMaxSizedBlob(this BlobType type)
    {
        return type == BlobType.Flag || type == BlobType.Switch;
    }
   
    
    public static bool IsTraversable(this TileType type)
    {
        return type == TileType.Normal || type == TileType.Sigil;
    }
    public static bool IsSpikeTile(this TileType type)
    {
        return type == TileType.Spike;
    }

    public static bool IsLaserTile(this TileType type)
    {
        return type == TileType.Laser;
    }
     public static bool IsSigilTile(this TileType type)
    {
        return type == TileType.Sigil;
    }

    public static bool IsTrainTile(this TileType type)
    {
        return type == TileType.Train;
    }
}