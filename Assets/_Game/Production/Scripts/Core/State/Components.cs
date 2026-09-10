namespace Blobs.Core
{
    public sealed class BlobComponents
    {
        public ColorComponent? Color { get; }
        public TrailComponent? Trail { get; }

        public BlobComponents(ColorComponent? color = null, TrailComponent? trail = null)
        {
            Color = color;
            Trail = trail;
        }

        public BlobComponents WithColor(BlobColor color)
        {
            return new BlobComponents(color: new ColorComponent(color), trail: Trail);
        }
        public BlobComponents WithTrail(BlobColor trailColor)
        {
            return new BlobComponents(trail: new TrailComponent(trailColor), color: Color);
        }

    }

    public readonly struct ColorComponent
    {
        public ColorComponent(BlobColor color)
        {
            Color = color;
        }

        public BlobColor Color { get; }
    }

    public readonly struct TrailComponent
    {


        public TrailComponent(BlobColor trailColor)
        {
            TrailColor = trailColor;
        }

        /// <summary>
        /// Color of the normal blobs a Trail blob leaves behind
        /// </summary>
        public BlobColor TrailColor { get; }
    }
}