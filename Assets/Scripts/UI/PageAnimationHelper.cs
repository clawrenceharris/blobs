using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;


public class PageAnimationHelper
{

    public static IEnumerator FadeIn(CanvasGroup canvasGroup, float speed, UnityEvent onEnd)
    {
        if (canvasGroup)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;

            float time = 0;
            while (time < 1)
            {
                canvasGroup.alpha = Mathf.Lerp(0, 1, time);
                yield return null;
                time += Time.deltaTime * speed;
            }

            canvasGroup.alpha = 1;
        }
        onEnd?.Invoke();


    }

    public static IEnumerator FadeOut(CanvasGroup canvasGroup, float speed, UnityEvent onEnd)
    {
        if (canvasGroup)
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            float time = 0;
            while (time < 1)
            {
                canvasGroup.alpha = Mathf.Lerp(1, 0, time);
                yield return null;
                time += Time.deltaTime * speed;
            }

            canvasGroup.alpha = 0;
        }
        onEnd?.Invoke();
    }
    public static IEnumerator SlideIn(RectTransform transform, EntryDirection direction, float speed, UnityEvent onEnd)
    {
        Vector2 startPos = Vector2.zero;
        switch (direction)
        {
            case EntryDirection.Up:
                startPos = new Vector2(0, -Screen.height);
                break;
            case EntryDirection.Right:
                startPos = new Vector2(-Screen.width, 0);
                break;
            case EntryDirection.Left:
                startPos = new Vector2(Screen.width, 0);
                break;
            case EntryDirection.Down:
                startPos = new Vector2(0, Screen.height);
                break;

        }
        transform.anchoredPosition = startPos;
        Tween tween = transform.DOAnchorPos(Vector2.zero, speed);
        yield return tween.WaitForCompletion();
        onEnd?.Invoke();
        transform.anchoredPosition = Vector2.zero;
        onEnd?.Invoke();
    }

    public static IEnumerator SlideOut(RectTransform transform, EntryDirection direction, float speed, UnityEvent onEnd)
    {
        Vector2 targetPos = Vector2.zero;
        switch (direction)
        {
            case EntryDirection.Up:
                targetPos = new Vector2(0, Screen.height);
                break;
            case EntryDirection.Right:
                targetPos = new Vector2(Screen.width, 0);
                break;
            case EntryDirection.Left:
                targetPos = new Vector2(-Screen.width, 0);
                break;
            case EntryDirection.Down:
                targetPos = new Vector2(0, -Screen.height);
                break;

        }
        Tween tween = transform.DOAnchorPos(targetPos, speed);
        yield return tween.WaitForCompletion();
        transform.anchoredPosition = targetPos;
        onEnd?.Invoke();
    }
}