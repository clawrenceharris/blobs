# Blobs

**Blobs** is a grid-based puzzle game about clearing colorful, squishy blob characters by merging them across a board. The game combines simple source-to-target movement with increasingly complex blob behaviors, board hazards, and chain reactions.

The visual direction is soft, playful, and polished, with rounded jelly-like characters, clean top-down boards, and satisfying motion inspired by games such as *Two Dots* and *Candy Crush Soda Saga*.

---

## Core Goal

Clear the required blobs from the board by planning valid merges and taking advantage of each blob and tile mechanic.

A level may also include a special goal/flag condition that must be satisfied before the level is complete.

---

## Core Interaction

Every player move begins with **two blobs**:

1. Select the **source blob**.
2. Select the **target blob**.
3. The game interprets this as an intent to merge the source into the target.

The same intent may also be expressed by swiping from the source blob toward the target.

A target is never inferred automatically: every merge always begins with an explicitly chosen source and target.

---

## Basic Merge Rules

For a standard merge:

- The source and target must be aligned on the same **row or column**.
- The source and target cannot be the same color.
- The source travels one tile at a time toward the target.
- Every occupant on that path must itself be a valid merge for the moving blob. If any collision is invalid, the whole move is rejected and the board is unchanged.
- After a surviving merge the mover continues; a merge that consumes the mover (Flag capture) ends the move at that tile even if the player selected a farther blob.
- A successful merge clears or transforms blobs according to the active blob rules.

Click order matters. The first selected blob is always the **source**, and the second selected blob is always the **target**. Reversing the click order creates a different merge intent with the opposite source and target, even if the same two blobs are involved.

For a normal source-to-target merge, the target cell anchors the visual moment. The source blob should move toward the target blob, impact it, and then the target should clear or transform according to the resolved rule. In a production-ready presentation this should read as one fluid merge with squash, stretch, impact response, and other juice rather than as a discrete "move, then remove" technical sequence.

The important distinction is:

> The player chooses the intended destination, but the board determines what actually happens along the way.

For example, a source blob might be consumed by a Flag before reaching a farther selected blob, leave trail blobs on empty tiles, or chain-merge through several occupants on the way to the selected target.

---

## Path-Based Movement

A merge is resolved as a **timeline of per-tile steps** between the source and the selected target.

Each step is one tile of locomotion plus whatever happens at that beat:

- **Traverse** — the next cell is empty. The mover steps onto it. Movement behaviors (Trail spawning) fire for the tile being left behind.
- **Merge** — the next cell is occupied. The collision strategy for `(mover type, occupant type)` must succeed. The occupant is resolved, then the mover enters the cell unless the collision consumes the mover.

This allows mechanics to react to positions the blob crosses rather than only the starting and ending cells.

Examples include:

- leaving Normal blobs behind on departed empty tiles (Trail);
- chain-merging through every occupant on the way to the selected target;
- ending early when a Flag consumes the mover;
- future: being blocked by an active laser, sliding on ice, stopping on sticky terrain, or appending a Ghost haunt after locomotion.

This path-based timeline is the foundation for the game's puzzle depth.

---

## Multi-Merges (Chain Merging)

Chain merging is a **universal path rule**, not a Trail-only exception. Occupants no longer block the path by default.

A single player action walks the rook line. Every occupant must be a valid merge for the current mover. If any link fails (same color, unsupported pair, Flag rules not met), the entire intent fails atomically.

This is intended to create the game's most satisfying puzzle moments:

- one move;
- several sequential interactions along one route;
- clear visual cause and effect;
- later, larger cascades.

A merge that consumes the source stops the walk. A surviving merge continues locomotion, including Trail spawning on later empty tiles. A tile that hosted a merge never also receives a trail spawn — a successful merge should not leave a new blob on the same cell.

---

# Blob Types

## Normal Blob

The baseline blob.

Normal blobs establish the basic merge rules and are the primary unit used to teach new mechanics.

---

## Trail Blob

A Trail Blob moves under the same source-to-target rules as a Normal blob. It also has a **trail color**, shown as a puddle on the blob.

When a Trail Blob travels:

- it chain-merges like any other source;
- on every tile it **departs** that was not a merge site during this move, it leaves a Normal blob of its trail color;
- it never leaves a blob on the selected target's cell, and never on a cell where it just merged.

Example: a red Trail Blob at `(0,0)` with trail color blue, targeting a blob at `(0,3)`:

- empty path → blue Normal blobs spawn at `(0,0)`, `(0,1)`, and `(0,2)` as the Trail Blob leaves those tiles;
- purple occupant at `(0,1)` → the Trail Blob merges into purple (no spawn at `(0,1)`), then continues and still spawns at `(0,0)` and `(0,2)`.

Trail blobs are **clearable** and count toward the clear-all objective. Spawned trail leftovers are ordinary Normal blobs.

---

## Ghost Blob

Ghost Blobs are a planned post-merge behavior, not yet implemented in Core.

The intended hook is already in the collision contract: a strategy may append **follow-up steps** after the main locomotion timeline. Those steps can move a *different* blob id (the Ghost) toward the vacated source position.

After another blob merges with a Ghost Blob, the Ghost can move back toward the original source position, effectively **haunting** or taking over the position that was vacated.

Its return path can itself interact with board mechanics.

Example:

- a Trail Blob moves toward a Ghost Blob;
- the Trail Blob leaves blobs behind;
- the Ghost begins moving toward the Trail Blob's original position;
- if the Ghost crosses a Sigil Tile, it is cleared before completing its normal haunt behavior.

This is why post-merge effects are extra steps on the same timeline rather than one monolithic result.

---

## Flag / Goal Blob

A Flag blob is a level-completion piece, not a source.

Implemented rules:

- Flags cannot be selected as a source (`CanBeSource = false`).
- Capture requires **matching color**.
- Capture is allowed only when the board contains **exactly the mover and the flag** at the moment of collision (other blobs must already be gone, including via earlier chain-merge steps in the same move). Trail leftovers spawned on earlier tiles of the same move also count, so a Trail blob can capture a flag in one move only when it is adjacent (the origin spawn happens after the capture plan is accepted).
- Capture **consumes the source**; the flag stays in place.
- Flags are not clearable. Completing a flag capture that leaves only the flag satisfies the current clear-all-clearable objective.

---

## Other Blob Variants

The project has also explored or reserved room for variants such as:

- Bomb Blob
- Switch Blob

These are intended to create additional rule-driven interactions while preserving the same core source-to-target merge language.

---

# Tile & Board Mechanics

## Normal Tile

A traversable board position with no special behavior.

---

## Laser Tile

Linked Laser Tiles create a beam across a row or column.

A blob may be blocked from crossing the beam depending on:

- whether the laser is active;
- the laser's color;
- the moving blob's color;
- the blob's position relative to the linked laser pair.

Lasers act as path constraints rather than changing the basic input model.

---

## Sigil Tile

A Sigil Tile can react to special blob types crossing it.

The clearest current example is the Ghost Blob:

> If a Ghost crosses a Sigil Tile during its haunt movement, the Ghost is removed and its normal follow-up behavior terminates.

---

## Sticky Tile

Planned / expandable mechanic.

A Sticky Tile can stop or restrict a moving blob before it reaches its selected target.

This creates situations where the player's intended merge destination is known, but the resolved end position is altered by the board.

---

## Ice Tile

Planned / expandable mechanic.

Ice is intended to modify movement so a blob may:

- move farther;
- move faster;
- continue sliding;
- or otherwise behave differently while crossing an icy section.

The exact final rule should remain simple enough to be predicted by the player.

---

## Portal Tile

Planned mechanic.

A portal can alter the path by transporting the moving blob to another position while preserving the original move as a single resolved action.

---

# Size

Earlier versions of Blobs included **small** and **normal-sized** blobs.

The original rules included behaviors such as:

- two small blobs combining into a normal blob;
- a small blob being cleared when interacting with a normal blob;
- normal-to-normal merges removing the target.

The size system is currently a **design decision under review** rather than a locked core rule.

Possible directions include:

1. retain the original asymmetric size rules;
2. allow merges only between blobs of the same size;
3. remove size as a base mechanic and let special blob/tile variants provide the complexity instead.

Until this is finalized, size-dependent behavior should be treated as provisional.

---

# Level Structure

Levels are handcrafted grid layouts defined through `LevelData`.

A level can specify:

- board width and height;
- blob placement;
- blob type (Normal, Flag, Trail);
- blob color;
- Trail color (Trail blobs only);
- tile placement;
- tile type;
- linked lasers (planned);
- scoring data;
- tutorial steps;
- goal-specific setup.

The project includes a custom Unity LevelData editor for authoring these layouts.

---

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

# Resolution Architecture

Production Core uses a **step-based move timeline**, not a single source-to-target merge object.

## Flow

```text
Player Input
    ↓
MoveIntent (SourceId + TargetId)
    ↓
MoveResolver
    ↓
Validate source/target, alignment
    ↓
Simulate per-tile walk on a cloned board
    ↓
Traverse step or Merge step (CollisionPlan)
    ↓
IMoveBehavior.OnTileDeparted (Trail spawn)
    ↓
Commit flattened effects atomically
    ↓
MoveResult.Steps + Effects
    ↓
BoardPresenter step beats
```

---

## Merge Intent

There is one canonical player move:

```csharp
public readonly struct MoveIntent
{
    public MoveIntent(string sourceId, string targetId);
    public string SourceId { get; }
    public string TargetId { get; }
}
```

Clicking two blobs and swiping from one blob to another both produce this same intent.

---

## Steps and effects

Resolution produces a sequential list of `MoveStep`s. Effects **inside** a step are logically simultaneous (Presentation may Join them). Steps play one after another.

```text
MoveStepKind.Traverse | MoveStepKind.Merge
```

Atomic board effects include:

```text
MoveBlobEffect
RemoveBlobEffect
SpawnBlobEffect
MergeIntoFlagEffect
```

Collision strategies (`IMergeStrategy`) emit only occupant resolution via `CollisionPlan`:

- `Continue` — occupant resolved; mover survives and may keep walking.
- `ConsumeMover` — mover is consumed; locomotion ends at this tile.
- `Failed` — the whole intent is rejected.
- `FollowUpSteps` — extra steps after locomotion (Ghost haunt hook).

Locomotion (`MoveBlobEffect`) is owned by the resolver so the same strategy works for intermediate chain merges and for the final target.

Example Trail chain:

```text
Merge into purple at (0,1) + spawn blue at (0,0)
→ Traverse to (0,2) + spawn blue at (0,2)
→ Merge into green at (0,3)
```

---

## Why the timeline matters

The step list provides:

- deterministic ordering;
- scalable special mechanics without a giant merge function;
- chain-merge support;
- synchronization points for animation (Trail spawn joined to the tile-leave beat);
- a place for follow-up motion after the source has finished moving.

Rules and reactions enqueue effects onto the current step or append follow-up steps without placing every mechanic inside one merge function.

---

# Restart Instead Of Undo

The current design direction does **not** include player-facing undo.

As cascades, multi-merges, Trail, Ghost, Sigil, and other reaction chains become more important, undo creates extra implementation and presentation complexity that does not support the intended game feel. If a player realizes they made the wrong move, the expected recovery action is to restart the puzzle.

Restart should:

- restore the authored initial board state;
- clear current selection;
- reset move count and transient session state;
- avoid replaying or reversing cascades.

The effect architecture may still keep enough ordered information for animation, debugging, or future tooling, but production gameplay should not depend on inverse effects for player undo.

---

# Design Principles

## Simple Input, Complex Consequences

The player's action should remain easy to understand:

> Choose Blob A and Blob B.

Complexity should come from how the board resolves that decision.

---

## Mechanics Should Compose

New mechanics should interact with existing mechanics rather than exist in isolation.

For example:

```text
Trail + Ghost + Sigil
```

can produce a result that none of those mechanics create alone.

---

## Readability Before Complexity

Even when a move triggers several effects, the player should be able to follow the sequence.

A complicated result is acceptable.

An unexplained result is not.

---

## Keep Base Rules Stable

The base merge language should remain simple enough that future mechanics can extend it without constantly redefining how a normal blob works.

---

# Current Development Priorities

1. Keep the step-based MoveResolver timeline stable.
2. Make multi-tile movement and Trail spawn timing feel excellent.
3. Author Trail levels and polish the puddle visual.
4. Add Ghost as follow-up steps on the same timeline.
5. Add tile constraints (lasers, sticky, ice) as path-step reactions.
6. Build a small set of handcrafted levels.
7. Refine board rendering and visual feedback.
8. Expand the mechanic library only after the core loop consistently feels good.

---

## Project Summary

**Blobs** is built around a simple idea:

> Pick one colorful blob and merge it toward another.

The puzzle comes from everything that can happen between those two points.

As the game progresses, different blob variants and board mechanics transform that simple action into a system of path planning, interaction sequencing, multi-merges, and satisfying cascades—while keeping the controls approachable and the visual presentation soft, playful, and readable.
