using System;
using System.Collections.Generic;
using Blobs.Core.Merge;
using DG.Tweening;
using UnityEngine;

namespace Blobs.Merge.Animation
{
    /// <summary>
    /// Central registry mapping IMergeEvent types to IEventAnimator handlers.
    /// Used by MovePlanAnimator to dispatch event animations.
    /// </summary>
    public class EventAnimatorRegistry
    {
        private readonly Dictionary<Type, IEventAnimator> _forward = new();
        private readonly Dictionary<Type, IEventAnimator> _undo = new();

        public void Register<T>(IEventAnimator animator) where T : IMergeEvent
        {
            var t = typeof(T);
            _forward[t] = animator;
            _undo[t] = animator;
        }

        public void RegisterForward<T>(IEventAnimator animator) where T : IMergeEvent
        {
            _forward[typeof(T)] = animator;
        }

        public void RegisterUndo<T>(IEventAnimator animator) where T : IMergeEvent
        {
            _undo[typeof(T)] = animator;
        }

        public IEventAnimator GetAnimator(Type eventType, bool forUndo)
        {
            var map = forUndo ? _undo : _forward;
            return map.TryGetValue(eventType, out var a) ? a : null;
        }

        public bool TryGetAnimator(Type eventType, bool forUndo, out IEventAnimator animator)
        {
            animator = GetAnimator(eventType, forUndo);
            return animator != null;
        }
    }
}
