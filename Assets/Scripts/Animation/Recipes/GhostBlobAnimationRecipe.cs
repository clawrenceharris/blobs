using Blobs.Merge.Animation;
using UnityEngine;

[CreateAssetMenu(fileName = "GhostBlobAnimationRecipe", menuName = "Scriptable Objects/Animation Recipe (Ghost Blob)")]
public class GhostBlobAnimationRecipe : BlobAnimationRecipe
{

    [Header("Ghost / special (fade out before move, fade in after; shocked face before Sigil clear)")]
    public float fadeOutDuration = 0.2f;
    public float fadeInDuration = 0.2f;
    public bool useFadeForMove;
}