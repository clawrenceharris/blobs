
using DG.Tweening;

public interface ITilePresenter
{
    Tile Model { get; }
    TileView View { get; }

    void Initialize(IBoardPresenter board);
    void PlayTraversalEffect();
    Tween Remove();

    Tween Spawn();
}