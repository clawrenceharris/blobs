# Milestone 1 Usage

Milestone 1 can be exercised without a Unity scene by creating an Application `GameSession` from the production sample level.

```csharp
using Blobs.Application;
using Blobs.Core;

var session = new GameSession(ProductionSampleLevels.CreateMilestoneOnePuzzle());
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
- Undo applies inverse effects and restores the prior board state.
- Restart rebuilds the authored initial state and clears command history.

## Known Limitations

- Only normal blobs are supported.
- Blob size currently has one production value, `Normal`.
- Tiles, hazards, cascades, multi-merge, Trail, Ghost, Sigil, laser, sticky, ice, and portal behavior are not implemented in production Core yet.
- Sample content is code-only through `ProductionSampleLevels`; production content import is a later milestone.
- No Unity Presentation, Input, UI, or scene bootstrap exists for the production session yet.
- Effect ordering currently prioritizes deterministic board mutation; Presentation can translate the ordered model effects into richer merge animation in Milestone 2.
