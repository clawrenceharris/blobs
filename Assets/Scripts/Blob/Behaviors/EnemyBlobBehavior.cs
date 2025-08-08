

public class EnemyBlobBehavior : BlobMergeBehavior
{
    public EnemyBlobBehavior(Blob blob) : base(blob)
    {
    }
    public override void ModifyMergeFromSource(MergePlan plan, BoardLogic board)
    {

        if (_blob is EnemyBlob enemyBlob)
        {
            if (enemyBlob.IsWeakened)
            {
                plan.BlobsToRemoveDuringMerge.Add(enemyBlob);
            }
            else
            {
                enemyBlob.Weaken();
                plan.BlobsToRemoveAfterMerge.Add(plan.SourceBlob);

            }
        }
    }


}
