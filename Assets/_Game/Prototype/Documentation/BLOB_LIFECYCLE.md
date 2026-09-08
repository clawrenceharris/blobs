# Blob lifecycle and reference ownership

This document defines how blobs move through active/removed/respawned states, how IDs are used, and which components own references.

## States

- **Active**: Blob exists in `BoardModel` (`_blobsById`, `BlobGrid`) and has a presenter and view. It is visible and can be selected/moved.
- **Removed**: Blob has been taken off the board (e.g. merged away, cleared). The model no longer holds it in `_blobsById` or `BlobGrid`. For undo, we keep the presenter (and optionally the view) so that undo can restore it; the view may be deactivated and returned to the pool.
- **Respawned**: A previously removed blob is put back on the board (undo). Same ID is used so that event animators and presenters can resolve correctly.

## ID stability

- **Within an undoable action**: Blob IDs MUST remain stable. When we remove a blob and later undo, we respawn the same ID. If IDs changed, later events in the same plan (e.g. MoveBlobEvent) would reference stale IDs and break presenter lookup.
- **Across actions**: After an action is discarded from history (e.g. undo stack cleared), inactive presenters/views may be fully released; new spawns can use new IDs.

## Reference ownership

| Owner | What it holds | When it's updated |
|-------|----------------|-------------------|
| **BoardModel** | `_blobsById`, `BlobGrid`. Only active blobs. | On SpawnBlob, RemoveBlob, MoveBlob, RespawnBlob. |
| **BoardPresenter** | `_blobPresenters`: id → IBlobPresenter. Optionally `_inactivePresenters` for removed blobs still needed for undo. | On model OnBlobSpawned (add), OnBlobRemoved (move to inactive or keep in map but marked inactive), OnBlobRespawned (reactivate). |
| **BlobViewPool** | Inactive BlobView instances per BlobType. | Get(type) removes from pool; Release(view) returns to pool. |
| **BoardView** | `_blobViews`: id → BlobView for active (and optionally inactive) blobs. | CreateBlobView adds; ClearBoard and release paths remove. Must clear dictionaries when views are released/destroyed. |

## Lifecycle flow

1. **Spawn (new blob)**: Model adds blob → OnBlobSpawned → BoardPresenter creates presenter, BoardView gets view from pool (or creates), both register by blob.ID.
2. **Remove (merge/clear)**: Merge event calls board.RemoveBlob(id) → Model removes from grid and _blobsById, raises OnBlobRemoved → Presenter deactivates view, may move presenter to inactive set; view is released to pool (or kept associated with that ID for undo).
3. **Respawn (undo)**: Merge event Undo calls board.RespawnBlob(blob) with snapshot → Model re-adds blob to grid and _blobsById, raises OnBlobRespawned → Presenter reactivates or recreates from pool, view re-initialized with blob; both register by same blob.ID.
4. **Move**: Model updates blob position in grid; presenter/view stay the same, animator uses event From/To for visuals.

## Undo ordering rule

Events must be ordered so that on undo: **movers vacate a cell before any respawn places an occupant there.** Concretely:

- `MergeBlobsEvent.Undo`: first move the mover blob back to From, then respawn the removed blob (atomic; order is correct in code).
- `RemoveBlobEvent.Undo`: when used with a preceding visual-only `MoveBlobEvent`, the snapshot is taken at Execute time with the blob still at From, so respawn restores the blob at From and no conflicting move is needed.
- Plan event order: when mixing move and remove events, place `RemoveBlobEvent` after any `MoveBlobEvent` (visual or not) so that on undo (reversed order) the move is undone first if it would free a cell.

## Validation

- **BoardIntegrityValidator**: static helper that checks each grid cell has at most one blob, `BlobCount` matches grid occupancy, and every blob in the registry is at its grid position. Call `BoardPresenter.ValidateBoardIntegrity(context)` after merge execute/undo in debug, or use menu **Blobs > Validate board integrity** in the Editor (play mode, level loaded).
