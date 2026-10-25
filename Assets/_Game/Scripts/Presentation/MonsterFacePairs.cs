using System;
using UnityEngine;

namespace Blobs.Presentation
{
    [CreateAssetMenu(menuName = "Blobs/Presentation/Monster Face Pairs")]
    public sealed class MonsterFacePairs : ScriptableObject
    {
        [Serializable]
        public struct Pair { public Sprite Idle; public Sprite Eating; }
        [SerializeField] private Pair[] pairs = Array.Empty<Pair>();
        public Sprite EatingFor(Sprite idle)
        {
            if (idle == null) return null;
            foreach (var pair in pairs)
                if (pair.Idle == idle && pair.Eating != null) return pair.Eating;
            return idle;
        }
    }
}
