# Prototype presentation context

Inspected 2026-09-10 at commit 05f5cbf, with existing uncommitted Unity upgrade and prototype changes preserved.

- Unity 6000.3.6f1; URP 17.3.0; DOTween; Newtonsoft JSON. Legacy mouse input is used by this prototype, despite the Input System package also being installed.
- Gameplay scene: `Assets/Scenes/Blobs.unity`. Build Settings still reference a missing `Assets/Scenes/SampleScene.unity`; a standalone build is not validated.
- Prototype source: `Assets/Scripts`, compiled into Assembly-CSharp. Existing untracked `Assets/_Game` work is outside this change.
- Level data: `Assets/Levels/level_1.json` through `level_7.json`. Tutorials exist for 1, 2 and 7; other levels use free play.
- LevelManager owns startup, completion, restart and progression. BoardPresenter owns model/view creation, animation and cleanup. TutorialPresenter owns per-board TutorialLogic, pointer, messages and subscriptions.
- BoardLogic and BoardPresenter expose static events, so subscriptions must be explicitly released. DOTween animations and coroutines require cancellation during board replacement.
- Level loading currently reads Application.dataPath/Levels directly. This is an Editor prototype path, not validated player packaging.
- Unity MCP tools are unavailable. The local Unity Editor can compile and run the existing scene through computer use.

## Regression procedure

Enter Play Mode in Blobs, then choose Tools > Presentation > Run Lifecycle Smoke Check. The check performs actual guided selections for levels 1 and 2, verifies automatic progression into level 3, restarts every available level and checks camera framing, view counts and tutorial reset, then exercises the final completion animation and missing-level boundary. It does not save assets or player progress. Inspect Console for `PRESENTATION SMOKE PASS` and any exceptions or DOTween warnings, then exit Play Mode.

For presentation acceptance, also play through the free-play puzzles 3–6 and level 7 visually, and inspect pointer timing, particles and laser rendering. The smoke check's final-level animation uses a fixture rather than solving that puzzle.

## Level data corrections

Levels 3–5 were still in the legacy verbose JSON format and could not load. Their layout coordinates were preserved while migrating type/color/size keys. Legacy sizes 2/4/5 map to small/normal/big. Level 6 had numeric sizes and two normal-tile type codes inside its blob list; these now use blob codes. Level 7 incorrectly declared levelNum 1; it now declares 7.
