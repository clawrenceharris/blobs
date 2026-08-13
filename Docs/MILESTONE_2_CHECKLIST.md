# Milestone 2 Checklist - Production Scene Shell

**Goal:** a Unity scene can boot the production session and display the sample puzzle through Presentation code without prototype controllers.

## Status

- [x] Add Presentation assembly references required for Unity scene code.
- [x] Add a production bootstrap component that creates a `GameSession`.
- [x] Render the sample board from an Application snapshot.
- [x] Convert blob selection into Application merge commands.
- [x] Rebuild or update board views after execute, undo, and restart.
- [x] Expose undo and restart entry points for UI buttons or editor wiring.
- [x] Keep scene shell logic out of Core and Application.
- [x] Verify Presentation compiles with Unity references.

## Acceptance Checks

- [ ] Adding the bootstrap to a scene shows two sample blobs.
- [ ] Selecting the first blob and then the second blob executes a production merge.
- [ ] A successful merge clears the visible blobs.
- [ ] Undo restores the visible blobs.
- [ ] Restart restores the initial visible blobs and clears selection.
- [x] The scene shell uses `GameSession` and does not call prototype managers.

## Current Scope

The first implementation group is intentionally visual-minimal. It should establish the production loop and boundaries before adding polished animation, prefab-specific visuals, HUD, or safe-area UI.

## Implementation Notes

- Add `ProductionGameBootstrap` to a scene object to boot `ProductionSampleLevels.CreateMilestoneOnePuzzle()`.
- `ProductionGameBootstrap` creates and owns the Application `GameSession`.
- `BoardPresenter` rebuilds visible blob views from `GameSessionSnapshot`.
- `ProductionBlobView` reports selection through Presentation events and does not call Core or Application directly.
- `Undo()` and `Restart()` are public methods on `ProductionGameBootstrap` for temporary editor wiring or future UI buttons.

## Verification Log

- [x] Presentation compile passed from shell against Unity 6000.3.6f1 reference assemblies.
- [x] Source scan found no references from the scene shell to legacy `GameManager`, `MergeInvoker`, `LevelData`, or `Assets/Scripts` paths.
