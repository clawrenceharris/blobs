public abstract class VisualController : IVisualController
{
    protected virtual void Subscribe()
    {
        return;
    }
    
    public virtual void Init(BlobsGameObject bObject)
    {
        InitSprite();
        Subscribe();
    }
    protected virtual void InitSprite()
    {
        return;
    }
    public virtual void Unsubscribe()
    {
        return;
    }
}