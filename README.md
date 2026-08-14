# Blobs

Blobs is a Unity 6 grid puzzle game about selecting a source blob and merging it toward a target blob. The production rebuild lives under `Assets/_Game` and is intentionally separated from the older prototype code under `Assets/Scripts`.

## Current Production Direction

- The first selected blob is the source.
- The second selected blob is the target.
- A normal merge moves the source to the target position and removes the target blob.
- Player-facing undo is not part of the current design direction. If a player wants to recover from a bad move, restart is the intended action.
- Future mechanics should support cascades and chain reactions without requiring every effect to be reversible.

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
    -> MoveResolver
    -> ordered Core effects
    -> BoardPresenter
    -> BlobView / TileView animation and rendering
```

Presentation timing must never decide whether a move is valid. Core and Application update state first; Presentation consumes the resolved result.

## Useful Docs

- `Docs/GAME_DESIGN.md` describes game rules, merge feel, restart-over-undo direction, and planned mechanics.
- `Docs/TECHNICAL_ARCHITECTURE.md` describes layer responsibilities and dependency boundaries.
- `Docs/PRODUCTION_PLAN.md` describes production milestones.
- `Docs/MILESTONE_1_CHECKLIST.md` and `Docs/MILESTONE_2_CHECKLIST.md` track completed production targets.

## Tests

Edit Mode tests live in `Assets/_Game/Tests/EditMode`.

The current focused tests verify:

- Core normal merge behavior.
- Invalid move rejection.
- Selection-driven move execution.
- Restart restoring authored state.
- Scene shell wiring through `GameBootstrapper`, `GameplayInputAdapter`, `GameplayCommandAdapter`, and `BoardPresenter`.

When changing gameplay rules, update tests and docs together so the intended behavior stays clear.
