namespace Blobs.Core
{
    public readonly struct BlobTraits
    {
        public BlobTraits(bool canBeSource = true, bool isClearable = true, bool blocksMovement = false)
        {
            CanBeSource = canBeSource;
            IsClearable = isClearable;
            BlocksMovement = blocksMovement;
        }

        public bool CanBeSource { get; }
        public bool IsClearable { get; }

        /// <summary>
        /// True when a mover always stops on contact with this blob, so nothing beyond it is reachable.
        /// </summary>
        public bool BlocksMovement { get; }
    }
}
