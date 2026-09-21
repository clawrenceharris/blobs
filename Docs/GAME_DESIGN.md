# Blobs — game design

**Decision date: September 9, 2026.** Authority for intended player-facing rules, not a claim that every rule is implemented. Detailed [resolution rules](RULE_RESOLUTION.md), [Undo and Restart behavior](UNDO_AND_RESTART.md), [level authoring requirements](LEVEL_AUTHORING.md), and [presentation direction](PRESENTATION_DIRECTION.md) extend this overview. Track delivery in the [implementation checklist](RULES_IMPLEMENTATION_PLAN.md).

## Premise and objective

Blobs is a grid puzzle game. Select a blob and send it along a row or column toward another blob. Encounters can merge blobs, leave a Trail, stop movement, or trigger a Ghost's return. The puzzle is complete when **zero clearable blobs remain**. Normal, Trail, and Ghost blobs are clearable; Rocks and Flags are not.

Every level contains one Flag, and capturing it is required for victory.

## Playing a move

Select or swipe between a color blob source and a blob target in the same row or column. Rocks, Flags, and Ghosts cannot start moves; empty cells cannot be targets. Selecting the same source again or clicking outside blobs cancels selection. A rejected move clears selection and leaves the board unchanged.

Movement follows the reached path toward the target. Valid encounters may form a chain. If a reached encounter is illegal, the entire move is rejected; earlier effects in that attempted move do not remain. A missing or impassable cell blocks movement. A Rock or Ghost can end forward movement before later cells are reached. See [resolution rules](RULE_RESOLUTION.md) for ordering and edge cases.

## Blob and tile rules

### Normal and Trail

Normal and Trail blobs can merge with either type when their colors differ. The moving source keeps its identity, type, and color; the encountered blob is removed. Matching colors reject the move.

A Trail blob leaves a Normal blob of its trail color on each tile it departs, including its starting tile. It does not leave one on a tile where it merged during that move or on its final resting tile. Trail uses the same merge color rule as Normal.

### Rock

A Rock stops the source on the cell immediately before it. Valid effects earlier on the path remain. Contact gives a visible and audible response. Contact with an adjacent Rock changes no board state and does not count as a move.

### Flag

Only a matching-color Normal blob can capture a Flag. The source disappears, the Flag remains, and capture succeeds only if no clearable blobs remain after that move. Selecting a Flag means attempting to capture that Flag; a Rock or Ghost stopping the source first makes the attempt fail. Trail cannot capture a Flag.

### Ghost and Grave

A Ghost consumes an arriving source, then haunts back toward where that source began the move. It passes through blobs on the return path and consumes an occupant where it lands, regardless of type or color. The first Grave the ethereal Ghost occupies clears it instead; a blob on that Grave survives. A Ghost resting on a Grave is allowed and clears only when it begins haunting.

## Outcomes and recovery

Only a board-changing forward action adds one to the move count. Rejected moves and unchanged Rock contact do not. One Undo restores the board before one such action, with no use limit and no change to the move count. Undo cannot reverse a completed win. Restart returns to the authored starting board, resets the count, and clears history; it is available at any time. See [Undo and Restart behavior](UNDO_AND_RESTART.md) for full interaction and playback rules.

## Scope

The 15-week deliverable includes Normal, Trail, Rock, Flag, Ghost, Grave, traversable and missing or impassable cells, and Undo. Mini and Fat blobs are possible future types; size is not a universal merge property. Bombs, switches, lasers, spikes, ice, sticky tiles, portals, and other variants are future work. The former target-tile mechanic is scrapped.
