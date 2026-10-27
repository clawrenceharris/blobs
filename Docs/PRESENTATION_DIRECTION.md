# Blobs — presentation direction

The player-facing presentation direction for tutorials, art, and game feel. See [game design](GAME_DESIGN.md) for the rules and [technical architecture](TECHNICAL_ARCHITECTURE.md) for implementation boundaries.

## Tutorial philosophy

Tutorials should explain one idea at a time through a required move.

Typical tutorial structure:

1. short explanation at the top;
2. direct instruction near the board;
3. highlight or demonstrate the expected source and target;
4. allow the player to perform the move;
5. reveal the result through animation rather than excessive text.

The game should teach primarily through interaction.


## Visual direction

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

**Monster remodel (October 26, 2026).** The dome bodies stay, but each piece now wears a monster head: horns, an eye, and a mouth. A monster's identity comes from its idle face, not its body color, because body color is a gameplay rule. See [game design](GAME_DESIGN.md#monster-remodel) for terminology.


## Animation and game feel

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

## Monster eating presentation

Under the monster remodel, a merge must read as one monster eating another, not two blobs fusing.

- **Only the eater changes face.** The surviving monster swaps its idle face for an eating face. The eaten monster keeps its idle face and shrinks into the eater.
- **The mouth opens for the bite, not for movement.** It opens as the merge beat starts and closes as soon as the prey has vanished, before the eater settles. Sliding across empty cells, gaps between chain contacts, Rock bumps, and a Ghost's return travel all keep the idle face.
- **The mouth faces the prey.** Eating faces are authored as whole head variants: front (prey below), right, and left (the right face mirrored). Faces are never rotated, because the eye and horns establish which way is up.
- **Upward bites** fake the head tipping back: the front eating face placed higher on the body and squashed slightly, together with the existing upward body stretch. A dedicated top-of-head mouth can replace it later. Until it is authored, upward bites use the front eating face.
- **Rocks, Flags, and Ghosts** have no eating faces yet, so their faces never change. A Ghost eating face is the natural next addition.

---
