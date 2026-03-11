# Blobs Implementation Phases — MoveResolver + Resolution Pipeline

This is an incremental, shippable phase plan (foundation → complexity). Each phase keeps the game playable while you migrate off the old merge-plan traversal.

---

## Design notes

- **MoveIntentType:** The enum is kept for future expansion, but only `Merge` is used. All move intents currently require a source blob and a target blob; the `Directional` member was removed as redundant. See `MoveIntent.cs`.

---

## Phase 0 — Guardrails + Migration Strategy

**Goal:** Introduce the new system without breaking the current game loop.

**Tasks**

- Add a feature flag: `UseMoveResolver` (ScriptableObject, const, or ProjectSettings).
- Pick a migration mode:
  - **A (recommended):** Resolver drives gameplay only when flag is ON.
  - **B:** Resolver runs “dry-run” (logs predicted effects) while old logic runs.

**Deliverables**

- `GameConfig.UseMoveResolver` bool
- Single switch point where merges currently start

**Done when**

- You can run the game with either system by toggling one flag.

---

## Phase 1 — Effects (Atomic, Ordered, Invertible)

**Goal:** Define a minimal effect language the whole game can grow around.

**Tasks**

- Create `IEffect` + core effects:
  - `MoveBlobEffect(blobId, from, to, tag)`
  - `RemoveBlobEffect(blobId, at, tag)`
  - `SpawnBlobEffect(spawnDataWithDeterministicId, at, tag)`
  - `ResizeBlobEffect(blobId, fromSize, toSize, tag)`
  - `SetTileStateEffect(tileId, key, from, to, tag)` (or typed variants)
  - `TriggerEffect(tag, at)` (optional, no model change)
- Add `EffectTags` constants: `MoveStep`, `AfterMerge`, `BombExplode`, etc.

**Deliverables**

- `/Core/Effects/*`

**Done when**

- A basic merge can be represented as a list of effects without touching animation.

---

## Phase 2 — BoardTransaction (Apply + Inverse Recording)

**Goal:** Centralize model mutations and generate perfect granular undo.

**Tasks**

- Implement `BoardTransaction.Apply(effect)`:
  - Mutate `BoardModel` (move/remove/spawn/resize/set-tile-state)
  - Record exact inverse effects
- Expose:
  - `IReadOnlyList<IEffect> EffectsApplied`
  - `IReadOnlyList<IEffect> GetInverseEffectsReversed()`

**Deliverables**

- `/Core/Resolution/BoardTransaction.cs`

**Done when**

- Apply effects → board changes; apply inverse effects → board restores exactly.

---

## Phase 3 — Resolver Skeleton (Intent → Result)

**Goal:** Create the API your input/game loop calls.

**Tasks**

- Add:
  - `MoveIntent { string sourceId; string targetId; }`
  - `MoveResult { bool IsValid; string InvalidReason; List<IEffect> Effects; List<IEffect> InverseEffects; }`
- Stub `MoveResolver.Resolve(intent, board)`:
  - Build context (Phase 4)
  - Run pipeline (Phase 5)
  - Apply effects via transaction and capture inverse (Phase 2)

**Deliverables**

- `/Core/Resolution/MoveIntent.cs`
- `/Core/Resolution/MoveResult.cs`
- `/Core/Resolution/MoveResolver.cs`

**Done when**

- Resolver returns valid/invalid and can output effects (even if minimal).

---

## Phase 4 — MoveContextBuilder (Compute Path Once)

**Goal:** Collect all context once (source/target/path/tiles/blobs).

**Tasks**

- `MoveContext` fields:
  - `Board`, `Source`, `Target`, `Start`, `End`, `Direction`
  - `List<CellContext> Path` ordered positions from start+dir onward
  - `CellContext { Vector2Int pos; Tile tile; Blob blob; }`
- `MoveContextBuilder.Build(intent, board)`:
  - Validate IDs exist
  - Normalize direction to 4-way (`Clamp` or custom normalize)
  - Build ordered path cells

**Deliverables**

- `/Core/Resolution/MoveContext.cs`
- `/Core/Resolution/MoveContextBuilder.cs`

**Done when**

- The path matches your current traversal behavior.

---

## Phase 5 — Pipeline v1 (Validation → Move → Merge)

**Goal:** Replace `CalculateMergePlan` with a clean pipeline that outputs effects.

**Tasks**

- Implement `ResolutionPipeline.Run(ctx) → EffectQueue`.
- Start with only “normal merge” behavior.
- Minimal rules:
  1. `ValidateIntentRule` (different ids, same row/col)
  2. `ValidateTraversalRule` (tiles traversable)
  3. `ValidateLaserRule` (use your `IsLaserBlocking`)
  4. `BuildMoveStepsRule` (enqueue `MoveBlobEffect` per cell)
  5. `BuildBasicMergeRule` (remove target, apply size rules, etc.)
  6. Enqueue `TriggerEffect(AfterMerge)`

**Deliverables**

- `/Core/Resolution/Pipeline/ResolutionPipeline.cs`
- `/Core/Resolution/Rules/*.cs`

**Done when**

- With `UseMoveResolver=true`, normal merges work and model ends correct.

---

## Phase 6 — Commands Integration (Undo via InverseEffects)

**Goal:** Keep your command pattern; move logic becomes effects-driven.

**Tasks**

- `MoveCommand` stores `Effects` + `InverseEffects` (from `MoveResult`) and `BoardModel`.
- Execute: no-op for model (effects are already applied during `MoveResolver.Resolve`).
- Undo: apply `InverseEffects` to the board via `BoardTransaction`.

**Deliverables**

- `/Commands/MoveCommand.cs`
- Update invoker/command manager to accept `MoveResult` when using the new resolver (e.g. `MergeInvoker` or feature-flag path).

**Done when**

- Undo reliably restores the exact prior board state.

**Implementation note:** The resolver applies effects during resolution so that the board is updated in one place. `MoveCommand` therefore only needs to apply `InverseEffects` on Undo; Execute is a no-op for the model.

**Implemented:** `MoveCommand(MoveResult result, BoardModel board)` stores the result; `Undo()` applies `InverseEffects` via `BoardTransaction`. Integration with `MergeInvoker`/game loop is done when the feature flag (Phase 0) is added.

---

## Phase 7 — Reactions (Trail as First “Non-Trivial” Mechanic)

**Goal:** Add scalable side-effects without bloating core rules.

**Tasks**

- Introduce `IReaction.OnEffectApplied(ctx, effect, queue)`.
- Add `TrailReaction`:
  - When `MoveBlobEffect` applied, enqueue `SpawnBlobEffect` at previous cell(s) per your trail logic.
- Processing loop:
  - Dequeue effect → `txn.Apply(effect)` → reactions enqueue more effects → continue

**Deliverables**

- `/Core/Reactions/IReaction.cs`
- `/Core/Reactions/TrailReaction.cs`

**Done when**

- Trail spawn/removal is granular and undo removes them in reverse order automatically.

**Implemented:** `IReaction` already had `OnEffectApplied(ctx, e, queue)`. `TrailReaction` enqueues one `SpawnBlobEffect` (Normal blob with trail color/size) at `move.From` for each `MoveBlobEffect` when `ctx.Source` is a `TrailBlob`; ids from `ResolutionIds.Next()`. Pipeline `CreateDefault()` now includes `TrailReaction`. `ResolutionIds.Reset()` is called at the start of each `MoveResolver.Resolve()` so spawn ids are deterministic per move and undo order matches.

---

## Phase 8 — Bomb + Ghost + Sigil (Post-Merge Mechanics)

**Goal:** Replace “deferred plan ordering” with explicit triggers + reactions.

**Tasks**

- Bomb:
  - Reaction listens for `TriggerEffect(AfterMerge)`
  - Enqueue `TriggerEffect(BombExplode)` then remove neighbors, source/target as needed
- Ghost:
  - Reaction enqueues a “haunt move” sequence as stepwise `MoveBlobEffect` end→start
- Sigil:
  - Reaction checks if ghost enters a sigil cell; enqueue `RemoveBlobEffect(ghost)` and stop remaining ghost steps.

**Deliverables**

- `BombReaction.cs`, `GhostReaction.cs`, `SigilReaction.cs`

**Done when**

- “Ghost crosses sigil → banished” works without mutating flat plans.

---

## Phase 9 — Lasers + Switch (Stateful Tiles)

**Goal:** Integrate stateful obstacles with the same effect language.

**Tasks**

- Add `SetTileStateEffect` (or `ToggleLaserEffect`) for laser activation and color rules.
- Ensure laser blocking reads model state, not view state.
- Switch merge enqueues tile state changes.
- Add validation rule to block movement when between linked lasers.

**Deliverables**

- `/Core/Effects/SetTileStateEffect.cs`
- Laser/switch validation + reaction(s)

**Done when**

- Switch toggles lasers deterministically; undo restores prior laser state.

---

## Phase 10 — Cascades / Stabilization Loop (Complex Chains)

**Goal:** Support candy-crush-like chained outcomes without spaghetti.

**Tasks**

- Add a stabilization loop:
  - Process queued effects until empty
  - Optional re-scan rules (only if needed)
  - Hard-cap iterations to prevent infinite loops
- Define cascade triggers:
  - Prefer explicit triggers (`AfterMerge`, `BombExplode`) + reactions producing new effects.

**Deliverables**

- Pipeline processing loop with safety cap + debug logging

**Done when**

- Bomb → trail → ghost → sigil chains work deterministically.

---

## Phase 11 — Retire Old Plan/Traversal System (Integration)

**Goal:** One authoritative move path.

**Tasks**

- Remove/disable old `MergePlan` usages.
- Add new architecture instrumentation

**Deliverables**

- Game uses `MoveResolver.Resolve()` everywhere

**Done when**

- All moves + undo are effects-driven.

**Implemented:** SelectionPresenter now uses MoveResolver + MoveCommand exclusively: resolves via `MoveResolver.Resolve(MoveIntent.Merge(sourceId, targetId), boardModel)`, creates `MoveCommand(result, boardModel)`, and calls `MergeInvoker.Execute(command)`. MergeInvoker uses a single `Stack<ICommand>` and `Execute(ICommand)` / `UndoMerge()`; MergeAction implements ICommand so legacy code paths still compile. BoardPresenter exposes `BoardModel` for the resolver; OnMergeExecuted/OnMergeUndone accept ICommand and branch on MoveCommand (SyncViewFromModel) vs MergeAction (existing plan animator). FeedbackPresenter has `ShowInvalid(string reason, string blobId)` for resolver invalid reasons. GameManager and TutorialPresenter handlers updated to ICommand. Old MergeService/MergePlan are no longer used for the main merge flow.

---

# Acceptance Tests (Add as You Go)

Add an edit-mode “resolution harness” that executes resolver and asserts board outcomes:

- Normal merge: removes target; moves source.
- Trail: spawns correct cells; undo removes one-by-one.
- Bomb: removes correct neighbors including newly spawned trail blobs.
- Ghost: haunts/swap; sigil banishes mid-move.
- Laser: blocks matching colors; switch toggles; undo restores.

---

# Scalability Notes

- Rules = validation + primary resolution.
- Reactions = side effects and post-merge behaviors.
- Keep determinism:
  - deterministic spawn IDs
  - stable neighbor order
  - stable queue ordering

This plan is designed so each phase is small, testable, and doesn’t require animation changes to validate correctness
