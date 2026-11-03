using Blobs.Content;
using UnityEngine;

namespace Blobs.Presentation
{
    /// <summary>
    /// Scene-local harness for comparing baseline and absorption readability slices. Growth
    /// Readability uses the state-backed size levels; size rules remain entirely in Core.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class MechanicTestSliceController : MonoBehaviour
    {
        public enum TestMode
        {
            Baseline,
            Absorption,
            GrowthReadability,
        }

        [SerializeField] private GameBootstrapper bootstrapper;
        [SerializeField] private BoardPresenter boardPresenter;
        [SerializeField] private LevelDefinitionAsset[] levels;
        [SerializeField, Min(0)] private int initialLevelIndex;
        [SerializeField] private TestMode initialMode = TestMode.Absorption;

        public int CurrentLevelIndex { get; private set; }
        public TestMode CurrentMode { get; private set; }



    }
}
