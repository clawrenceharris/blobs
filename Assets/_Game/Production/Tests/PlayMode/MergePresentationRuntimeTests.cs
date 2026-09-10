using System;
using System.Collections;
using Blobs.Core;
using Blobs.Presentation;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Blobs.Tests.PlayMode
{
    public sealed partial class PresentationRuntimeTests
    {
        [UnityTest]
        public IEnumerator ExplicitNormalMergeWorksInFlatListsAndSteps()
        {
            foreach (bool grouped in new[] { false, true })
            {
                var source = Blob("moving", BlobColor.Red, 0, 0);
                var target = Blob("target", BlobColor.Blue, 1, 0);
                var initial = Snapshot(2, 1, source, target);
                var presenter = CreatePresenter(initial);
                presenter.TryGetBlobView(source.Id, out BlobView movingView);
                presenter.TryGetBlobView(target.Id, out BlobView targetView);
                var merge = MergeEffect.NormalMerge(new MoveContext(initial.Board, MovePlan.Move(source, target), source, target));
                var expected = Snapshot(2, 1, source.WithPosition(target.Position));
                var playback = grouped
                    ? presenter.ApplyStepsAsync(new[] { new MoveStep(MoveStepKind.Merge, new IBoardEffect[] { merge }) }, expected)
                    : presenter.ApplyEffectsAsync(new IBoardEffect[] { merge }, expected);
                yield return playback.ToCoroutine();
                yield return null;
                Assert.That(presenter.TryGetBlobView(source.Id, out BlobView survivor), Is.True);
                Assert.That(survivor, Is.SameAs(movingView), "Must present the effect without falling back to rebuilding views.");
                Assert.That(targetView == null, Is.True);
                Assert.That(survivor.transform.localPosition.x, Is.EqualTo(presenter.CellSize).Within(0.001f));
                Assert.That(presenter.IsSynchronizedWith(expected), Is.True);
            }
        }

        [UnityTest]
        public IEnumerator ExplicitReverseMergePreservesFlagOrGhostInBothInputFormats()
        {
            foreach (var type in new[] { BlobType.Flag, BlobType.Ghost })
                foreach (bool grouped in new[] { false, true })
                {
                    var source = Blob("moving", BlobColor.Red, 0, 0);
                    var target = new BlobState("target", type, new GridPosition(1, 0));
                    var initial = Snapshot(2, 1, source, target);
                    var presenter = CreatePresenter(initial);
                    presenter.TryGetBlobView(source.Id, out BlobView movingView);
                    presenter.TryGetBlobView(target.Id, out BlobView targetView);
                    var merge = MergeEffect.ReverseMerge(new MoveContext(initial.Board, MovePlan.Move(source, target), source, target));
                    var expected = Snapshot(2, 1, target);
                    var playback = grouped
                        ? presenter.ApplyStepsAsync(new[] { new MoveStep(MoveStepKind.Merge, new IBoardEffect[] { merge }) }, expected)
                        : presenter.ApplyEffectsAsync(new IBoardEffect[] { merge }, expected);
                    yield return playback.ToCoroutine();
                    yield return null;
                    Assert.That(presenter.TryGetBlobView(target.Id, out BlobView survivor), Is.True);
                    Assert.That(survivor, Is.SameAs(targetView));
                    Assert.That(movingView == null, Is.True);
                    Assert.That(survivor.transform.localPosition.x, Is.EqualTo(presenter.CellSize).Within(0.001f));
                    Assert.That(presenter.IsSynchronizedWith(expected), Is.True);
                }
        }

        [UnityTest]
        public IEnumerator ExplicitMergeDoesNotDependOnNeighboringEffectOrder()
        {
            foreach (bool spawnFirst in new[] { false, true })
            {
                var source = Blob("moving", BlobColor.Red, 0, 0);
                var target = Blob("target", BlobColor.Blue, 1, 0);
                var trail = Blob("departure", BlobColor.Green, 0, 0);
                var initial = Snapshot(2, 1, source, target);
                var presenter = CreatePresenter(initial);
                presenter.TryGetBlobView(source.Id, out BlobView movingView);
                var merge = MergeEffect.NormalMerge(new MoveContext(initial.Board, MovePlan.Move(source, target), source, target));
                var spawn = new SpawnBlobEffect(trail);
                IBoardEffect[] effects = spawnFirst ? new IBoardEffect[] { spawn, merge } : new IBoardEffect[] { merge, spawn };
                var expected = Snapshot(2, 1, source.WithPosition(target.Position), trail);
                yield return presenter.ApplyStepsAsync(new[] { new MoveStep(MoveStepKind.Merge, effects) }, expected).ToCoroutine();
                Assert.That(presenter.TryGetBlobView(source.Id, out BlobView survivor), Is.True);
                Assert.That(survivor, Is.SameAs(movingView));
                Assert.That(presenter.IsSynchronizedWith(expected), Is.True);
            }
        }

        [UnityTest]
        public IEnumerator ExplicitReverseMergeThenGhostReturnKeepsSameGhost()
        {
            var source = Blob("moving", BlobColor.Red, 0, 0);
            var ghost = new BlobState("ghost", BlobType.Ghost, new GridPosition(1, 0));
            var initial = Snapshot(2, 1, source, ghost);
            var presenter = CreatePresenter(initial);
            presenter.TryGetBlobView(ghost.Id, out BlobView ghostView);
            var body = AddFadeGroup(ghostView, out var halo);
            var merge = MergeEffect.ReverseMerge(new MoveContext(initial.Board, MovePlan.Move(source, ghost), source, ghost));
            var expected = Snapshot(2, 1, ghost.WithPosition(source.Position));
            var steps = new[]
            {
                new MoveStep(MoveStepKind.Merge, new IBoardEffect[] { merge }),
                new MoveStep(MoveStepKind.Traverse, new IBoardEffect[]
                {
                    new GhostHauntEffect(ghost.Id, new[] { source.Position })
                })
            };
            var task = presenter.ApplyStepsAsync(steps, expected);
            yield return WaitUntil(() => body.color.a < 0.01f);
            Assert.That(halo.color.a, Is.EqualTo(1f));
            yield return task.ToCoroutine();
            Assert.That(presenter.TryGetBlobView(ghost.Id, out BlobView survivor), Is.True);
            Assert.That(survivor, Is.SameAs(ghostView));
            Assert.That(body.color.a, Is.EqualTo(0.6f).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator GhostRestRevealsBeforeDestructionAndRetiresItsRegistryEntry()
        {
            var source = Blob("moving", BlobColor.Red, 0, 0);
            var ghost = new BlobState("ghost", BlobType.Ghost, new GridPosition(1, 0));
            var initial = Snapshot(2, 1, source, ghost);
            var presenter = CreatePresenter(initial);
            presenter.TryGetBlobView(ghost.Id, out BlobView ghostView);
            var body = AddFadeGroup(ghostView, out var halo);
            var merge = MergeEffect.ReverseMerge(new MoveContext(initial.Board,
                MovePlan.Move(source, ghost), source, ghost));
            var expected = Snapshot(2, 1);
            var playback = presenter.ApplyStepsAsync(new[]
            {
                new MoveStep(MoveStepKind.Merge, new IBoardEffect[] { merge }),
                new MoveStep(MoveStepKind.Traverse, new IBoardEffect[]
                {
                    new GhostRestEffect(ghost.Id, new[] { source.Position })
                })
            }, expected);
            Assert.That(presenter.TryGetBlobView(ghost.Id, out _), Is.False,
                "A resting Ghost is retired from logical occupancy while its view stays alive.");
            Assert.That(ghostView != null, Is.True);
            yield return WaitUntil(() => body != null && body.color.a < 0.01f);
            yield return WaitUntil(() => body != null && body.color.a > 0.3f);
            Assert.That(ghostView != null, Is.True, "Reveal must precede destruction.");
            yield return playback.ToCoroutine();
            yield return null;
            Assert.That(ghostView == null, Is.True);
            Assert.That(presenter.IsSynchronizedWith(expected), Is.True);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ExplicitReverseMergeRebuildCancelsOldPlayback()
        {
            var source = Blob("moving", BlobColor.Red, 0, 0);
            var ghost = new BlobState("ghost", BlobType.Ghost, new GridPosition(1, 0));
            var initial = Snapshot(2, 1, source, ghost);
            var presenter = CreatePresenter(initial);
            var merge = MergeEffect.ReverseMerge(new MoveContext(initial.Board, MovePlan.Move(source, ghost), source, ghost));
            int notifications = 0;
            presenter.SnapshotChanged += _ => notifications++;
            presenter.ApplySteps(new[] { new MoveStep(MoveStepKind.Merge, new IBoardEffect[] { merge }) },
                Snapshot(2, 1, ghost));
            yield return new WaitForSeconds(0.03f);
            presenter.Rebuild(initial);
            yield return new WaitForSeconds(0.5f);
            Assert.That(presenter.IsPresenting, Is.False);
            Assert.That(presenter.IsSynchronizedWith(initial), Is.True);
            Assert.That(notifications, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator SeparateMoveAndRemovalAreNotInferredAsMerge()
        {
            var source = Blob("moving", BlobColor.Red, 0, 0);
            var target = Blob("target", BlobColor.Blue, 1, 0);
            var initial = Snapshot(2, 1, source, target);
            var presenter = CreatePresenter(initial);
            presenter.TryGetBlobView(source.Id, out BlobView movingView);
            var expected = Snapshot(2, 1, source.WithPosition(target.Position));
            var task = presenter.ApplyStepsAsync(new[]
            {
                new MoveStep(MoveStepKind.Merge, new IBoardEffect[]
                {
                    new RemoveBlobEffect(target.Id, target.Position),
                    new MoveBlobEffect(source.Id, source.Position, target.Position)
                })
            }, expected);
            yield return new WaitForSeconds(0.03f);
            Assert.That(movingView.BlobMotionAnimator.CurrentState, Is.EqualTo(BlobAnimationState.Moving));
            yield return task.ToCoroutine();
        }

        [UnityTest]
        public IEnumerator ExplicitMergeInstantPlaybackPreservesCorrectSurvivor()
        {
            foreach (bool reverse in new[] { false, true })
            {
                var source = Blob("moving", BlobColor.Red, 0, 0);
                var target = Blob("target", BlobColor.Blue, 1, 0);
                var initial = Snapshot(2, 1, source, target);
                var presenter = CreatePresenter(initial);
                presenter.enabled = false;
                var context = new MoveContext(initial.Board, MovePlan.Move(source, target), source, target);
                var merge = reverse ? MergeEffect.ReverseMerge(context) : MergeEffect.NormalMerge(context);
                presenter.TryGetBlobView(merge.SurvivingBlobId, out BlobView expectedView);
                var expected = Snapshot(2, 1, reverse ? target : source.WithPosition(target.Position));
                var task = presenter.ApplyEffectsAsync(new IBoardEffect[] { merge }, expected);
                Assert.That(task.Status, Is.EqualTo(UniTaskStatus.Succeeded));
                yield return task.ToCoroutine();
                Assert.That(presenter.TryGetBlobView(merge.SurvivingBlobId, out BlobView survivor), Is.True);
                Assert.That(survivor, Is.SameAs(expectedView));
            }
        }
    }
}
