# Blobs — level authoring

**Decision date: September 9, 2026.** Structural requirements for authored levels. See [game design](GAME_DESIGN.md) for the objective and [resolution rules](RULE_RESOLUTION.md) for occupancy during moves.

Authored levels must have:

- Exactly one Flag.
- No two blobs in the same cell.
- At least one clearable blob and at least one selectable Normal or Trail source.
- Valid IDs, types, colors, coordinates, dimensions, and supported content data.

These checks do not prove solvability. Deliberate level design, solution records, and playtesting establish solvability and whether bypassing Flag capture is desirable. Production authoring uses `LevelDefinitionAsset` mapped to Core data.
