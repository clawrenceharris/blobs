namespace Blobs.Core
{
    public enum MergeSurvivor
    {
        MovingBlob,
        TargetBlob
    }

    public sealed class MergeEffect : IBoardEffect
    {
        public MergeEffect(
            string sourceBlobId,
            string movingBlobId,
            string targetBlobId,
            MergeSurvivor survivor,
            GridPosition from,
            GridPosition at,
            BlobState consumedBlob = null
        )
        {
            SourceBlobId = sourceBlobId;
            Survivor = survivor;
            TargetBlobId = targetBlobId;
            MovingBlobId = movingBlobId;
            At = at;
            From = from;
            ConsumedBlob = consumedBlob;
        }


        public static MergeEffect NormalMerge(MoveContext context) => new MergeEffect(
            sourceBlobId: context.Source.Id,
            movingBlobId: context.Source.Id,
            targetBlobId: context.Target.Id,
            survivor: MergeSurvivor.MovingBlob,
            from: context.Source.Position,
            at: context.Target.Position,
            consumedBlob: context.Target);
        public static MergeEffect ReverseMerge(MoveContext context) => new MergeEffect(
            sourceBlobId: context.Source.Id,
            movingBlobId: context.Source.Id,
            targetBlobId: context.Target.Id,
            survivor: MergeSurvivor.TargetBlob,
            from: context.Source.Position,
            at: context.Target.Position,
            consumedBlob: context.Source);
        public MergeSurvivor Survivor { get; }
        public string SurvivingBlobId =>
            Survivor == MergeSurvivor.MovingBlob
                ? MovingBlobId
                : TargetBlobId;

        public string ConsumedBlobId =>
            Survivor == MergeSurvivor.MovingBlob
                ? TargetBlobId
                : MovingBlobId;
        public BlobState ConsumedBlob { get; }
        public string TargetBlobId { get; }
        public string MovingBlobId { get; }
        public string SourceBlobId { get; }
        public GridPosition At { get; }
        public GridPosition To => At;
        public GridPosition From { get; }
        public void Apply(BoardState board)
        {
            board.RemoveBlob(ConsumedBlobId);


        }
    }
}
