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
            StartSelectionLoop();
        }
        public override void Exit(AnimationStateContext context)
        {
            StopSelectionLoop();
        }
        public override IEnumerator Update(AnimationStateContext context)
        {
            yield return null;
        }
        private Sequence StartSelectionLoop()
        {
            BlobSelectionSettings settings = AnimationSettings(_context).BlobSelectionSettings;
            if (settings == null)
            {
                return null;
            }
            float squishDuration = settings.SelectionSquishDuration;
            float squishAmount = settings.SelectionSquishAmount;
            float stretchAmount = settings.SelectionStretchAmount;


            // Kill any existing loop
            StopSelectionLoop();

            // Create squish in/out loop: squash down (Y smaller, X larger) then return to base
            _selectionSequence = DOTween.Sequence()
                .SetLink(_context.BlobView.gameObject, LinkBehaviour.KillOnDestroy);
            Vector3 squishScale = new Vector3(
               _context.BaseScale.x * stretchAmount,
                _context.BaseScale.y * squishAmount,
                _context.BaseScale.z
           );

            // Squish in
            Transform visual = VisualTransform(_context);
            _selectionSequence.Append(visual.DOScale(squishScale, squishDuration)
                .SetEase(Ease.OutQuad)
                .SetLink(_context.BlobView.gameObject, LinkBehaviour.KillOnDestroy));

            // Squish out (back to base)
            _selectionSequence.Append(visual.DOScale(_context.BaseScale, squishDuration)
                .SetEase(Ease.InQuad)
                .SetLink(_context.BlobView.gameObject, LinkBehaviour.KillOnDestroy));

            // Loop indefinitely
            return _selectionSequence.SetLoops(-1, LoopType.Restart);
        }
        private void StopSelectionLoop()
        {
            if (_selectionSequence != null)
            {
                // Kill the loop and smoothly blend back to base scale
                _selectionSequence.Kill();
                VisualTransform(_context).localScale = _context.BaseScale;
            }

            _selectionSequence = null;
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
