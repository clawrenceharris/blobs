# Explicit merge presentation

Presentation consumes `MergeEffect` directly. Its moving, target, surviving, and consumed IDs describe the interaction; no neighboring move/removal effects are inspected to discover a merge.

## Flow

1. `BoardPresenter` receives resolved effects or move steps.
2. `BoardEffectPresentationPipeline` dispatches each effect by its registered type.
3. `MergePresentationHandler` sends `MergeEffect` to `BlobPresenter.Merges`.
4. `MergePresenter` retires the explicit consumed view and animates the explicit mover. A surviving mover uses normal merge deformation; a surviving target uses absorption choreography. This decision depends on the effect's roles, not a blob-type check.
5. The owning `PresentationTimeline` awaits playback and handles cancellation through `BoardPresenter`.

Ghost return and Sigil clear remain separate handlers. A ghost merge is the same reverse merge interaction used for flag capture, followed by ghost-specific effects.

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
