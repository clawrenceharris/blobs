using UnityEngine;
using System.Collections.Generic;
using Blobs.Core.Merge;


/// <summary>
/// Interface for grid presenter (grid management logic)
/// </summary>
public interface IBoardPresenter
{
    //Board State
    int Width { get; }
    int Height { get; }
    LevelData CurrentLevel { get; }
    BoardModel Model { get; }
    IBoardLayout Layout { get; }

    // Initialization
    void Initialize(LevelData levelData);
    void ClearBoard();

    // Blob management
    void MoveBlob(Blob blob, Vector2Int endPosition);

    IBlobPresenter GetBlobAt(Vector2Int position);
    IBlobPresenter GetBlobAt(int x, int y);


    void RemoveBlob(Blob blob);
    List<IBlobPresenter> GetAllBlobs();
    
    // Grid queries
    // bool IsValidPosition(Vector2Int position);
    // bool IsPositionOccupied(Vector2Int position);
    
    // Win condition
    int GetPlayableBlobCount();
    IBlobPresenter GetBlobById(string id);
    void PlaceBlob(Blob blob);
}
