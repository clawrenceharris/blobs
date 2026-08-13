# Milestone 1 Checklist - Solvable Production Puzzle

**Goal:** one authored puzzle is solvable through production Core and Application code in `Assets/_Game` without prototype controllers.

## Status

- [x] Create `Docs/PRODUCTION_PLAN.md` with production architecture boundaries.
- [x] Keep Milestone 1 implementation inside `Assets/_Game/Scripts/Core` and `Assets/_Game/Scripts/Application`.
- [x] Keep Core and Application free of Unity scene/object dependencies.
- [x] Add minimal Core model for grid positions, blobs, board state, level definitions, and objective evaluation.
- [x] Add production sample level with two matching blobs.
- [x] Add deterministic normal-blob source-to-target merge resolution.
- [x] Emit ordered board effects for successful moves.
- [x] Emit inverse effects that can restore the previous board state.
- [x] Reject non-aligned moves without mutating the board.
- [x] Reject color mismatches without mutating the board.
- [x] Add Application session flow for execute move, undo, restart, move count, and completion state.
- [x] Smoke-check the sample puzzle through standalone Core/Application code.

## Remaining Work

- [ ] Add Edit Mode tests for the Milestone 1 acceptance checks.
- [ ] Add test assembly definitions for production Edit Mode tests.
- [ ] Verify the new production assemblies compile inside Unity, not only through standalone `csc`.
- [ ] Confirm asmdef references match the intended architecture after Unity regenerates project files.
- [ ] Decide whether Milestone 1 sample content stays code-only or moves into a plain data fixture.
- [ ] Add a small developer-facing usage example for booting `GameSession` with `ProductionSampleLevels.CreateMilestoneOnePuzzle()`.
- [ ] Document known limitations of Milestone 1 normal merge behavior before starting Milestone 2.

## Acceptance Checks

- [x] Same-row matching normal merge resolves successfully.
- [x] Same-column matching normal merge is supported by the same resolver path.
- [x] Solving the sample level clears all clearable blobs.
- [x] Solving the sample level sets Application completion state.
- [x] Undo after completion restores two blobs and returns completion to false.
- [x] Restart restores the authored initial state and clears command history.
- [x] Non-aligned move returns `MoveFailureReason.NotAligned`.
- [x] Color mismatch returns `MoveFailureReason.ColorMismatch`.
- [ ] Automated tests cover the successful merge path.
- [ ] Automated tests cover invalid move immutability.
- [ ] Automated tests cover undo after completion.
- [ ] Automated tests cover restart.

## Boundary Checks

- [x] `Blobs.Core` references no other game assembly.
- [x] `Blobs.Application` references Core only.
- [x] Core code does not reference `UnityEngine`, `MonoBehaviour`, `GameObject`, `Transform`, coroutines, rendering, audio, input, or DOTween.
- [x] Application code does not reference Unity scene objects, views, input adapters, UI, Presentation, or Platform implementations.
- [ ] Unity assembly inspector confirms the same boundaries in-editor.

## Definition of Done

Milestone 1 is done when the remaining automated tests and Unity compilation checks pass, and `Docs/PRODUCTION_PLAN.md` plus this checklist accurately describe the current state.
