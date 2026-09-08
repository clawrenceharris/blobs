
using DG.Tweening;

public interface ITilePresenter
{
    Tile Model { get; }
    TileView View { get; }

    void SetModel(Tile model);
    void BindView(TileView view);
    Sequence Remove();
    Sequence Spawn();
    Sequence Enter();
    Sequence Exit();
    Sequence LeaveTrail(BlobColor color);
    Sequence RemoveTrail();
}