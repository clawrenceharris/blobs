using UnityEngine;
using UnityEngine.UI;

// <summary>
/// Base class for visual controllers that manage the appearance of Page game objects.
/// </summary>
/// <remarks>
/// Provides common functionality for initializing, setting up, and subscribing to events.
/// Derived classes can implement specific visual behavior.
/// </remarks>
public class PageVisualController : IPageVisualController
{
    private IPageVisuals visuals;
    protected Page page;
    public virtual T GetVisuals<T>() where T : class, IPageVisuals
    {
        return default;
    }
    public virtual void Init(Page page)
    {
        this.page = page;
        if (page.TryGetComponent<IPageVisuals>(out var visuals))
        {
            this.visuals = visuals;
        }
        Subscribe();

    }
   
   
    public virtual void Unsubscribe()
    {

    }
    public virtual void Subscribe()
    {

    }
    public virtual void Update()
    {

    }

    public virtual void ActivateButton(Button button)
    {
        button.interactable = true;
        button.image.color = new Color32(255, 129, 131, 255); //This should be changed so it is not hard coded

    }
    public virtual void DeactivateButton(Button button)
    {
        button.interactable = true;
        button.image.color = Color.gray;
    }
}