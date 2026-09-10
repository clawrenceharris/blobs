using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using Blobs.Application;
using Blobs.Content;
using Blobs.Core;
using Blobs.Input;
using Blobs.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Blobs.Tests.PlayMode
{
    public sealed class BoardPlaybackTests : PresentationTestFixture
    {
        [UnityTest]
        public IEnumerator MoveStepBeatsRunSequentially()
        {
            BoardPresenter presenter = CreatePresenter(EmptySnapshot());
            var events = new List<string>();
            presenter.RegisterMoveStepHandler(new RecordingStepHandler(events));

            presenter.ApplySteps(
                new[]
                {
                    new MoveStep(MoveStepKind.Traverse, Array.Empty<IBoardEffect>()),
                    new MoveStep(MoveStepKind.Traverse, Array.Empty<IBoardEffect>())
                },
                EmptySnapshot());

            yield return WaitUntil(() => events.Count == 4);

            CollectionAssert.AreEqual(
                new[] { "start-1", "end-1", "start-2", "end-2" },
                events);
        }

        [UnityTest]
        public IEnumerator RebuildDuringMergeCleansUpInterruptedViewsAndAnimationState()
        {
            BlobState source = Blob("source", BlobColor.Red, 0, 0);
            BlobState target = Blob("target", BlobColor.Blue, 1, 0);
            GameSessionSnapshot initial = Snapshot(2, 1, source, target);
            BoardPresenter presenter = CreatePresenter(initial);
            Assert.That(presenter.TryGetBlobView(source.Id, out BlobView oldSource), Is.True);
            Assert.That(presenter.TryGetBlobView(target.Id, out BlobView oldTarget), Is.True);

            presenter.ApplySteps(
                new[]
                {
                    new MoveStep(
                        MoveStepKind.Merge,
                        new IBoardEffect[]
                        {
                            MergeEffect.NormalMerge(new MoveContext(
                                initial.Board, MovePlan.Move(source, target), source, target))
                        })
                },
                Snapshot(2, 1, source.WithPosition(target.Position)));

            yield return new WaitForSeconds(0.03f);
            Assert.That(oldSource.BlobMotionAnimator.CurrentState, Is.EqualTo(BlobAnimationState.Merging));

            presenter.Rebuild(initial);
            yield return null;

            Assert.That(oldSource == null, Is.True);
            Assert.That(oldTarget == null, Is.True);
            Assert.That(presenter.TryGetBlobView(source.Id, out BlobView restoredSource), Is.True);
            Assert.That(presenter.TryGetBlobView(target.Id, out BlobView restoredTarget), Is.True);
            Assert.That(restoredSource.GridPosition, Is.EqualTo(source.Position));
            Assert.That(restoredTarget.GridPosition, Is.EqualTo(target.Position));
            Assert.That(
                restoredSource.BlobMotionAnimator.CurrentState,
                Is.EqualTo(BlobAnimationState.Idle));
            Assert.That(presenter.IsSynchronizedWith(initial), Is.True);
        }

        [UnityTest]
        public IEnumerator AwaitedPlaybackCancellationRestoresCommittedSnapshot()
        {
            var source = Blob("source", BlobColor.Red, 0, 0);
            var ghost = new BlobState("ghost", BlobType.Ghost, new GridPosition(3, 0));
            var initial = Snapshot(4, 1, source, ghost);
            BoardPresenter presenter = CreatePresenter(initial);
            BoardState board = initial.Board.Clone();
            var result = new MoveResolver().Resolve(board, new MoveIntent(source, ghost));
            var final = new GameSessionSnapshot("ghost", board, 1, false);
            using var cancellation = new CancellationTokenSource();
            var task = presenter.ApplyStepsAsync(result.Steps, final,
                cancellationToken: cancellation.Token).SuppressCancellationThrow();
            cancellation.Cancel();
            yield return task.ToCoroutine(wasCanceled => Assert.That(wasCanceled, Is.True));
            Assert.That(presenter.IsPresenting, Is.False);
            Assert.That(presenter.IsSynchronizedWith(final), Is.True);
        }
    }
}
