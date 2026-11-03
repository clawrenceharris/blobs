using UnityEngine;

namespace Blobs.Presentation
{
    /// <summary>
    /// Marks a Flag presentation as a nest home. After a reverse merge into this view,
    /// <see cref="NestVictorySequence"/> seats the arriving monster in the nest.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NestBlobView : MonoBehaviour
    {
        [SerializeField] private Transform nest;
        [SerializeField] private Transform eggs;
        [SerializeField] private Transform occupantMount;

        [SerializeField] private Vector3 occupantScale;
        [SerializeField] private SpriteMaskInteraction occupantMaskInteraction;
        [SerializeField, Min(0.05f)] private float riseDuration = 0.45f;
        [SerializeField] private Vector3 hiddenLocalOffset = new(0f, -0.42f, 0f);
        [SerializeField] private Vector3 settledLocalOffset = new(0f, 0.06f, 0f);
        [SerializeField] private int occupantSortingOrderOffset = 2;

        private BlobView _occupant;
        private bool _eggsWereActive = true;

        public float RiseDuration => riseDuration;
        public Vector3 HiddenLocalOffset => hiddenLocalOffset;
        public Vector3 SettledLocalOffset => settledLocalOffset;
        public BlobView Occupant => _occupant;
        public bool HasOccupant => _occupant != null;

        public static bool TryResolve(BlobView view, out NestBlobView nest)
        {
            nest = null;
            if (view == null)
                return false;

            if (view.TryGetComponent(out nest))
                return true;

            // Structural fallback for nests authored before NestBlobView was added.
            Transform visual = view.VisualRoot;
            if (visual == null || visual.Find("Nest") == null || visual.Find("Eggs") == null)
                return false;

            nest = view.gameObject.AddComponent<NestBlobView>();
            nest.CacheHierarchy();
            return true;
        }

        private void Awake() => CacheHierarchy();

        private void Start()
        {
            // Nest hosts inherit MonsterFaceView from the normal monster prefab; hide it so only
            // an arriving occupant can show a face after capture.
            if (!HasOccupant && TryGetComponent(out MonsterFaceView hostFace))
                hostFace.Hide();
        }

        private void CacheHierarchy()
        {
            Transform visual = transform;
            if (TryGetComponent(out BlobView blob) && blob.VisualRoot != null)
                visual = blob.VisualRoot;

            if (nest == null)
                nest = visual.Find("Nest");
            if (eggs == null)
                eggs = visual.Find("Eggs");
            if (occupantMount == null)
                occupantMount = visual;
        }

        /// <summary>
        /// Parents the consumed monster under the nest, hides the eggs, and places the
        /// monster behind the nest bowl so the rise beat can bring it into view.
        /// </summary>
        public void BeginOccupation(BlobView monster)
        {
            CacheHierarchy();
            if (monster == null)
                return;

            ClearOccupation(destroyOccupant: true);
            _occupant = monster;


            Transform mount = occupantMount != null ? occupantMount : transform;
            monster.transform.SetParent(mount, worldPositionStays: false);
            monster.transform.localPosition = Vector3.zero;
            monster.transform.localRotation = Quaternion.identity;
            monster.transform.localScale = occupantScale;
            monster.BlobRenderer.MaskableVisual.ApplyMaskInteraction(occupantMaskInteraction);


            if (monster.BlobRenderer.ShadowRenderer != null)
                monster.BlobRenderer.ShadowRenderer.enabled = false;
            if (monster.SortingGroup != null)
                monster.SortingGroup.enabled = false;

            Transform visual = monster.VisualRoot != null ? monster.VisualRoot : monster.transform;
            visual.localPosition = hiddenLocalOffset;
            visual.localScale = monster.BaseVisualScale;
            ApplyOccupantSorting(visual, behindNest: true);

            monster.BlobMotionAnimator?.SetIdle();
            monster.Face?.ShowVictory();
        }

        /// <summary>Snaps the seated monster to its resting pose in the nest.</summary>
        public void ApplySettledPose()
        {
            if (_occupant == null)
                return;

            Transform visual = _occupant.VisualRoot != null ? _occupant.VisualRoot : _occupant.transform;
            visual.localPosition = settledLocalOffset;
            visual.localScale = _occupant.BaseVisualScale;
            ApplyOccupantSorting(visual, behindNest: false);
            _occupant.Face?.ShowVictory();
        }

        public void ClearOccupation(bool destroyOccupant = true)
        {
            if (_occupant != null && destroyOccupant)
            {
                if (UnityEngine.Application.isPlaying)
                    Destroy(_occupant.gameObject);
                else
                    DestroyImmediate(_occupant.gameObject);
            }

            _occupant = null;
            if (eggs != null)
                eggs.gameObject.SetActive(_eggsWereActive);
        }

        private void ApplyOccupantSorting(Transform visual, bool behindNest)
        {
            int order = behindNest ? -1 : occupantSortingOrderOffset;
            SpriteRenderer[] renderers = visual.GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
                renderers[i].sortingOrder += order;
        }

        private void OnDestroy()
        {
            // Board clears destroy the nest; abandon any seated monster with it.
            _occupant = null;
        }
    }
}
