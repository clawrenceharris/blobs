# Milestone 1 Usage

Milestone 1 can be exercised without a Unity scene by creating an Application `GameSession` from production level data.

```csharp
using Blobs.Application;
using Blobs.Core;

var level = new LevelDefinition(
    "milestone-1-sample",
    1,
    3,
    1,
    new BlobDefinition[]
    {
        new NormalBlobDefinition("red_a", new GridPosition(0, 0), BlobColor.Red, BlobSize.Normal),
        new NormalBlobDefinition("red_b", new GridPosition(2, 0), BlobColor.Red, BlobSize.Normal)
    },
    new TileDefinition[0]);

var session = new GameSession(level);
var result = session.ExecuteMove(new MoveIntent("red_a", "red_b"));

if (result.Succeeded && session.IsComplete)
{
    // The first production puzzle is solved through Core and Application.
}
```

## Current Behavior

- The sample level has two matching normal blobs on one row.
- A valid merge removes both clearable blobs and completes the level.
- Invalid moves return a `MoveFailureReason` and do not record history.
- Superseded direction: Milestone 1 originally proved inverse effects for undo, but current production design favors restart over player-facing undo.
- Restart rebuilds the authored initial state and clears command history.

## Known Limitations

- Only normal blobs are supported.
- Blob size currently has one production value, `Normal`.
- Tiles, hazards, cascades, multi-merge, Trail, Ghost, Sigil, laser, sticky, ice, and portal behavior are not implemented in production Core yet.
- Runtime scene content now maps `LevelDefinitionAsset` into Core `LevelDefinition` through `LevelAssetMapper`.
- No Unity Presentation, Input, UI, or scene bootstrap exists for the production session yet.
- Effect ordering currently prioritizes deterministic board mutation; Presentation can translate the ordered model effects into richer merge animation in Milestone 2.
