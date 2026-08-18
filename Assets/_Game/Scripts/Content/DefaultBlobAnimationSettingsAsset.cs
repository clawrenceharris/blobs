using UnityEngine;

namespace Blobs.Content
{
    [CreateAssetMenu(fileName = "DefaultBlobAnimationSettingsAsset", menuName = "Blobs/Animation/DefaultBlobAnimationSettingsAsset")]
    public class DefaultBlobAnimationSettingsAsset : ScriptableObject
    {
        public float selectionSquishDuration = 0.16f;
        public float selectionSquishAmount = 1.065f;
        public float selectionStretchAmount = 1.02f;
        public float idleSquishDuration = 0.8f;
        public float idleSquishAmount = 1.065f;
        public float idleStretchAmount = 0.99f;

        private void OnValidate()
        {
            selectionSquishDuration = Mathf.Max(0.01f, selectionSquishDuration);
            idleSquishDuration = Mathf.Max(0.01f, idleSquishDuration);
            selectionSquishAmount = Mathf.Max(0.01f, selectionSquishAmount);
            selectionStretchAmount = Mathf.Max(0.01f, selectionStretchAmount);
            idleSquishAmount = Mathf.Max(0.01f, idleSquishAmount);
            idleStretchAmount = Mathf.Max(0.01f, idleStretchAmount);
        }
    }
}
