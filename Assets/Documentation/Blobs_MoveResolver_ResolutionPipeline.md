# Blobs Move Resolution Architecture

**MoveResolver + Resolution Pipeline + Effect Queue**  
_(Model-only. Animation/VFX are consumed later by presenters.)_

## Why this exists

- Add mechanics (trail, bomb, ghost, sigil, lasers, ice, sticky, portals) without bloating input or a monolithic `MergePlan`.
- Support **cascades** (multi-merges / after-effects) deterministically.
- Make **undo** visually incremental by replaying inverse effects in reverse order.

---

## High-level flow

1. **SelectionPresenter** emits a **MoveIntent** (no validation).
2. **MoveResolver.Resolve(intent, board)** produces a **MoveResult**:
   - `IsValid`, `InvalidReason`
   - ordered `List<IEffect> Effects`
   - ordered `List<IEffect> InverseEffects` (for undo)
3. **CommandManager** executes a `MoveCommand` that applies effects to the model.
4. Presenter later plays `Effects` (or `InverseEffects` for undo) with DOTween/shaders/VFX.

```csharp
// Input layer (dumb)
var intent = MoveIntent.Merge(sourceId, targetId);
var result = moveResolver.Resolve(intent, board);

if (!result.IsValid) { feedback.Show(result.InvalidReason); return; }

commandManager.Execute(new MoveCommand(result)); // model changes happen here
// Presenter/Animation consumes result.Effects separately (later).
```

---

## Core types

### MoveIntent (input-level)

Describes what the player attempted, nothing else. All moves currently require both source and target blob.

**Note:** `MoveIntentType` is kept as an enum with only `Merge` for future expansion (e.g. directional/swipe intents). The `Directional` member was removed; the intent is always merge (source + target) for now.

```csharp
public readonly record struct MoveIntent(...)
{
    public static MoveIntent Merge(string sourceId, string targetId)
        => new(MoveIntentType.Merge, sourceId, targetId, default);
}
```

### MoveContext (resolver-level)

Computed once; may be adjusted by rules (ice/portals/sticky).

```csharp
public sealed class MoveContext
{
    public BoardModel Board;
    public Blob Source;
    public Blob Target;                 // optional (may be discovered)
    public Vector2Int Start;
    public Vector2Int End;              // current intended landing cell
    public List<CellContext> Path;       // traversal order: start excluded, end included
    public string MoveTag = "Normal";    // e.g., "IceSlide"
    public bool Terminate;              // stop further resolution (e.g., ghost banished)
}

public readonly record struct CellContext(
    Vector2Int Pos,
    Tile Tile,
    Blob Blob
);
```

### Effects (atomic + ordered + invertible)

Effects are the _only_ resolution output. Granularity matters for “juice” + incremental undo.

Minimum effect set:

- `MoveBlobEffect(blobId, from, to, moveTag)`
- `RemoveBlobEffect(blobId, at, causeTag)`
- `SpawnBlobEffect(spawnData, at)` _(must include deterministic blob id!)_
- `ResizeBlobEffect(blobId, fromSize, toSize)`
- `SetTileStateEffect(tileId, key, from, to)` _(laser toggle)_
- `TriggerEffect(tag, at)` _(VFX hook; no model change)_

```csharp
public interface IEffect { }

public readonly record struct MoveBlobEffect(
    string BlobId, Vector2Int From, Vector2Int To, string MoveTag
) : IEffect;

public readonly record struct RemoveBlobEffect(
    string BlobId, Vector2Int At, string CauseTag
) : IEffect;

public readonly record struct SpawnBlobEffect(
    BlobSpawnData Data, Vector2Int At
) : IEffect;

public readonly record struct ResizeBlobEffect(
    string BlobId, BlobSize From, BlobSize To
) : IEffect;

public readonly record struct TriggerEffect(
    string Tag, Vector2Int At
) : IEffect;
```

### BoardTransaction (diff recorder)

Applies effects to the model and captures inverse effects precisely.

```csharp
public sealed class BoardTransaction
{
    private readonly BoardModel _board;
    private readonly List<IEffect> _inverse = new();

    public BoardTransaction(BoardModel board) => _board = board;

    public void Apply(IEffect effect)
    {
        switch (effect)
        {
            case MoveBlobEffect m:
                _inverse.Add(new MoveBlobEffect(m.BlobId, m.To, m.From, m.MoveTag));
                _board.MoveBlob(_board.GetBlob(m.BlobId), m.To);
                break;

            case ResizeBlobEffect r:
                _inverse.Add(new ResizeBlobEffect(r.BlobId, r.To, r.From));
                _board.GetBlob(r.BlobId).Size = r.To;
                break;

            case RemoveBlobEffect rem:
            {
                var blob = _board.GetBlob(rem.BlobId);
                var snapshot = BlobSpawnData.FromBlob(blob); // includes id/type/color/size
                _inverse.Add(new SpawnBlobEffect(snapshot, rem.At));
                _board.RemoveBlob(rem.BlobId);
                break;
            }

            case SpawnBlobEffect sp:
                _inverse.Add(new RemoveBlobEffect(sp.Data.Id, sp.At, "UndoSpawn"));
                _board.PlaceBlob(BlobFactory.FromSpawnData(sp.Data, sp.At));
                break;

            case TriggerEffect:
                // No model change; optionally no inverse needed.
                break;
        }
    }

    public IReadOnlyList<IEffect> InverseEffectsReversed()
    {
        _inverse.Reverse();
        return _inverse;
    }
}
```

> **Determinism note:** Anything spawned must get a deterministic id in the spawn data so it can be removed on undo.

---

## Resolver + Pipeline

### Effect Queue

Resolver uses a queue to build and apply effects in order. Applying effects immediately updates the simulation board so later phases see newly created blobs (fixes “bomb didn’t see trail spawn” type issues).

```csharp
public sealed class MoveResult
{
    public bool IsValid;
    public string InvalidReason;
    public List<IEffect> Effects = new();
    public List<IEffect> InverseEffects = new();
}
```

```csharp
public sealed class MoveResolver
{
    private readonly ResolutionPipeline _pipeline;

    public MoveResolver(ResolutionPipeline pipeline) => _pipeline = pipeline;

    public MoveResult Resolve(MoveIntent intent, BoardModel board)
    {
        var result = new MoveResult();
        var ctx = MoveContextBuilder.Build(intent, board, result);
        if (!result.IsValid && result.InvalidReason != null) return result;

        var txn = new BoardTransaction(board);
        _pipeline.Resolve(ctx, txn, result);

        result.InverseEffects = new List<IEffect>(txn.InverseEffectsReversed());
        return result;
    }
}
```

### Pipeline phases (recommended for Blobs)

1. **Intent**: determine target/path.
2. **Validation**: traversable tiles, same row/col, lasers block, base merge eligibility.
3. **Movement**: compute final landing, move style (ice/sticky/portals); enqueue movement effects.
4. **Interaction (cross/enter)**: path cell reactions (sigil banish, portal redirect, sticky stop).
5. **Merge**: apply merge rules at end cell.
6. **PostMerge**: bombs, ghost haunt/swap, trail completion, switch toggles.
7. **Cascade**: repeat select phases if new effects create new interactions (until stable).

```csharp
public sealed class ResolutionPipeline
{
    private readonly List<IRule> _rules;         // pre/post rules
    private readonly List<IReaction> _reactions; // event-driven hooks

    public void Resolve(MoveContext ctx, BoardTransaction txn, MoveResult result)
    {
        var queue = new Queue<IEffect>();

        // Phase: Validation + Movement + Merge (simplified example)
        foreach (var rule in _rules)
        {
            if (!rule.Apply(ctx, queue, out var reason))
            {
                result.IsValid = false;
                result.InvalidReason = reason;
                return;
            }
        }

        // Apply queued effects immediately (simulation step)
        while (queue.Count > 0)
        {
            var e = queue.Dequeue();
            result.Effects.Add(e);
            txn.Apply(e);

            // Dispatch reactions that may enqueue more effects
            foreach (var r in _reactions)
                r.OnEffectApplied(ctx, e, queue);
        }

        result.IsValid = true;
    }
}
```

---

## Mechanics mapping (examples)

### Trail Blob

During movement, for each crossed cell:

- enqueue a `SpawnBlobEffect` per cell (granular).
- apply immediately so later effects (bomb) see them.

```csharp
// reaction example: on movement step, spawn trail at "from" cell (or crossed cell)
public sealed class TrailReaction : IReaction
{
    public void OnEffectApplied(MoveContext ctx, IEffect e, Queue<IEffect> q)
    {
        if (e is not MoveBlobEffect move) return;
        if (ctx.Source is not TrailBlob trail) return;

        var trailData = BlobSpawnData.Create(
            id: Ids.Next(), type: BlobType.Normal, color: trail.TrailColor, size: trail.Size
        );

        q.Enqueue(new SpawnBlobEffect(trailData, move.From));
    }
}
```

### Bomb Blob (post-merge)

After merge is resolved, enqueue removals around end cell + trigger marker.

```csharp
public sealed class BombReaction : IReaction
{
    private static readonly Vector2Int[] Neighbors =
    {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
        new(1,1), new(-1,1), new(1,-1), new(-1,-1)
    };

    public void OnEffectApplied(MoveContext ctx, IEffect e, Queue<IEffect> q)
    {
        if (e is not TriggerEffect t || t.Tag != "AfterMerge") return;
        if (ctx.Target is not BombBlob && ctx.Source is not BombBlob) return;

        q.Enqueue(new TriggerEffect("BombExplode", ctx.End));

        foreach (var dir in Neighbors)
        {
            var pos = ctx.End + dir;
            var blob = ctx.Board.GetBlobAt(pos);
            if (blob != null)
                q.Enqueue(new RemoveBlobEffect(blob.ID, pos, "Bomb"));
        }
    }
}
```

> In practice, you’ll emit `"AfterMerge"` once the merge phase completes, letting post-merge reactions run cleanly.

### Ghost + Sigil

Ghost performs a post-merge move (swap/haunt). Sigil can terminate it.

- Ghost: enqueue a `MoveBlobEffect(ghost, end→start, tag:"Haunt")`
- Sigil: on ghost entering sigil cell, enqueue `RemoveBlobEffect(ghost)` and mark `ctx.Terminate=true` so further ghost movement effects are not enqueued.

---

## What changes in your codebase

### MergePlan becomes optional

- Replace with `MoveResult.Effects`.
- If you still need “plan grouping” for animation timing later, add `EffectGroupId` or “phase tags” to effects.

### Undo becomes trivial & incremental

- Undo replays `InverseEffects` (already granular) so trail blobs remove one-by-one and movement reverses stepwise.

---

## Notes

- **Animation is out of scope.** Effects carry tags (`MoveTag`, `CauseTag`, `TriggerEffect.Tag`) that presenters can map to DOTween curves + shader/VFX later.
- **Order matters.** Always use fixed direction arrays and consistent traversal order.
- **Apply effects during resolution.** This is how “bomb sees trail spawns” works reliably.
