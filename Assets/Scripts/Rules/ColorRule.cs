

public abstract class ColorRule : IRule<IColorable>
{
    public abstract bool Validate(IColorable target, IColorable other, BoardLogic board);
}


/// <summary>
/// A rule that requires the target color blob and the compared blob to be the same color.
/// </summary>
public class SameColorRule : ColorRule
{
    public override bool Validate(IColorable target, IColorable other, BoardLogic board)
    {
        return target.Color == other.Color || other.Color == BlobColor.Blank || target.Color == BlobColor.Blank;
    }
}



/// <summary>
/// A rule that requires the target color blob and the compared blob to be the differert colors.
/// </summary>
public class DifferentColorRule : ColorRule
{
    public override bool Validate(IColorable target, IColorable other, BoardLogic board)
    {
        return target.Color != other.Color || other.Color == BlobColor.Blank || target.Color == BlobColor.Blank;
    }
}