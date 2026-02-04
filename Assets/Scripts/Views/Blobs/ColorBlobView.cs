public abstract class ColorBlobView : BlobView
{
    public override void Setup(Blob model)
    {
        base.Setup(model);           
        // Apply the color to the sprite renderer's material.
        Blobs.Utilities.ColorUtility.ApplyColorsToMaterial(Visuals.SpriteRenderer.material, model.Color);
    }
}