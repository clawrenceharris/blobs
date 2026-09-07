The presenter split is a good first step, but the main scalability pressure has shifted into `BoardPresenter`’s effect sequencing and `BlobPresenter`’s interaction-specific behavior. No code was edited during this review.

## Highest-priority findings

1. Rock-specific feedback leaks through generic movement APIs  
   Severity: High · Confidence: Confirmed

   Evidence:

   - [BoardPresenter.cs:75](/Users/caleb/Dev/blobs/Assets/_Game/Scripts/Presentation/Presenters/BoardPresenter.cs:75) checks `BlobType.Rock`.
   - [BoardPresenter.cs:188](/Users/caleb/Dev/blobs/Assets/_Game/Scripts/Presentation/Presenters/BoardPresenter.cs:188) propagates `playRockThumpOnFinalMove`.
   - [BlobPresenter.cs:147](/Users/caleb/Dev/blobs/Assets/_Game/Scripts/Presentation/Presenters/BlobPresenter.cs:147) accepts `playRockThumpOnArrival`.
   - [MergeAnimationOrchestrator.cs:234](/Users/caleb/Dev/blobs/Assets/_Game/Scripts/Presentation/State/Animation/MergeAnimationOrchestrator.cs:234) contains rock-only audio.

   Better alternative: put contact behavior on the target blob prefab behind something like `IBlobContactFeedback`. `BoardPresenter` would report a generic contact and provide timing context; the rock prefab’s component would produce the thump cue. Future blobs could independently provide bounce, sparks, dialogue, haptics, or nothing.

   A useful shape would be:

   ```text
   MoveResult
       → interaction presentation resolver
       → target IBlobContactFeedback
       → presentation cues
       → audio / VFX / haptics players
   ```

   The board would never check `BlobType.Rock`.

2. `BoardPresenter` must understand every concrete effect and their ordering  
   Severity: High · Confidence: Confirmed

   [BoardPresenter.cs:134](/Users/caleb/Dev/blobs/Assets/_Game/Scripts/Presentation/Presenters/BoardPresenter.cs:134) switches over every supported effect. It also recognizes a normal merge by finding a particular adjacent `RemoveBlobEffect`/`MoveBlobEffect` pattern.

   [BoardPresenter.cs:249](/Users/caleb/Dev/blobs/Assets/_Game/Scripts/Presentation/Presenters/BoardPresenter.cs:249) then walks the same effects three times to assign locomotion, spawning, and removal phases.

   Adding a tile effect, teleport, split, transform, explosion, push, or status effect will require editing this coordinator.

   Better alternative:

   - Introduce presentation-layer effect handlers such as `IBoardEffectPresenter<T>`.
   - Register handlers by effect type.
   - Have handlers contribute animations to named beat phases such as `Departure`, `Travel`, `Arrival`, and `Aftermath`.
   - Give special step kinds their own `IMoveStepPresenter` when the interaction is more than a collection of ordinary effects.

   Keep these handlers in Presentation; adding Unity-aware visitor methods to Core effects would invert the existing dependency boundary.

3. Timeline composition is controlled through flags and nulls  
   Severity: Medium · Confidence: Confirmed

   Examples include:

   - `isFinalMoveBeat`
   - `playRockThumpOnArrival`
   - `appendToSequence`
   - Passing `Sequence == null` to mean “apply immediately”

   See [BoardPresenter.cs:216](/Users/caleb/Dev/blobs/Assets/_Game/Scripts/Presentation/Presenters/BoardPresenter.cs:216) and [BlobPresenter.cs:205](/Users/caleb/Dev/blobs/Assets/_Game/Scripts/Presentation/Presenters/BlobPresenter.cs:205).

   Better alternative: animation-producing methods should return a tween/beat and let one timeline composer decide whether it is appended or joined. Immediate application could use an explicit `PresentationMode.Immediate` or an `ImmediateTimeline`, rather than nullable sequences.

4. `BlobPresenter` still has several independent reasons to change  
   Severity: Medium · Confidence: Confirmed

   [BlobPresenter.cs](/Users/caleb/Dev/blobs/Assets/_Game/Scripts/Presentation/Presenters/BlobPresenter.cs) currently owns:

   - View creation and destruction
   - Registry synchronization
   - Movement animation
   - Normal merge choreography
   - Flag capture choreography
   - Rock audio
   - Interrupted-animation recovery
   - Merge-orchestrator discovery

   Better alternative: keep `BlobPresenter` focused on blob view lifecycle and tracking. Move interaction choreography into focused collaborators such as:

   - `BlobViewRegistry`
   - `BlobLifecyclePresenter`
   - `NormalMergePresenter`
   - `FlagCapturePresenter`
   - `BlobContactFeedbackResolver`

   These do not all need to be MonoBehaviours; only authored configuration and scene integrations need Unity components.

5. One orchestrator owns animation, VFX, audio, haptics, and a special blob case  
   Severity: Medium · Confidence: Confirmed

   [MergeAnimationOrchestrator.cs](/Users/caleb/Dev/blobs/Assets/_Game/Scripts/Presentation/State/Animation/MergeAnimationOrchestrator.cs) combines normal-merge deformation, sorting, VFX instantiation, two audio categories, and a UnityEvent haptics hook.

   Better alternative: let the merge presenter emit data-driven cues:

   - `AudioCue`
   - `VfxCue`
   - `HapticCue`
   - `CameraCue`

   Dedicated players handle each channel. A `PresentationCueProfile` ScriptableObject can define the clip, VFX prefab, volume, pitch variation, haptic strength, and concurrency policy for an interaction.

6. Tile presentation is not actually extensible by tile type  
   Severity: Medium · Confidence: Confirmed

   [TileView.cs:19](/Users/caleb/Dev/blobs/Assets/_Game/Scripts/Presentation/Views/TileView.cs:19) ignores `TileType`; [TilePresenter.cs:141](/Users/caleb/Dev/blobs/Assets/_Game/Scripts/Presentation/Presenters/TilePresenter.cs:141) uses one prefab for all tiles. `TilePresenter` also owns the board surface and a second legacy cell-prefab system.

   Better alternative:

   - Add `TileViewCatalogAsset` and `ITileViewFactory`, paralleling blobs.
   - Allow prefab-composed tile behaviors such as `ITileEnterFeedback` or `ITileStateBinding`.
   - Separate `BoardSurfacePresenter` from logical tile presentation.
   - Remove the legacy cell layer once its usage is confirmed unnecessary.

7. Every blob view subscribes to global gameplay selection state  
   Severity: Medium · Confidence: Confirmed

   [BlobMotionAnimator.cs:30](/Users/caleb/Dev/blobs/Assets/_Game/Scripts/Presentation/State/Animation/BlobMotionAnimator.cs:30) receives the complete gameplay state and every instantiated blob subscribes to `BlobSelected`.

   This creates one listener per blob and gives selection events and board movement two authorities over animation state.

   Better alternative: a centralized selection presenter should track the previously and currently selected IDs, look up those two views, and update only them. For growing animation needs, separate orthogonal channels—selection loop, locomotion, one-shot interaction—instead of continually expanding a single enum state machine.

8. Synchronization checks are too shallow for future effects  
   Severity: Medium · Confidence: Confirmed

   [BoardPresenter.cs:306](/Users/caleb/Dev/blobs/Assets/_Game/Scripts/Presentation/Presenters/BoardPresenter.cs:306) verifies only IDs and positions. It would consider a view synchronized even if a future effect changed blob type, color, skin, tile type, charge level, or another visible component.

   Better alternative: each view should apply and retain a presentation-relevant state signature or revision. Synchronization should compare that signature, not merely identity and position.

9. Runtime component creation hides invalid scene configuration  
   Severity: Medium · Confidence: Confirmed

   [BoardPresenter.cs:421](/Users/caleb/Dev/blobs/Assets/_Game/Scripts/Presentation/Presenters/BoardPresenter.cs:421) silently adds missing presenters. Similar fallbacks add an unconfigured merge orchestrator, tile view, surface view, and `AudioSource`.

   This is convenient for tests but dangerous in production: missing authored references degrade into default-looking or partially functional objects.

   Better alternative:

   - Use `RequireComponent` for mandatory collaborators.
   - Validate required catalogs, roots, and cue profiles in `OnValidate`.
   - Fail clearly during initialization when production configuration is incomplete.
   - Give tests an explicit composition helper instead of relying on production fallbacks.

10. Rendering code creates per-renderer material instances  
    Severity: Medium · Confidence: Confirmed

    [BlobRenderer.cs:64](/Users/caleb/Dev/blobs/Assets/_Game/Scripts/Presentation/BlobRenderer.cs:64), [FlagBlobColorBinding.cs:34](/Users/caleb/Dev/blobs/Assets/_Game/Scripts/Presentation/Skinning/FlagBlobColorBinding.cs:34), and [TrailBlobColorBinding.cs:34](/Users/caleb/Dev/blobs/Assets/_Game/Scripts/Presentation/Skinning/TrailBlobColorBinding.cs:34) access `renderer.material`.

    Unity clones the material when that property is accessed. This undermines the otherwise-correct use of `MaterialPropertyBlock`, increases material count, and can hurt batching as blob counts grow.

    Better alternative: use `sharedMaterial` only for the common shader/material assignment, then set per-blob colors exclusively through `MaterialPropertyBlock`.

## Additional smells

- `BoardPresenter.Initialize` accepts an unused `BoardSurfaceLayoutAsset`, while width and height duplicate information already present in `snapshot.Board`. See [BoardPresenter.cs:46](/Users/caleb/Dev/blobs/Assets/_Game/Scripts/Presentation/Presenters/BoardPresenter.cs:46). Prefer one immutable board-presentation context derived from the level/snapshot.

- `BoardPresenter.Unsubscribe` sets `SnapshotChanged = null` at [BoardPresenter.cs:434](/Users/caleb/Dev/blobs/Assets/_Game/Scripts/Presentation/Presenters/BoardPresenter.cs:434). Reinitializing the presenter silently removes subscriptions owned by other objects. Only subscribers should control their unsubscription.

- `GameplayFeedbackPresenter.MessageFor` is a growing hard-coded enum switch at [GameplayFeedbackPresenter.cs:83](/Users/caleb/Dev/blobs/Assets/_Game/Scripts/Presentation/Feedback/GameplayFeedbackPresenter.cs:83). Meanwhile, `ShouldShowFeedback` lives in Core even though visibility is presentation policy. Move both into a localized/data-driven failure-feedback catalog.

- `MergeImpactVfx` is instantiated and destroyed for every impact. That is acceptable at low frequency, but chain reactions or effect-heavy levels should use a small reusable pool after profiling.

- [CameraPresenter.cs:29](/Users/caleb/Dev/blobs/Assets/_Game/Scripts/Presentation/Presenters/CameraPresenter.cs:29) relies on a serialized `aspectRatio` and branches on board width versus height. Prefer `max(verticalExtent, horizontalExtent / camera.aspect)` so new device aspect ratios do not require authored tuning.

## Healthy patterns worth expanding

- `BlobViewCatalogAsset` already moves blob-prefab selection into authored data.
- `BlobColorBinding` is a good prefab-composition extension point.
- `IMergeTargetFeedback` demonstrates the right general direction, though `BlobView` currently retains only the first implementation. Supporting multiple cue contributors would make it more scalable.
- The new blob/tile registries give view state a much clearer owner.


Remaining work, in recommended order:

1. Move-step presentation handlers — medium priority  
   [BoardEffectPresentationPipeline.cs](/Users/caleb/Dev/blobs/Assets/_Game/Scripts/Presentation/Presenters/BoardEffectPresentationPipeline.cs:207) still recognizes normal merges by inspecting `MoveBlobEffect`/`RemoveBlobEffect` combinations. Introduce `IMoveStepPresentationHandler` so future bomb, portal, push, or switch interactions register their own composite choreography.

2. Centralize selection animation — medium priority  
   Every [BlobMotionAnimator.cs](/Users/caleb/Dev/blobs/Assets/_Game/Scripts/Presentation/State/Animation/BlobMotionAnimator.cs:30) subscribes to global selection state. A single selection presenter should update only the previously and currently selected views.

3. Stop material instancing — medium priority  
   [BlobRenderer.cs](/Users/caleb/Dev/blobs/Assets/_Game/Scripts/Presentation/BlobRenderer.cs:64), `FlagBlobColorBinding`, and `TrailBlobColorBinding` access `renderer.material`, creating material instances. Use `sharedMaterial` with `MaterialPropertyBlock`.

4. Finish configuration hardening — medium priority  
   Major fallbacks were removed, but `BlobPresenter` can still add a merge orchestrator, `BoardSurfacePresenter` can create an unconfigured surface, `TileView` adds a fixed circle collider, and feedback components add audio sources. Required pieces should be authored or explicitly validated; optional self-contained fallbacks can remain.

5. Make failure feedback data-driven — low/medium priority  
   [GameplayFeedbackPresenter.cs](/Users/caleb/Dev/blobs/Assets/_Game/Scripts/Presentation/Feedback/GameplayFeedbackPresenter.cs:79) contains a growing hard-coded message switch. A localized feedback catalog would scale better.

6. Correct camera sizing for arbitrary devices — medium priority  
   [CameraPresenter.cs](/Users/caleb/Dev/blobs/Assets/_Game/Scripts/Presentation/Presenters/CameraPresenter.cs:29) uses a serialized aspect ratio. It should calculate the required horizontal and vertical extents using `Camera.aspect`.

7. Add PlayMode presentation coverage — medium validation gap  
   There are currently only EditMode tests. DOTween sequencing, interruption cleanup, prefab composition, and scene startup are not exercised in PlayMode.

Pooling merge VFX can wait until profiling demonstrates meaningful churn.
