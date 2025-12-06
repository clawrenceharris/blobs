

public abstract class ColorRule : IRule<IColorable>
{
    public abstract bool Validate(IColorable target, IColorable other, BoardModel board);
}


/// <summary>
/// A rule that requires the target Color Blob and the compared Blob to be the same color.
/// </summary>
public class SameColorRule : ColorRule
{
    public override bool Validate(IColorable target, IColorable other, BoardModel board)
    {
        return target.Color == other.Color || other.Color == BlobColor.Blank || target.Color == BlobColor.Blank;
    }
}



/// <summary>
/// A rule that requires the target Color Blob and the compared Blob to be the differert colors.
/// </summary>
public class DifferentColorRule : ColorRule
{
    public override bool Validate(IColorable target, IColorable other, BoardModel board)
    {
        return target.Color != other.Color || other.Color == BlobColor.Blank || target.Color == BlobColor.Blank;
    }
}