# Presentation validation — 2026-09-10

Scope: the early prototype in Assets/Scenes/Blobs.unity, Unity 6000.3.6f1. Existing unsaved scene changes and unrelated working-tree modifications were preserved.

Status: Ready with limitations for the Editor presentation lifecycle. Final refreshed-assembly smoke run passed; no new runtime exceptions or DOTween safe-mode errors appeared in that run. Unity was left out of Play Mode.

## Evidence

- Forced Unity Assets Refresh rebuilt Assembly-CSharp and Assembly-CSharp-Editor successfully (Tundra build success).
- Runtime smoke check: Tools > Presentation > Run Lifecycle Smoke Check. Exercises real guided selection/merge animations in levels 1 and 2, including automatic transition into level 3.
- Loads and restarts levels 1–7; checks model level identity, tutorial reset versus free play, stale view destruction, exact blob/tile view counts, and tile positions inside the camera viewport.
- Runs the final completion animation and checks that no level 8 is loaded, board state/views are cleared, and input is disabled.
- All seven level JSON files pass identifier, type, size, coordinate bounds and duplicate-cell checks.
- Targeted git diff whitespace checks pass.

## Limitations and baseline issues

- The smoke check does not solve free-play puzzles 3–6 or the level 7 puzzle. Its final-level completion is an animation fixture. Full puzzle-solvability coverage remains manual.
- A standalone player build was not run. Existing Build Settings point to a missing SampleScene; level loading also uses an Editor filesystem path.
- Pre-existing unused-event warnings and duplicate serialized LaserTileView._visuals remain outside this change. Earlier runtime failures from legacy JSON and level 7's incorrect number were fixed.
- The initial external dotnet build could not run because generated NuGet project.assets.json was absent; Unity compilation was used instead.

## Legacy swipe input follow-up

Implemented with UnityEngine.Input mouse down/up and TouchPhase/finger tracking. Source is the blob at press; target is the blob at release. Empty endpoints cancel; target choice never uses swipe direction. Tap selection remains available. Disabling input invalidates pending gestures; per-blob swipe subscriptions are removed with the existing view cleanup.

Unity compilation passed. Tools > Presentation > Run Legacy Swipe Smoke Check produced `LEGACY SWIPE SMOKE PASS`: canceled press across an input lock, empty tile between source and target, tap tolerance, endpoint-based horizontal/vertical merges, animation lock and automatic level-two tutorial reset. This Play Mode check calls the shared pointer handlers with screen positions; native touch delivery on a physical device was not tested. No new runtime errors appeared in the check. Unity was left out of Play Mode.
