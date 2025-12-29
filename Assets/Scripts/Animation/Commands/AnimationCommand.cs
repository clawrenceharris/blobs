using System.Collections;
using UnityEngine;

public abstract class AnimationCommand : IAnimationCommand
{
    protected Blob _blob;
    protected MergeContext _context;
    public BlobPresenter Presenter
    {
        get
        {
            if (_blob == null) return null;
            return _context.Board.GetBlobPresenter(_blob.ID);
        }
    }
    public AnimationCommand(Blob blob, MergeContext context)
    {
        _blob = blob;
        _context = context;
    }
    public abstract IEnumerator Execute();
    
   
}