# Blobs Production Rebuild Plan

**Status:** Active implementation plan  
**Target folder:** `Assets/_Game`  
**Primary constraint:** each layer has one responsibility and references only the layers allowed by `Docs/TECHNICAL_ARCHITECTURE.md`.

## Architecture Boundaries

Production code lives under `Assets/_Game` and replaces prototype behavior behind explicit assembly boundaries. Prototype code in `Assets/Scripts` remains a feel and rule reference until production systems cover the same behavior.

### Core

Owns deterministic puzzle rules and board mutation.

- Allowed references: .NET / C# only.
- Must not reference Unity, Application, Input, Presentation, UI, Platform, scene objects, or assets.
- Owns board state, level definitions after validation, blobs, tiles, move intents, resolution, ordered effects, and objective evaluation.
- Provides stable data and effect results for outer layers.

### Application

Owns the playable session around Core.

- Allowed references: Core.
- Coordinates level start, move execution, restart, move count, and completion state.
- Does not animate, read hardware input, or know Unity scene objects.
- Exposes state snapshots and command results to Presentation and UI.

### Input

Owns hardware and UI-control input translation.

- Allowed references: Application.
- Converts pointer, touch, keyboard, and button actions into Application commands.
- Does not inspect or mutate Core state directly except through Application-facing command APIs.

### Presentation

Owns board views and feedback.

- Allowed references: Core and Application.
- Consumes Core effect lists and Application state snapshots.
- May use Unity, DOTween, pooling, VFX, haptics hooks, and camera response.
- Presentation timing must never affect whether a move is valid or what state results.

### UI

Owns screen-level game UI.

- Allowed references: Application, and Platform where needed for safe areas/settings.
- Displays moves, restart, objectives, tutorial text, pause/settings, and win flow.
- Sends user actions to Application commands.

### Platform

Owns environment-specific services.

- Allowed references: service contracts needed by Application/UI; no game-rule ownership.
- Implements storage, haptics, audio focus, safe-area data, analytics, and Web/native differences.
- Optional services fail gracefully.

## MVP Pattern

The rebuilt MVP pattern should be dependency-safe:

- Model: Core `BoardState`, level data, effects, and objective result.
- Presenter: Presentation classes that observe Application/Core results and drive views.
- View: Unity `MonoBehaviour` objects, prefabs, animation components, UI widgets.

Presenters may read immutable result data from Core and invoke Application commands. They must not mutate Core board state directly, invent move outcomes, or make rule decisions.

## Product Targets

### Milestone 1 - Solvable Production Puzzle

Completion means one authored puzzle can be solved through production Core and Application code without prototype controllers.

- Create minimal Core domain model: grid positions, blobs, board state, level definition, objective state.
- Create deterministic normal merge resolution for source-to-target moves.
- Emit ordered effects for successful moves.
- Reject invalid moves without mutating board state.
- Create Application session command flow: start level, execute move, restart, completion.
- Add one production sample level in code or plain data with two matching blobs that clears successfully.
- Keep all code Unity-free in Core and Application.

Acceptance checks:

- A same-row or same-column matching normal merge resolves successfully.
- A non-aligned move is rejected and leaves state unchanged.
- A color mismatch is rejected and leaves state unchanged.
- Solving the sample level sets Application completion state.
- Restart restores the authored initial level.

### Milestone 2 - Production Scene Shell

Completion means a Unity scene can boot the production session and display the sample puzzle.

- Add a production bootstrap in Presentation/Application composition.
- Render the board from Application snapshots.
- Convert view selection into Application merge commands.
- Play basic effect-driven movement/removal feedback.
- Show moves, restart, and completion UI.

### Milestone 3 - Content Pipeline

Completion means production levels can be authored outside code and validated before play.

- Define production level JSON or ScriptableObject import format.
- Convert authoring assets into Core `LevelDefinition`.
- Validate IDs, dimensions, coordinates, occupancy, colors, blob types, and objectives.
- Load at least three handcrafted MVP levels.

### Milestone 4 - Cascade-Ready Presentation

Completion means Presentation and Core remain synchronized across execute, cascades, and restart.

- Map blob IDs to views.
- Apply ordered forward effects in a readable source-to-target sequence.
- Support cascaded effect chains without requiring player-facing undo.
- Rebuild from Application snapshots after restart or desync.
- Add smoke tests for model/view agreement.

### Milestone 5 - First Special Mechanic

Completion means one non-normal mechanic composes with the same move/effect pipeline.

- Add the smallest special rule with tests, likely Trail or Ghost + Sigil.
- Keep the normal merge language unchanged.
- Add focused levels demonstrating the mechanic.

## First Actionable Group

Implement Milestone 1 in `Assets/_Game/Scripts/Core` and `Assets/_Game/Scripts/Application`.

1. Build the minimal Core domain and resolver.
2. Build Application `GameSession` over Core.
3. Include a sample level factory for the first solvable puzzle.
4. Verify by compiling the new production assemblies or, if Unity compilation is unavailable from the shell, by static inspection and a small pure C# smoke harness where practical.
