/// <summary>
/// Constants for effect tags used by MoveResolver resolution pipeline.
/// </summary>
public static class EffectTags
{
    public const string MoveStep = "MoveStep";
    public const string AfterMerge = "AfterMerge";
    public const string BombExplode = "BombExplode";
    public const string Haunt = "Haunt";
    public const string Trail = "Trail";
    public const string Merge = "Merge";
    public const string UndoSpawn = "UndoSpawn";

    public const string AfterMove = "AfterMove";

    public static string Win { get; internal set; }
}
