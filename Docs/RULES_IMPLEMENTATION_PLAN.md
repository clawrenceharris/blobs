# Agreed rules — implementation and acceptance checklist

September 9, 2026. Authority: [game design](GAME_DESIGN.md). This is a pending implementation plan, not a test-run report. No runtime changes are made by the documentation pass. Paths below are under `Assets/_Game/Production/`.

## Current gaps and implementation order

- [x] **1. Resolution outcomes and stopping.** Update `Scripts/Core/Resolution/MoveResolver.cs`, `CollisionPlan.cs`, and `Scripts/Core/Strategy/MoveStrategy.cs` / `MergeStrategy.cs` to distinguish rejection, valid unchanged contact, and board-changing success. Intermediate Rocks must terminate safely. Validate terrain only until the reached terminator. Keep all failures atomic.
- [ ] **2. Normal/Trail registrations and occupancy.** Complete `Scripts/Core/BlobRuleBook.cs` movement registrations for Trail targets. Preserve current-action merge-site suppression. Reject undefined occupied-cell spawning. Preserve source identity/type/color across valid normal merges.
- [ ] **3. Flag contract.** Require Normal source and matching color; explicitly selected Flag must actually be captured and leave zero clearable blobs after the whole simulation. Reject intermediate Flags. Preserve earlier chain-clears in simulation when deciding capture eligibility. Implement agreed reason precedence in result/feedback mapping.
- [ ] **4. Ghost/Sigil edge cases.** Update `GhostMergeStrategy` and `GhostReturnEffect` for a Ghost starting haunt on a Sigil, including a zero-distance clearing event. Preserve intermediate occupants and restore consumed landing occupants during Undo.
- [ ] **5. Authoring validation.** Extend `Scripts/Core/Levels/LevelValidator.cs` with exactly one Flag, positive clearable count, and a selectable source. Reconcile `Scripts/Editor/LevelDefinitionAssetEditor.cs` defaults with `Scripts/Content/LevelAssetMapper.cs`. Migrate real level fixtures deliberately; isolated Core rule tests can construct boards without requiring a full authored level.
- [ ] **6. Session history and accounting.** `Scripts/Application/GameSession.cs` currently derives `MoveCount` from `_history.Count`, stores intent-only entries, and has no player Undo command. Introduce an independent count and whole-action restoration records, no Redo, unchanged-action filtering, and availability rules. Snapshot restoration alone is insufficient for the required reverse animation; retain the ordered resolved action and restoration data.
- [ ] **7. Reverse presentation.** Extend the presentation timeline and effect handlers to reverse full actions, including restoring removed/consumed blobs with stable identities and traits. Add dedicated Undo feedback. Do not rerun forward rules to guess the past outcome.
- [ ] **8. Input, HUD, victory, cancellation.** Connect Undo controls and command availability through Application interfaces. Delay victory display until winning playback completes; lock Undo immediately for a logical win. Restart must interrupt forward/reverse/winning sequences safely. Rejection/no-change feedback must remain nonblocking and must not clear newer user selection through delayed callbacks.
- [ ] **9. Integration validation.** Reconcile test assets/fixtures against current paths and stricter level validation, run EditMode and PlayMode suites, then test the production player build. The earlier baseline reported stale paths; some test files already have workspace edits, so inspect their current state before changing them.

## Acceptance scenarios

These are executable-test specifications to implement, not completed tests. Every rejection asserts identical pre/post board, unchanged count/history, cleared selection, and correct rejection feedback where applicable.

| ID  | Scenario                                                                         | Expected result                                                                                            |
| --- | -------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------- |
| R01 | Each of Normal→Normal, Normal→Trail, Trail→Normal, Trail→Trail; differing colors | Occupant removed; source retains identity/type/color.                                                      |
| R02 | Same-color collision after earlier valid normal merges                           | Reject whole action; no earlier simulated effects survive.                                                 |
| R03 | Valid merge followed by reached gap                                              | Reject whole action: Path Blocked.                                                                         |
| R04 | Valid merge → Rock → gap or same-color blob → non-Flag target                    | Stop before Rock; ignore unreachable obstacles.                                                            |
| R05 | Ghost → gap → non-Flag target                                                    | Consume source and haunt; ignore forward terrain beyond Ghost.                                             |
| R06 | Same-color blob before Rock                                                      | Reject whole action by color rule.                                                                         |
| R07 | Adjacent Rock, with Normal and Trail sources                                     | Contact feedback; identical board/count/history; no trail spawn.                                           |
| R08 | Trail → empty cell → Rock                                                        | Trail stops on empty cell; spawn at starting cell only.                                                    |
| R09 | Undefined spawn into occupied cell                                               | Reject whole action without arbitrary consumption.                                                         |
| F01 | Matching Normal clears other blobs en route to selected Flag                     | Accept if capture leaves zero clearable blobs; Rocks may remain.                                           |
| F02 | Trail source, wrong color, remaining clearable blob                              | Flag attempt rejects; source eligibility takes precedence over path, then final remaining-clearable check. |
| F03 | Rock or Ghost terminates before selected Flag                                    | Reject whole action, even if Ghost/Sigil would otherwise clear the board.                                  |
| F04 | Intermediate Flag while another blob is selected target                          | Reject with explanation.                                                                                   |
| F05 | Non-Flag action clears last clearable blob                                       | Win without separate Flag-capture requirement.                                                             |
| T01 | Trail crosses empty cells and merge sites                                        | Spawn on departures including origin; skip current-action merge sites and resting cell.                    |
| G01 | Chain merges before Ghost                                                        | Haunt destination is original action start.                                                                |
| G02 | Trail into Ghost with leftovers                                                  | Intermediate leftovers remain; destination leftover consumed.                                              |
| G03 | Occupied Sigil crossed during haunt or at destination                            | Ghost clears; occupant survives; no later landing.                                                         |
| G04 | Idle Ghost on Sigil begins haunt                                                 | Idle state legal; Ghost clears immediately on becoming ethereal.                                           |
| G05 | Occupied non-Sigil haunt destination                                             | Occupant consumed regardless of color/type.                                                                |
| V01 | Zero/two Flags, overlapping blobs, no clearables, or no Normal/Trail             | Authored level validation rejects each case.                                                               |
| V02 | One Flag, legal sources/positions, idle Ghost on Sigil                           | Structural validation permits it; solvability remains separate.                                            |
| S01 | Reject, click same source, or click outside blobs                                | Selection cancels; next blob click starts a fresh attempt.                                                 |
| S02 | Valid move followed by rejection/unchanged contact, then Undo                    | Undo restores prior board-changing action.                                                                 |
| U01 | Two changes; Undo twice                                                          | Initial board restored; count remains two; Undo unavailable with empty history.                            |
| U02 | New change after Undo                                                            | Count increments; new history branch; no Redo.                                                             |
| U03 | Undo Trail/chain/Ghost/Sigil action                                              | Entire action reverses in order with dedicated feedback and exact restoration.                             |
| P01 | Forward/Undo playback                                                            | Selection and Undo disabled; Restart enabled.                                                              |
| P02 | Rejection or unchanged contact feedback                                          | Selection/Undo remain available according to session state.                                                |
| P03 | Winning action committed                                                         | Undo/selection locked; victory shown only after final playback.                                            |
| P04 | Restart during forward, Undo, winning playback, or victory                       | Initial board, zero count, empty history; no stale effects or victory callbacks.                           |

## Semester scheduling

Keep the deliverable at exactly 15 weeks. Weeks 1–3 establish rules, resolution, validation, and baseline build; week 4 stabilizes special interactions. Use weeks 5–6 for reverse playback/Undo feedback alongside forward game-feel work. Weeks 7–10 remain level design; week 11 completes HUD/Undo integration and player flow. Weeks 12–13 playtest and revise; weeks 14–15 validate and deliver. Limit optional new mechanics to preserve time for the now-required Undo work.

## Completion evidence

Record actual test commands, results, build target, and remaining failures when implementation is performed. No acceptance row is satisfied solely by adding its test or documenting its intended outcome.
