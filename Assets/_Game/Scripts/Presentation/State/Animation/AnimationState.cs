using System.Collections;
using Blobs.Content;
using DG.Tweening;
using UnityEngine;

namespace Blobs.Presentation
{

    public abstract class AnimationState : IState<AnimationStateContext>
    {
        public abstract BlobAnimationState State { get; }
        public abstract void Enter(AnimationStateContext context);
        public abstract void Exit(AnimationStateContext context);
        public abstract IEnumerator Update(AnimationStateContext context);

        protected static Transform VisualTransform(AnimationStateContext context)
        {
            return context.BlobView.VisualRoot != null
                ? context.BlobView.VisualRoot
                : context.BlobView.transform;
        }

        protected static BlobAnimationSettingsAsset AnimationSettings(
            AnimationStateContext context)
        {
            return context.BlobAnimationSettings != null
                ? context.BlobAnimationSettings
                : null;
        }
    }

    public class BlobSelectedState : AnimationState
    {
        public override BlobAnimationState State => BlobAnimationState.Selected;
        private Sequence _selectionSequence;
        private AnimationStateContext _context;

        public override void Enter(AnimationStateContext context)
        {
            _context = context;
            StartSelectionLoop(context.BaseScale);
        }
        public override void Exit(AnimationStateContext context)
        {
            StopSelectionLoop();
        }
        public override IEnumerator Update(AnimationStateContext context)
        {
            yield return null;
        }
        private Sequence StartSelectionLoop(Vector3 pulseBaseScale)
        {
            BlobSelectionSettings settings = AnimationSettings(_context)?.BlobSelectionSettings;
            if (settings == null)
                return null;

            StopSelectionLoop();

            Transform visual = VisualTransform(_context);
            Vector3 squishScale = new Vector3(
                pulseBaseScale.x * settings.SelectionStretchAmount,
                pulseBaseScale.y * settings.SelectionSquishAmount,
                pulseBaseScale.z);

            _selectionSequence = DOTween.Sequence()
                .SetLink(_context.BlobView.gameObject, LinkBehaviour.KillOnDestroy);
            _selectionSequence.Append(visual.DOScale(squishScale, settings.SelectionSquishDuration)
                .SetEase(Ease.OutQuad)
                .SetLink(_context.BlobView.gameObject, LinkBehaviour.KillOnDestroy));
            _selectionSequence.Append(visual.DOScale(pulseBaseScale, settings.SelectionSquishDuration)
                .SetEase(Ease.InQuad)
                .SetLink(_context.BlobView.gameObject, LinkBehaviour.KillOnDestroy));
            return _selectionSequence.SetLoops(-1, LoopType.Restart);
        }
        private void StopSelectionLoop()
        {
            if (_selectionSequence != null)
            {
                _selectionSequence.Kill();
                VisualTransform(_context).localScale = _context.BaseScale;
            }

            _selectionSequence = null;
        }

    }

    /// <summary>
    /// Source-blob hold pose: lifts and scales the visual so a finger press does not fully cover it,
    /// then keeps the normal selection pulse around that elevated resting scale.
    /// </summary>
    public class BlobPickedUpState : AnimationState
    {
        public override BlobAnimationState State => BlobAnimationState.PickedUp;

        private Sequence _pickUpSequence;
        private AnimationStateContext _context;
        private Vector3 _heldScale;

        public override void Enter(AnimationStateContext context)
        {
            _context = context;
            BlobSelectionSettings settings = AnimationSettings(context)?.BlobSelectionSettings;
            if (settings == null)
            {
                RestoreRestPose();
                return;
            }

            Transform visual = VisualTransform(context);
            _heldScale = context.BaseScale * settings.PickUpScale;
            Vector3 heldPosition = context.BaseLocalPosition + Vector3.up * settings.PickUpLift;

            _pickUpSequence?.Kill();
            _pickUpSequence = DOTween.Sequence()
                .SetLink(context.BlobView.gameObject, LinkBehaviour.KillOnDestroy)
                .Append(visual.DOScale(_heldScale, settings.PickUpDuration)
                    .SetEase(Ease.OutBack)
                    .SetLink(context.BlobView.gameObject, LinkBehaviour.KillOnDestroy))
                .Join(visual.DOLocalMove(heldPosition, settings.PickUpDuration)
                    .SetEase(Ease.OutCubic)
                    .SetLink(context.BlobView.gameObject, LinkBehaviour.KillOnDestroy))
                .OnComplete(StartHeldPulse);
        }

        public override void Exit(AnimationStateContext context)
        {
            _context = context;
            StopHeldAnimation();
            RestoreRestPose();
        }

        public override IEnumerator Update(AnimationStateContext context)
        {
            yield return null;
        }

        private void StartHeldPulse()
        {
            BlobSelectionSettings settings = AnimationSettings(_context)?.BlobSelectionSettings;
            if (settings == null || _context == null)
                return;

            Transform visual = VisualTransform(_context);
            Vector3 squishScale = new Vector3(
                _heldScale.x * settings.SelectionStretchAmount,
                _heldScale.y * settings.SelectionSquishAmount,
                _heldScale.z);

            _pickUpSequence = DOTween.Sequence()
                .SetLink(_context.BlobView.gameObject, LinkBehaviour.KillOnDestroy)
                .Append(visual.DOScale(squishScale, settings.SelectionSquishDuration)
                    .SetEase(Ease.OutQuad)
                    .SetLink(_context.BlobView.gameObject, LinkBehaviour.KillOnDestroy))
                .Append(visual.DOScale(_heldScale, settings.SelectionSquishDuration)
                    .SetEase(Ease.InQuad)
                    .SetLink(_context.BlobView.gameObject, LinkBehaviour.KillOnDestroy))
                .SetLoops(-1, LoopType.Restart);
        }

        private void StopHeldAnimation()
        {
            _pickUpSequence?.Kill();
            _pickUpSequence = null;
        }

        private void RestoreRestPose()
        {
            if (_context == null)
                return;

            Transform visual = VisualTransform(_context);
            visual.DOKill();
            visual.localScale = _context.BaseScale;
            visual.localPosition = _context.BaseLocalPosition;
        }
    }

    /// <summary>
    /// One-shot refusal beat: the visual leans a short way along the refused swipe, squashed along
    /// that axis, then springs back to rest and hands control to idle. Any other transition cuts it
    /// short and restores the rest pose, so it never competes with pick-up or merge motion.
    /// </summary>
    public class BlobRejectedState : AnimationState
    {
        private readonly Vector2 _direction;
        private Sequence _nudgeSequence;

        public BlobRejectedState(Vector2 direction)
        {
            _direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.zero;
        }

        public override BlobAnimationState State => BlobAnimationState.Rejected;

        public override void Enter(AnimationStateContext context)
        {
            BlobRejectNudgeSettings settings = AnimationSettings(context)?.RejectNudgeSettings;
            if (settings == null || _direction == Vector2.zero)
            {
                context.BlobAnimator.SetIdle();
                return;
            }

            Transform visual = VisualTransform(context);
            Vector3 leanPosition = context.BaseLocalPosition + (Vector3)(_direction * settings.NudgeDistance);
            Vector3 leanScale = Vector3.Scale(context.BaseScale, AxisScale(settings.PushScale));

            _nudgeSequence = DOTween.Sequence()
                .SetLink(context.BlobView.gameObject, LinkBehaviour.KillOnDestroy)
                .Append(visual.DOLocalMove(leanPosition, settings.PushDuration).SetEase(Ease.OutQuad))
                .Join(visual.DOScale(leanScale, settings.PushDuration).SetEase(Ease.OutQuad))
                .Append(visual.DOLocalMove(context.BaseLocalPosition, settings.SettleDuration).SetEase(Ease.OutBack))
                .Join(visual.DOScale(context.BaseScale, settings.SettleDuration).SetEase(Ease.OutBack))
                .OnComplete(() =>
                {
                    _nudgeSequence = null;
                    context.BlobAnimator.SetIdle();
                });
        }

        public override void Exit(AnimationStateContext context)
        {
            _nudgeSequence?.Kill();
            _nudgeSequence = null;
            Transform visual = VisualTransform(context);
            visual.localPosition = context.BaseLocalPosition;
            visual.localScale = context.BaseScale;
        }

        public override IEnumerator Update(AnimationStateContext context)
        {
            yield return null;
        }

        /// <summary>Maps along/across multipliers onto x/y for the axis-aligned swipe.</summary>
        private Vector3 AxisScale(Vector2 alongAcross)
        {
            bool horizontal = Mathf.Abs(_direction.x) >= Mathf.Abs(_direction.y);
            return horizontal
                ? new Vector3(alongAcross.x, alongAcross.y, 1f)
                : new Vector3(alongAcross.y, alongAcross.x, 1f);
        }
    }

    public class BlobMovingState : AnimationState
    {
        public override BlobAnimationState State => BlobAnimationState.Moving;
        public override void Enter(AnimationStateContext context)
        {
            VisualTransform(context).localScale = context.BaseScale;
        }
        public override void Exit(AnimationStateContext context)
        {
        }
        public override IEnumerator Update(AnimationStateContext context)
        {
            yield return null;
        }
    }

    public class BlobIdleState : AnimationState
    {
        private AnimationStateContext _context;
        private Sequence _idleSequence;
        public override BlobAnimationState State => BlobAnimationState.Idle;
        public override void Enter(AnimationStateContext context)
        {
            _context = context;
            StartIdleLoop();
        }
        public override void Exit(AnimationStateContext context)
        {
            StopIdleLoop();
        }
        public override IEnumerator Update(AnimationStateContext context)
        {
            yield return null;
        }

        private Sequence StartIdleLoop()
        {
            BlobIdleSettings settings = AnimationSettings(_context).BlobIdleSettings;
            if (settings == null)
            {
                return null;
            }
            float squishDuration = settings.IdleSquishDuration;
            float squishAmount = settings.IdleSquishAmount;
            float stretchAmount = settings.IdleStretchAmount;


            // Kill any existing loop
            StopIdleLoop();

            // Create squish in/out loop: squash down (Y smaller, X larger) then return to base
            _idleSequence = DOTween.Sequence()
                .SetLink(_context.BlobView.gameObject, LinkBehaviour.KillOnDestroy);
            Vector3 squishScale = new Vector3(
               _context.BaseScale.x * stretchAmount,
                _context.BaseScale.y * squishAmount,
                _context.BaseScale.z
           );

            // Squish in
            Transform visual = VisualTransform(_context);
            _idleSequence.Append(visual.DOScale(squishScale, squishDuration)
                .SetEase(Ease.Linear)
                .SetLink(_context.BlobView.gameObject, LinkBehaviour.KillOnDestroy));

            // Squish out (back to base)
            _idleSequence.Append(visual.DOScale(_context.BaseScale, squishDuration)
                .SetEase(Ease.Linear)
                .SetLink(_context.BlobView.gameObject, LinkBehaviour.KillOnDestroy));

            // Loop indefinitely
            return _idleSequence.SetLoops(-1, LoopType.Restart);
        }

        private void StopIdleLoop()
        {
            if (_idleSequence != null)
            {
                // Kill the loop and smoothly blend back to base scale
                _idleSequence.Kill();
                VisualTransform(_context).localScale = _context.BaseScale;
            }

            _idleSequence = null;
        }
    }

    public class BlobMergingState : AnimationState
    {
        public override BlobAnimationState State => BlobAnimationState.Merging;
        public override void Enter(AnimationStateContext context)
        {
            // The merge orchestrator owns the one-shot deformation. This state only
            // stops looping idle/selection animation for the duration of that beat.
        }
        public override void Exit(AnimationStateContext context)
        {
            VisualTransform(context).localScale = context.BaseScale;
        }
        public override IEnumerator Update(AnimationStateContext context)
        {
            yield return null;
        }
    }

}
