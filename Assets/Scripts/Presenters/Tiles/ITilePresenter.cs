
using DG.Tweening;

public interface ITilePresenter
{
    Tile Model { get; }
    TileView View { get; }
    ITileAnimator Animator { get; }

    void Initialize(IBoardPresenter board);
    Tween Remove(float duration);

    Tween Spawn(float duration);
}