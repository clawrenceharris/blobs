public abstract class ColorBlobView : BlobView
{
    public override void Setup(Blob blob)
    {
        base.Setup(blob);           
        // Apply the color to the sprite renderer's material.
        ColorUtils.ApplyColorsToMaterial(Visuals.SpriteRenderer.material, blob.Color);
    }
}