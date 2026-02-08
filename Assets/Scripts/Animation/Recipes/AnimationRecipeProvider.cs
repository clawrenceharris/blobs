using System;
using System.Collections.Generic;
using UnityEngine;

namespace Blobs.Animation
{
    /// <summary>
    /// Loads AnimationRecipe per blob type and implements IAnimationRecipeProvider.
    /// Assign recipes in the Inspector or via code; fallback to default recipe if blob type not found.
    /// </summary>
    public class AnimationRecipeProvider : MonoBehaviour
    {
       
        private static AnimationRecipeProvider _instance;

        public static AnimationRecipeProvider Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindFirstObjectByType<AnimationRecipeProvider>();
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
                Destroy(gameObject);
            else
                _instance = this;
        }
        public AnimationRecipe DefaultRecipe;
        [SerializeField] private List<BlobTypeRecipeEntry> _perType = new();

        [Serializable]
        public class BlobTypeRecipeEntry
        {
            public BlobType blobType;
            public AnimationRecipe recipe;
        }
        
        private Dictionary<BlobType, AnimationRecipe> _lookup;
        public T GetBlobRecipe<T>(BlobType type) where T : AnimationRecipe
        {
            if (_lookup == null)
            {
                _lookup = new Dictionary<BlobType, AnimationRecipe>();
                foreach (var e in _perType)
                {
                    if (e.recipe != null)
                        _lookup[e.blobType] = e.recipe;
                }
            }
            if (_lookup.TryGetValue(type, out var r))
            {
                return r as T;
            }
            return null;
        }
    }
}
