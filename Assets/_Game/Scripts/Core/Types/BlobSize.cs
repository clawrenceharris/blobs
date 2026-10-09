using System;

namespace Blobs.Core
{
    public enum BlobSize
    {
        Small = 1,
        Normal = 2,
        Large = 3,
    }

    public static class BlobSizeExtensions
    {
        public static BlobSize Next(this BlobSize size)
        {
            return size switch
            {
                BlobSize.Small => BlobSize.Normal,
                BlobSize.Normal => BlobSize.Normal,
                BlobSize.Large => BlobSize.Large,
                _ => throw new InvalidOperationException("Invalid blob size: " + size),
            };
        }
    }
}
