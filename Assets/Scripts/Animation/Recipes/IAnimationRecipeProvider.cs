using UnityEngine;

namespace Blobs.Merge.Animation
{
    /// <summary>
    /// Provides animation recipe (ScriptableObject or config) per blob type for event animators.
    /// </summary>
    public interface IAnimationRecipeProvider
    {
        /// <summary>Returns the recipe for the given blob type, or null to use defaults.</summary>
        BlobAnimationRecipe GetRecipe(BlobType blobType);
    }
}
