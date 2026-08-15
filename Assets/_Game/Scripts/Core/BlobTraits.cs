namespace Blobs.Core
{
    public readonly struct BlobTraits
    {
        public BlobTraits(bool canBeSource, bool isClearable)
        {
            CanBeSource = canBeSource;
            IsClearable = isClearable;
        }

        public bool CanBeSource { get; }
        public bool IsClearable { get; }
    }
}