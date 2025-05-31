using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DG.Tweening;
using UnityEngine;

public class TutorialPointer : MonoBehaviour
{

    private TutorialManager tutorial;
    private float offsetX = 0.2f;
    private float offsetY = -0.5f;
    private float time = 0;
    private float maxTime = 6f;
    private SpriteRenderer spriteRenderer;
    private void Awake()
    {
        tutorial = FindFirstObjectByType<TutorialManager>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

   

    public IEnumerator Show()
    {
        
            spriteRenderer.DOFade(1, 0.3f);

            transform.position = new Vector2(tutorial.StartBlob.Position.x + offsetX, tutorial.StartBlob.Position.y + offsetY);
            transform.DOMove(new Vector3(tutorial.EndBlob.Position.x + offsetX, tutorial.EndBlob.Position.y + offsetY), Visuals.moveTime).SetEase(Ease.InOutCirc);
            yield return new WaitForSeconds(1.2f);
            Hide();
        
        
    }

    private void Hide()
    {
            spriteRenderer.DOFade(0, 0.3f);

    }

    private void Update()
    {
        
        time += Time.deltaTime;
        if (time > maxTime)
        {
            time = 0;

            if (tutorial.isTutorial )

                StartCoroutine(Show());
            


        }

        

    }
}
