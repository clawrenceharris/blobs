using UnityEngine;

namespace Blobs.Content
{
    [CreateAssetMenu(fileName = "FlagBlobAnimationSettingsAsset_New", menuName = "Blobs/Animation/FlagBlobAnimationSettingsAsset")]
    public class FlagBlobAnimationSettingsAsset : ScriptableObject
    {
        public float mergeDuration = 0.5f;
        public float mergePunchScale = 0.15f;
    }
}
