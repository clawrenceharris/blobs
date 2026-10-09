using System.Collections;
using Blobs.Core;
using Blobs.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Blobs.Tests.PlayMode
{
    public sealed class BlobSizePresentationTests : PresentationTestFixture
    {
        [UnityTest]
        public IEnumerator SmallMergeGrowthPersistsThroughTheNextMergeStateCycle()
        {
            BlobState source = Blob("source", BlobColor.Red, 0, 0).WithSize(BlobSize.Small);
            BlobState target = Blob("target", BlobColor.Blue, 1, 0).WithSize(BlobSize.Small);
            BlobState nextTarget = Blob("next-target", BlobColor.Green, 2, 0)
                .WithSize(BlobSize.Small);
            var initial = Snapshot(3, 1, source, target, nextTarget);
            BoardPresenter presenter = CreatePresenter(initial);
            presenter.TryGetBlobView(source.Id, out BlobView sourceView);
            float smallScale = sourceView.BaseVisualScale.x;

            var context = new MoveContext(
                initial.Board,
                MovePlan.Default(source, target),
                source,
                target,
                new MoveIntent(source, target));
            var steps = new[]
            {
                new MoveStep(
                    MoveStepKind.Merge,
                    new IBoardEffect[]
                    {
                        MergeEffect.NormalMerge(context),
                        new ChangeBlobSizeEffect(source.Id, BlobSize.Small, BlobSize.Normal)
                    })
            };
            BlobState grown = source.WithPosition(target.Position).WithSize(BlobSize.Normal);
            var grownSnapshot = Snapshot(3, 1, grown, nextTarget);

            presenter.ApplySteps(steps, grownSnapshot);
            // Merge finishes at the small rest scale; growth is a separate aftermath beat.
            yield return WaitUntil(() =>
                presenter.TryGetBlobView(source.Id, out BlobView growing) &&
                growing.VisualRoot.localScale.x > smallScale * 1.05f);
            Assert.That(presenter.IsPresenting, Is.True);
            yield return WaitUntil(() => !presenter.IsPresenting);

            Assert.That(presenter.TryGetBlobView(source.Id, out BlobView survivor), Is.True);
            Assert.That(survivor.BaseVisualScale.x, Is.GreaterThan(smallScale));
            Assert.That(survivor.VisualRoot.localScale, Is.EqualTo(survivor.BaseVisualScale));
            Assert.That(presenter.IsSynchronizedWith(grownSnapshot), Is.True);

            var secondContext = new MoveContext(
                grownSnapshot.Board,
                MovePlan.Default(grown, nextTarget),
                grown,
                nextTarget,
                new MoveIntent(grown, nextTarget));
            BlobState afterSecondMerge = grown.WithPosition(nextTarget.Position);
            var final = Snapshot(3, 1, afterSecondMerge);

            presenter.ApplySteps(
                new[]
                {
                    new MoveStep(
                        MoveStepKind.Merge,
                        new IBoardEffect[] { MergeEffect.NormalMerge(secondContext) })
                },
                final);
            yield return WaitUntil(() => !presenter.IsPresenting);

            Assert.That(presenter.TryGetBlobView(source.Id, out survivor), Is.True);
            Assert.That(survivor.BaseVisualScale.x, Is.GreaterThan(smallScale));
            Assert.That(survivor.VisualRoot.localScale, Is.EqualTo(survivor.BaseVisualScale));
            Assert.That(presenter.IsSynchronizedWith(final), Is.True);
        }
    }
}
