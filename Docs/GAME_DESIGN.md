# Blobs — agreed game rules

**Decision date: September 9, 2026.** This is the authoritative design contract from the Week 1 discussion. It describes intended behavior, not a claim that the current code implements every rule. See [implementation and acceptance checklist](RULES_IMPLEMENTATION_PLAN.md) for remaining work and [historical baseline](PROJECT_BASELINE.md) for the earlier inspection.

## Objective and scope

The sole win condition is **zero clearable blobs remaining**. Normal, Trail, and Ghost blobs are clearable; Rocks and Flags are not. Every authored level must contain exactly one Flag for consistency, but capturing it is not a separate victory condition. A non-Flag action that clears all clearable blobs also wins.

Semester mechanics: Normal, Trail, Rock, Flag, Ghost, Sigil, traversable cells, and missing/impassable cells. Undo is required in the 15-week deliverable. Mini and Fat blobs are possible future types; size is not a universal merge property. Bombs, switches, lasers, spikes, ice, sticky tiles, portals, and other variants are future work with no additional mechanics implied by this document. Existing examples of impassable terrain illustrate the path rule, not a commitment to implement every tile. The former target-tile mechanic is scrapped.

## Selection

Select a source blob, then a target blob. Both must exist and share a row or column. Normal and Trail are selectable sources; Rock, Flag, and Ghost are not. Empty cells cannot be destinations. Clicking the selected source again or anywhere without a blob cancels selection. A rejected action clears selection so the next click begins a fresh attempt.

## Resolution and atomicity

Simulate the action in order before committing any changes. Evaluate only the forward path actually reached, ending at the selected target, a stopping Rock, or a consuming Ghost. Valid Normal/Trail merges continue movement. An encountered invalid interaction rejects the entire action, including all earlier simulated effects. No partial movement, spawning, removal, move count, or history change survives rejection.

Missing cells or impassable terrain encountered before forward movement ends reject the entire action with **Path Blocked** feedback. Same-colored Normal/Trail collisions reject the entire action with color-rule feedback. Obstacles beyond a terminating Rock or Ghost are irrelevant. Ghost haunt follows its own rules below.

A selected Flag is an explicit completion attempt: simulation must actually capture that Flag and leave no clearable blobs. Stopping at a Rock or being consumed by a Ghost before the selected Flag rejects the whole action, even if the alternative outcome would clear the board. Encountering an intermediate Flag while targeting another blob also rejects with an explanation.

| Intended forward sequence                           | Outcome                                        |
| --------------------------------------------------- | ---------------------------------------------- |
| Source → valid Normal merge → gap → target          | Reject all effects: Path Blocked.              |
| Source → valid Normal merge → Rock → gap → target   | Accept; stop immediately before Rock.          |
| Source → Ghost → gap → non-Flag target              | Accept; source is consumed and haunt resolves. |
| Source → same-colored blob → Rock → target          | Reject all effects: color rule.                |
| Source → Rock → same-colored blob → non-Flag target | Accept; stop before Rock.                      |
| Source → Rock or Ghost → selected Flag              | Reject: selected Flag cannot be captured.      |

## Blob interactions

### Normal and Trail merges

Normal→Normal, Normal→Trail, Trail→Normal, and Trail→Trail use the same color rule: colors must differ. The occupant is removed; the source retains its type and color and continues if appropriate. No universal size checks or resizing apply.

### Trail movement

Trail is a movement behavior, not a different merge rule. On each departed tile, spawn a Normal blob of the Trail's trail color, including at the action's starting position. Skip cells that were merge sites **during this action**. Do not spawn on the final resting cell because it has not been departed. A previous action's merge site does not exempt this action's starting cell.

A Trail that stops before a Rock spawns only on the tiles it actually departs. Adjacent Rock contact does not move or spawn anything. Trail cannot capture a Flag.

### Rock contact

A Rock cannot be cleared, consumed by the current forward movers, or selected as a source. It is a valid stopping contact: the source moves as far as the cell immediately before it, retaining valid earlier effects. Play contact feedback such as a nudge/shake and rocky thump, including when adjacent. Adjacent contact leaves the board unchanged, so it adds neither a move nor Undo history.

### Flag capture

Only a **matching-color Normal** source may capture a Flag. The source is consumed; the Flag remains. Simulation must finish with zero clearable blobs. Other clearable blobs may be cleared through intermediate merges within the same action before capture. Remaining Rocks do not prevent capture. Multiple Flags are invalid authored content.

Targeting a Flag requires successful capture; intermediate Flag encounters while targeting something else reject. A rejected Flag attempt changes nothing and explains why capture is unavailable. For competing rejection reasons, check Flag source eligibility (type/color) first, then encountered path/interactions, then whether the simulated capture leaves clearable blobs. Basic source/target existence and alignment must also be valid.

### Ghost and haunt

A Ghost consumes the arriving source in a reverse merge, then becomes ethereal and **haunts** toward the source's position at the start of the entire action. Earlier intermediate merges do not change that destination.

While ethereal, it passes through other blobs without affecting them. At its destination it becomes non-ethereal and consumes any occupant regardless of color or type. Thus a Trail→Ghost action leaves its intermediate trail blobs intact, but the trail blob spawned at the original starting cell is consumed on Ghost landing. Trail departures resolve before haunt.

The first Sigil occupied while ethereal clears the Ghost, including its starting cell when haunt begins and its destination. A blob sharing a crossed Sigil is preserved because the Ghost clears without landing. An idle Ghost on a Sigil is legal and is not cleared until it becomes ethereal. No level-validation prohibition is needed for that arrangement.

Haunt retraces the source's validated forward route, so current mechanics cannot put impassable terrain on that route. A future terrain-changing mechanic would need an explicit interaction rule.

## Occupancy and level validation

A resolved cell has at most one blob. Each interaction explicitly determines which blob survives or whether entry stops. Do not infer a generic winner merely from occupancy. An attempted spawn into an occupied cell without a defined interaction rejects the whole action. Ethereal Ghost transit does not establish occupancy in intermediate cells.

Authored levels must have:

- Exactly one Flag.
- No two blobs in the same cell.
- At least one clearable blob and at least one selectable Normal or Trail source.
- Valid IDs, types, colors, coordinates, dimensions, and supported content data.

These are structural checks, not a proof of solvability. Deliberate level design, solution records, and playtesting establish solvability and whether bypassing Flag capture is desirable. Production authoring uses `LevelDefinitionAsset` mapped to Core data; obsolete prototype `LevelData` is not the production rule contract.

## Outcomes, feedback, and move count

| Result                      | Board                    | Move count / history                           | Feedback                                                  |
| --------------------------- | ------------------------ | ---------------------------------------------- | --------------------------------------------------------- |
| Rejected action             | Unchanged                | Unchanged                                      | Rejection shake/text; no merge feedback; clear selection. |
| Valid unchanged contact     | Unchanged                | Unchanged                                      | Contact audio/visuals.                                    |
| Valid board-changing action | Commit entire result     | Add one move and one Undo entry                | Forward sequence.                                         |
| Undo                        | Restore one whole action | Count unchanged; remove latest available entry | Reverse sequence with dedicated Undo audio/effects.       |
| Restart                     | Restore authored start   | Count zero; history cleared                    | Cancel pending playback and victory.                      |

Move count measures committed board-changing forward actions since restart. It is **not Undo history length**. Rejections and unchanged contacts do not alter which move Undo will restore.

## Undo and restart

Undo is in semester scope. One Undo restores the complete state before one board-changing action: intermediate merges, Trail spawns, Ghost consumption/haunt/landing, and Sigil clearing all belong to that unit. Play the entire action in reverse order, with special Undo audio and effects rather than forward impact feedback. Preserve enough action information to restore removed occupants and their identities/properties.

Undo never reduces or increases move count. Repeated Undo is allowed back to the initial state with no use limit. There is no Redo; a new forward action discards the undone future.

Selection and Undo are disabled during board-changing forward playback and Undo playback. Reject/unchanged-contact feedback does **not** lock selection or Undo. Undo is unavailable when history is empty and from confirmation of a winning action through victory; it cannot undo a win. Logical completion locks gameplay immediately, while victory presentation waits for all winning effects to finish.

Restart is always available, including forward playback, reverse playback, winning playback, and the victory screen. It cancels all pending playback and victory callbacks, clears selection/history, restores the authored board, resets move count to zero, and resumes a fresh session. Stale callbacks must never overwrite the restarted board or display an old victory.

## Architectural contract

Core determines legality and the complete ordered outcome; Application owns session commands, accounting, history, and command availability. Presentation animates the resolved sequence and reports playback completion; it never decides whether an interaction is legal. Restart and Undo must preserve this boundary. See [technical architecture](TECHNICAL_ARCHITECTURE.md).

# Tutorial Philosophy

Tutorials should explain one idea at a time through a required move.

Typical tutorial structure:

1. short explanation at the top;
2. direct instruction near the board;
3. highlight or demonstrate the expected source and target;
4. allow the player to perform the move;
5. reveal the result through animation rather than excessive text.

The game should teach primarily through interaction.

---

# Visual Direction

The current direction has returned to the original **squishy dome-like blobs** rather than tile-shaped characters.

The target aesthetic is:

- top-down / orthographic board;
- soft rounded board geometry;
- pastel color palette;
- glossy jelly-like blobs;
- minimal faces with strong personality;
- clean UI;
- subtle shadows and highlights;
- polished, satisfying movement and cascade feedback.

The goal is not realism. The blobs should feel like soft, tactile puzzle pieces.

---

# Animation & Game Feel

Merges should feel organic rather than like rigid pieces translating between grid coordinates.

The intended animation language includes:

- anticipation before movement;
- continuous sliding rather than stopping at every cell;
- subtle squash and stretch;
- wobble and recovery;
- target reaction on impact;
- source-to-target merge motion where the first selected blob visibly travels into the second selected blob;
- small particles or visual crumbs for important interactions;
- synchronized path effects;
- clear cascade timing.

Movement and gameplay resolution remain separate:

> Game logic decides **what happened**.  
> The animation layer decides **how that result is presented**.

This allows effects such as Trail spawns, Ghost movement, ice movement, and tile reactions to occur at the correct point along an otherwise continuous animation.

---
