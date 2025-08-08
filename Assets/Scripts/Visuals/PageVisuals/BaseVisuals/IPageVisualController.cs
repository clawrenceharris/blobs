
public interface IPageVisualController
{
    void Init(Page page);

    void Unsubscribe();

    void Update();

    T GetVisuals<T>() where T :class, IPageVisuals;
}
