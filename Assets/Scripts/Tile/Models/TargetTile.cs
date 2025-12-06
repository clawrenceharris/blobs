
using UnityEngine;

public class TargetTile : Tile, IColorable, ICollidable, ITarget
{
    public BlobColor Color { get; set; }
    public override TileMergeBehavior Behavior => new TargetTileBehavior(this);

    public TargetTile(BlobColor color, Vector2Int position) : base(TileType.Target, position)
    {
        Color = color;

    }

    public void HandleCollision(Blob player, ICollidable other, BoardModel board)
    {
        CollisionHandler<TargetCollisionCommand> collisionHandler = new();
        collisionHandler.HandleCollision(player, other, board);
    }
}
