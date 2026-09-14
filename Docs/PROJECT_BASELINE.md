# Blobs: prototype and production baseline

> Historical inspection: this records the pre-discussion implementation. The September 9 [agreed rules](GAME_DESIGN.md) supersede its design recommendations, including restart-only scope, Flag eligibility, and movement policy. Undo is now required. Findings are not a current test report; subsequent workspace edits may have addressed some issues. Track remaining work in the [implementation checklist](RULES_IMPLEMENTATION_PLAN.md).


Reviewed September 8, 2026, against commit `d2319d3` and the current working tree.

## Purpose and limits

This is the starting-state inventory for the 15-week independent study. It compares the older implementation with the production rebuild, identifies reusable work, and separates unfinished features from concrete issues.

Current roots:

- Production: `Assets/_Game/Production/`
- Unity: 6000.3.6f1; URP 17.0.3; Input System 1.18.0; Unity Test Framework 1.4.5; DOTween and UniTask used for presentation.

Existing uncommitted edits to production `Level_Ghost.asset`, `ViewCatalog.asset`, and `Game.unity` were preserved.

## Overall assessment

The production rebuild already contains a good architectural foundation that was described in the original proposal. Rules and session management have moved out of scene-driven prototype code into separate assemblies, with ordered effect playback and focused presentation collaborators. Normal movement, chain merges, Trail spawning, Flag capture, Ghost/Grave interactions, restart, level authoring, and feedback all have substantial implementations.

The largest remaining work is to fix rule and authoring gaps, establish a reliable test/build baseline, complete the player-facing level flow, and turn existing content and feedback into a polished sequence. A second wholesale architecture rewrite is not justified by this inspection.

The prototype is also more structured than the original proposal suggests: it already has commands, rules, reactions, effects, and presenters. Its key limitation is that resolution and transactions operate through Unity-facing board presenters, rather than an independent board simulation.

## Systems comparison

Paths below are relative to the repository. In this table, `P/` means `Assets/_Game/Production/Scripts/` and `L/` means `Assets/_Game/Prototype/Scripts/`.

| System                       | Prototype evidence                                                                                                                       | Production state                                                                                                                                               | Remaining semester work                                                                                                                            |
| ---------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------- |
| Architecture                 | `L/Core/Resolution/MoveResolver.cs` resolves through `BoardPresenter`; transactions and game flow depend on scene objects.               | Implemented separation: Core, Application, Content, Input, Presentation, UI, and Debugging assemblies. Core/Application declare no engine references.          | Targeted boundary review and documentation; keep rule changes independent of animation.                                                            |
| Movement and chain merging   | `L/Core/Rules/BuildBasicMergeRule.cs` walks a path and emits merge/resize effects.                                                       | `P/Core/Resolution/MoveResolver.cs` simulates per-cell effects on a clone before replaying them onto the real board.                                           | Fix Trail target and intermediate Rock cases; extend regression coverage.                                                                          |
| Selection, moves, restart    | `L/Input/SelectionPresenter.cs`, `L/Core/GameStateManager.cs`, and `L/Merge/MergeInvoker.cs` coordinate selection and command history.   | `P/Application/GameSession.cs` owns selection, successful move count, restart, completion, and snapshots.                                                      | Validate rapid input, restart during playback, and complete-session interaction.                                                                   |
| Undo                         | Prototype command/inverse-effect history and `UndoMerge` exist.                                                                          | No player-facing undo command. Restart restores authored state.                                                                                                | Intentional omission under current design; do not automatically migrate undo.                                                                      |
| Flag/Target goal             | `L/Models/Blobs/TargetBlob.cs` and `L/Core/Rules/ValidateTargetRule.cs` implement matching-color and remaining-clearable checks.         | `FlagMergeStrategy` consumes the mover; Flag remains. Objective checks clearable blobs.                                                                        | Clarify documentation and test additional Rocks/Flags remaining on the board.                                                                      |
| Trail                        | `L/Core/Reactions/TrailReaction.cs` emits trail tile state and spawns after merge triggers.                                              | `P/Core/Strategy/MoveBehavior.cs` spawns on departures, suppressing spawns at earlier merge sites.                                                             | Correct direct Trail targeting; tune spawn readability and combined puzzles.                                                                       |
| Rock                         | `L/Behaviors/BlobBehaviors.cs` terminates a path before a Rock.                                                                          | `RockMoveStrategy` stops before a selected Rock; Rock cannot be a source.                                                                                      | Intermediate Rocks are not safely handled; adjacent Rock attempts can count as zero-distance successes.                                            |
| Ghost and Grave              | Models/views exist, but Ghost/Grave reaction files are empty; default pipeline registers only Trail.                                     | `GhostMergeStrategy`, `GhostReturnEffect`, presentation handlers, prefabs, and dedicated tests implement return and Grave clearing.                            | Runtime verification, feedback refinement, and teaching levels; not a mechanic to build from scratch.                                              |
| Bomb                         | Model, view, and visual classes exist; `BombReaction.cs` is empty and absent from default reactions.                                     | No Bomb type, strategy, or content mapping.                                                                                                                    | Optional new mechanic requiring full implementation, not merely refactoring an existing explosion system.                                          |
| Laser/Switch and other tiles | Laser validation and beam presentation exist. `SwitchReaction.cs` is empty.                                                              | Core special tile enum supports Grave only.                                                                                                                    | Scope decision before migration; existing prototype assets do not establish production support.                                                    |
| Size rules                   | Prototype basic merge code checks size and emits resize behavior.                                                                        | No equivalent size-dependent merge rule.                                                                                                                       | Confirm intentional simplification versus desired mechanic; do not assume old size puzzles transfer unchanged.                                     |
| Level authoring              | `L/Editor/LevelDataEditor.cs` and `LevelDataJsonSerializer.cs` provide richer authoring/import/export code.                              | ScriptableObject levels, mapper, validator, and a custom Inspector exist.                                                                                      | Fix invalid editor defaults; improve iteration only where needed. Production JSON files are not proof of a connected import pipeline.              |
| Content                      | Prototype level loading uses `LevelData` and Resources.                                                                                  | Five ScriptableObject level files and ten JSON files are present under production `Content/Levels`.                                                            | Validate each candidate, record solutions, curate progression, and remove duplicates. Counts are files, not certified unique/solvable levels.      |
| Animation and game feel      | Prototype animators, recipes, particles, and visual effects offer reference material.                                                    | Focused presenters, a presentation timeline, merge orchestration, configurable motion, selection, impact, and Ghost return exist.                              | Compare timings, refine readability, and test combined effects; reuse the framework.                                                               |
| Audio/VFX/haptics            | Prototype audio management and visual feedback exist.                                                                                    | Audio/contact/VFX feedback components exist. Haptics is a UnityEvent hook.                                                                                     | Verify actual clips/prefab wiring and polish; a haptic hook is not a verified device implementation.                                               |
| HUD and player flow          | Menu, tutorial, pause, win states, and level progression code exist.                                                                     | `P/UI/GameplayHudPresenter.cs` displays moves/status/completion and forwards restart. Bootstrap starts a serialized level.                                     | Finish production level selection, next-level flow, tutorial/pause/settings as scoped. A copied Menu scene is not proof of production integration. |
| Saving and stars             | `L/Core/LevelProgressManager.cs` implements PlayerPrefs stars/unlocking for nine levels. `GameManager.ProcessWin` still has a save TODO; | No production progress persistence.                                                                                                                            | Decide whether saving/stars are required; integrate and verify end to end if retained.                                                             |
| Tests and recovery           | Prototype has board-integrity validation and inverse effects.                                                                            | EditMode and PlayMode suites cover content, Flags, Ghosts, HUD, feedback, composition, and presentation recovery. `BoardPresenter` can rebuild from snapshots. | Repair stale paths, run suites, add rule regressions, and establish actual passing evidence.                                                       |

## Current production rules, reconciled with the proposal

These describe the inspected implementation. Disagreements with prose and unresolved behavior should be settled before authoring the final puzzle sequence.

1. **Selection and alignment:** select a source, then a target. Normal and Trail can be sources; Flag, Rock, and Ghost cannot. Moves require a shared row or column. See `Core/BlobRuleBook.cs` and `Core/Resolution/MoveResolver.cs`.
2. **Normal merging:** differing colors merge; matching colors fail when both blobs have color components. The target occupant is removed and the source survives. Intermediate occupants are resolved along the path. Failure results produced during simulation leave the real board unchanged; the Rock exception below is an unhandled failure path, not a successful validation result.
3. **Trail spawning:** the mover leaves Normal blobs of its trail color on departed cells, except cells where a merge already occurred during that move. Directly selecting a Trail as target currently fails because the movement registration is missing, although collision strategies exist.
4. **Flags:** capture requires matching colors when both carry colors and no more than one clearable blob at the collision moment. The source is consumed and the Flag remains. Rocks and other Flags do not count as clearable. Consequently, “exactly two blobs left” in older documentation is stricter than the code. The prototype's target validation also counts clearable blobs rather than all blobs.
5. **Completion:** `ObjectiveEvaluator` completes when there are no clearable blobs: Normal, Trail, and Ghost count; Rock and Flag do not. “Clear the board” means clear these pieces, not remove every object. There is no separate mandatory Flag-capture objective in the current evaluator; a Ghost/Grave route may complete without capturing a Flag if no clearable blobs remain.
6. **Rocks:** selecting a Rock computes a destination one cell before it. Intermediate Rocks are a defect, not a supported alternative rule. Decide whether an adjacent Rock attempt should be rejected or just provide contact feedback without a move count.
7. **Ghost/Grave:** merging into a Ghost consumes the mover. The Ghost returns toward the move's starting position. Intermediate occupants do not block the ethereal return. Without a Grave, an occupant at the final landing is removed. The first crossed Grave clears the Ghost; an occupant on that Grave is preserved. See `Core/Strategy/MergeStrategy.cs`, `Core/Effects/GhostReturnEffect.cs`, and `Tests/EditMode/GhostMechanicTests.cs`.
8. **Recovery:** restart is the current player-facing recovery action. Undo, Bombs, lasers, switches, and size-based merging are not required merely because prototype types exist.

## Known issues and gaps

## Current standing as of September 13, 2026

This baseline is now historical. Since the September 8 inspection, the production branch has moved quickly through the early rule and stability work. Treat the list below as the original gap register, then use this status summary for current planning:

| ID  | Current status | Evidence and remaining work |
| --- | -------------- | --------------------------- |
| B01 | **Partially open** | `ProjectSettings/EditorBuildSettings.asset` still lists prototype scenes, so the production player build path is not yet proven. The prototype folder no longer exists at the searched path, which makes this more important to clean up before a build. |
| B02 | **Done, pending fresh full-suite validation** | `BlobRuleBook` now registers Normal/Trail movement and collision combinations, with coverage in `MoveResolverPathTests`. |
| B03 | **Done, pending fresh full-suite validation** | Intermediate and adjacent Rock behavior is implemented and covered by resolver/session tests, including unchanged contact not incrementing move count. |
| B04 | **Done, pending fresh full-suite validation** | The level editor now creates Grave tiles and initializes special blob `type` values that match the mapper. |
| B05 | **Done, pending fresh full-suite validation** | Production tests now reference `Assets/_Game/Production/...` asset paths. README records a recent local pass of 114 Edit Mode and 20 Play Mode tests. |
| B06 | **Done, pending fresh full-suite validation** | `MoveResolver` checks board bounds, `EmptyPositions`, and non-traversable tiles during reached-path simulation. |
| B07 | **Open** | This is now the main product gap: production still needs a complete menu/level sequence, completion flow, scoped persistence decision, and end-to-end player journey validation. |
| B08 | **Mostly done, ongoing** | Core rule docs, architecture notes, README, and implementation checklist have been reconciled. Continue updating docs alongside code and validation evidence. |

For week planning, the early baseline tasks should no longer occupy weeks 1-3. They are now a Week 1 closeout and verification lane. The semester should pivot toward content, player flow, polish, and evidence.


### B01 — Build configuration and prototype Editor dependency

**High · Confirmed source/configuration issue · Small–medium.** `ProjectSettings/EditorBuildSettings.asset` lists old `Assets/Scenes/Menu.unity` and `Blobs.unity` paths. Their GUIDs now resolve to `Assets/_Game/Prototype/Scenes/`; the production Game scene is absent. Unity may reconcile moved paths by GUID, but that still selects the prototype flow. Separately, prototype `Scripts/Level/LevelLoader.cs` imports `UnityEditor` and calls `AssetDatabase.GetAssetPath` outside `#if UNITY_EDITOR`, in a runtime script with no enclosing editor-only assembly found.

**Impact:** the configured flow does not establish a production deliverable, and the unguarded Editor API is a player-compilation blocker if this script remains in the runtime assembly. **Next:** correct the runtime/editor boundary and select the intended production entry flow; make a development player build and verify launch through completion. Do not infer an observed build failure: no build was run.

### B02 — Direct moves into Trail blobs are unsupported

**High · Confirmed code defect · Small.** `Production/Scripts/Core/BlobRuleBook.cs`, `CreateDefault`, registers Normal→Trail and Trail→Trail merge strategies but neither movement strategy. `MoveResolver.Resolve` requires a movement strategy for the selected source/target pair before traversing.

**Trigger/impact:** select a Normal or Trail source and an aligned, differently colored Trail target; resolution returns `UnsupportedInteraction`. This contradicts the intended shared normal-merge behavior and restricts Trail puzzles. **Next:** implement the intended registrations and verify both combinations, same-color rejection, and unchanged state on failure. Intermediate Trail collisions are a separate path and should also be covered.

### B03 — An intermediate Rock leads to an occupied-cell exception

**High · Confirmed code path · Small–medium.** `RockMergeStrategy.BuildPlan` returns a successful empty collision plan. `MoveResolver.Resolve` then appends a move into the Rock's occupied cell. `BoardState.MoveBlob` throws instead of returning a move failure.

**Trigger/impact:** source at (0,0), Rock at (1,0), otherwise valid selected Normal target at (2,0). Simulation reaches the Rock and attempts to occupy it. The exception occurs before normal commit, so this is not evidence of real-board corruption, but it bypasses normal player feedback. **Next:** define blocking/stopping behavior for intermediate Rocks and test both direct Rock targets and Rocks between source and target.

### B04 — Level editor creates data the mapper cannot load

**Medium · Confirmed code defect · Small.** `Production/Scripts/Editor/LevelDefinitionAssetEditor.cs` offers “Add Normal Tile,” producing `NormalTileAssetData`; `Content/LevelAssetMapper.cs` accepts only `GraveTileAssetData`. In addition, “Add Flag/Trail/Rock/Ghost Blob” constructs subclasses without initializing their `type` field; its default is Normal, while the mapper checks type/subclass agreement.

**Impact:** a level author can create entries that throw on load, even after filling in IDs/positions; special blobs require manually correcting the enum. **Next:** align editor creation with the actual content model. Verify that every Add button creates a mappable entry after required fields are supplied, and that malformed imported data is rejected clearly.

### B05 — Asset-based tests refer to old locations

**Medium · Confirmed stale references · Small–medium.** Examples include `Production/Tests/EditMode/MilestoneThreeContentTests.cs`, `GhostPrefabTests.cs`, `PresenterSeparationTests.cs`, and `GameplayFeedbackPresenterTests.cs`. They contain `Assets/_Game/Content/...` and `Assets/_Game/Prefabs/...` paths that no longer exist. Some catalog names also require reconciliation with the current assets, not merely inserting `Production/`.

**Impact:** those AssetDatabase loads cannot locate the intended assets by their current literal paths; presence of the test suite does not establish a passing baseline. **Next:** resolve actual assets, update references, then run EditMode and PlayMode suites. Check other stale API expectations revealed by compilation rather than assuming path repair is sufficient.

### B06 — Authored board holes are not movement blockers

**Medium · Confirmed implementation mismatch; intended traversal policy needs confirmation · Medium.** `BoardState.EmptyPositions` records excluded cells, and board-surface composition uses them. `MoveResolver` treats an unoccupied intermediate position as traversable without consulting that set; `BoardState.MoveBlob` checks rectangular bounds and occupancy only. The prototype's `ValidateTraversalRule` explicitly rejects missing/nontraversable tiles.

**Impact:** normal movement can cross a visually absent cell; Trail behavior can spawn into it. If holes intentionally allow crossing, that rule needs explicit visual communication. **Next:** agree on hole semantics, implement the policy if required, and verify Normal/Trail paths and Ghost return separately on a board with a missing middle cell.

### B07 — Production campaign and persistence remain incomplete

**Medium · Confirmed implementation gap · Medium–large.** Production `GameBootstrapper.StartLevel` can initialize a supplied level, but there is no production campaign/progress service found. HUD completion is a display state, not next-level navigation. The production Menu scene still contains `gameplaySceneName: Blobs`. Prototype stars APIs have no discovered first-party `SetStars` caller, and its win handler retains a save TODO.

**Impact:** single-level gameplay is substantially ahead of the complete player journey. **Next:** implement an explicitly scoped menu → level → completion → next-level flow; add saving only if required. Verify a fresh launch, full sequence, final level, restart, and save/reload if included. Do not count prototype saving as a finished reusable feature.

### B08 — Documentation overstates and understates current behavior

**Low · Confirmed documentation drift · Small.** README/context/production-plan paths predate the folder split. `Docs/GAME_DESIGN.md` still calls Ghost a planned mechanic; Flag prose says exactly two blobs while code counts clearable blobs. Old milestone checklists are historical implementation records, not current test/build evidence.

**Next:** use this baseline for semester planning and reconcile the broader design/architecture docs after rule decisions. Validate links and compare each claimed milestone with current test results.

## Validation still needed

- Compile and run current EditMode/PlayMode tests after repairing stale dependencies and paths. Existing Ghost tests explicitly cover return, first-Grave clearing, intermediate occupancy, and occupied Graves; their presence does not mean they passed today.
- Exercise Normal→Trail, Trail→Trail, intermediate Rock, adjacent Rock, and hole traversal cases. An adjacent selected Rock currently produces an empty successful timeline, so `GameSession` increments the move count; decide whether this is intended.
- Load each authored level through the production mapper, verify a solution, and record mechanics taught. Structural validation does not prove solvability or difficulty.
- Verify actual prefab/catalog/scene wiring, null references, animation interruption, UI input overlap, sound, and visual clarity in Unity. Those were not exhaustively audited here.
- Build and test on the chosen platform. Touch behavior, haptics, performance, audio, safe areas, and persistence are unverified.
- Gather playtest evidence before calling the level progression or game feel complete. No current playtest metrics were supplied.

## Implications for the 15-week plan

| Week | Work justified by current state | Reviewable outcome |
| ---- | ------------------------------- | ------------------ |
| 1 | Rule alignment and baseline gap closure: finish B02-B06, implement required Undo foundation, document decisions, and identify remaining build/player-flow risks. | Agreed rules, updated baseline status, targeted rule tests, Undo foundation, and a current issue list. |
| 2 | Production build and player-loop stabilization: fix B01 build settings, verify production scene launch, finish queued Undo/input/victory/restart behavior, and rerun EditMode/PlayMode tests. | Launchable production build or documented blocker, current test results, and stable single-level runtime flow. |
| 3 | Level set audit: load every production level asset, remove duplicates/test-only content from the playable list, record intended solution paths, and classify each level by taught mechanic. | Level inventory with pass/fail validation, solution notes, and a proposed teaching order. |
| 4 | Core level sequence v1: create or revise the first playable run of tutorial and early challenge levels using Normal, Rock, Trail, Flag, Ghost, and Grave. | Playable level sequence v1 with each level’s goal, mechanic lesson, and expected move route. |
| 5 | Campaign shell and level navigation: menu to level, completion to next/retry/menu, restart behavior, and final-level handling. Decide whether persistence is required for the course deliverable. | Complete no-Editor player loop through the current sequence. |
| 6 | Game feel pass 1: tune selection, merge, Rock contact, Trail spawning, Ghost haunt/rest, Undo feedback, and camera framing for readability. | Before/after gameplay captures and revised timing/settings. |
| 7 | Content expansion: build additional levels around one mechanic at a time, then simple combinations. | Expanded curated level set with solution notes and difficulty labels. |
| 8 | Content expansion with combination puzzles: Ghost/Grave plus Trail/Rock/Flag interactions, avoiding optional mechanics that would displace polish. | Mid-semester playable sequence with all retained mechanics represented. |
| 9 | Internal playtest and difficulty revision: play through the full sequence, log confusion points, failed reads, move-count outliers, and unclear feedback. | Playtest notes and prioritized changes. |
| 10 | Production flow polish: HUD, pause/settings if retained, completion presentation, menu polish, audio mix, and accessibility/readability fixes. | Full flow feels cohesive and understandable. |
| 11 | Persistence or deliberate omission: implement simple level unlock/progress if required, or document why the deliverable stays session-based. | Verified save/reload behavior or documented scope decision. |
| 12 | External playtest 1: observe players, collect completion times, retries, misunderstood rules, and level ratings. | Playtest report and revised backlog. |
| 13 | External playtest 2 and content lock: revise levels and feedback, then freeze mechanics and level count. | Content-lock candidate and remaining bug list. |
| 14 | Release validation: full EditMode/PlayMode run, production player build, smoke test from fresh launch through completion, and regression fixes. | Release candidate with validation evidence. |
| 15 | Final documentation and presentation: package final build, update docs, prepare website/process evidence, and write the semester reflection. | Final deliverables and evidence of progress from the baseline. |

Foundations already implemented should be credited as the starting point. AI-assisted development means the early rule-fix lane can finish quickly, but the semester still needs time for puzzle design, playtesting, polish, and validation. Optional Bombs, lasers, switches, size mechanics, stars, Mini/Fat blobs, and other new mechanics should not silently become required migration work.
