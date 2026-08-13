
using UnityEngine;

namespace Blobs.Presentation
{
    public interface ISkinApplier<T> where T : MonoBehaviour
    {

        void Apply(T entity, Skin skin);
    }

    public class BlobSkinApplier : ISkinApplier<BlobView>
    {
        public void Apply(BlobView view, Skin skin)
        {
            if (view == null || view.BlobRenderer == null) return;
            view.BlobRenderer.ApplySkin(skin);
        }
    }

}