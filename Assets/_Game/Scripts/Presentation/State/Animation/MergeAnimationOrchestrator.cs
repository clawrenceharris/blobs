using System;
using System.Collections.Generic;
using Blobs.Content;
using Blobs.Core;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;

namespace Blobs.Presentation
{
    /// <summary>
    /// Builds normal-merge deformation and broadcasts merge choreography cues. Each beat captures
    /// its supplied settings so queued interactions do not share mutable presenter settings.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MergeAnimationOrchestrator : MergeAnimationOrchestratorBase
    {
        private readonly List<IMergeImpactFeedback> _impactFeedbackChannels = new();

        /// <summary>
        /// Transforms, captured base state, and authored settings shared by every stage of one beat.
        /// </summary>
        private readonly struct MergeBeat
        {
            public MergeBeat(
                BlobView source,
                BlobView target,
                bool sourceSurvives,
                Vector3 direction,
                BlobMergeImpactSettings settings)
            {
                Transform sourceVisual = source.VisualRoot != null ? source.VisualRoot : source.transform;
                Transform targetVisual = target.VisualRoot != null ? target.VisualRoot : target.transform;

                Source = source;
                Target = target;
                Survivor = sourceSurvives ? source : target;
                Consumed = sourceSurvives ? target : source;
                Direction = direction;
                Settings = settings;
                SourceVisual = sourceVisual;
                TargetVisual = targetVisual;
                SourceBaseScale = source.BaseVisualScale;
                TargetBaseScale = target.BaseVisualScale;
                SourceBasePosition = sourceVisual.localPosition;
                TargetBasePosition = targetVisual.localPosition;
                DestinationPoint = target.transform.position;
                CalculateContactPose(
                    source,
                    target,
                    direction,
                    out Vector3 sourceContactPosition,
                    out Vector3 contactPoint,
                    out float approachProgress);
                SourceContactPosition = sourceContactPosition;
                ContactPoint = contactPoint;
                ApproachProgress = approachProgress;
            }

            public BlobView Source { get; }
            public BlobView Target { get; }
            public BlobView Survivor { get; }
            public BlobView Consumed { get; }
            public Vector3 Direction { get; }
            public BlobMergeImpactSettings Settings { get; }
            public Transform SourceVisual { get; }
            public Transform TargetVisual { get; }
            public Vector3 SourceBaseScale { get; }
            public Vector3 TargetBaseScale { get; }
            public Vector3 SourceBasePosition { get; }
            public Vector3 TargetBasePosition { get; }
            public Vector3 SourceContactPosition { get; }
            public Vector3 ContactPoint { get; }
            public Vector3 DestinationPoint { get; }
            public float ApproachProgress { get; }
        }

        /// <summary>
        /// Creates a self-contained merge beat. State changes are callbacks inside the
        /// sequence, so queued chain merges do not enter Merging before their visual turn.
        /// </summary>
        public override Sequence CreateMergeBeat(
            BlobView source,
            BlobView target,
            bool sourceSurvives,
            Vector2Int gridDirection,
            Action onContact,
            Action onTargetConsumed,
            BlobMergeImpactSettings settings,
            GridPosition? destination = null
            )
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            Vector3 direction = new(gridDirection.x, gridDirection.y, 0f);
            if (direction.sqrMagnitude < 0.001f)
                direction = Vector3.right;
            direction.Normalize();

            var beatContext = new MergeBeat(source, target, sourceSurvives, direction, settings);

            SortingGroup sourceSorting = source.SortingGroup;
            SortingGroup targetSorting = target.SortingGroup;
            int sourceOriginalLayer = sourceSorting != null
                ? sourceSorting.sortingLayerID
                : 0;
            int targetOriginalLayer = targetSorting != null
                ? targetSorting.sortingLayerID
                : 0;
            int sourceOriginalOrder = sourceSorting != null
                ? sourceSorting.sortingOrder
                : 0;
            int targetOriginalOrder = targetSorting != null
                ? targetSorting.sortingOrder
                : 0;
            int mergeBaseOrder = Mathf.Max(sourceOriginalOrder, targetOriginalOrder);

            Sequence beat = DOTween.Sequence();
            beat.AppendCallback(() =>
            {
                source.BlobMotionAnimator?.SetMerging();
                target.BlobMotionAnimator?.SetMerging();
                beatContext.SourceVisual.localPosition = beatContext.SourceBasePosition;
                beatContext.TargetVisual.localPosition = beatContext.TargetBasePosition;
                beatContext.SourceVisual.localScale = beatContext.SourceBaseScale;
                beatContext.TargetVisual.localScale = beatContext.TargetBaseScale;

                if (sourceSorting != null)
                    sourceSorting.sortingOrder = mergeBaseOrder + settings.SourceSortingOffset;
                if (targetSorting != null)
                    targetSorting.sortingOrder = mergeBaseOrder + settings.TargetSortingOffset;
            });

            AppendAnticipation(beat, beatContext);
            AppendApproach(beat, beatContext);
            AppendContact(beat, beatContext, onContact);
            AppendAbsorption(beat, beatContext, destination);
            AppendRecovery(beat, beatContext);

            beat.OnComplete(() =>
            {
                RestoreSorting(
                    sourceSorting,
                    sourceOriginalLayer,
                    sourceOriginalOrder,
                    targetSorting,
                    targetOriginalLayer,
                    targetOriginalOrder);
                beatContext.SourceVisual.localPosition = beatContext.SourceBasePosition;
                beatContext.SourceVisual.localScale = beatContext.SourceBaseScale;
                beatContext.Survivor.BlobMotionAnimator?.SetIdle();
                onTargetConsumed?.Invoke();
            });
            beat.OnKill(() => RestoreSorting(
                sourceSorting,
                sourceOriginalLayer,
                sourceOriginalOrder,
                targetSorting,
                targetOriginalLayer,
                targetOriginalOrder));
            return beat;
        }

        /// <summary>Source winds up while the target braces for the incoming blob.</summary>
        private void AppendAnticipation(Sequence beat, MergeBeat context)
        {
            BlobMergeImpactSettings settings = context.Settings;
            Vector2 anticipationMultipliers = DirectionalScale(settings.AnticipationScale, context.Direction);
            Vector2 braceMultipliers = DirectionalScale(settings.TargetBraceScale, context.Direction);

            beat.AppendCallback(() => BeginAnticipation(CreateImpactContext(
                context.Source,
                context.Target,
                context.Direction,
                worldPosition: context.ContactPoint)));

            beat.Append(context.SourceVisual
                .DOLocalMove(context.SourceBasePosition - context.Direction * settings.AnticipationBackstep,
                    settings.AnticipationDuration)
                .SetEase(Ease.OutQuad));
            beat.Join(context.SourceVisual
                .DOScale(Multiply(context.SourceBaseScale, anticipationMultipliers),
                    settings.AnticipationDuration)
                .SetEase(Ease.OutQuad));
            beat.Join(context.TargetVisual
                .DOScale(Multiply(context.TargetBaseScale, braceMultipliers),
                    settings.AnticipationDuration)
                .SetEase(Ease.OutQuad));
        }

        /// <summary>Source accelerates only as far as the first visible silhouette contact.</summary>
        private void AppendApproach(Sequence beat, MergeBeat context)
        {
            BlobMergeImpactSettings settings = context.Settings;
            Vector2 travelMultipliers = DirectionalScale(settings.TravelScale, context.Direction);
            float approachDuration = ApproachDuration(settings.TravelDuration, context.ApproachProgress);

            beat.Append(context.Source.transform
                .DOMove(context.SourceContactPosition, approachDuration)
                .SetEase(Ease.InQuad)
                .SetLink(context.Source.gameObject, LinkBehaviour.KillOnDestroy));
            beat.Join(context.SourceVisual
                .DOScale(Multiply(context.SourceBaseScale, travelMultipliers),
                    approachDuration)
                .SetEase(Ease.InOutQuad));
            beat.Join(context.SourceVisual
                .DOLocalMove(context.SourceBasePosition, approachDuration)
                .SetEase(Ease.OutQuad));
        }

        /// <summary>
        /// Both blobs hold a shared compression at the moment of contact, so the impact reads
        /// as a collision rather than an instant hand-off into absorption.
        /// </summary>
        private void AppendContact(Sequence beat, MergeBeat context, Action onContact)
        {
            BlobMergeImpactSettings settings = context.Settings;
            Vector2 contactMultipliers = DirectionalScale(settings.ContactScale, context.Direction);

            beat.AppendCallback(() =>
            {
                PlayImpact(CreateImpactContext(
                    context.Source,
                    context.Target,
                    context.Direction,
                    worldPosition: context.ContactPoint,
                    destinationWorldPosition: context.DestinationPoint));
                onContact?.Invoke();
            });

            beat.Append(context.SourceVisual
                .DOScale(Multiply(context.SourceBaseScale, contactMultipliers),
                    settings.ContactDuration)
                .SetEase(Ease.OutQuad));
            beat.Join(context.TargetVisual
                .DOScale(Multiply(context.TargetBaseScale, contactMultipliers),
                    settings.ContactDuration)
                .SetEase(Ease.OutQuad));
        }

        /// <summary>The consumed blob collapses while the survivor inflates along the merge axis.</summary>
        private void AppendAbsorption(
            Sequence beat,
            MergeBeat context,
            GridPosition? destination)
        {
            BlobMergeImpactSettings settings = context.Settings;
            Vector2 survivorMultipliers = DirectionalScale(settings.SurvivorImpactScale, context.Direction);
            float approachDuration = ApproachDuration(settings.TravelDuration, context.ApproachProgress);
            float absorptionDuration = Mathf.Max(0.01f, settings.TravelDuration - approachDuration);
            float beatDuration = Mathf.Max(absorptionDuration, settings.ConsumeDuration);
            Transform consumedVisual = context.Consumed.VisualRoot != null
                ? context.Consumed.VisualRoot
                : context.Consumed.transform;
            Transform survivorVisual = context.Survivor.VisualRoot != null
                ? context.Survivor.VisualRoot
                : context.Survivor.transform;

            beat.Append(context.Source.AnimateMoveTo(
                destination ?? context.Target.GridPosition,
                absorptionDuration,
                Ease.OutQuad));
            beat.Join(consumedVisual
                .DOScale(Vector3.zero, settings.ConsumeDuration)
                .SetEase(Ease.InBack));
            beat.Join(survivorVisual
                .DOScale(Multiply(context.Survivor.BaseVisualScale, survivorMultipliers), beatDuration)
                .SetEase(Ease.OutQuad));
        }

        /// <summary>The survivor rebounds back to its resting shape and reports the settle cue.</summary>
        private void AppendRecovery(Sequence beat, MergeBeat context)
        {
            BlobMergeImpactSettings settings = context.Settings;

            Transform survivorVisual = context.Survivor.VisualRoot != null
                ? context.Survivor.VisualRoot
                : context.Survivor.transform;
            Vector3 survivorBasePosition = context.Survivor == context.Source
                ? context.SourceBasePosition
                : context.TargetBasePosition;

            beat.Append(survivorVisual
                .DOScale(context.Survivor.BaseVisualScale, settings.SettleDuration)
                .SetEase(Ease.OutBack));
            beat.Join(survivorVisual
                .DOLocalMove(survivorBasePosition, settings.SettleDuration)
                .SetEase(Ease.OutQuad));
            beat.AppendCallback(() => PlaySettled(CreateImpactContext(
                context.Source,
                context.Target,
                context.Direction,
                worldPosition: context.ContactPoint,
                destinationWorldPosition: context.DestinationPoint)));
        }

        /// <summary>
        /// Builds the presentation-only cue payload shared by every feedback channel.
        /// </summary>
        public MergeImpactFeedbackContext CreateImpactContext(
            BlobView source,
            BlobView target,
            Vector2 direction,
            float intensity = 1f,
            Vector3? worldPosition = null,
            Vector3? destinationWorldPosition = null)
        {
            Color sourceColor = source != null ? source.MergeEffectColor : Color.white;
            Color targetColor = target != null ? target.MergeEffectColor : sourceColor;
            Skin sourceSkin = source != null ? source.MergeEffectSkin : new Skin(sourceColor, sourceColor, sourceColor);
            Skin targetSkin = target != null ? target.MergeEffectSkin : sourceSkin;

            return new MergeImpactFeedbackContext(
                worldPosition ?? Midpoint(source, target),
                direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.right,
                sourceColor,
                targetColor,
                Color.Lerp(sourceColor, targetColor, 0.5f),
                source,
                target,
                intensity,
                sourceSkin,
                targetSkin,
                BlendSkins(sourceSkin, targetSkin),
                destinationWorldPosition ?? (target != null ? target.transform.position : worldPosition));
        }

        private static float ApproachDuration(float travelDuration, float approachProgress)
        {
            // The original travel used Ease.InQuad. sqrt preserves approximately the same
            // point in time at which that curve would reach the geometric contact distance.
            return Mathf.Max(
                0.01f,
                travelDuration * Mathf.Sqrt(Mathf.Clamp01(approachProgress)));
        }

        private static void CalculateContactPose(
            BlobView source,
            BlobView target,
            Vector3 direction,
            out Vector3 sourceContactPosition,
            out Vector3 contactPoint,
            out float approachProgress)
        {
            Vector3 sourcePosition = source.transform.position;
            Vector3 targetPosition = target.transform.position;
            float distance = Vector3.Dot(targetPosition - sourcePosition, direction);

            if (distance <= 0.001f)
            {
                sourceContactPosition = targetPosition;
                contactPoint = targetPosition;
                approachProgress = 1f;
                return;
            }

            float fallbackRadius = distance * 0.4f;
            float sourceRadius = ProjectedBodyRadius(source, direction, fallbackRadius);
            float targetRadius = ProjectedBodyRadius(target, direction, fallbackRadius);
            float gap = Mathf.Max(0f, distance - sourceRadius - targetRadius);

            sourceContactPosition = sourcePosition + direction * gap;
            contactPoint = targetPosition - direction * targetRadius;
            approachProgress = Mathf.Clamp01(gap / distance);
        }

        private static float ProjectedBodyRadius(
            BlobView blob,
            Vector3 direction,
            float fallback)
        {
            SpriteRenderer body = blob.BlobRenderer != null
                ? blob.BlobRenderer.BaseRenderer
                : blob.GetComponent<BlobRenderer>()?.BaseRenderer;
            if (body == null || body.sprite == null)
                return fallback;

            Vector3 extents = body.bounds.extents;
            return Mathf.Abs(direction.x) * extents.x + Mathf.Abs(direction.y) * extents.y;
        }

        private static Skin BlendSkins(Skin a, Skin b)
        {
            return new Skin(
                Color.Lerp(a.BaseColor, b.BaseColor, 0.5f),
                Color.Lerp(a.ShadowColor, b.ShadowColor, 0.5f),
                Color.Lerp(a.HighlightColor, b.HighlightColor, 0.5f));
        }

        /// <summary>Broadcasts the wind-up cue to every enabled feedback channel on this object.</summary>
        public void BeginAnticipation(MergeImpactFeedbackContext context)
        {
            Broadcast(context, static (channel, cue) => channel.BeginAnticipation(cue));
        }

        /// <summary>Broadcasts the contact cue to every enabled feedback channel on this object.</summary>
        public void PlayImpact(MergeImpactFeedbackContext context)
        {
            Broadcast(context, static (channel, cue) => channel.PlayImpact(cue));
        }

        /// <summary>Broadcasts the settle cue to every enabled feedback channel on this object.</summary>
        public void PlaySettled(MergeImpactFeedbackContext context)
        {
            Broadcast(context, static (channel, cue) => channel.PlaySettled(cue));
        }

        private void Broadcast(
            MergeImpactFeedbackContext context,
            Action<IMergeImpactFeedback, MergeImpactFeedbackContext> cue)
        {
            if (_impactFeedbackChannels.Count == 0)
                RefreshImpactFeedbackChannels();

            foreach (IMergeImpactFeedback channel in _impactFeedbackChannels)
            {
                if (channel is Behaviour behaviour && !behaviour.isActiveAndEnabled)
                    continue;

                cue(channel, context);
            }
        }

        /// <summary>
        /// Refreshes prefab-composed feedback channels after runtime composition changes.
        /// </summary>
        public void RefreshImpactFeedbackChannels()
        {
            _impactFeedbackChannels.Clear();
            foreach (MonoBehaviour component in GetComponents<MonoBehaviour>())
            {
                if (component is IMergeImpactFeedback feedback)
                    _impactFeedbackChannels.Add(feedback);
            }
        }

        private void Awake()
        {
            RefreshImpactFeedbackChannels();
        }

        private void OnEnable()
        {
            RefreshImpactFeedbackChannels();
        }

        private void OnValidate()
        {
            RefreshImpactFeedbackChannels();
        }

        private static Vector3 Midpoint(BlobView source, BlobView target)
        {
            if (source != null && target != null)
                return Vector3.Lerp(source.transform.position, target.transform.position, 0.5f);
            if (target != null)
                return target.transform.position;
            if (source != null)
                return source.transform.position;
            return Vector3.zero;
        }

        private static Vector2 DirectionalScale(Vector2 scale, Vector3 direction)
        {
            return Mathf.Abs(direction.x) >= Mathf.Abs(direction.y)
                ? new Vector2(scale.y, scale.x)
                : scale;
        }

        private static Vector3 Multiply(Vector3 baseScale, Vector2 multiplier)
        {
            return new Vector3(
                baseScale.x * multiplier.x,
                baseScale.y * multiplier.y,
                baseScale.z);
        }

        private static void RestoreSorting(
            SortingGroup source,
            int sourceLayer,
            int sourceOrder,
            SortingGroup target,
            int targetLayer,
            int targetOrder)
        {
            if (source != null)
            {
                source.sortingLayerID = sourceLayer;
                source.sortingOrder = sourceOrder;
            }

            if (target != null)
            {
                target.sortingLayerID = targetLayer;
                target.sortingOrder = targetOrder;
            }
        }

    }
}
