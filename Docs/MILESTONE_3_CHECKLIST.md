# Milestone 3 Checklist - Content Pipeline

**Goal:** production levels can be authored as Unity assets, converted into Core data, and rejected before play when malformed.

## Status

- [x] Use `LevelDefinitionAsset` ScriptableObjects as the production authoring format.
- [x] Convert authored blob, tile, board, identity, schema, and objective data into immutable Core definitions.
- [x] Reject unsupported schema versions.
- [x] Validate required and unique blob/tile IDs.
- [x] Validate positive dimensions and in-bounds coordinates.
- [x] Validate single-blob and single-tile occupancy.
- [x] Validate blob colors, blob types, blob sizes, and tile types.
- [x] Define and validate the MVP `ClearAllClearableBlobs` objective.
- [x] Pass the authored objective through Application move and restart evaluation.
- [x] Add three handcrafted MVP level assets.
- [x] Add focused Edit Mode coverage for asset loading and malformed level rejection.

## Authored Levels

- `Sample_Level.asset` teaches a horizontal source-to-target merge.
- `Level_02.asset` presents the same rule vertically.
- `Level_03.asset` chains the rule around two board edges.

All three use schema version 1 and the `ClearAllClearableBlobs` objective. The scene continues to start with `Sample_Level.asset`; the additional assets are ready for later level-selection/progression work.

## Validation Boundary

`LevelAssetMapper.ToCore` converts Unity serialization types into plain Core types and then calls `LevelValidator.ValidateOrThrow`. `LevelFactory.CreateInitialBoard` validates again so callers cannot bypass the boundary by constructing a Core definition directly.

Blob and tile IDs are unique within their respective registries. A blob and its supporting tile may share a coordinate, while two blobs or two tiles may not occupy the same coordinate.

## Verification Log

- [x] Core, Application, and Content projects compile with zero warnings and errors.
- [x] The new Milestone 3 Edit Mode test source compiles with zero warnings and errors against Unity and NUnit references.
- [x] `git diff --check` passes.
- [ ] Run the focused Edit Mode suite in the open Unity editor. A second batch-mode editor cannot open the project concurrently.
