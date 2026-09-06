namespace Blobs.Presentation
{
    /// <summary>
    /// Implemented by target-prefab components that contribute presentation feedback when contacted.
    /// Multiple components may respond to the same contact without changing board orchestration code.
    /// </summary>
    public interface IBlobContactFeedback
    {
        void PlayContactFeedback(BlobContactFeedbackContext context);
    }

    /// <summary>
    /// Presentation-only information supplied to contact feedback components.
    /// </summary>
    public readonly struct BlobContactFeedbackContext
    {
        public BlobContactFeedbackContext(BlobView source, BlobView target)
        {
            Source = source;
            Target = target;
        }

        public BlobView Source { get; }
        public BlobView Target { get; }
    }
}
