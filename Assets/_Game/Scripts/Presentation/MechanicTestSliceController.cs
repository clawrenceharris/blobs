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

        private void Awake()
        {
            CurrentLevelIndex = ClampLevelIndex(initialLevelIndex);
            ApplyMode(initialMode);

            if (bootstrapper != null && levels != null && levels.Length > 0)
                bootstrapper.ConfigureStartingLevel(levels[CurrentLevelIndex]);
        }

        public void SetMode(int mode)
        {
            ApplyMode((TestMode)Mathf.Clamp(mode, 0, 2));
            if (bootstrapper != null && bootstrapper.CurrentState != null)
                LoadLevel(CurrentLevelIndex);
        }

        public void LoadLevel(int index)
        {
            if (bootstrapper == null || levels == null || levels.Length == 0)
                return;

            CurrentLevelIndex = ClampLevelIndex(index);
            bootstrapper.StartLevel(levels[CurrentLevelIndex]);
        }

        public void NextLevel()
        {
            if (levels == null || levels.Length == 0)
                return;

            LoadLevel((CurrentLevelIndex + 1) % levels.Length);
        }

        public void PreviousLevel()
        {
            if (levels == null || levels.Length == 0)
                return;

            LoadLevel((CurrentLevelIndex - 1 + levels.Length) % levels.Length);
        }

        private void ApplyMode(TestMode mode)
        {
            CurrentMode = mode;

            boardPresenter?.SetEffectsAnimated(mode != TestMode.Baseline);
        }

        private int ClampLevelIndex(int index)
        {
            return levels == null || levels.Length == 0
                ? 0
                : Mathf.Clamp(index, 0, levels.Length - 1);
        }
    }
}
