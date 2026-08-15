# Milestone 4 Checklist - Cascade-Ready Presentation

**Goal:** Presentation and Core remain synchronized across forward effect chains, restart, and recovery from unsupported or inconsistent view state.

## Status

- [x] Keep a stable blob-ID-to-view map in `BoardPresenter`.
- [x] Expose logical grid positions on blob and tile views for model/view agreement checks.
- [x] Consume remove, move, and spawn effects as one ordered DOTween sequence.
- [x] Complete or cancel an active sequence safely before applying another result or rebuilding.
- [x] Support multi-effect chains containing remove, move, and spawn operations.
- [x] Keep logical view state current independently of animation completion.
- [x] Detect missing view IDs, unsupported effects, and final snapshot mismatches.
- [x] Rebuild from the authoritative Application snapshot after desynchronization.
- [x] Rebuild from the Application snapshot after restart.
- [x] Add smoke tests for stable ID mapping, chained effects, restart agreement, and desync recovery.

## Effect Playback Contract

Core and Application finish resolving a move before Presentation receives its ordered effects. `BoardPresenter` updates its logical ID/position map while constructing a single visual sequence, then compares that logical state with the authoritative post-move snapshot.

If every effect is supported and the final IDs and positions agree, the sequence plays in order. The normal merge's adjacent target-remove/source-move pair is presented as one semantic beat: the source arrives at the target, then the target clears. This visual ordering does not change Core's occupancy-safe mutation order. If an effect cannot be applied or the states disagree, the sequence is discarded and the board is rebuilt from the snapshot. Presentation never changes the gameplay result.

Starting a new effect chain while another is active completes the prior visual state first. Restart and explicit rebuild cancel transient presentation state and reconstruct every view from the supplied snapshot.

## Verification

- Static source checks and `git diff --check` are used for this pass.
- Build and Unity Test Runner execution were intentionally skipped at the user's request.
