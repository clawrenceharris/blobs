namespace Blobs.Core
{
    public enum BlobType
    {
        Normal,
        Nest,
        Trail,
        Rock,
        Ghost,
    }
    public static class BlobTypeExtensions
    {
        public static bool IsClearable(this BlobType type)
        {
            return type == BlobType.Normal || type == BlobType.Trail || type == BlobType.Ghost;
        }
    }
}
