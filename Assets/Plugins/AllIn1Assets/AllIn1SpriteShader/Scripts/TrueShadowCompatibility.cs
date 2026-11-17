#if LETAI_TRUESHADOW
using LeTai.TrueShadow;
using LeTai.TrueShadow.PluginInterfaces;
using UnityEngine;

namespace AllIn1Assets.AllIn1SpriteShader
{
    [ExecuteAlways]
    public class TrueShadowCompatibility : MonoBehaviour, ITrueShadowCustomHashProvider
    {
        [Tooltip("Refreshes the True Shadow every frame in Play mode, for animated effects. Outside Play mode it always refreshes.")]
        public bool updateTrueShadowEveryFrame = false;
        private TrueShadow shadow;

        public void UpdateTrueShadow()
        {
            if (!shadow) shadow = GetComponent<TrueShadow>();
            if (!shadow) return;

            UpdateTrueShadow(shadow);
        }

        public static void UpdateTrueShadow(TrueShadow shadow)
        {
            shadow.CustomHash = Random.Range(int.MinValue, int.MaxValue);
        }

        public void Update()
        {
            bool shouldDirty = updateTrueShadowEveryFrame;
#if UNITY_EDITOR
            shouldDirty |= !Application.isPlaying;
#endif
            if (shouldDirty)
                UpdateTrueShadow();
        }
    }
}
#endif