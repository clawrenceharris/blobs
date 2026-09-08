using UnityEngine;

namespace Blobs.Input
{
    /// <summary>
    /// Marker component placed on blob GameObjects so pointer/raycast systems can identify
    /// which logical blob a view represents.
    /// </summary>
    public sealed class BlobInputTarget : MonoBehaviour
    {
        public string BlobId { get; private set; }

        /// <summary>
        /// Associates this Unity object with a Core blob id.
        /// </summary>
        public void Initialize(string blobId)
        {
            BlobId = blobId;
        }
    }
}
