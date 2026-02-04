using System;
using System.Collections.Generic;
using UnityEngine;

namespace Blobs.Merge.Animation
{
    /// <summary>
    /// Loads BlobAnimationRecipeSO per blob type and implements IAnimationRecipeProvider.
    /// Assign recipes in the Inspector or via code; fallback to default recipe if blob type not found.
    /// </summary>
    [CreateAssetMenu(fileName = "AnimationRecipeProvider", menuName = "Blobs/Animation Recipe Provider")]
    public class AnimationRecipeProviderSO : ScriptableObject, IAnimationRecipeProvider
    {
        [SerializeField] private BlobAnimationRecipeSO _defaultRecipe;
        [SerializeField] private List<BlobTypeRecipeEntry> _perType = new();

        [Serializable]
        public class BlobTypeRecipeEntry
        {
            public BlobType blobType;
            public BlobAnimationRecipeSO recipe;
        }

        private Dictionary<BlobType, BlobAnimationRecipeSO> _lookup;

        public object GetRecipe(BlobType blobType)
        {
            if (_lookup == null)
            {
                _lookup = new Dictionary<BlobType, BlobAnimationRecipeSO>();
                foreach (var e in _perType)
                {
                    if (e.recipe != null)
                        _lookup[e.blobType] = e.recipe;
                }
            }
            return _lookup.TryGetValue(blobType, out var r) ? r : _defaultRecipe;
        }
    }
}
