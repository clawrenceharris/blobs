using System.Diagnostics;
using System.Linq;
using Blobs.Core.Merge;

public class FlagRule : IMergeRule
{
    public int Priority => 10;

    public bool Apply(MergeContext ctx, MergePlan plan, out MergeFailReason failReason)
    {
        
        failReason = MergeFailReason.None;
        UnityEngine.Debug.Log("FlagRule: " + ctx.Source.Color + " " + ctx.HitBlob.Color);
        if (ctx.HitBlob is not FlagBlob flag)
            return true;
        

            if (flag.Color != ctx.Source.Color)
            {
                failReason = MergeFailReason.FlagRejected;
                return false;
            }
            if (ctx.Board.GetAllBlobs().Count(blob => blob.Model.Type != BlobType.Flag) > 1)
            {
                failReason = MergeFailReason.FlagRejected;
                return false;
            }

        plan.Events.Add(new RemoveBlobEvent { BlobId = ctx.Source.ID });
  
        return true;
    }
}