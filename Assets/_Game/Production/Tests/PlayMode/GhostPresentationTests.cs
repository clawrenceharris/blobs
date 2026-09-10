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
    public sealed class GhostPresentationTests : PresentationTestFixture
    {
        [UnityTest]
        public IEnumerator GhostReturnFadesBodyOnceAndKeepsHaloVisible()
        {
            BlobState source = Blob("source", BlobColor.Red, 0, 0);
            var ghost = new BlobState("ghost", BlobType.Ghost, new GridPosition(3, 0));
            var initial = Snapshot(4, 1, source, ghost);
            BoardPresenter presenter = CreatePresenter(initial);
            presenter.TryGetBlobView("ghost", out BlobView view);
            SpriteRenderer body = AddFadeGroup(view, out SpriteRenderer halo);
            Vector3 start = view.transform.localPosition;
            BoardState board = initial.Board.Clone();
            MoveResult result = new MoveResolver().Resolve(board, new MoveIntent(source, ghost));
            var final = new GameSessionSnapshot("ghost", board, 1, false);

            presenter.ApplySteps(result.Steps, final);
            yield return WaitUntil(() => body.color.a < 0.01f);
            Assert.That(halo.color.a, Is.EqualTo(1f));
            yield return WaitUntil(() => view.transform.localPosition.x < start.x - 0.1f);
            Assert.That(body.color.a, Is.LessThan(0.01f));
            yield return WaitUntil(() => !presenter.IsPresenting);
            Assert.That(body.color.a, Is.EqualTo(0.6f).Within(0.001f));
            Assert.That(halo.color.a, Is.EqualTo(1f));
            Assert.That(presenter.TryGetBlobView("ghost", out BlobView survivor), Is.True);
            Assert.That(survivor, Is.SameAs(view));
            Assert.That(presenter.IsSynchronizedWith(final), Is.True);
        }

        [UnityTest]
        public IEnumerator RebuildDuringInvisibleReturnCancelsOldCompletion()
        {
            BlobState source = Blob("source", BlobColor.Red, 0, 0);
            var ghost = new BlobState("ghost", BlobType.Ghost, new GridPosition(3, 0));
            var initial = Snapshot(4, 1, source, ghost);
            BoardPresenter presenter = CreatePresenter(initial);
            presenter.TryGetBlobView("ghost", out BlobView view);
            SpriteRenderer body = AddFadeGroup(view, out _);
            BoardState board = initial.Board.Clone();
            var result = new MoveResolver().Resolve(board, new MoveIntent(source, ghost));
            int completed = 0;
            presenter.SnapshotChanged += _ => completed++;
            presenter.ApplySteps(result.Steps, new GameSessionSnapshot("ghost", board, 1, false));
            yield return WaitUntil(() => body.color.a < 0.01f);
            presenter.Rebuild(initial);
            yield return new WaitForSeconds(0.8f);
            Assert.That(presenter.IsPresenting, Is.False);
            Assert.That(presenter.IsSynchronizedWith(initial), Is.True);
            Assert.That(completed, Is.EqualTo(1), "Canceled playback must not publish its old snapshot.");
        }

        [UnityTest]
        public IEnumerator SigilReturnClearsOnlyAfterTravel()
        {
            BlobState source = Blob("source", BlobColor.Red, 0, 0);
            var ghost = new BlobState("ghost", BlobType.Ghost, new GridPosition(3, 0));
            var board = new BoardState(4, 1, new[] { source, ghost }, new[]
            {
                new TileState("sigil", new GridPosition(1, 0), TileType.Sigil)
            });
            // Use the normal test surface; tiles are tested independently by Core.
            var initial = Snapshot(4, 1, source, ghost);
            BoardPresenter presenter = CreatePresenter(initial);
            presenter.TryGetBlobView("ghost", out BlobView view);
            SpriteRenderer body = AddFadeGroup(view, out SpriteRenderer halo);
            var result = new MoveResolver().Resolve(board, new MoveIntent(source, ghost));
            var final = Snapshot(4, 1);
            presenter.ApplySteps(result.Steps, final);
            yield return WaitUntil(() => body != null && body.color.a < 0.01f);
            Assert.That(view != null, Is.True);
            Assert.That(halo.color.a, Is.EqualTo(1f));
            yield return WaitUntil(() => !presenter.IsPresenting);
            yield return null;
            Assert.That(view == null, Is.True);
            Assert.That(presenter.VisibleBlobCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator LandingAbsorptionKeepsIntermediateTrailViews()
        {
            var source = new BlobState("source", BlobType.Trail, new GridPosition(0, 0))
                .WithColor(BlobColor.Red).WithTrail(BlobColor.Blue);
            var ghost = new BlobState("ghost", BlobType.Ghost, new GridPosition(3, 0));
            var initial = Snapshot(4, 1, source, ghost);
            var presenter = CreatePresenter(initial);
            var board = initial.Board.Clone();
            var result = new MoveResolver().Resolve(board, new MoveIntent(source, ghost));
            var final = new GameSessionSnapshot("ghost", board, 1, false);
            yield return presenter.ApplyStepsAsync(result.Steps, final).ToCoroutine();
            Assert.That(presenter.IsSynchronizedWith(final), Is.True);
            Assert.That(presenter.VisibleBlobCount, Is.EqualTo(3));
        }

        [UnityTest]
        public IEnumerator DisabledPresenterAppliesGhostEffectsImmediately()
        {
            var source = Blob("source", BlobColor.Red, 0, 0);
            var ghost = new BlobState("ghost", BlobType.Ghost, new GridPosition(3, 0));
            var initial = Snapshot(4, 1, source, ghost);
            var presenter = CreatePresenter(initial);
            presenter.enabled = false;
            var board = initial.Board.Clone();
            var result = new MoveResolver().Resolve(board, new MoveIntent(source, ghost));
            var final = new GameSessionSnapshot("ghost", board, 1, false);
            var playback = presenter.ApplyStepsAsync(result.Steps, final);
            Assert.That(playback.Status, Is.EqualTo(UniTaskStatus.Succeeded));
            Assert.That(presenter.IsSynchronizedWith(final), Is.True);
            yield return playback.ToCoroutine();
        }
    }
}
