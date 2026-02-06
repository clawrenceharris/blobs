using System;
using UnityEngine;

namespace Blobs.Utilities
{
    public static class GridUtility
    {


        public static Vector2Int WorldToGrid(float worldX, float worldY)
        {
            float x = (worldX / (TilePresenter.TileSize / 2) + worldY / (TilePresenter.TileSize / 4)) / 2f;
            float y = (worldY / (TilePresenter.TileSize / 2) - worldX / (TilePresenter.TileSize / 4)) / 2f;
            return new Vector2Int(Mathf.FloorToInt(x), Mathf.FloorToInt(y));
        }
        public static Vector2Int WorldToGrid(Vector3 worldPos)
        {

            return WorldToGrid(worldPos.x, worldPos.y);
        }

        public static Vector3 GridToWorld(Vector2Int gridPos)
        {
            return GridToWorld(gridPos.x, gridPos.y);
        }
        public static Vector3 GridToWorld(int gridX, int gridY)
        {
            return new Vector3(
                (gridX - gridY) * TilePresenter.TileSize / 2f,
                (gridX + gridY) * TilePresenter.TileSize / 4f, 0
            );
        }
        public static Vector2Int WorldToGridWithBlobOffset(Vector3 worldPos)
        {

            return WorldToGridWithBlobOffset(worldPos.x, worldPos.y);
        }
        public static Vector2Int WorldToGridWithBlobOffset(float worldX, float worldY)
        {
            return WorldToGrid(new Vector3(worldX, worldY - BlobPresenter.BlobOffsetY, 0));
        }
        public static Vector3 GridToWorldWithBlobOffset(Vector2Int gridPos)
        {
            var worldPos = GridToWorld(gridPos.x, gridPos.y);
            return new Vector3(worldPos.x, worldPos.y + BlobPresenter.BlobOffsetY, 0);
        }
        public static Vector3 GridToWorldWithBlobOffset(int x, int y)
        {
            var worldPos = GridToWorld(x, y);
            return new Vector3(worldPos.x, worldPos.y + BlobPresenter.BlobOffsetY, 0);
        }
    }
}
