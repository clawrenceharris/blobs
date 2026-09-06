using Blobs.Content;
using Blobs.Core;
using UnityEngine;

namespace Blobs.Presentation
{
    /// <summary>
    /// Extension point for prefab-specific color sources. BlobView invokes bindings without
    /// knowing which blob type or renderer they support.
    /// </summary>
    public abstract class BlobColorBinding : MonoBehaviour
    {
        public abstract void Apply(
            BlobState blob,
            LevelColorPaletteAsset palette);
    }

}
