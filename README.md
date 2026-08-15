# Blobs

Blobs is a Unity 6 grid puzzle game about selecting a source blob and merging it toward a target blob. The production rebuild lives under `Assets/_Game` and is intentionally separated from the older prototype code under `Assets/Scripts`.

## Current Production Direction

- The first selected blob is the source.
- The second selected blob is the target.
- A move is a rook-aligned path from source to target. Every occupied cell on that path must be a valid merge (chain merging). Empty cells are traversed.
- A normal merge removes the occupant; the source survives and may continue along the path.
- Flag blobs cannot be selected as a source. A matching-color source can capture a flag only when those two blobs are the last remaining blobs; capture consumes the source.
- Trail blobs move like normal blobs and leave Normal blobs of their trail color on departed tiles that were not merge sites.
- Player-facing undo is not part of the current design direction. If a player wants to recover from a bad move, restart is the intended action.
- Future mechanics (Ghost follow-up motion, tiles, cascades) compose as extra steps or collision follow-ups on the same timeline.

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

- `Docs/GAME_DESIGN.md` describes game rules, merge feel, restart-over-undo direction, and planned mechanics.
- `Docs/TECHNICAL_ARCHITECTURE.md` describes layer responsibilities and dependency boundaries.
- `Docs/PRODUCTION_PLAN.md` describes production milestones.
- `Docs/MILESTONE_1_CHECKLIST.md`, `Docs/MILESTONE_2_CHECKLIST.md`, and
  `Docs/MILESTONE_3_CHECKLIST.md` track completed production targets.
- `Docs/MILESTONE_4_CHECKLIST.md` tracks the cascade-ready Presentation target.

## Tests

Edit Mode tests live in `Assets/_Game/Tests/EditMode`.

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
