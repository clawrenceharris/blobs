# Blobs

Blobs is a Unity 6 grid puzzle game about selecting a source blob and merging it toward a target blob. The production rebuild lives under `Assets/_Game/Production` and is intentionally separated from the older prototype code under `Assets/_Game/Prototype`.

## Current Production Direction

The authoritative rules are in [Game design](Docs/GAME_DESIGN.md); pending code changes and acceptance scenarios are in [Rules implementation plan](Docs/RULES_IMPLEMENTATION_PLAN.md). These describe the September 9 decisions, some of which are not yet implemented.

- Select a source and a blob target on the same row or column.
- Simulate reached interactions atomically. Normal/Trail merges continue; Rocks stop; Ghosts consume and haunt. Reached color/terrain failures reject the whole action.
- Every level has one Flag; only a matching Normal may capture it, leaving zero clearable blobs. Victory always means zero clearable blobs.
- Trail spawns on departures except current-action merge sites. Ghost haunt returns to the original source position; ethereal Sigil contact clears it.
- Undo reverses one whole action with dedicated feedback and never reduces move count. Restart always resets count/history and cancels playback.

## Architecture

The production code follows the boundaries documented in `Docs/TECHNICAL_ARCHITECTURE.md`.

- `Blobs.Core` owns deterministic board state, move rules, ordered effects, and objective evaluation. It has no Unity references.
- `Blobs.Application` owns the playable session around Core: source/target selection, move execution, restart, move count, completion state, and snapshots.
- `Blobs.Input` translates Unity input into Application commands.
- `Blobs.Presentation` observes Application/Core results and updates Unity views, animation, camera, and feedback.
- `Blobs.Content` converts authored Unity assets into Core level definitions.
- `Blobs.UI` is reserved for HUD and screen-level UI.

`GameBootstrapper` is a composition root only. It creates the session and wires scene collaborators; it should not execute moves, apply effects, or own gameplay presentation.

Canvas UI binds through `IGameplaySessionHost` and Application interfaces. UI presenters should observe snapshots and call command interfaces; they should not reference Core rules, board presenters, or the concrete bootstrapper type.

## Key Runtime Flow

```text
Pointer / UI input
    -> GameplayInputAdapter or GameplayCommandAdapter
    -> GameSession
    -> MoveResolver (per-tile simulation, atomic commit)
    -> MoveResult.Steps + flattened effects
    -> BoardPresenter (step beats; traverse move + trail spawn joined)
    -> BlobView / TileView animation and rendering
```

`MoveResolver` plans the whole intent on a cloned board as a timeline of `MoveStep`s (`Traverse` or `Merge`). Collision strategies emit only occupant resolution (`CollisionPlan`); locomotion and trail spawning are owned by the resolver. Failed intents leave the real board untouched.

Presentation timing must never decide whether a move is valid. Core and Application update state first; Presentation consumes the resolved steps.

## Useful Docs

- [Game design](Docs/GAME_DESIGN.md): agreed rules and presentation direction.
- [Rules implementation plan](Docs/RULES_IMPLEMENTATION_PLAN.md): pending changes and acceptance cases.
- [Technical architecture](Docs/TECHNICAL_ARCHITECTURE.md): layer responsibilities.
- [Production plan](Docs/PRODUCTION_PLAN.md): current delivery priorities.
- [Project baseline](Docs/PROJECT_BASELINE.md): historical prototype/production inspection.

## Tests

Edit Mode tests live in `Assets/_Game/Production/Tests/EditMode`.

The current focused tests verify:

- Core normal merge behavior and per-tile step timelines.
- Chain merging, consuming merges, and atomic failure.
- Flag capture rules.
- Trail spawning, trail color, and merge-site spawn suppression.
- Collision contract (`CollisionPlan`, merge strategies, follow-up steps).
- Invalid move rejection.
- Selection-driven move execution.
- Restart restoring authored state.
- Scene shell wiring through `GameBootstrapper`, `GameplayInputAdapter`, `GameplayCommandAdapter`, and `BoardPresenter`.
- Step-driven Presentation apply/rebuild.

When changing gameplay rules, update tests and docs together so the intended behavior stays clear.
