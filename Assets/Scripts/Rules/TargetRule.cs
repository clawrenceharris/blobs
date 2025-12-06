using System.Linq;
using UnityEngine;

public class TargetRule
{
    public bool Validate(Blob blob, ITarget target, BoardModel board)
    {
        Blob blobOnTarget = board.GetBlobAt(target.GridPosition);
        
        return blob.Color == target.Color && board.GetAllBlobs().Select((blob) => blob.ID != blobOnTarget?.ID).Count() == 1

        ;
    }
}