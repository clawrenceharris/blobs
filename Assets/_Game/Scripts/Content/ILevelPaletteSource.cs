namespace Blobs.Content
{
    /// <summary>
    /// Exposes the palette of the level currently being played so UI icons can match board colors.
    /// </summary>
    public interface ILevelPaletteSource
    {
        LevelColorPaletteAsset CurrentPalette { get; }
    }
}
