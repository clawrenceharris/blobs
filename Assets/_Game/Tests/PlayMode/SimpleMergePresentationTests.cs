using System.Collections;
using Blobs.Core;
using Blobs.Presentation;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Blobs.Tests.PlayMode
{
    public sealed class SimpleMergePresentationTests : PresentationTestFixture
    {
        [TestCase(true)]
        [TestCase(false)]
        public void SimpleMergeSlidesBehindThenGrowsOverTarget(bool sourceSurvives)
        {
            var initial = Snapshot(2, 1, Blob("source", BlobColor.Red, 0, 0), Blob("target", BlobColor.Blue, 1, 0));
            var presenter = CreatePresenter(initial);
            presenter.TryGetBlobView("source", out BlobView source);
            presenter.TryGetBlobView("target", out BlobView target);
            var orchestrator = CreateGameObject("Simple Merge").AddComponent<SimpleMergeAnimationOrchestrator>();
            BlobView survivor = sourceSurvives ? source : target;
            BlobView consumed = sourceSurvives ? target : source;
            Color resultColor = survivor.MergeEffectColor;
            int contacts = 0;
            int completions = 0;
            int originalOrder = source.SortingGroup.sortingOrder;
            Sequence beat = orchestrator.CreateMergeBeat(source, target, sourceSurvives, Vector2Int.right,
                () => contacts++, () => completions++, null).Pause().SetAutoKill(false);
            try
            {
                Assert.That(source.BlobMotionAnimator.CurrentState, Is.Not.EqualTo(BlobAnimationState.Merging));
                Assert.That(beat.Duration(), Is.EqualTo(sourceSurvives ? 0.39f : 0.27f).Within(0.001f));
                beat.Goto(0.075f);
                Assert.That(source.transform.localPosition.x, Is.EqualTo(0.5f).Within(0.001f));
                Assert.That(source.gameObject.activeSelf && target.gameObject.activeSelf, Is.True);
                Assert.That(source.VisualRoot.localScale, Is.EqualTo(source.BaseVisualScale));
                Assert.That(target.VisualRoot.localScale, Is.EqualTo(target.BaseVisualScale));
                Assert.That(source.SortingGroup.sortingOrder, Is.LessThan(target.SortingGroup.sortingOrder));
                Assert.That(contacts, Is.Zero);

                beat.Goto(0.1501f);
                Assert.That(source.transform.position, Is.EqualTo(target.transform.position));
                Assert.That(consumed.gameObject.activeSelf, Is.EqualTo(sourceSurvives));
                Assert.That(survivor.gameObject.activeSelf, Is.True);
                Assert.That(survivor.MergeEffectColor, Is.EqualTo(resultColor));
                Assert.That(contacts, Is.EqualTo(1));
                Assert.That(completions, Is.Zero);

                if (sourceSurvives)
                {
                    Assert.That(source.VisualRoot.localScale.x, Is.LessThan(0.001f));
                    Assert.That(source.SortingGroup.sortingOrder, Is.GreaterThan(target.SortingGroup.sortingOrder));
                    beat.Goto(0.21f);
                    Assert.That(target.gameObject.activeSelf, Is.True);
                    Assert.That(source.VisualRoot.localScale.x,
                        Is.EqualTo(source.BaseVisualScale.x * 0.5f).Within(0.001f));
                }
                float pulseStart = sourceSurvives ? 0.27f : 0.15f;
                beat.Goto(pulseStart + 0.06f);
                Assert.That(consumed.gameObject.activeSelf, Is.False);
                Assert.That(survivor.VisualRoot.localScale.x,
                    Is.EqualTo(survivor.BaseVisualScale.x * 1.15f).Within(0.001f));
                Assert.That(completions, Is.Zero);
                beat.Goto(beat.Duration());
                Assert.That(consumed.gameObject.activeSelf, Is.False);
                Assert.That(survivor.VisualRoot.localScale, Is.EqualTo(survivor.BaseVisualScale));
                Assert.That(source.SortingGroup.sortingOrder, Is.EqualTo(originalOrder));
                Assert.That(contacts, Is.EqualTo(1));
                Assert.That(completions, Is.EqualTo(1));
            }
            finally { beat.Kill(); }
        }

        [UnityTest]
        public IEnumerator AssignedSimpleOrchestratorSupportsBothSurvivorsAndChainedMerges()
        {
            foreach (bool reverse in new[] { false, true })
            {
                var source = Blob("source", BlobColor.Red, 0, 0);
                var target = Blob("target", BlobColor.Blue, 1, 0);
                var last = Blob("last", BlobColor.Green, 2, 0);
                var initial = Snapshot(3, 1, source, target, last);
                var orchestrator = CreateGameObject("Assigned Simple Merge").AddComponent<SimpleMergeAnimationOrchestrator>();
                var presenter = CreatePresenter(initial, mergeOrchestrator: orchestrator);
                BlobState survivor = reverse ? target : source.WithPosition(target.Position);
                presenter.TryGetBlobView(survivor.Id, out BlobView originalSurvivor);
                var first = new MergeEffect(source.Id, source.Id, target.Id,
                    reverse ? MergeSurvivor.TargetBlob : MergeSurvivor.MovingBlob,
                    source.Position, target.Position, reverse ? source : target);
                var second = new MergeEffect(survivor.Id, survivor.Id, last.Id, MergeSurvivor.MovingBlob,
                    target.Position, last.Position, last);
                var expected = Snapshot(3, 1, survivor.WithPosition(last.Position));

                yield return presenter.ApplyStepsAsync(new[]
                {
                    new MoveStep(MoveStepKind.Merge, new IBoardEffect[] { first }),
                    new MoveStep(MoveStepKind.Merge, new IBoardEffect[] { second })
                }, expected).ToCoroutine();
                yield return null;

                Assert.That(presenter.IsSynchronizedWith(expected), Is.True);
                Assert.That(presenter.TryGetBlobView(survivor.Id, out BlobView survivorView), Is.True);
                Assert.That(survivorView, Is.SameAs(originalSurvivor));
                Assert.That(presenter.GetComponentsInChildren<BlobView>(true).Length, Is.EqualTo(1));
            }
        }

        [UnityTest]
        public IEnumerator RebuildDuringSimpleGrowthCancelsPlaybackAndRestoresBoard()
        {
            var source = Blob("source", BlobColor.Red, 0, 0);
            var target = Blob("target", BlobColor.Blue, 1, 0);
            var initial = Snapshot(2, 1, source, target);
            var orchestrator = CreateGameObject("Interruptible Simple Merge").AddComponent<SimpleMergeAnimationOrchestrator>();
            SetPrivateField(orchestrator, "scaleDuration", 0.5f);
            var presenter = CreatePresenter(initial, mergeOrchestrator: orchestrator);
            presenter.TryGetBlobView(source.Id, out BlobView sourceView);
            var merge = new MergeEffect(source.Id, source.Id, target.Id, MergeSurvivor.MovingBlob,
                source.Position, target.Position, target);
            presenter.ApplySteps(new[] { new MoveStep(MoveStepKind.Merge, new IBoardEffect[] { merge }) },
                Snapshot(2, 1, source.WithPosition(target.Position)));
            yield return WaitUntil(() => sourceView.VisualRoot.localScale.x < sourceView.BaseVisualScale.x * 0.8f);
            presenter.Rebuild(initial);
            yield return new WaitForSeconds(0.6f);
            Assert.That(presenter.IsPresenting, Is.False);
            Assert.That(presenter.IsSynchronizedWith(initial), Is.True);
            Assert.That(presenter.GetComponentsInChildren<BlobView>(true).Length, Is.EqualTo(2));
            LogAssert.NoUnexpectedReceived();
        }
    }
}
