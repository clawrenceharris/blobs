using Blobs.Core.Merge;
using UnityEngine;

namespace Blobs.Merge.Animation
{
    /// <summary>
    /// Registers event animators and optional recipe provider. Attach to a scene GameObject or call from game init.
    /// </summary>
    public class MergeAnimationBootstrap : MonoBehaviour
    {
        [SerializeField] private AnimationRecipeProviderSO _recipeProvider;

        private void Awake()
        {
            if (_recipeProvider != null)
                MovePlanAnimator.RecipeProvider = _recipeProvider;
        }
    }
}
