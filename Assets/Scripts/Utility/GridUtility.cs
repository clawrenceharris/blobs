using UnityEngine;

namespace Blobs.Utilities
{
    public class GridUtility
    {

        public static Vector2 GridToIso(int gridX, int gridY)
        {
            return new Vector2(
                (gridX - gridY) * TilePresenter.TileSize / 2f,
                (gridX + gridY) * TilePresenter.TileSize / 4f
            );
        }
        public static Vector2Int IsoToGrid(float isoX, float isoY, float tileSize)
        {
            float x = (isoX / (TilePresenter.TileSize / 2) + isoY / (TilePresenter.TileSize / 4)) / 2f;
            float y = (isoY / (TilePresenter.TileSize / 2) - isoX / (TilePresenter.TileSize / 4)) / 2f;
            return new Vector2Int(Mathf.FloorToInt(x), Mathf.FloorToInt(y));
        }

        public static Vector3 GridToIso(Vector2Int position)
        {
            return GridToIso(position.x, position.y);
        }
        public static Vector3 GridToIsoWithBlobOffset(Vector2Int position)
        {
            var iso = GridToIso(position.x, position.y);
            return new Vector2(iso.x, iso.y + BlobPresenter.BlobOffsetY);
        }
         public static Vector3 GridToIsoWithBlobOffset(int x, int y)
        {
            var iso = GridToIso(x, y);
            return new Vector2(iso.x, iso.y + BlobPresenter.BlobOffsetY);
        }
    }
}
