using UnityEngine;

namespace Blobs.Animation
{
  
    [CreateAssetMenu(fileName = "GhostBlobAnimationRecipe", menuName = "Scriptable Objects/Animation Recipe (Ghost Blob)")]
    public class GhostBlobAnimationRecipe : AnimationRecipe
    {

        [Header("Ghost / special (fade out before move, fade in after; shocked face before Sigil clear)")]
        public float fadeOutDuration = 0.2f;
        public float fadeInDuration = 0.2f;
    }
}