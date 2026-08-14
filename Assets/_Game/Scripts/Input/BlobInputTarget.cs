using UnityEngine;

namespace Blobs.Input
{
    public sealed class BlobInputTarget : MonoBehaviour
    {
        public string BlobId { get; private set; }

        public void Initialize(string blobId)
        {
            BlobId = blobId;
        }
    }
}
