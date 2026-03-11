/// <summary>
/// Deterministic id generator for resolution spawns (e.g. trail blobs).
/// Reset per level/move so undo replays produce matching ids.
/// </summary>
public static class ResolutionIds
{
    private static int _next;

    public static string Next()
    {
        return "res_" + (++_next);
    }

    public static void Reset()
    {
        _next = 0;
    }
}
