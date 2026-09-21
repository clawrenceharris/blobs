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

---
