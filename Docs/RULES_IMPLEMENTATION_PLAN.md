# Agreed rules — implementation and acceptance checklist

September 9, 2026. Authority: [game design](GAME_DESIGN.md). This is a pending implementation plan, not a test-run report. No runtime changes are made by the documentation pass. Paths below are under `Assets/_Game/Production/`.

## Current gaps and implementation order

- [x] **1. Resolution outcomes and stopping.** Update `Scripts/Core/Resolution/MoveResolver.cs`, `CollisionPlan.cs`, and `Scripts/Core/Strategy/MoveStrategy.cs` / `MergeStrategy.cs` to distinguish rejection, valid unchanged contact, and board-changing success. Intermediate Rocks must terminate safely. Validate terrain only until the reached terminator. Keep all failures atomic.
- [x] **2. Normal/Trail registrations and occupancy.** Complete `Scripts/Core/BlobRuleBook.cs` movement registrations for Trail targets. Preserve current-action merge-site suppression. Reject undefined occupied-cell spawning. Preserve source identity/type/color across valid normal merges.
- [x] **3. Flag contract.** Require Normal source and matching color; explicitly selected Flag must actually be captured and leave zero clearable blobs after the whole simulation. Reject intermediate Flags. Preserve earlier chain-clears in simulation when deciding capture eligibility. Implement agreed reason precedence in result/feedback mapping.
- [x] **4. Ghost/Grave edge cases.** Update `GhostMergeStrategy` and `GhostReturnEffect` for a Ghost starting haunt on a Grave, including a zero-distance clearing event. Preserve intermediate occupants and restore consumed landing occupants during Undo.
- [x] **5. Authoring validation.** Extend `Scripts/Core/Levels/LevelValidator.cs` with exactly one Flag, positive clearable count, and a selectable source. Reconcile `Scripts/Editor/LevelDefinitionAssetEditor.cs` defaults with `Scripts/Content/LevelAssetMapper.cs`. Migrate real level fixtures deliberately; isolated Core rule tests can construct boards without requiring a full authored level.
- [x] **6. Session history and accounting.** `Scripts/Application/GameSession.cs` uses an independent committed move count, records only board-changing actions, exposes `CanUndo`, and stores the resolved action plus restoration data needed for whole-action Undo. Undo restores the previous board without decreasing the move count, clears selection, has no Redo branch, and restart resets count/history.
- [x] **7. Reverse presentation.** The presentation timeline can reverse ordered move steps/effects, restore removed/consumed blobs with stable identities and traits, and play dedicated Undo feedback. Reverse playback consumes the recorded action rather than rerunning forward rules to infer the past.
- [ ] **8. Input, HUD, victory, cancellation.** Finish the player-facing polish around completed systems: keep Undo visually available whenever `GameSessionSnapshot.CanUndo` is true, queue Undo commands submitted during forward/reverse playback, prevent blob selection during playback, delay victory display until winning playback completes, and verify restart interrupts forward/reverse/winning sequences without stale callbacks. Rejection/no-change feedback must remain nonblocking and must not clear newer user selection through delayed callbacks.
- [ ] **9. Integration validation.** Keep the current Unity evidence fresh by running EditMode and PlayMode suites after the input/HUD/victory pass, then test the production player build. Record commands, counts, failures, and target platform here or in a linked validation note.

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
| F03 | Rock or Ghost terminates before selected Flag                                    | Reject whole action, even if Ghost/Grave would otherwise clear the board.                                  |
| F04 | Intermediate Flag while another blob is selected target                          | Reject with explanation.                                                                                   |
| F05 | Non-Flag action clears last clearable blob                                       | Win without separate Flag-capture requirement.                                                             |
| T01 | Trail crosses empty cells and merge sites                                        | Spawn on departures including origin; skip current-action merge sites and resting cell.                    |
| G01 | Chain merges before Ghost                                                        | Haunt destination is original action start.                                                                |
| G02 | Trail into Ghost with leftovers                                                  | Intermediate leftovers remain; destination leftover consumed.                                              |
| G03 | Occupied Grave crossed during haunt or at destination                            | Ghost clears; occupant survives; no later landing.                                                         |
| G04 | Idle Ghost on Grave begins haunt                                                 | Idle state legal; Ghost clears immediately on becoming ethereal.                                           |
| G05 | Occupied non-Grave haunt destination                                             | Occupant consumed regardless of color/type.                                                                |
| V01 | Zero/two Flags, overlapping blobs, no clearables, or no Normal/Trail             | Authored level validation rejects each case.                                                               |
| V02 | One Flag, legal sources/positions, idle Ghost on Grave                           | Structural validation permits it; solvability remains separate.                                            |
| S01 | Reject, click same source, or click outside blobs                                | Selection cancels; next blob click starts a fresh attempt.                                                 |
| S02 | Valid move followed by rejection/unchanged contact, then Undo                    | Undo restores prior board-changing action.                                                                 |
| U01 | Two changes; Undo twice                                                          | Initial board restored; count remains two; Undo unavailable with empty history.                            |
| U02 | New change after Undo                                                            | Count increments; new history branch; no Redo.                                                             |
| U03 | Undo Trail/chain/Ghost/Grave action                                              | Entire action reverses in order with dedicated feedback and exact restoration.                             |
| P01 | Forward/Undo playback                                                            | Blob selection disabled; Undo stays interactable when history exists and queued requests play afterward; Restart enabled. |
| P02 | Rejection or unchanged contact feedback                                          | Selection/Undo remain available according to session state.                                                |
| P03 | Winning action committed                                                         | Undo/selection locked; victory shown only after final playback.                                            |
| P04 | Restart during forward, Undo, winning playback, or victory                       | Initial board, zero count, empty history; no stale effects or victory callbacks.                           |

## Semester scheduling

Keep the deliverable at exactly 15 weeks. Weeks 1–3 establish rules, resolution, validation, and baseline build; week 4 stabilizes special interactions. Use weeks 5–6 for reverse playback/Undo feedback alongside forward game-feel work. Weeks 7–10 remain level design; week 11 completes HUD/Undo integration and player flow. Weeks 12–13 playtest and revise; weeks 14–15 validate and deliver. Limit optional new mechanics to preserve time for the now-required Undo work.

## Completion evidence

Latest local README evidence reports 114/114 Edit Mode tests and 20/20 Play Mode tests passing on Unity 6000.3.6f1. Treat that as the current baseline until the remaining input/HUD/victory/cancellation work is completed and rerun.

Record actual test commands, results, build target, and remaining failures when new implementation is performed. No acceptance row is satisfied solely by adding its test or documenting its intended outcome.
