# Reversible drag preview

Enable Drag Preview on GameBootstrapper (enabled by default). A selected normal
monster lifts above the finger, scales up, and travels along valid cardinal
routes. Targets shrink and pull toward the eater as contact approaches; the
eater opens its matching mouth. At a reached target the drag can turn onto a
new row or column. Backtracking along the previous leg restores its meals,
body color, and size. Invalid movement cannot advance the preview.

Release on the most recently reached valid endpoint to commit. The complete
path is validated on a cloned board and becomes one move/history entry. Empty
or off-route release cancels the entire drag. Restart, undo, focus loss,
UI interference, disabling input, and session replacement clear the preview.
Targets and authoritative game state are unchanged until release.

## Eating faces

1. Create > Blobs > Presentation > Monster Face Pairs.
2. Add one pair per face variant: the exact Idle sprite and its matching Eating
   sprite. Body color does not choose a face. Export both sprites with matching
   canvas size, pivot, and facial alignment.
3. Add GameplayDragPreview to the GameBootstrapper object in Edit Mode and assign
   the asset to Face Pairs. The bootstrapper otherwise adds this component at
   runtime, with default motion settings.
4. Faces on child renderers named Face are detected automatically. For another
   hierarchy, add BlobEatingFace to the monster prefab and assign Face Renderer
   explicitly. Its optional Face Pairs asset overrides the preview's fallback.

Every instance captures its own current idle face when dragging starts. Unmapped
or missing eating sprites keep the idle face. Cancel and release restore it;
committed drags preserve surviving views and their face identity. No new face
art was generated or assigned automatically.

Adjust Pickup Scale, Finger Lift (in cells), and Eating Distance on
GameplayDragPreview. The final commit retains the short pulse/settle beat.

## Scope

Live preview supports normal monsters, size-based normal/reverse eating, size
and color effects emitted by the resolver, and final flag captures. A consumed
source ends the path. Ghost/rock interactions use ordinary release playback when
dragged directly; trails retain ordinary input. They are not additional turning
waypoints in the live path. Their special aftermath is not approximated.
Disabling Enable Drag Preview restores ordinary source-to-release input.

## Validation

Edit Mode: DragPreviewPathTests covers turning, backtracking, atomic rejection,
and committing/undoing a complete bent path as one action.
Play Mode: DragPreviewTests covers lifted/scaled selection, reversible meals,
turning and backtracking, view identity on commit, and matched eating/idle faces.

Unity 6000.3.6f1 compiled the implementation. Targeted results: 3/3 Edit Mode and
3/3 Play Mode tests passed. No on-device touch test was performed.
