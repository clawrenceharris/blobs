using System;
using System.Collections.Generic;
using Blobs.Application;
using Cysharp.Threading.Tasks;
using Blobs.Core;
using Blobs.Content;
using Blobs.Input;
using UnityEngine;
using Blobs.Debugging;
using DG.Tweening;

namespace Blobs.Presentation
{
    /// <summary>
    /// Scene composition root for production gameplay. It creates the Application session and wires
    /// Presentation/Input collaborators, but it must not apply effects or own gameplay flow.
    /// </summary>
    public sealed class GameBootstrapper :
        MonoBehaviour,
        IGameplaySessionHost,
        IGameplayPresentationStatus,
        ILevelPaletteSource
    {
        [SerializeField] private BoardPresenter boardPresenter;

        [SerializeField] private LevelDefinitionAsset[] levels;

        [SerializeField] private CameraPresenter cameraPresenter;
        [SerializeField] private GameplayInputAdapter inputAdapter;
        [SerializeField] private GameplayCommandAdapter commandAdapter;
        [SerializeField] private GameplayFeedbackPresenter feedbackPresenter;
        [SerializeField, Min(0)] private int initialLevelIndex;

        [SerializeField, Tooltip("Optional. Tutorials run in order, each fading into the next; after the last " +
            "one the level at Initial Level Index fades in.")]
        private List<TutorialSequenceAsset> tutorials = new();

        [SerializeField, HideInInspector, Tooltip("Legacy single tutorial; folded into Tutorials.")]
        private TutorialSequenceAsset tutorial;

        private bool _isTutorial;
        private int _tutorialIndex = -1;
        private float _pendingFadeIn;

        private TutorialSequenceAsset _tutorialAsset;
        private TutorialSequencer _tutorialSequencer;
        private TutorialCoachPresenter _tutorialCoach;
        private LevelDefinition _currentLevel;
        private LevelDefinitionAsset _currentLevelAsset;
        private GameSession _session;
        private QueuedGameplayCommands _commands;
        private GameSessionSnapshot _presentedSnapshot;

        public int CurrentLevelIndex { get; private set; }

        /// <inheritdoc />
        public LevelDefinition CurrentLevel => _currentLevel;

        /// <inheritdoc />
        public bool IsTutorial => _isTutorial;
        /// <inheritdoc />
        public LevelColorPaletteAsset CurrentPalette =>
            _currentLevelAsset != null ? _currentLevelAsset.Palette : null;

        /// <inheritdoc />
        public event Action<IGameplayCommands, IGameplayState> SessionStarted;

        /// <inheritdoc />
        public event Action<GameSessionSnapshot> PresentationSettled;

        /// <inheritdoc />
        public IGameplayCommands CurrentCommands => _commands;

        /// <inheritdoc />
        public IGameplayState CurrentState => _session;

        /// <inheritdoc />
        public GameSessionSnapshot PresentedSnapshot => _presentedSnapshot;

        private void Awake() => new Debugger(message => Debug.Log(message));

        private void Start()
        {

            CurrentLevelIndex = ClampLevelIndex(initialLevelIndex);

            MigrateLegacyTutorial();
            int firstTutorial = NextTutorialIndex(-1);
            if (firstTutorial >= 0)
            {
                StartTutorial(tutorials[firstTutorial]);
                return;
            }

            if (levels != null && levels.Length > 0)
                LoadLevel(CurrentLevelIndex);
        }




        public void LoadLevel(int index)
        {
            if (levels == null || levels.Length == 0)
                return;

            CurrentLevelIndex = ClampLevelIndex(index);
            StartLevel(levels[CurrentLevelIndex]);
        }

        public void PreviousLevel()
        {
            if (levels == null || levels.Length == 0)
                return;

            LoadLevel((CurrentLevelIndex - 1 + levels.Length) % levels.Length);
        }

        private int ClampLevelIndex(int index)
        {
            return levels == null || levels.Length == 0
                ? 0
                : Mathf.Clamp(index, 0, levels.Length - 1);
        }

        public void ExitGame()
        {
            UnityEngine.Application.Quit();
        }

        public void NextLevel()
        {
            if (levels == null || levels.Length == 0)
                return;

            LoadLevel((CurrentLevelIndex + 1) % levels.Length);
        }

        /// <summary>
        /// Starts a level from authored content by converting it to Core data and wiring scene collaborators.
        /// </summary>
        public void StartLevel(LevelDefinitionAsset asset)
        {
            if (asset == null)
            {
                Debug.LogError("GameBootstrapper cannot start without a level asset.", this);
                return;
            }

            if (!HasRequiredSceneCollaborators())
                return;
            _isTutorial = false;
            var level = LevelAssetMapper.ToCore(asset);

            StartSession(asset, level, null);
        }

        /// <summary>
        /// Starts an authored tutorial on its shared board. The session begins with the pieces that
        /// are visible from the start and the sequencer adds each step's reveals as the player meets
        /// the previous gate. When it ends it fades into the next entry of <c>tutorials</c>, or into
        /// the level at <c>initialLevelIndex</c> after the last one.
        /// </summary>
        public void StartTutorial(TutorialSequenceAsset asset)
        {
            if (asset == null)
            {
                Debug.LogError("GameBootstrapper cannot start without a tutorial asset.", this);
                return;
            }

            if (!HasRequiredSceneCollaborators())
                return;
            _isTutorial = true;

            TutorialDefinition definition = TutorialSequenceMapper.ToApplication(asset);
            _tutorialAsset = asset;
            int listed = tutorials.IndexOf(asset);
            // A tutorial started from outside the list goes straight to the levels when it ends.
            _tutorialIndex = listed >= 0 ? listed : tutorials.Count;
            StartSession(asset.Board, definition.InitialLevel, definition.Steps);
        }

        private void StartSession(
            LevelDefinitionAsset asset,
            LevelDefinition level,
            IReadOnlyList<TutorialStep> tutorialSteps)
        {
            DisposeTutorial();

            _currentLevel = level;
            _currentLevelAsset = asset;


            _session = new GameSession(level, new MoveResolver());

            // Built before the board presenter subscribes so move resolution reaches the sequencer
            // ahead of a synchronous presentation settle.
            if (tutorialSteps != null)
                _tutorialSequencer = new TutorialSequencer(tutorialSteps, _session, this, DeferToNextFrame);

            boardPresenter.SnapshotChanged -= HandlePresentationSettled;
            boardPresenter.SnapshotChanged += HandlePresentationSettled;
            boardPresenter.Initialize(
                _session,
                asset.Palette,
                // Tutorial captions come from the sequencer, not the per-level coach.
                level: tutorialSteps == null ? asset : null);
            float fadeIn = _pendingFadeIn;
            _pendingFadeIn = 0f;
            if (fadeIn > 0f)
            {
                // Tutorials fade the board in from their first step; levels fade in here.
                boardPresenter.SetBoardAlpha(0f);
                if (tutorialSteps == null)
                    boardPresenter.FadeBoardTo(1f, fadeIn)
                        .SetLink(boardPresenter.gameObject, LinkBehaviour.KillOnDestroy);
            }
            _commands = new QueuedGameplayCommands(_session, boardPresenter);

            inputAdapter.Initialize(_commands, boardPresenter.CellSize, boardPresenter.Origin);
            commandAdapter.Initialize(_commands);
            if (feedbackPresenter != null)
                feedbackPresenter.Initialize(inputAdapter, _session);
            FitCameraToSession();

            SessionStarted?.Invoke(_commands, _session);

            if (_tutorialSequencer != null)
                BeginTutorialPresentation();
        }

        private void BeginTutorialPresentation()
        {
            CoachPointerView coachView = boardPresenter.CoachPointerView;
            if (coachView == null)
            {
                Debug.LogWarning(
                    "Tutorial captions need a CoachPointerView on the BoardPresenter.", this);
            }

            _tutorialCoach = new TutorialCoachPresenter(
                coachView,
                boardPresenter,
                _tutorialSequencer,
                _session,
                _tutorialAsset);

            _tutorialCoach.OutroCompleted += HandleTutorialOutroCompleted;
            _tutorialSequencer.Start();
        }

        /// <summary>Runs once the finished tutorial has faded out: start the next tutorial or the first level.</summary>
        private void HandleTutorialOutroCompleted()
        {
            _pendingFadeIn = _tutorialCoach != null ? Mathf.Max(0.0001f, _tutorialCoach.OutroFadeDuration) : 0f;

            int next = NextTutorialIndex(_tutorialIndex);
            if (next >= 0)
            {
                StartTutorial(tutorials[next]);
                return;
            }

            if (levels == null || levels.Length == 0)
            {
                _pendingFadeIn = 0f;
                boardPresenter.FadeBoardTo(1f, 0.3f)
                    .SetLink(boardPresenter.gameObject, LinkBehaviour.KillOnDestroy);
                boardPresenter.SetInteractionLocked(false);
                return;
            }

            LoadLevel(initialLevelIndex);
        }

        private int NextTutorialIndex(int after)
        {
            for (int i = Mathf.Max(0, after + 1); i < tutorials.Count; i++)
            {
                if (tutorials[i] != null)
                    return i;
            }

            return -1;
        }

        private void OnValidate() => MigrateLegacyTutorial();

        private void MigrateLegacyTutorial()
        {
            tutorials ??= new List<TutorialSequenceAsset>();
            if (tutorial == null)
                return;
            if (!tutorials.Contains(tutorial))
                tutorials.Insert(0, tutorial);
            tutorial = null;
        }

        private void FitCameraToSession()
        {
            if (cameraPresenter != null)
                cameraPresenter.FitCameraToBoard(
                    _session.CurrentState.Width,
                    _session.CurrentState.Height,
                    boardPresenter.CellSize);
        }

        private void DisposeTutorial()
        {
            if (_tutorialCoach != null)
                _tutorialCoach.OutroCompleted -= HandleTutorialOutroCompleted;
            _tutorialCoach?.Dispose();
            _tutorialCoach = null;
            _tutorialSequencer?.Dispose();
            _tutorialSequencer = null;
        }

        /// <summary>Runs an advance after the current frame's event handlers finish.</summary>
        private static void DeferToNextFrame(Action action)
        {
            RunNextFrame(action).Forget();
        }

        private static async UniTaskVoid RunNextFrame(Action action)
        {
            await UniTask.Yield();
            action();
        }

        private void HandlePresentationSettled(GameSessionSnapshot snapshot)
        {
            _presentedSnapshot = snapshot;
            PresentationSettled?.Invoke(snapshot);
        }

        private void OnDestroy()
        {
            DisposeTutorial();
            if (boardPresenter != null)
                boardPresenter.SnapshotChanged -= HandlePresentationSettled;
        }

        /// <summary>
        /// Validates required authored references before creating a gameplay session. Silently
        /// adding replacements here would bypass prefab catalogs, input configuration, and roots.
        /// </summary>
        private bool HasRequiredSceneCollaborators()
        {
            if (boardPresenter != null &&
                inputAdapter != null &&
                commandAdapter != null)
                return true;

            Debug.LogError(
                "GameBootstrapper requires authored BoardPresenter, " +
                "GameplayInputAdapter, and GameplayCommandAdapter references.",
                this);
            return false;
        }
    }
}
