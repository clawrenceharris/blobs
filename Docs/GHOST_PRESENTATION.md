# Ghost merges and async presentation

## Gameplay

A ghost cannot initiate a move. A normal or trail blob can merge into it; the source is consumed and the same ghost returns toward the source's original position for that player move.

The return passes through intermediate occupants without changing them. On landing it absorbs the occupant, including a trail blob spawned during the source's departure. The first Sigil crossed ends the return and clears the ghost instead; an occupant on that Sigil survives. Sigils are reusable. Ghosts count toward the clear-board objective and must be cleared before flag capture.

`GhostMergeStrategy` emits a reverse `MergeEffect` followed by a `GhostReturnEffect` containing the ordered path, and optionally `ClearGhostEffect`. The resolver finalizes landing occupancy and applies the entire aftermath to its simulation before committing any effects to the real board. This also handles adjacent Sigils and empty return cells.

## Playback

See [Explicit merge presentation](PRESENTATION_MERGES.md) for the current shared merge presenter, handler layout, and the remaining Core reverse-merge integration limitation.

`BoardPresenter.ApplyStepsAsync` and `ApplyEffectsAsync` return `UniTask` and accept cancellation. The existing void methods remain event-facing entry points; they start the async operation and report exceptions. Gameplay rules remain synchronous and deterministic.

`PresentationTimeline` composes paused DOTween sequences, child beats, and deferred async operations. `PlayAsync` awaits those operations in order. Low-level `Append`/`Join` composition and synchronous effect/step handler matching remain useful for simultaneous effects; they do not start animated playback. Handler contracts still return whether composition succeeded and which effects were handled. Async execution is owned by the timeline, rather than changing effect matching into asynchronous work.

To add multi-stage presentation in a handler:

```csharp
timeline.AppendAsync(async token =>
{
    await fade.FadeTo(0f, fadeDuration, token);
    await PresentationTimeline.AwaitTweenAsync(
        view.AnimateMoveTo(destination, travelDuration), token);
    await fade.FadeTo(1f, fadeDuration, token);
});
```

Handle `!timeline.IsAnimated` by immediately applying the final visual state. Never put `async` lambdas into DOTween callbacks: DOTween cannot await them. Callbacks remain appropriate for instantaneous contact feedback inside a tween.

The ghost merge animates and retires the source. Its return fades once, travels continuously at constant speed, absorbs a landing occupant if present, and fades back in. At a Sigil it remains ethereal and the halo contracts during the clear beat. This is a functional clear animation; elaborate particles/audio can be added independently.

Undo/rebuild, restart, disable, destruction, and replacement playback cancel the previous operation. Canceled playback cannot publish an old snapshot. Explicit cancellation settles views to the committed snapshot; undo/rebuild uses its supplied snapshot. Snapshot synchronization is checked after awaited playback. Ghost opacity and retiring views are cleaned up on interruption.

## Prefabs and content

- `PF_GhostBlob` uses the reusable `FadeableVisual` component through `BlobRenderer.FadeableVisual`. Its body sprite (including the face) and shadow are assigned; the halo is excluded.
- Fade opacity multiplies each renderer's authored alpha, preserving the translucent shadow. It uses sprite tint rather than cloning materials.
- `BlobRenderer` preserves the previous `_fallbackBaseRenderer` and `_targets` field names through serialization migration attributes.
- The existing `PF_SigilTile` is registered for `TileType.Sigil` in `TileViewCatalog`, retaining the existing Normal registration.
- In level authoring, add a tile entry and set its type to `Sigil`. The existing tile data maps to `SigilTileDefinition`. No existing level layout was changed.

## Validation

Tests run in an isolated local copy using Unity 6000.3.6f1, preserving the open Editor's unsaved Game scene.

- Final ghost EditMode coverage: 13 passed (rules, authored fade behavior, and Ghost/Sigil catalog wiring).
- PlayMode suite: 11 passed, 0 failed, including six new ghost/async tests.
- Full EditMode run: 63 passed, 24 failed. The committed HEAD baseline had 50 passed, 25 failed; every remaining failure also failed on the baseline. The flag-capture source-position regression now passes.

Existing EditMode failures include outdated asset paths and scene composition expectations, selection/feedback expectations, invalid board fixtures, and a flag-chain expectation. They were not suppressed or rewritten for this feature. No player build or device visual review was performed.
