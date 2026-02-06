using DG.Tweening;
using UnityEngine;


namespace Blobs.Animation
{
    public class AnimationRecipe : ScriptableObject
    {
        [Header("Spawn/Despawn")]
        public float spawnDuration = 0.25f;
        public float despawnDuration = 0.2f;
        public Ease spawnEase = Ease.OutBack;
        public Ease despawnEase = Ease.InBack;

       


    }

}