using System;
using System.Collections;
using UnityEngine;

public class LevelPageVisualController : PageVisualController
{
    private LevelPageVisuals visuals;
    private float countdownTime;
    public override T GetVisuals<T>()
    {
        return visuals as T;
    }
    public override void Init(Page page)
    {
        visuals = page.GetComponent<LevelPageVisuals>();
        base.Init(page);

    }

    public override void Subscribe()
    {
        StateManager.OnStateEnter += OnStateEnter;
    }

    public override void Unsubscribe()
    {
        StateManager.OnStateEnter -= OnStateEnter;

    }



    private void OnStateEnter(IState state)
    {
        //This logic should really be improved but this will work for now

        if (state is PausedState)
        {
            visuals.Overlay.gameObject.SetActive(true);
        }
        else
        {
            visuals.Overlay.gameObject.SetActive(false);
        }
       
       
    }

    public IEnumerator StartCountdown()
    {
        countdownTime = 4;
        visuals.CountdownText.gameObject.SetActive(true);
        visuals.CountdownText.text = countdownTime.ToString();
        while (countdownTime > 0)
        {
            
            yield return null;
            countdownTime -= Time.deltaTime;
            visuals.CountdownText.text = ((int)countdownTime).ToString();
        }
        visuals.CountdownText.gameObject.SetActive(false);
    }



    
    public override void Update()
    {
    }

    public void StopCountdown()
    {
        countdownTime = 0;
    }

    
}