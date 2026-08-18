using UnityEngine;

namespace Blobs.Content
{
    [CreateAssetMenu(fileName = "AnimationSettingsAsset_New", menuName = "Blobs/Animation/AnimationSettingsAsset")]
    public class BlobAnimationSettingsAsset : ScriptableObject
    {
        public DefaultBlobAnimationSettingsAsset defaultSettings;
        public FlagBlobAnimationSettingsAsset flagSettings;

    }
}