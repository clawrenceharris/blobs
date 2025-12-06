
using System;
using System.Collections;
using UnityEngine;
public class CoroutineHandler : MonoBehaviour
{
    private static CoroutineHandler instance;

   
    

    private void Awake()
    {  
        instance = this;   
    }

 
    public static Coroutine StartStaticCoroutine(IEnumerator coroutine, Action callback = null)
    {
        return instance.StartCoroutine(RunCoroutine(coroutine, callback));
    }

    // Wrapper coroutine that runs the original coroutine and then invokes the callback
    private static IEnumerator RunCoroutine(IEnumerator coroutine, Action callback)
    {
        yield return coroutine;
        callback?.Invoke();
    }

    public static void StopStaticCoroutine(Coroutine coroutine)
    {
        if(coroutine != null)
            instance.StopCoroutine(coroutine);
    }


}
