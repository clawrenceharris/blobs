# Blobs Technical Architecture

**Status:** Proposed production architecture 0.9 — migration target for the current prototype  
**Target:** Unity 6, native mobile, and Web

## Architectural goal

Game rules, board state, restart behavior, and reaction chains must be deterministic and testable without loading a Unity scene. Unity objects present results; they do not decide the rules.

The production implementation must not grow out of the prototype controllers (`GameManager`, presenters mixed with model mutation, etc.). Preserve the current playable build as a feel reference and rebuild the production loop behind explicit boundaries. Existing pieces such as `MoveResolver`, `ResolutionPipeline`, `IEffect` / `BoardTransaction`, and `BoardModel` are the seed of Core; they should move behind assembly boundaries rather than drive scene objects directly.

## Layers

### Core

Plain C# containing board state, blobs, tiles, merge rules, move intents, the resolution pipeline, ordered effects, restartable initial state, and win / objective evaluation.

- No `MonoBehaviour`, `GameObject`, `Transform`, coroutine, audio, input, or rendering references.
- Grid coordinates are logical integers, not world positions.
- State changes occur through explicit effects (and commands that apply them), not ad-hoc presenter mutation.
- The same initial state and move sequence always produce the same result.
- Cascades and multi-step resolution run from one coherent board snapshot per resolution pass.

### Application

Coordinates level loading, move execution, restart, progression, saves, and scene flow. It owns the session but delegates rules to Core.

### Input

Converts touch, mouse, and keyboard actions into application commands (select blob, merge intent, restart, pause). Hardware-specific input never reaches Core.

### Presentation

Turns resolved effects and restored states into board views, animation (DOTween), VFX, merge juice, haptics, and camera response. Presentation timing cannot alter resolved rules; it only consumes effect lists produced by Core.

### UI

Owns HUD (moves, restart), objective / tutorial display, pause, settings, level-complete / win flow, and responsive safe-area layout.

### Platform

Small interfaces isolate storage, haptics, audio focus, safe areas, analytics if later approved, and Web/native differences. Optional services must fail gracefully.

## Data flow

```text
Touch / mouse / keyboard
          ↓
       Input intent
          ↓
 Selection or merge / restart command
          ↓
 Application session state
          ↓
 Deterministic Core resolution ← Validated level data
          ↓
 New state + ordered effects
          ↓
 Presentation / UI / audio
          ↓
 Progress and score save data
```

## Source layout

```text
Assets/_Game/Production/
  Art/
  Audio/
  Content/
    Levels/
    ColorSchemes/
  Prefabs/
  Scenes/
  Scripts/
    Core/
    Application/
    Input/
    Presentation/
    UI/
    Platform/
  Tests/
    EditMode/
    PlayMode/
```

Prototype code under `Assets/Scripts/` remains the feel reference until production assemblies replace it piece by piece.

## Assembly boundaries

Create assembly definitions when implementation begins:

- `Blobs.Core`: deterministic runtime rules (board, blobs, tiles, merge rules, resolver, effects).
- `Blobs.Application`: sessions, move execution, restart, progression, and game flow; references Core.
- `Blobs.Input`: input adapters; references Application.
- `Blobs.Presentation`: Unity views, presenters, and feedback; references Core and Application.
- `Blobs.UI`: HUD and screen UI; references Application.
- `Blobs.Platform`: platform service implementations.
- `Blobs.Tests.EditMode`: Core and Application tests.
- `Blobs.Tests.PlayMode`: integration smoke tests.

Dependencies flow inward. Core never references another game assembly.

## Scene composition and MVP split

The production scene keeps setup separate from gameplay presentation. The composition root may build and connect objects, but it must not become a presenter or controller.

### Composition root

`GameBootstrapper` is limited to scene composition and startup:

- Locate or create scene collaborators (`BoardPresenter`, `GameplayInputAdapter`, gameplay presenter, camera/background helpers, UI roots).
- Convert authored content assets into validated Core/Application level data.
- Create the active Application `GameSession`.
- Initialize collaborators with the active session and initial level presentation data.

It must not apply Core effects, own source/target selection, execute moves, process restart outcomes, subscribe to gameplay-result streams, mutate board views, or update HUD state. If a behavior depends on move results, snapshots, or restart outcomes, it belongs in Application, Presentation, or UI, not the bootstrapper.

### MVP responsibilities

The MVP pattern remains dependency-safe by treating Application as the gameplay-facing model boundary:

- Model: Core state/rules plus Application session state and command history.
- Presenter: Presentation classes that observe Application outcomes and drive views.
- View: Unity scene objects and `MonoBehaviour` views that expose rendering/input surfaces without deciding rules.

Current production split:

- `GameSession` owns move execution, source/target selection policy, restart, and completion state.
- `GameplayInputAdapter` converts pointer/grid input into Application command calls; it does not mutate views.
- `GameplayCommandAdapter` exposes non-pointer commands such as restart for UI buttons or editor wiring.
- `BoardPresenter` listens to Application state/result events, owns board view state, applies ordered effects when available, and rebuilds from snapshots when required.
- `GameplayHudPresenter` listens to Application snapshots and forwards UI button actions through `IGameplayCommands`.
- `IGameplaySessionHost` lets scene UI bind to the active session without referencing the concrete bootstrapper type.
- `GameBootstrapper` wires the above collaborators together and then gets out of the gameplay loop.

## Core concepts

Likely starting concepts (align with existing types where they already fit):

```text
BoardState
GridPosition
Blob
BlobType / BlobColor / BlobSize
Tile / TileType
MoveIntent
MoveContext
MoveResult
IEffect
BoardTransaction
ResolutionPipeline / IResolutionRule
MergeRules
DomainEvent (optional semantic layer over effects)
LevelDefinition
WinCondition / LevelComplete
```

Design the exact API alongside representative merge and cascade tests. Do not create abstract frameworks without an immediate behavior to support.

## Move resolution

A forward player move conceptually:

1. Input produces a `MoveIntent` (today: merge source → target; no validation at the input boundary).
2. Core builds a `MoveContext` from the intent and a board snapshot (path, ice/portals/sticky adjustments, etc.).
3. The resolution pipeline runs ordered rules: validate intent, path, lasers, merge eligibility, cascades.
4. Rules emit atomic `IEffect` values (`MoveBlob`, `RemoveBlob`, `SpawnBlob`, `ResizeBlob`, `SetTileState`, `Trigger`, …) without touching views.
5. A `BoardTransaction` applies effects to board state in deterministic order.
6. Win / clearable evaluation runs on the post-resolution state.
7. Application records move count/progression state and can restart from the authored initial state.
8. Presentation plays the ordered effect list as one readable source-to-target action, including cascades when present.

A merge is valid only when Core rules allow it (alignment, color/size rules, special blob/tile rules). Invalid intents return a reason without mutating state. Cascades must remain deterministic: same board + intent → same effect sequence.

Blob IDs stay stable within a resolved action so presenters can resolve moves, removals, and spawns; see `Assets/Documentation/BLOB_LIFECYCLE.md`.

## Restart and session history

The player-facing recovery action is restart, not undo.

Cascades and multi-step reactions should be allowed to grow without requiring every visual or rule effect to support clean reversal. Ordered effects remain important for deterministic resolution, animation synchronization, debugging, and tests, but production gameplay should not depend on inverse effects for player undo.

- Retain the authored initial level state.
- Restart discards all commands and reconstructs the authored state.
- Restart clears selection, move count, transient cascade state, and completion state before reevaluating the initial board.
- Application may keep move history for analytics, scoring, debugging, or replay tooling, but that history is not a player-facing undo stack.
- Presentation rebuilds from the restarted snapshot and must not rely on reversing animations.

## Win and objective evaluation

- Clearing the board of clearable blobs (and satisfying target / flag rules) is evaluated in Core from board state, not from animation completion.
- Target / flag blobs require matching color and “last clearable” semantics as defined by merge rules.
- Application exposes level-complete as explicit session state and emits a completion event when a forward move crosses from incomplete to complete.
- Restarting after completion returns the session to the initial incomplete state unless the authored board is already complete.
- Scoring (moves, stars, gems) is Application/content policy fed by Core outcomes; presentation only displays it.

## Content model

Use JSON (current) and/or ScriptableObjects as authoring assets for levels, tutorials, color schemes, audio mappings, and tuning. Convert them into validated plain data before starting Core.

Validate at minimum:

- Unique level identity and schema version.
- Board dimensions and in-bounds coordinates.
- Valid blob types, colors, sizes, and tile types.
- Valid tutorial step bounds when present.
- Scoring thresholds when used.
- No impossible serialized state (e.g. two blobs in one cell).

Never store mutable session state in a level asset.

## Input contracts

Use intent-based Input System actions such as:

```text
Point / Select
MergeConfirm (or drag-to-target)
Restart
Pause
```

- Touch and mouse share pointer interaction logic.
- UI and board input cannot consume the same pointer action.
- Selection and merge intents describe what the player attempted; adapters create application commands and never mutate views or Core directly.
- Restart keys and HUD buttons map to the same application commands as touch controls.

## Save data

Use versioned save data separate from level assets. Initially save settings, unlocked/completed levels, and best score / star results.

- Handle missing, older, or malformed data safely.
- Write atomically where supported.
- Do not save transient mid-level reaction history unless resume testing proves it necessary.
- Never make save migration depend on a loaded scene.
- Cloud saves are deferred.

## Testing strategy

### Edit Mode

- Merge alignment, color, and size rules.
- Target / flag win conditions.
- Path blocking and laser / special-tile rejection.
- Occupied-cell and single-occupant integrity.
- Effect application for move, remove, spawn, resize, tile state.
- Cascades / multi-merge determinism.
- Deterministic ID assignment across resolve and restart.
- Session history and restart behavior.
- Level-data validation.
- Board integrity (registry ↔ grid occupancy).

### Play Mode

- Production scene boots without errors.
- Touch/mouse can select and merge blobs.
- Restart restores model and Presentation agreement.
- Win panel appears only after Core reports completion.
- UI respects safe areas and representative aspect ratios.

### Build verification

- C# compilation succeeds without new warnings.
- Focused and full Edit Mode suites pass.
- Relevant Play Mode smoke tests pass.
- A Web development build launches in a supported browser.
- A representative native mobile build launches on hardware.

## Performance principles

- Target stable 60 FPS on representative hardware, with graceful 30 FPS support if required.
- Avoid per-frame managed allocations during routine playback.
- Keep materials, lights, transparent effects, and simultaneous audio voices constrained.
- Profile before adding pooling beyond the existing view pool, or before adding history snapshots.
- Measure Web startup size and memory throughout production.

## Deliberate non-goals

- No networked simulation.
- No runtime level editor in the first production milestone (Editor tooling may remain editor-only).
- No procedural generation until handcrafted level rules are proven.
- No dependency-injection framework without demonstrated need.
- No general-purpose visual scripting layer for rules.
- No remote-content system during the initial production milestones.

## Migration notes

Move toward this layout without a big-bang rewrite:

1. Extract pure Core types behind assemblies; stop referencing `BoardPresenter` from resolvers (pass `BoardModel` / `BoardState` only).
2. Keep `MoveResolver` → effects → `BoardTransaction` as the Core spine; thin Application over command history.
3. Push animation, pooling, and DOTween entirely into Presentation consumers of effect lists.
4. Replace scene-coupled win checks and input with Application session + Input adapters.
5. Add Edit Mode tests for every merge rule and cascade before adding new mechanics (trail, bomb, ghost, sigil, ice, sticky, portals).
