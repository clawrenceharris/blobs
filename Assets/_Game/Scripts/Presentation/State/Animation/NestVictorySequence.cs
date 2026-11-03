using System;
using DG.Tweening;
using UnityEngine;
namespace Blobs.Presentation
{
    /// <summary>
    /// Finishing beat after a nest capture: the monster rises from behind the nest with a
    /// contented face, covering the eggs so the nest reads as a happy home.
    /// </summary>
    internal static class NestVictorySequence
    {
        public static Sequence Create(NestBlobView nest, BlobView monster, Action onFinished = null)
        {
            if (nest == null)
                throw new ArgumentNullException(nameof(nest));
            if (monster == null)
                throw new ArgumentNullException(nameof(monster));

            float duration = Mathf.Max(0.05f, nest.RiseDuration);
            Transform visual = monster.VisualRoot != null ? monster.VisualRoot : monster.transform;
            Sequence beat = DOTween.Sequence().SetLink(nest.gameObject, LinkBehaviour.KillOnDestroy);
            beat.AppendCallback(() =>
            {
                monster.gameObject.SetActive(true);
                nest.BeginOccupation(monster);
            });
            // Delay the rise tween until after BeginOccupation so it starts from the hidden pose.
            beat.AppendCallback(() => { });
            beat.Append(
                (monster.VisualRoot != null ? monster.VisualRoot : monster.transform).DOLocalMove(nest.SettledLocalOffset, duration)
                    .SetEase(Ease.OutBack));
            beat.AppendCallback(() => nest.ApplySettledPose());
            beat.OnComplete(() => onFinished?.Invoke());
            return beat;
        }
        public static void ApplyInstant(NestBlobView nest, BlobView monster)
        {
            if (nest == null || monster == null)
                return;
            monster.gameObject.SetActive(true);
            nest.BeginOccupation(monster);
            nest.ApplySettledPose();
        }
    }
}