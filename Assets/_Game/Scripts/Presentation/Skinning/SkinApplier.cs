
using UnityEngine;

namespace Blobs.Presentation
{
    /// <summary>
    /// Applies a resolved skin to a Unity view.
    /// </summary>
    public interface ISkinApplier<T> where T : MonoBehaviour
    {

        /// <summary>
        /// Applies a presentation skin to the supplied view.
        /// </summary>
        void Apply(T entity, Skin skin);
    }

    /// <summary>
    /// Applies blob skins through <see cref="BlobRenderer"/>.
    /// </summary>
    public class BlobSkinApplier : ISkinApplier<BlobView>
    {
        /// <inheritdoc />
        public void Apply(BlobView view, Skin skin)
        {
            if (view == null || view.BlobRenderer == null) return;
            view.BlobRenderer.ApplySkin(skin);
        }
    }

}
