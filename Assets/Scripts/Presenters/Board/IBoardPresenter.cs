using UnityEngine;
using System.Collections.Generic;
using System.Collections;


/// <summary>
/// Interface for grid presenter (grid management logic)
/// </summary>
public interface IBoardPresenter
{
    //Board State
    int Width { get; }
    int Height { get; }
    LevelData CurrentLevel { get; }
    
    // Initialization
    void Initialize(LevelData levelData);
    void ClearBoard();

    // Blob management
    void MoveBlob(string id, Vector2Int endPosition);
    void RemoveBlob(string id);


    // Tile management
    void PlaceTile(Tile tile);
    void RemoveTile(string tile);


    // Blob queries
    IBlobPresenter GetBlobAt(Vector2Int position);
    IBlobPresenter GetBlobAt(int x, int y);
    IBlobPresenter GetBlob(string id);

    List<IBlobPresenter> GetAllBlobs();

    // Tile queries
    ITilePresenter GetTileAt(Vector2Int gridPos);
    ITilePresenter GetTileAt(int x, int y);
    ITilePresenter GetTile(string id);
    List<ITilePresenter> GetAllTiles();

    // Grid queries
    int GetPlayableBlobCount();

    bool IsValidPosition(Vector2Int position);
    bool IsLaserBlocking(IBlobPresenter source, Vector2Int position);
    void RespawnBlob(string id);
    void SpawnBlob(Blob blob);
    IEnumerator AnimateEndTurnSequence();
}
