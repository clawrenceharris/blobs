# Explicit merge presentation

Presentation consumes `MergeEffect` directly. Its moving, target, surviving, and consumed IDs describe the interaction; no neighboring move/removal effects are inspected to discover a merge.

## Choosing merge choreography

On the `BlobPresenter` component, drag a component derived from
`MergeAnimationOrchestratorBase` into **Merge Animation Orchestrator**:

- `MergeAnimationOrchestrator` retains the full deformation and impact-feedback sequence. `Game.unity` uses this component.
- `SimpleMergeAnimationOrchestrator` slides behind the target in 0.15 seconds, sets the moving survivor to zero scale at arrival, brings it in front, and grows it to its original scale over 0.12 seconds. The target stays visible underneath until the growth completes. The survivor then overshoots to 115% over 0.06 seconds and settles to 100% over 0.06 seconds. Reverse merges keep the target and hide the mover at arrival. `Graybox.unity` uses this component. Its slide, scale, overshoot, and settle durations and pulse scale are editable in the Inspector, and it does not broadcast the full animation's impact/VFX cues.

The field accepts a component on another GameObject. When unassigned, it falls
back to an orchestrator on the same GameObject; assign it explicitly when several
are present. `BlobPresenter` no longer automatically adds the full orchestrator.
Existing scene references to the full component remain valid.

Both versions preserve the survivor and color chosen by the game rules. In a
normal merge the moving piece survives at the destination with its existing
color; in a reverse merge the target survives. The simple animation presents this
as one piece at the shared center, without changing identity, blending gameplay
colors, or adding permanent growth. Contact callbacks still run at arrival.
Undo and ghost follow-up effects retain their existing presentation handlers.

## Eating faces (monster remodel)

Merges are presented as the survivor eating the consumed monster; see [presentation direction](PRESENTATION_DIRECTION.md#monster-eating-presentation). Face swapping is presentation-only and reads no Core state beyond the survivor role already carried by `MergeEffect`.

- `MonsterFaceView` (on the monster prefab root) references authored face objects: **Idle**, **Eating Front**, **Eating Left**, **Eating Right**, and an optional **Eating Up** that falls back to Eating Front. Exactly one is active; Idle is restored on spawn.
- `BlobView.Face` exposes the component, or null for prefabs without it (Ghost, Rock, Flag, graybox blobs). Those pieces simply keep their face.
- Both orchestrators call `survivor.Face.ShowEating(towardPrey)` when the beat starts. `towardPrey` is the merge direction when the mover survives and its inverse when the target survives. The dominant axis picks the variant.
- `MergeAnimationOrchestrator` restores Idle after absorption, before recovery. `SimpleMergeAnimationOrchestrator` restores it at arrival, when the consumed view is hidden. A killed beat also restores Idle.
- `PF_Monster_Normal` places the face prefabs (`PF_Monster_Face_Idle_A`, `PF_Monster_Face_Eating_Front`, and `PF_Monster_Face_Eating_Side` twice, with the left instance mirrored) under `VisualRoot`, and wires them into `MonsterFaceView`. Every face prefab includes the horns, so each variant swaps the whole head.

New monster prefabs need their own `MonsterFaceView` and face objects. `BlobEatingFace` and `MonsterFacePairs` were an earlier sprite-swap approach and are not used by this flow.

## Flow

1. `BoardPresenter` receives resolved effects or move steps.
2. `BoardEffectPresentationPipeline` dispatches each effect by its registered type.
3. `MergePresentationHandler` sends `MergeEffect` to `BlobPresenter.Merges`.
4. `MergePresenter` retires the explicit consumed view and animates the explicit mover. A surviving mover uses normal merge deformation; a surviving target uses absorption choreography. This decision depends on the effect's roles, not a blob-type check.
5. The owning `PresentationTimeline` awaits playback and handles cancellation through `BoardPresenter`.

Ghost return and Grave clear remain separate handlers. A ghost merge is the same reverse merge interaction used for flag capture, followed by ghost-specific effects.

## Flat lists versus beats

`PresentOrdered` composes one effect in the order supplied by a flat list. It has no merge-recognition or look-ahead behavior.

`PresentInBeat` composes an effect inside a grouped `MoveStep`. The pipeline dispatches its phase, and the handler decides where its animation belongs. Movement establishes the beat's travel animation; a trail spawn joins its beginning; ordinary removal follows arrival. The phase iteration order is composition order, not necessarily playback start order: a `Join` attaches to an earlier animation.

For example, a step containing a `MergeEffect` and a departure `SpawnBlobEffect` presents the same merge regardless of their array order, because they have explicit roles and phases. A separate `MoveBlobEffect` and `RemoveBlobEffect` stays an ordinary move and removal, even if the enclosing step is labelled `Merge`.

The generic step-handler extension remains available for other composite interactions. The built-in normal-merge step matcher and flattened-sequence matcher have been removed.

## File ownership

- `BoardEffectPresentationPipeline.cs`: registration, dispatch, phase composition, and playback entry points.
- `BoardEffectPresentationHandler.cs`: effect contracts, context, and phase documentation.
- `MergePresentationHandler.cs`, `MoveBlobPresentationHandler.cs`, and the other handler files: concrete effect adapters.
- `MergePresenter.cs`: shared merge choreography and participant validation.
- `BlobPresenter.cs`: view tracking, retirement, and dependency/configuration wiring.
- `MergeAnimationOrchestrator.cs`: normal-merge deformation and impact feedback. Each beat captures its supplied settings instead of changing shared settings on the orchestrator.

`BlobPresenter` supplies the existing settings sections: `MergeImpactSettings` for normal-merge deformation and `BlobMotionSettings` for reverse-merge movement/despawn. No new settings assets or scene wiring were introduced.

## Migration boundary

Merge producers should emit `MergeEffect`. The old `MergeIntoFlagEffect` and `MergeIntoGhostEffect` no longer have Presentation handlers, and separate movement/removal effects are never upgraded into a merge automatically.

Core was not changed in this integration. At the time of this change, `MergeEffect.Apply` still removes `TargetBlobId` for reverse merges, despite their consumed/surviving metadata. Presentation can correctly render an explicit reverse effect, but end-to-end ghost resolution remains blocked by that Core behavior. The new Presentation tests supply effects and expected snapshots directly, while the existing Core-driven integration tests remain in place.

## Validation

Unity 6000.3.6f1 compiled the isolated project and ran all 18 PlayMode tests: 12 passed, 6 failed. All seven new explicit-merge tests passed, covering both input formats, both survivor outcomes, neighboring-effect order, instant mode, ghost follow-up, rebuild cancellation, and the absence of move/removal inference. The six failures are existing Core-driven ghost cases throwing `Blob does not exist: ghost`; they remain visible and were not disabled. All Core files were verified unchanged by content hashes.
