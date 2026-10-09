namespace Blobs.Core
{
    public sealed class BlobComponents
    {
        public ColorComponent? Color { get; }
        public TrailComponent? Trail { get; }
        public SizeComponent? Size { get; }

        public BlobComponents(ColorComponent? color = null, TrailComponent? trail = null, SizeComponent? size = null)
        {
            Color = color;
            Trail = trail;
            Size = size;
        }

        public BlobComponents WithColor(BlobColor color)
        {
            return new BlobComponents(color: new ColorComponent(color), trail: Trail, size: Size);
        }
        public BlobComponents WithTrail(BlobColor trailColor)
        {
            return new BlobComponents(color: Color, trail: new TrailComponent(trailColor), size: Size);
        }

        public BlobComponents WithSize(BlobSize size)
        {
            return new BlobComponents(color: Color, trail: Trail, size: new SizeComponent(size));
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

    public readonly struct SizeComponent
    {
        public SizeComponent(BlobSize size)
        {
            Size = size;
        }

        public BlobSize Size { get; }
    }
}