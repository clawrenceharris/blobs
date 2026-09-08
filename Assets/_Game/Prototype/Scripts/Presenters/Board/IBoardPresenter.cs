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
    
    // Initialization
    void Initialize(LevelData levelData);
    void ClearBoard();

    // Blob management
    void MoveBlob(string id, Vector2Int endPosition);
    void RemoveBlob(string id);


    // Tile management
    void SpawnTile(Tile tile);
    void RemoveTile(string tile);


    // Blob queries
    IBlobPresenter GetBlobAt(Vector2Int position);
    IBlobPresenter GetBlobAt(int x, int y);
    IBlobPresenter GetBlob(string id);
    bool BlobExists(string id);

    List<IBlobPresenter> GetBlobPresenters();

    // Tile queries
    ITilePresenter GetTileAt(Vector2Int gridPos);
    ITilePresenter GetTileAt(int x, int y);
    ITilePresenter GetTile(string id);
    List<ITilePresenter> GetAllTiles();

    // Grid queries
    List<IBlobPresenter> GetBlobsOnBoard();
    List<IBlobPresenter> GetBlobsBetween(Vector2Int gridPosition1, Vector2Int gridPosition2);

    bool IsValidPosition(Vector2Int position);
    bool IsLaserBlocking(string sourceId, Vector2Int position);
    
    IBlobPresenter SpawnBlob(Blob blob);
    
    IEnumerator AnimateEndTurnSequence();
}
