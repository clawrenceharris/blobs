using UnityEngine;

public interface IBoardLayout
{
    Vector3 GridToWorld(Vector2Int gridPos);
    Vector3 GridToWorld(int gridX, int gridY);
    Vector2Int WorldToGrid(float worldX, float worldY);
    Vector2Int WorldToGrid(Vector3 worldPos);
    Vector3 GridToWorldWithBlobOffset(Vector2Int gridPos);
    Vector3 GridToWorldWithBlobOffset(int gridX, int gridY);
    Vector2Int WorldToGridWithBlobOffset(Vector3 vector3);
    Vector2Int WorldToGridWithBlobOffset(float worldX, float worldY);
}