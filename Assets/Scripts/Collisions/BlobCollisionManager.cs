using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BlobCollisionManager : MonoBehaviour
{
    private BoardPresenter board;

    private void Awake()
    {
        board = FindFirstObjectByType<BoardPresenter>();
    }

    private void Start()
    {
        BoardModel.OnBlobMoved += HandleBlobMoved;


    }

    private void OnBlobRemoved(Blob blob)
    {
        throw new NotImplementedException();
    }

    private void HandleBlobMoved(Blob blob, Vector2Int from, Vector2Int to)
    {

        if (from.x > to.x || from.x < to.x)
        {
            List<ICollidable> collidables = board.BoardModel.FindObjectsInRow<ICollidable>(blob.GridPosition.y);
            foreach (ICollidable collidable in collidables)
            {
                collidable.HandleCollision(blob, collidable, board.BoardModel);
            }

        }
        else if(from.y > to.y || from.y < to.y)
        {
            List<ICollidable> collidables = board.BoardModel.FindObjectsInColumn<ICollidable>(blob.GridPosition.x);

            foreach (ICollidable collidable in collidables)
            {

                collidable.HandleCollision(blob, collidable, board.BoardModel);
            }
        }
    }
}