
using System;
using System.Collections;
using System.Collections.Generic;

public class AnimationInvoker
{
    private static readonly Queue<AnimationCommand> _animationQueue = new();
    
    public static void EnqueueAnimation(AnimationCommand animation)
    {
        _animationQueue.Enqueue(animation);
    }
    public static IEnumerator ExecuteAnimations()
    {
        while (_animationQueue.Count > 0)
        {
            AnimationCommand animation = _animationQueue.Dequeue();
            if(animation?.Presenter != null)
                yield return animation.Execute();
        }
    }
   
}