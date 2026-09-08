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
            string consumedBlobId,
            GridPosition from,
            GridPosition at

        )
        {
            SourceBlobId = sourceBlobId;
            Survivor = survivor;
            TargetBlobId = targetBlobId;
            MovingBlobId = movingBlobId;
            At = at;
            From = from;
        }


        public static MergeEffect NormalMerge(MoveContext context) => new MergeEffect(
            sourceBlobId: context.Source.Id,
            movingBlobId: context.Source.Id,
            targetBlobId: context.Target.Id,
            survivor: MergeSurvivor.MovingBlob,
            consumedBlobId: context.Target.Id,
            from: context.Source.Position,
            at: context.Target.Position);
        public static MergeEffect ReverseMerge(MoveContext context) => new MergeEffect(
            sourceBlobId: context.Source.Id,
            movingBlobId: context.Source.Id,
            targetBlobId: context.Target.Id,
            survivor: MergeSurvivor.TargetBlob,
            consumedBlobId: context.Source.Id,
            from: context.Source.Position,
            at: context.Target.Position);
        public MergeSurvivor Survivor { get; }
        public string SurvivingBlobId =>
            Survivor == MergeSurvivor.MovingBlob
                ? MovingBlobId
                : TargetBlobId;

        public string ConsumedBlobId =>
            Survivor == MergeSurvivor.MovingBlob
                ? TargetBlobId
                : MovingBlobId; public string TargetBlobId { get; }
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