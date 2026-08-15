namespace Blobs.Presentation
{
    /// <summary>
    /// Declares how a renderer receives color. Specialized targets remain part of shared
    /// alpha/sorting operations but are colored by a prefab-specific binding.
    /// </summary>
    public enum BlobColorBindingMode
    {
        Blob = 1, // Use the blob's color to color the renderer using a shader skin.
        Uncolored = 2, // No color binding, use the material's base color.
        Specialized = 3, // Specialized color binding, use a specialized material and shader skin.
    }
}
