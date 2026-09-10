# Resolver and Ghost rest fix validation

## Verified changes already present in the user implementation

The current resolver now evaluates intermediate collisions on the simulation board, records the actual stopping Rock, and supplies the original action start through `MoveContext.Plan`. Ghost haunt uses that original start. Before this fix pass, 8 of the 12 path tests passed, including stopping/contact identity, same-color rejection, Trail stopping, Ghost origin, and chain capture into a Flag. The four remaining failures were reached-gap rejection and three Flag-intent constraints.

## Fixes applied

- Restored reached-cell terrain validation in `MoveResolver`; the check had been left in the commented-out old planning method. Removed that obsolete commented method.
- Preserved explicit Flag intent: Normal source eligibility, matching color, no intermediate Flag capture, and no successful action that stops/consumes before the selected Flag. Added corresponding failure text with new enum values appended to preserve existing serialized values.
- Added Normal/Trail movement registrations for Trail targets.
- Preserved valid adjacent Rock feedback without incrementing `GameSession` history/move count.
- A Ghost beginning haunt on a Grave now emits rest immediately at that cell.
- Updated tests to the current separate `GhostHauntEffect` and `GhostRestEffect` contracts. Removed an incorrect test landing ID that referred to the source already consumed by the preceding reverse merge.

## Ghost view lifetime cause

The existing Unity Editor log records a `MissingReferenceException` for a destroyed `BlobView` from `GhostRestPresenter.ReturnAsync`, through `BlobMotionAnimator.SetIdle` and the idle state's transform access.

The old sequence faded out, moved, despawned, destroyed the Ghost, then awaited fade-in and attempted `SetIdle`. Across the await, Unity completed destruction of the native object. A C# reference can remain while Unity's destroyed object behaves as null; null-conditional access does not restore it. This was a use-after-destruction error, not Core removing the board model too early.

The rest presenter also fetched rather than retired the view, so its active registry retained the destroyed entry. The fix retires it during composition (removes logical occupancy without destroying the object), then animates fade-out → travel → reveal → despawn → destroy. Nothing accesses the view after destruction. Cancellation cleanup checks Unity object validity before restoring its fade.

## Validation

- **28 passed, 0 failed:** current Core/Application plus `MoveResolverPathTests` and `GhostMechanicTests`, executed with the project's NUnit library through an isolated .NET reflection runner. Parameterized Ghost and Trail cases included.
- **0 compile errors:** production runtime source and PlayMode test source compiled together against installed Unity assemblies in an isolated .NET Standard 2.1 project. Warnings included serialized fields and an existing `GridPosition`/null comparison. This combined compile does not validate Unity's individual assembly boundaries.
- Added `GhostRestRevealsBeforeDestructionAndRetiresItsRegistryEntry` PlayMode regression: checks retirement, reveal before destruction, and final synchronization without unexpected logs. Existing reverse-merge/haunt and restart tests remain enabled.
- **Play Mode not run successfully:** sandboxed Unity could not open its package-manager socket; the permitted retry in an isolated project stalled at licensing initialization. Stopped only the isolated test Editor. The user's active project was not launched, closed, or saved by this validation.
- No visual playback or target build is claimed. Run the new rest regression and existing reverse-merge/haunt/restart tests in the user's licensed Editor to finish runtime verification.

Temporary evidence: `/tmp/blobs-path-validation/ghost-fix-core-results.txt`, `/tmp/blobs-presentation-compile/results.txt`, `/tmp/blobs-ghost-validation/ghost-fix-final.log`. Other existing project edits were preserved.

## Core/Application test output

```text
/Users/caleb/Dev/blobs/Assets/_Game/Production/Scripts/Core/State/BoardState.cs(158,17): warning CS8073: The result of the expression is always 'false' since a value of type 'GridPosition' is never equal to 'null' of type 'GridPosition?' [/tmp/blobs-path-validation/Validation.csproj]
PASS AdjacentRockDoesNotIncrementSessionMoveCountButStillPublishesContact 
PASS TrailTargetsUseNormalColorMergeRules Normal
PASS TrailTargetsUseNormalColorMergeRules Trail
PASS TrailCannotCaptureFlagEvenWhenAdjacentAndMatching 
PASS IntermediateRockStopsBeforeItAndIgnoresLaterGap 
PASS ResultIdentifiesTheRockThatActuallyStoppedTheMove 
PASS ValidMergeBeforeRockCommitsButDoesNotReachSelectedTarget 
PASS GapAfterValidMergeRejectsWithoutPartialCommit 
PASS SameColorBeforeRockRejectsWithoutPartialCommit 
PASS AdjacentRockContactProducesNoBoardEffects 
PASS TrailStopsBeforeRockAndSpawnsOnlyOnDepartedCells 
PASS GhostBeforeGapEndsForwardTravelAndHauntsOriginalStart 
PASS FlagCaptureCanClearOtherBlobsEarlierInTheSameAction 
PASS SelectedFlagCannotBeReplacedByAnIntermediateRock 
PASS SelectedFlagCannotBeReplacedByAnIntermediateGhost 
PASS IntermediateFlagRejectsEvenWhenItsColorMatches 
PASS GhostCannotBeSourceAndCountsTowardObjective 
PASS EmptyReturnPathPreservesGhostIdentityAndConsumesSource 
PASS GraveCrossingIncludesGraveAndClearsGhost 2
PASS GraveCrossingIncludesGraveAndClearsGhost 1
PASS GraveCrossingIncludesGraveAndClearsGhost 0
PASS FirstGraveStopsReturn 
PASS ReturnPhasesThroughTrailAndAbsorbsOnlyLandingOccupant 
PASS OccupiedGraveClearsGhostWithoutAbsorbingTrailOccupant 
PASS ChainMergeReturnsToOriginalSourcePosition 
PASS InvalidOutboundCollisionLeavesBoardUnchanged 
PASS GhostAlreadyOnGraveRestsWhenHauntBegins 
PASS EmptyReturnIsRejectedAtConstruction 
TOTAL: 28 passed, 0 failed

```
