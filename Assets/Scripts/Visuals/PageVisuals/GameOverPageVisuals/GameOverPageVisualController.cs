
public class GameOverPageVisualController : PageVisualController
{
    private GameOverPageVisuals visuals;

    public override T GetVisuals<T>()
    {
        return visuals as T;
    }
    public override void Init(Page page)
    {
        visuals = page.GetComponent<GameOverPageVisuals>();
        base.Init(page);

    }
    public override void Subscribe()
    {
        StateManager.OnStateEnter += OnStateEnter;
        base.Subscribe();
    }

    public override void Unsubscribe()
    {
        StateManager.OnStateEnter -= OnStateEnter;

        base.Unsubscribe();
    }
    private void OnStateEnter(IState state)
    {
        //This logic should really be improved but this will work for now

        if (state is GameEndedState)
        {
            visuals.Overlay.gameObject.SetActive(true);
        }
        else
        {
            visuals.Overlay.gameObject.SetActive(false);
        }


    }


    
   
}