using UnityEngine;

public class LaserTileView : TileView
{
    private LaserTileVisuals _visuals;
    private LaserTile _model;
    private GameObject LaserBeam => transform.Find("Laser Beam").gameObject;
    public override void Setup(Tile tile, BoardPresenter board)
    {
        base.Setup(tile, board);

        _visuals = GetVisuals<LaserTileVisuals>();
        _model = GetModel<LaserTile>();

        ColorUtils.ApplyColorsToMaterial(
            GetVisuals<LaserTileVisuals>().LaserBeam.GetComponent<SpriteRenderer>().sharedMaterial,
            GetModel<LaserTile>().LaserColor);
    }
    private void Update()
    {
        if (_model.IsActive && !LaserBeam.activeSelf)
        {
            LaserBeam.SetActive(true);
        }
        else if (!_model.IsActive && LaserBeam.activeSelf)
        {
            LaserBeam.SetActive(false);
        }
    }
    public void SetRotationFromDirection(Vector2Int direction)
    {
        float angle = 0f;

        if (direction == Vector2Int.right) angle = 0f;
        else if (direction == Vector2Int.up) angle = 90f;
        else if (direction == Vector2Int.left) angle = 180f;
        else if (direction == Vector2Int.down) angle = 270f;

        GetVisuals<LaserTileVisuals>().LaserPointer.transform.rotation = Quaternion.Euler(0, 0, angle);
    }
    
   
}