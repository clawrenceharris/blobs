

public class EnemyBlobBehavior : BlobMergeBehavior
{
    public EnemyBlobBehavior(Blob blob) : base(blob)
    {
    }
    public override void ModifyMergeFromSource(MergeContext context)
    {

        if (_blob is EnemyBlob enemyBlob)
        {
            if (enemyBlob.IsWeakened)
            {
                context.Plan.BlobsToRemoveOnPath.TryAdd(enemyBlob, enemyBlob.GridPosition);
            }
            else
            {
                enemyBlob.Weaken();
                context.Plan.BlobsToRemoveOnPath.TryAdd(context.Plan.SourceBlob, enemyBlob.GridPosition);

            }
        }
    }


}
