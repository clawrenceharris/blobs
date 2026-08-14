# Milestone 2 Checklist - Production Scene Shell

**Goal:** a Unity scene can boot the production session and display the sample puzzle through Presentation code without prototype controllers.

## Status

- [x] Add Presentation assembly references required for Unity scene code.
- [x] Add a production bootstrap component that creates a `GameSession`.
- [x] Render the sample board from an Application snapshot.
- [x] Convert blob selection into Application merge commands.
- [x] Rebuild or update board views after execute and restart.
- [x] Expose restart entry points for UI buttons or editor wiring.
- [x] Keep scene shell logic out of Core and Application.
- [x] Verify Presentation compiles with Unity references.
- [x] Wire `Assets/_Game/Scenes/Game.unity` to `GameBootstrapper`, `BoardPresenter`, and `Sample_Level.asset`.
- [x] Make `Sample_Level.asset` solvable with two different-color normal blobs.
- [x] Wire `GameplayInputAdapter` to the active Application `GameSession`.
- [x] Move source/target selection state and move execution policy into `GameSession`.
- [x] Move effect application and restart presentation flow out of `GameBootstrapper`.

## Acceptance Checks

- [x] Play Mode shows two sample blobs in `Assets/_Game/Scenes/Game.unity`.
- [x] Selecting the first blob and then the second blob executes a production merge through `GameSession`.
- [x] A successful merge clears the visible blobs.
- [x] Restart restores the initial visible blobs and clears selection.
- [x] The scene shell uses `GameSession` and does not call prototype managers.

## Current Scope

The first implementation group is intentionally visual-minimal. It should establish the production loop and boundaries before adding polished animation, prefab-specific visuals, HUD, or safe-area UI.

## Implementation Notes

- `Assets/_Game/Scenes/Game.unity` contains a `GameBootstrapper` scene object wired to `Sample_Level.asset`.
- `Sample_Level.asset` contains two different color normal blobs.
- `GameBootstrapper` is the composition root: it creates the Application `GameSession` and wires scene collaborators.
- `GameplayInputAdapter` is initialized with `IGameplayCommands` and sends select intents to Application.
- `GameplayCommandAdapter` exposes restart command entry points for UI buttons or editor wiring.
- `GameSession.SelectBlobAt` owns the source/target selection flow and calls `ExecuteMove`.
- `BoardPresenter` listens to `IGameplayState` events and updates visible board views.
- Restart restores the board through an Application state-restored event and snapshot rebuild.
- Forward moves apply Core effect lists through `BoardPresenter` before falling back to full snapshot rebuilds for unsupported effects.

## Verification Log

- [x] Presentation compile passed from shell against Unity 6000.3.6f1 reference assemblies.
- [x] Core/Application compile passed after restart-only direction replaced player-facing undo as the production target.
- [x] EditMode test source compile passed after adding restart coverage.
- [x] EditMode test source compile passed after adding `GameSession.SelectBlob` coverage.
- [x] Pure C# smoke check passed for selection-driven merge execution.
- [x] Pure C# smoke check passed for merge completion and restart behavior.
- [x] `MilestoneTwoSceneShellTests` covers visible blob clearing after merge.
- [x] `MilestoneTwoSceneShellTests` covers visible blob restoration and selection clearing after restart.
- [x] Expanded EditMode test source compile passed against Unity references.
- [x] Source scan confirms `GameBootstrapper` does not apply effects, subscribe to move results, or own restart gameplay flow.
- [x] Source scan found no references from the scene shell to legacy `GameManager`, `MergeInvoker`, `LevelData`, or `Assets/Scripts` paths.
- [x] Scene serialization points at `Blobs.Presentation.GameBootstrapper`.
- [x] Normal blob prefab serialization points at `Blobs.Presentation.BlobView`.
- [x] Scene sample content has two different-color blobs, so the first source-to-target merge is solvable.
