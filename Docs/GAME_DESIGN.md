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
- The source travels along the path toward the target.
- The move may be rejected or modified by blobs, tiles, hazards, or other mechanics on that path.
- A successful merge clears or transforms blobs according to the active blob rules.

Click order matters. The first selected blob is always the **source**, and the second selected blob is always the **target**. Reversing the click order creates a different merge intent with the opposite source and target, even if the same two blobs are involved.

For a normal source-to-target merge, the target cell anchors the visual moment. The source blob should move toward the target blob, impact it, and then the target should clear or transform according to the resolved rule. In a production-ready presentation this should read as one fluid merge with squash, stretch, impact response, and other juice rather than as a discrete "move, then remove" technical sequence.

The important distinction is:

> The player chooses the intended destination, but the board determines what actually happens along the way.

For example, a source blob might be stopped by a sticky tile, redirected by a future portal mechanic, blocked by a laser, or interact with another blob before reaching its selected target.

---

## Path-Based Movement

A merge is resolved across the complete path between the source and target.

This allows mechanics to react to positions the blob crosses rather than only the starting and ending cells.

Examples include:

- leaving blobs behind while traveling;
- being blocked by an active laser;
- being removed by a special tile;
- sliding differently on ice;
- stopping on sticky terrain;
- triggering a multi-merge or chained interaction.

This path-based behavior is one of the main foundations for the game's puzzle depth.

---

## Multi-Merges

Some moves may interact with more than one blob before the source reaches its selected target.

A **multi-merge** allows a single player action to create several sequential interactions along the same route.

This is intended to create the game's most satisfying puzzle moments:

- one move;
- several meaningful interactions;
- clear visual cause and effect;
- potentially larger cascades.

Multi-merges should still remain deterministic and readable so the player can understand why each result occurred.

---

# Blob Types

## Normal Blob

The baseline blob.

Normal blobs establish the basic merge rules and are the primary unit used to teach new mechanics.

---

## Trail Blob

A Trail Blob leaves additional normal blobs behind while moving.

When the Trail Blob travels across the board:

- it moves toward its selected target;
- small normal blobs are created along its previous path;
- the spawned blobs use the trail color associated with the Trail Blob.

This turns a single merge into a board-state-changing move and can create new opportunities or obstacles for later merges.

---

## Ghost Blob

Ghost Blobs have a special post-merge behavior.

After another blob merges with a Ghost Blob, the Ghost can move back toward the original source position, effectively **haunting** or taking over the position that was vacated.

Its return path can itself interact with board mechanics.

Example:

- a Trail Blob moves toward a Ghost Blob;
- the Trail Blob leaves blobs behind;
- the Ghost begins moving toward the Trail Blob's original position;
- if the Ghost crosses a Sigil Tile, it is cleared before completing its normal haunt behavior.

This is an example of why post-merge effects are treated as sequential interactions rather than one monolithic result.

---

## Flag / Goal Blob

A special goal-oriented blob can be used as a level completion condition.

The current concept is that a compatible blob must merge into the goal blob to satisfy the level objective.

The exact naming and final rule set for this blob can evolve as the game is refined.

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
- blob type;
- blob color;
- blob size;
- tile placement;
- tile type;
- linked lasers;
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

The current architecture is moving toward a:

**MoveResolver + Resolution Pipeline + Effect Queue**

model.

## Flow

```text
Player Input
    ↓
Merge Intent (Source + Target)
    ↓
MoveResolver
    ↓
Build Path / Move Context
    ↓
Validation + Rules
    ↓
Resolution Pipeline
    ↓
Ordered Effect Queue
    ↓
Board Mutation / Command
    ↓
Animation & Feedback
```

---

## Merge Intent

There is one canonical player move:

```csharp
public readonly struct MergeIntent
{
    public readonly string SourceId;
    public readonly string TargetId;

    public MergeIntent(string sourceId, string targetId)
    {
        SourceId = sourceId;
        TargetId = targetId;
    }
}
```

Clicking two blobs and swiping from one blob to another both produce this same intent.

---

## Effects

Instead of one large merge object describing every special case, resolution produces small ordered effects.

Examples:

```csharp
MoveBlobEffect
RemoveBlobEffect
SpawnBlobEffect
ResizeBlobEffect
SetTileStateEffect
TriggerEffect
```

This makes complex moves easier to reason about.

For example:

```text
Move source
→ spawn Trail blob
→ continue moving
→ merge
→ remove target
→ trigger Ghost haunt
→ Ghost crosses Sigil
→ remove Ghost
```

Each meaningful sub-action can be represented individually.

---

## Why the Effect Queue Matters

The Effect Queue provides:

- deterministic ordering;
- scalable special mechanics;
- multi-merge support;
- cascade support;
- synchronization points for animation.

Rules and reactions can enqueue new effects without placing every possible mechanic inside one giant merge function.

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

1. Finalize the basic merge rules.
2. Complete the MoveResolver / Resolution Pipeline foundation.
3. Make normal movement and merging feel excellent.
4. Add one special mechanic at a time.
5. Test multi-merges and cascades.
6. Build a small set of handcrafted levels.
7. Refine board rendering and visual feedback.
8. Expand the mechanic library only after the core loop consistently feels good.

---

## Project Summary

**Blobs** is built around a simple idea:

> Pick one colorful blob and merge it toward another.

The puzzle comes from everything that can happen between those two points.

As the game progresses, different blob variants and board mechanics transform that simple action into a system of path planning, interaction sequencing, multi-merges, and satisfying cascades—while keeping the controls approachable and the visual presentation soft, playful, and readable.
