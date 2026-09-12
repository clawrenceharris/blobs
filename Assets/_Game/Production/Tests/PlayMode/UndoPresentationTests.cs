using System.Collections;
using System.Collections.Generic;
using Blobs.Application;
using Blobs.Core;
using Blobs.Presentation;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Blobs.Tests.PlayMode
{
    public sealed class UndoPresentationTests : PresentationTestFixture
    {
        [UnityTest]
        public IEnumerator ReverseNormalMergeRestoresConsumedIdentityWithoutRebuild()
        {
            var source = Blob("moving", BlobColor.Red, 0, 0);
            var target = Blob("target", BlobColor.Blue, 1, 0);
            var initial = Snapshot(2, 1, source, target);
            var presenter = CreatePresenter(initial);
            presenter.TryGetBlobView(source.Id, out BlobView movingView);
            var undoFeedback = presenter.gameObject.AddComponent<RecordingUndoFeedback>();
            var merge = MergeEffect.NormalMerge(new MoveContext(
                initial.Board,
                MovePlan.Default(source, target),
                source,
                target,
                new MoveIntent(source, target)));
            var after = Snapshot(2, 1, source.WithPosition(target.Position));
            yield return presenter.ApplyStepsAsync(
                new[] { new MoveStep(MoveStepKind.Merge, new IBoardEffect[] { merge }) },
                after).ToCoroutine();

            Assert.That(presenter.TryGetBlobView(target.Id, out _), Is.False);
            Assert.That(movingView != null, Is.True);

            var catalog = Catalog(source, target);
            yield return presenter.ApplyStepsReversedAsync(
                new[] { new MoveStep(MoveStepKind.Merge, new IBoardEffect[] { merge }) },
                initial,
                catalog).ToCoroutine();
            yield return null;

            Assert.That(presenter.TryGetBlobView(source.Id, out BlobView survivor), Is.True);
            Assert.That(survivor, Is.SameAs(movingView));
            Assert.That(presenter.TryGetBlobView(target.Id, out BlobView restored), Is.True);
            Assert.That(restored.BlobId, Is.EqualTo(target.Id));
            Assert.That(restored.BlobType, Is.EqualTo(BlobType.Normal));
            Assert.That(presenter.IsSynchronizedWith(initial), Is.True);
            Assert.That(undoFeedback.PlayCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator ReverseTrailSpawnAndMoveRestoresDepartureCell()
        {
            var source = Blob("moving", BlobColor.Red, 0, 0).WithTrail(BlobColor.Green);
            var target = Blob("target", BlobColor.Blue, 1, 0);
            var trail = Blob("moving-trail-1", BlobColor.Green, 0, 0);
            var initial = Snapshot(2, 1, source, target);
            var presenter = CreatePresenter(initial);
            presenter.TryGetBlobView(source.Id, out BlobView movingView);
            var merge = MergeEffect.NormalMerge(new MoveContext(
                initial.Board,
                MovePlan.Default(source, target),
                source,
                target,
                new MoveIntent(source, target)));
            var spawn = new SpawnBlobEffect(trail);
            var steps = new[]
            {
                new MoveStep(MoveStepKind.Merge, new IBoardEffect[] { merge, spawn })
            };
            var after = Snapshot(2, 1, source.WithPosition(target.Position), trail);
            yield return presenter.ApplyStepsAsync(steps, after).ToCoroutine();
            Assert.That(presenter.TryGetBlobView(trail.Id, out _), Is.True);

            yield return presenter.ApplyStepsReversedAsync(steps, initial, Catalog(source, target, trail))
                .ToCoroutine();
            yield return null;

            Assert.That(presenter.TryGetBlobView(source.Id, out BlobView survivor), Is.True);
            Assert.That(survivor, Is.SameAs(movingView));
            Assert.That(presenter.TryGetBlobView(trail.Id, out _), Is.False);
            Assert.That(presenter.TryGetBlobView(target.Id, out _), Is.True);
            Assert.That(presenter.IsSynchronizedWith(initial), Is.True);
        }

        [UnityTest]
        public IEnumerator ReverseGhostHauntRestoresLandingOccupantAndGhostOrigin()
        {
            var source = Blob("moving", BlobColor.Red, 0, 0);
            var ghost = new BlobState("ghost", BlobType.Ghost, new GridPosition(1, 0));
            var initial = Snapshot(2, 1, source, ghost);
            var presenter = CreatePresenter(initial);
            presenter.TryGetBlobView(ghost.Id, out BlobView ghostView);
            AddFadeGroup(ghostView, out _);
            var intent = new MoveIntent(source, ghost);
            var merge = MergeEffect.ReverseMerge(new MoveContext(
                initial.Board, MovePlan.Default(source, ghost), source, ghost, intent));
            var haunt = new GhostHauntEffect(ghost.Id, new[] { source.Position }, origin: ghost.Position);
            var steps = new[]
            {
                new MoveStep(MoveStepKind.Merge, new IBoardEffect[] { merge }),
                new MoveStep(MoveStepKind.Traverse, new IBoardEffect[] { haunt })
            };
            var after = Snapshot(2, 1, ghost.WithPosition(source.Position));
            yield return presenter.ApplyStepsAsync(steps, after).ToCoroutine();
            Assert.That(presenter.TryGetBlobView(source.Id, out _), Is.False);

            yield return presenter.ApplyStepsReversedAsync(steps, initial, Catalog(source, ghost))
                .ToCoroutine();
            yield return null;

            Assert.That(presenter.TryGetBlobView(ghost.Id, out BlobView restoredGhost), Is.True);
            Assert.That(restoredGhost, Is.SameAs(ghostView));
            Assert.That(presenter.TryGetBlobView(source.Id, out BlobView restoredSource), Is.True);
            Assert.That(restoredSource.BlobId, Is.EqualTo(source.Id));
            Assert.That(presenter.IsSynchronizedWith(initial), Is.True);
        }

        [UnityTest]
        public IEnumerator ReverseGhostRestRecreatesGhostIdentity()
        {
            var source = Blob("moving", BlobColor.Red, 0, 0);
            var ghost = new BlobState("ghost", BlobType.Ghost, new GridPosition(1, 0));
            var initial = Snapshot(2, 1, source, ghost);
            var presenter = CreatePresenter(initial);
            var intent = new MoveIntent(source, ghost);
            var merge = MergeEffect.ReverseMerge(new MoveContext(
                initial.Board, MovePlan.Default(source, ghost), source, ghost, intent));
            var rest = new GhostRestEffect(ghost.Id, new[] { source.Position });
            var steps = new[]
            {
                new MoveStep(MoveStepKind.Merge, new IBoardEffect[] { merge }),
                new MoveStep(MoveStepKind.Traverse, new IBoardEffect[] { rest })
            };
            var after = Snapshot(2, 1);
            yield return presenter.ApplyStepsAsync(steps, after).ToCoroutine();
            Assert.That(presenter.TryGetBlobView(ghost.Id, out _), Is.False);

            yield return presenter.ApplyStepsReversedAsync(steps, initial, Catalog(source, ghost))
                .ToCoroutine();
            yield return null;

            Assert.That(presenter.TryGetBlobView(ghost.Id, out BlobView restoredGhost), Is.True);
            Assert.That(restoredGhost.BlobType, Is.EqualTo(BlobType.Ghost));
            Assert.That(presenter.TryGetBlobView(source.Id, out _), Is.True);
            Assert.That(presenter.IsSynchronizedWith(initial), Is.True);
        }

        [UnityTest]
        public IEnumerator UndoResolvedPlaysDedicatedFeedbackAndSkipsForwardContact()
        {
            var source = Blob("moving", BlobColor.Red, 0, 0);
            var target = Blob("target", BlobColor.Blue, 1, 0);
            var initial = Snapshot(2, 1, source, target);
            var after = Snapshot(2, 1, source.WithPosition(target.Position));
            var state = new FakeGameplayState(initial);
            var presenter = CreatePresenter(initial, state);
            var undoFeedback = presenter.gameObject.AddComponent<RecordingUndoFeedback>();
            var merge = MergeEffect.NormalMerge(new MoveContext(
                initial.Board,
                MovePlan.Default(source, target),
                source,
                target,
                new MoveIntent(source, target)));
            yield return presenter.ApplyStepsAsync(
                new[] { new MoveStep(MoveStepKind.Merge, new IBoardEffect[] { merge }) },
                after).ToCoroutine();

            state.RaiseUndoResolved(new UndoResult(
                new MoveResult(
                    source.Id,
                    target.Id,
                    true,
                    MoveFailureReason.None,
                    new IBoardEffect[] { merge },
                    false,
                    new[] { new MoveStep(MoveStepKind.Merge, new IBoardEffect[] { merge }) }),
                initial,
                Catalog(source, target)));
            yield return WaitUntil(() => undoFeedback.PlayCount == 1);
            yield return WaitUntil(() => presenter.IsPresenting == false);
            Assert.That(presenter.IsSynchronizedWith(initial), Is.True);
        }

        [UnityTest]
        public IEnumerator RebuildDuringReverseSettlesToAuthoredBoard()
        {
            var source = Blob("moving", BlobColor.Red, 0, 0);
            var target = Blob("target", BlobColor.Blue, 1, 0);
            var initial = Snapshot(2, 1, source, target);
            var presenter = CreatePresenter(initial);
            var merge = MergeEffect.NormalMerge(new MoveContext(
                initial.Board,
                MovePlan.Default(source, target),
                source,
                target,
                new MoveIntent(source, target)));
            var after = Snapshot(2, 1, source.WithPosition(target.Position));
            yield return presenter.ApplyStepsAsync(
                new[] { new MoveStep(MoveStepKind.Merge, new IBoardEffect[] { merge }) },
                after).ToCoroutine();

            presenter.ApplyStepsReversedAsync(
                new[] { new MoveStep(MoveStepKind.Merge, new IBoardEffect[] { merge }) },
                initial,
                Catalog(source, target)).Forget();
            yield return new WaitForSeconds(0.03f);
            presenter.Rebuild(initial);
            yield return new WaitForSeconds(0.2f);
            Assert.That(presenter.IsPresenting, Is.False);
            Assert.That(presenter.IsSynchronizedWith(initial), Is.True);
        }

        private static Dictionary<string, BlobState> Catalog(params BlobState[] blobs)
        {
            var catalog = new Dictionary<string, BlobState>();
            foreach (BlobState blob in blobs)
                catalog[blob.Id] = blob;
            return catalog;
        }

        private sealed class RecordingUndoFeedback : MonoBehaviour, IUndoPlaybackFeedback
        {
            public int PlayCount { get; private set; }

            public void PlayUndo()
            {
                PlayCount++;
            }
        }
    }
}
